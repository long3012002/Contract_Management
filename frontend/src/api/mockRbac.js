// Mock RBAC Database stored in localStorage for persistence and live demo

const DEFAULT_FEATURES = [
  { id: 'f-1', code: 'PROJECT', name: 'Quản lý Dự Án', description: 'Xem, thêm, sửa, xóa các dự án đầu tư' },
  { id: 'f-2', code: 'CONTRACT', name: 'Quản lý Hợp Đồng', description: 'Quản lý thông tin và phụ lục hợp đồng' },
  { id: 'f-3', code: 'PARTNER', name: 'Quản lý Đối Tác', description: 'Quản lý danh sách nhà thầu, đối tác' }
];

const DEFAULT_ROLES = [
  { id: 'r-1', name: 'Kế toán trưởng', description: 'Phụ trách duyệt chi và đối soát hợp đồng', isActive: true },
  { id: 'r-2', name: 'Quản lý Dự án', description: 'Điều hành, tạo mới và điều chỉnh thông tin dự án', isActive: true },
  { id: 'r-3', name: 'Nhân viên nghiệp vụ', description: 'Chỉ xem thông tin dự án và đối tác liên quan', isActive: true }
];

const DEFAULT_ROLE_PERMISSIONS = {
  // Kế toán trưởng: Xem/Sửa hợp đồng, Xem dự án & đối tác
  'r-1': [
    { featureId: 'f-1', featureCode: 'PROJECT', canAccess: true, canCreate: false, canUpdate: false, canDelete: false },
    { featureId: 'f-2', featureCode: 'CONTRACT', canAccess: true, canCreate: true, canUpdate: true, canDelete: false },
    { featureId: 'f-3', featureCode: 'PARTNER', canAccess: true, canCreate: false, canUpdate: false, canDelete: false }
  ],
  // Quản lý dự án: Toàn quyền dự án & đối tác, Xem hợp đồng
  'r-2': [
    { featureId: 'f-1', featureCode: 'PROJECT', canAccess: true, canCreate: true, canUpdate: true, canDelete: true },
    { featureId: 'f-2', featureCode: 'CONTRACT', canAccess: true, canCreate: false, canUpdate: false, canDelete: false },
    { featureId: 'f-3', featureCode: 'PARTNER', canAccess: true, canCreate: true, canUpdate: true, canDelete: true }
  ],
  // Nhân viên nghiệp vụ: Chỉ xem
  'r-3': [
    { featureId: 'f-1', featureCode: 'PROJECT', canAccess: true, canCreate: false, canUpdate: false, canDelete: false },
    { featureId: 'f-2', featureCode: 'CONTRACT', canAccess: true, canCreate: false, canUpdate: false, canDelete: false },
    { featureId: 'f-3', featureCode: 'PARTNER', canAccess: true, canCreate: false, canUpdate: false, canDelete: false }
  ]
};

const DEFAULT_USERS = [
  { id: 'u-1', username: 'admin', fullName: 'Lê Nguyễn Hoàng Anh (Admin)', isActive: true, isSystemAdmin: true, roleIds: [] },
  { id: 'u-2', username: 'manager', fullName: 'Trần Minh Tâm (Manager)', isActive: true, isSystemAdmin: false, roleIds: ['r-2'] },
  { id: 'u-3', username: 'accountant', fullName: 'Phạm Thanh Thủy (Accountant)', isActive: true, isSystemAdmin: false, roleIds: ['r-1'] },
  { id: 'u-4', username: 'staff', fullName: 'Nguyễn Văn Nam (Staff)', isActive: true, isSystemAdmin: false, roleIds: ['r-3'] }
];

function getStorage(key, defaultValue) {
  const data = localStorage.getItem(key);
  if (!data) {
    localStorage.setItem(key, JSON.stringify(defaultValue));
    return defaultValue;
  }
  return JSON.parse(data);
}

function setStorage(key, value) {
  localStorage.setItem(key, JSON.stringify(value));
}

export const mockRbacService = {
  getFeatures: () => getStorage('rbac_features', DEFAULT_FEATURES),
  
  getRoles: () => getStorage('rbac_roles', DEFAULT_ROLES),
  
  saveRole: (role) => {
    const roles = mockRbacService.getRoles();
    if (role.id) {
      const idx = roles.findIndex(r => r.id === role.id);
      if (idx !== -1) roles[idx] = { ...roles[idx], ...role };
    } else {
      const newRole = { ...role, id: `r-${Date.now()}`, isActive: true };
      roles.push(newRole);
      setStorage('rbac_roles', roles);
      // Initialize empty permissions matrix for new role
      const features = mockRbacService.getFeatures();
      const perms = features.map(f => ({
        featureId: f.id,
        featureCode: f.code,
        canAccess: false,
        canCreate: false,
        canUpdate: false,
        canDelete: false
      }));
      mockRbacService.saveRolePermissions(newRole.id, perms);
      return newRole;
    }
    setStorage('rbac_roles', roles);
    return role;
  },

  getRolePermissions: (roleId) => {
    const allPerms = getStorage('rbac_role_permissions', DEFAULT_ROLE_PERMISSIONS);
    const features = mockRbacService.getFeatures();
    
    // Ensure all features are represented in the permissions list
    const rolePerms = allPerms[roleId] || [];
    const completePerms = features.map(f => {
      const existing = rolePerms.find(p => p.featureId === f.id || p.featureCode === f.code);
      return existing ? { ...existing, featureId: f.id, featureCode: f.code } : {
        featureId: f.id,
        featureCode: f.code,
        canAccess: false,
        canCreate: false,
        canUpdate: false,
        canDelete: false
      };
    });
    return completePerms;
  },

  saveRolePermissions: (roleId, permissions) => {
    const allPerms = getStorage('rbac_role_permissions', DEFAULT_ROLE_PERMISSIONS);
    allPerms[roleId] = permissions;
    setStorage('rbac_role_permissions', allPerms);
  },

  getUsers: () => getStorage('rbac_users', DEFAULT_USERS),

  saveUserRoles: (userId, roleIds) => {
    const users = mockRbacService.getUsers();
    const idx = users.findIndex(u => u.id === userId);
    if (idx !== -1) {
      users[idx].roleIds = roleIds;
      setStorage('rbac_users', users);
    }
  },

  // Calculate union permissions for a given user based on their roles
  calculateUserPermissions: (username) => {
    const users = mockRbacService.getUsers();
    const user = users.find(u => u.username?.toLowerCase() === username?.toLowerCase());
    
    if (!user) return { isSystemAdmin: false, permissions: [] };
    if (user.isSystemAdmin) return { isSystemAdmin: true, permissions: [] };

    const activeRoles = mockRbacService.getRoles().filter(r => r.isActive && user.roleIds.includes(r.id));
    const features = mockRbacService.getFeatures();

    // Perform Union OR logic
    const permissions = features.map(f => {
      let canAccess = false;
      let canCreate = false;
      let canUpdate = false;
      let canDelete = false;

      activeRoles.forEach(role => {
        const perms = mockRbacService.getRolePermissions(role.id);
        const perm = perms.find(p => p.featureId === f.id);
        if (perm) {
          if (perm.canAccess) canAccess = true;
          if (perm.canCreate) canCreate = true;
          if (perm.canUpdate) canUpdate = true;
          if (perm.canDelete) canDelete = true;
        }
      });

      return {
        featureId: f.id,
        featureCode: f.code,
        canAccess,
        canCreate,
        canUpdate,
        canDelete
      };
    });

    return {
      isSystemAdmin: false,
      permissions
    };
  }
};
