import React, { useState, useEffect } from 'react';
import { toast } from 'sonner';
import { adminApi } from '../api/adminApi';
import Button from '../components/Button/Button';
import Checkbox from '../components/Checkbox/Checkbox';

const PREDEFINED_PAGES = [
  { code: 'DASHBOARD', name: 'Trang chủ Dashboard', description: 'Trang tổng quan thông tin, chỉ số hoạt động hệ thống.' },
  { code: 'SYSTEM_PERMISSIONS', name: 'Quản lý phân quyền', description: 'Thiết lập nhóm vai trò, cấu hình ma trận quyền hạn và gán quyền cho tài khoản.' },
  { code: 'SYSTEM_TEST', name: 'Kiểm thử hệ thống', description: 'Giao diện thực hiện các bài test API và xác thực token JWT.' },
  { code: 'CONTRACT_MANAGEMENT', name: 'Quản lý hợp đồng', description: 'Quản lý thông tin hợp đồng CNTT, phụ lục và tiến độ thực hiện.' },
  { code: 'REPORT_STATISTICS', name: 'Báo cáo thống kê', description: 'Xem biểu đồ phân tích số liệu, thống kê doanh thu hợp đồng.' }
];

export default function Permissions() {
  const [activeTab, setActiveTab] = useState('roles'); // 'roles' | 'matrix' | 'users' | 'features'

  // --- Tab 4: Features State ---
  const [features, setFeatures] = useState([]);
  const [newFeatureCode, setNewFeatureCode] = useState('');
  const [newFeatureName, setNewFeatureName] = useState('');
  const [newFeatureDesc, setNewFeatureDesc] = useState('');
  const [editingFeature, setEditingFeature] = useState(null);
  const [editFeatureCode, setEditFeatureCode] = useState('');
  const [editFeatureName, setEditFeatureName] = useState('');
  const [editFeatureDesc, setEditFeatureDesc] = useState('');
  const [editFeatureActive, setEditFeatureActive] = useState(true);

  // Common loading states
  const [loading, setLoading] = useState(false);

  // --- Tab 1: Roles State ---
  const [roles, setRoles] = useState([]);
  const [newRoleName, setNewRoleName] = useState('');
  const [newRoleDesc, setNewRoleDesc] = useState('');
  const [editingRole, setEditingRole] = useState(null);
  const [editRoleName, setEditRoleName] = useState('');
  const [editRoleDesc, setEditRoleDesc] = useState('');
  const [editRoleActive, setEditRoleActive] = useState(true);

  // --- Tab 2: Permission Matrix State ---
  const [selectedRoleId, setSelectedRoleId] = useState('');
  const [matrixPermissions, setMatrixPermissions] = useState([]);
  const [savingMatrix, setSavingMatrix] = useState(false);

  // --- Tab 3: User Assignment State ---
  const [users, setUsers] = useState([]);
  const [selectedUserId, setSelectedUserId] = useState('');
  const [userRoles, setUserRoles] = useState([]); // List of roleIds assigned to selected user
  const [savingUserRoles, setSavingUserRoles] = useState(false);

  // Load initial data
  useEffect(() => {
    fetchRoles();
    fetchUsers();
    fetchFeatures();
  }, []);

  // Fetch Features
  const fetchFeatures = async () => {
    try {
      setLoading(true);
      const data = await adminApi.features();
      setFeatures(data || []);
    } catch (error) {
      console.error(error);
      toast.error('Không thể tải danh sách tính năng.');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateFeature = async (e) => {
    e.preventDefault();
    if (!newFeatureCode.trim()) {
      toast.warning('Mã tính năng không được để trống.');
      return;
    }
    if (!newFeatureName.trim()) {
      toast.warning('Tên tính năng không được để trống.');
      return;
    }
    try {
      setLoading(true);
      await adminApi.createFeature({
        code: newFeatureCode.trim(),
        name: newFeatureName.trim(),
        description: newFeatureDesc.trim(),
      });
      toast.success('Thêm tính năng mới thành công!');
      setNewFeatureCode('');
      setNewFeatureName('');
      setNewFeatureDesc('');
      fetchFeatures();
    } catch (error) {
      console.error(error);
      toast.error('Lỗi khi thêm tính năng mới.');
    } finally {
      setLoading(false);
    }
  };

  const handleUpdateFeature = async (e) => {
    e.preventDefault();
    if (!editFeatureCode.trim()) {
      toast.warning('Mã tính năng không được để trống.');
      return;
    }
    if (!editFeatureName.trim()) {
      toast.warning('Tên tính năng không được để trống.');
      return;
    }
    try {
      setLoading(true);
      await adminApi.updateFeature(editingFeature.id, {
        code: editFeatureCode.trim(),
        name: editFeatureName.trim(),
        description: editFeatureDesc.trim(),
        isActive: editFeatureActive,
      });
      toast.success('Cập nhật tính năng thành công!');
      setEditingFeature(null);
      fetchFeatures();
    } catch (error) {
      console.error(error);
      toast.error('Lỗi khi cập nhật tính năng.');
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteFeature = async (featureId) => {
    if (!window.confirm('Bạn có chắc chắn muốn xóa tính năng này? Hành động này cũng sẽ xóa tất cả các quyền liên quan trong ma trận phân quyền.')) {
      return;
    }
    try {
      setLoading(true);
      await adminApi.deleteFeature(featureId);
      toast.success('Xóa tính năng thành công!');
      fetchFeatures();
    } catch (error) {
      console.error(error);
      toast.error('Lỗi khi xóa tính năng.');
    } finally {
      setLoading(false);
    }
  };

  const startEditFeature = (feature) => {
    setEditingFeature(feature);
    setEditFeatureCode(feature.code || '');
    setEditFeatureName(feature.name || '');
    setEditFeatureDesc(feature.description || '');
    setEditFeatureActive(feature.isActive !== false);
  };

  // Fetch Roles
  const fetchRoles = async () => {
    try {
      setLoading(true);
      const data = await adminApi.rolesAll();
      setRoles(data || []);
      if (data && data.length > 0 && !selectedRoleId) {
        setSelectedRoleId(data[0].id);
      }
    } catch (error) {
      console.error(error);
      toast.error('Không thể tải danh sách vai trò.');
    } finally {
      setLoading(false);
    }
  };

  // Fetch Users
  const fetchUsers = async () => {
    try {
      const data = await adminApi.users();
      setUsers(data || []);
    } catch (error) {
      console.error(error);
      toast.error('Không thể tải danh sách người dùng.');
    }
  };

  // Fetch Permissions for selected Role
  useEffect(() => {
    if (selectedRoleId) {
      fetchRolePermissions(selectedRoleId);
    }
  }, [selectedRoleId]);

  const fetchRolePermissions = async (roleId) => {
    try {
      setLoading(true);
      const data = await adminApi.permissionsAll(roleId);
      setMatrixPermissions(data || []);
    } catch (error) {
      console.error(error);
      toast.error('Không thể tải cấu hình quyền của vai trò này.');
    } finally {
      setLoading(false);
    }
  };

  // Fetch Roles for selected User
  useEffect(() => {
    if (selectedUserId) {
      fetchUserRoles(selectedUserId);
    }
  }, [selectedUserId]);

  const fetchUserRoles = async (userId) => {
    try {
      setLoading(true);
      const data = await adminApi.rolesAll2(userId);
      setUserRoles(data || []);
    } catch (error) {
      console.error(error);
      toast.error('Không thể tải danh sách vai trò của người dùng.');
    } finally {
      setLoading(false);
    }
  };

  // --- Handlers ---
  const handleCreateRole = async (e) => {
    e.preventDefault();
    if (!newRoleName.trim()) {
      toast.warning('Tên vai trò không được để trống.');
      return;
    }
    try {
      setLoading(true);
      await adminApi.rolesPOST({
        name: newRoleName,
        description: newRoleDesc,
      });
      toast.success('Thêm vai trò mới thành công!');
      setNewRoleName('');
      setNewRoleDesc('');
      fetchRoles();
    } catch (error) {
      console.error(error);
      toast.error('Lỗi khi thêm vai trò mới.');
    } finally {
      setLoading(false);
    }
  };

  const handleUpdateRole = async (e) => {
    e.preventDefault();
    if (!editRoleName.trim()) {
      toast.warning('Tên vai trò không được để trống.');
      return;
    }
    try {
      setLoading(true);
      await adminApi.rolesPUT(editingRole.id, {
        name: editRoleName,
        description: editRoleDesc,
        isActive: editRoleActive,
      });
      toast.success('Cập nhật vai trò thành công!');
      setEditingRole(null);
      fetchRoles();
    } catch (error) {
      console.error(error);
      toast.error('Lỗi khi cập nhật vai trò.');
    } finally {
      setLoading(false);
    }
  };

  const startEditRole = (role) => {
    setEditingRole(role);
    setEditRoleName(role.name || '');
    setEditRoleDesc(role.description || '');
    setEditRoleActive(role.isActive !== false);
  };

  // Checkbox toggle inside Permission Matrix
  const handlePermissionChange = (featureId, field, val) => {
    setMatrixPermissions((prev) =>
      prev.map((p) => {
        if (p.featureId === featureId) {
          const updated = { ...p, [field]: val };
          // If giving create/update/delete permission, automatically grant access permission
          if ((field === 'canCreate' || field === 'canUpdate' || field === 'canDelete') && val) {
            updated.canAccess = true;
          }
          // If removing access permission, automatically remove create/update/delete permissions
          if (field === 'canAccess' && !val) {
            updated.canCreate = false;
            updated.canUpdate = false;
            updated.canDelete = false;
          }
          return updated;
        }
        return p;
      })
    );
  };

  // Save matrix permissions
  const handleSavePermissions = async () => {
    if (!selectedRoleId) return;
    try {
      setSavingMatrix(true);
      const payload = matrixPermissions.map((p) => ({
        featureId: p.featureId,
        canAccess: !!p.canAccess,
        canCreate: !!p.canCreate,
        canUpdate: !!p.canUpdate,
        canDelete: !!p.canDelete,
      }));
      await adminApi.permissions(selectedRoleId, payload);
      toast.success('Cập nhật cấu hình phân quyền thành công!');
      fetchRolePermissions(selectedRoleId);
    } catch (error) {
      console.error(error);
      toast.error('Lỗi khi lưu cấu hình phân quyền.');
    } finally {
      setSavingMatrix(false);
    }
  };

  // Checkbox toggle for user roles
  const handleUserRoleToggle = (roleId, checked) => {
    if (checked) {
      setUserRoles((prev) => [...prev, roleId]);
    } else {
      setUserRoles((prev) => prev.filter((id) => id !== roleId));
    }
  };

  // Save User Roles
  const handleSaveUserRoles = async () => {
    if (!selectedUserId) return;
    try {
      setSavingUserRoles(true);
      await adminApi.rolesPUT2(selectedUserId, {
        roleIds: userRoles,
      });
      toast.success('Cập nhật vai trò người dùng thành công!');
      // Update local users array representation
      setUsers((prev) =>
        prev.map((u) => (u.id === selectedUserId ? { ...u, roles: userRoles } : u))
      );
    } catch (error) {
      console.error(error);
      toast.error('Lỗi khi lưu phân vai trò người dùng.');
    } finally {
      setSavingUserRoles(false);
    }
  };

  return (
    <div className="min-h-screen bg-slate-50 text-slate-800 p-6 font-sans">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="flex flex-col md:flex-row md:items-center md:justify-between mb-8 pb-5 border-b border-slate-200">
          <div>
            <h1 className="text-3xl font-bold tracking-tight text-slate-900 font-display">
              Quản Lý Phân Quyền Hệ Thống
            </h1>
            <p className="text-slate-500 mt-1 text-sm md:text-base">
              Thiết lập nhóm vai trò, ma trận quyền hạn tính năng và gán vai trò cho người sử dụng.
            </p>
          </div>
          <div className="mt-4 md:mt-0 flex gap-2">
            <button
              onClick={() => {
                fetchRoles();
                fetchUsers();
                fetchFeatures();
                if (selectedRoleId) fetchRolePermissions(selectedRoleId);
                if (selectedUserId) fetchUserRoles(selectedUserId);
                toast.success('Đã tải lại dữ liệu.');
              }}
              className="px-4 py-2 bg-white border border-slate-200 hover:bg-slate-50 text-slate-700 font-medium rounded-custom text-sm shadow-sm transition-colors flex items-center gap-1.5 cursor-pointer"
            >
              <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 1121.21 15H18" />
              </svg>
              Tải lại
            </button>
          </div>
        </div>

        {/* Tab Headers */}
        <div className="flex bg-slate-200/60 p-1.5 rounded-xl mb-6 max-w-3xl shadow-inner flex-wrap gap-y-1">
          <button
            onClick={() => setActiveTab('roles')}
            className={`flex-1 min-w-[120px] py-2.5 text-center font-medium text-sm rounded-lg transition-all cursor-pointer ${
              activeTab === 'roles'
                ? 'bg-white text-primary shadow-sm'
                : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            Danh sách vai trò
          </button>
          <button
            onClick={() => setActiveTab('matrix')}
            className={`flex-1 min-w-[120px] py-2.5 text-center font-medium text-sm rounded-lg transition-all cursor-pointer ${
              activeTab === 'matrix'
                ? 'bg-white text-primary shadow-sm'
                : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            Ma trận chức năng
          </button>
          <button
            onClick={() => setActiveTab('users')}
            className={`flex-1 min-w-[120px] py-2.5 text-center font-medium text-sm rounded-lg transition-all cursor-pointer ${
              activeTab === 'users'
                ? 'bg-white text-primary shadow-sm'
                : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            Gán cho người dùng
          </button>
          <button
            onClick={() => setActiveTab('features')}
            className={`flex-1 min-w-[120px] py-2.5 text-center font-medium text-sm rounded-lg transition-all cursor-pointer ${
              activeTab === 'features'
                ? 'bg-white text-primary shadow-sm'
                : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            Quản lý tính năng
          </button>
        </div>

        {/* LOADING INDICATOR */}
        {loading && (
          <div className="w-full bg-blue-50 border border-blue-100 rounded-lg p-3 text-blue-700 mb-6 flex items-center gap-2.5 text-sm animate-pulse">
            <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24" fill="none">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
            </svg>
            <span>Đang tải thông tin cấu hình từ hệ thống...</span>
          </div>
        )}

        {/* TAB 1: ROLES */}
        {activeTab === 'roles' && (
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* Create or Edit Role */}
            <div className="bg-white p-6 rounded-xl border border-slate-200/80 shadow-sm h-fit">
              <h2 className="text-lg font-bold mb-4 font-display text-slate-950">
                {editingRole ? 'Cập Nhật Vai Trò' : 'Thêm Vai Trò Mới'}
              </h2>
              <form onSubmit={editingRole ? handleUpdateRole : handleCreateRole} className="space-y-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                    Tên Vai Trò
                  </label>
                  <input
                    type="text"
                    value={editingRole ? editRoleName : newRoleName}
                    onChange={(e) => (editingRole ? setEditRoleName(e.target.value) : setNewRoleName(e.target.value))}
                    placeholder="Ví dụ: QuanTriVien, NhanVienKiemDuyet"
                    className="w-full px-3.5 py-2 border border-slate-200 rounded-custom text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                    Mô Tả
                  </label>
                  <textarea
                    rows={3}
                    value={editingRole ? editRoleDesc : newRoleDesc}
                    onChange={(e) => (editingRole ? setEditRoleDesc(e.target.value) : setNewRoleDesc(e.target.value))}
                    placeholder="Mô tả quyền hạn của vai trò này"
                    className="w-full px-3.5 py-2 border border-slate-200 rounded-custom text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary resize-none"
                  />
                </div>

                {editingRole && (
                  <div className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      id="editRoleActive"
                      checked={editRoleActive}
                      onChange={(e) => setEditRoleActive(e.target.checked)}
                      className="w-4 h-4 rounded text-primary focus:ring-primary/20 accent-primary"
                    />
                    <label htmlFor="editRoleActive" className="text-sm font-medium text-slate-700 select-none">
                      Kích hoạt hoạt động (Active)
                    </label>
                  </div>
                )}

                <div className="flex gap-2 pt-2">
                  {editingRole && (
                    <button
                      type="button"
                      onClick={() => setEditingRole(null)}
                      className="flex-1 py-2 px-4 border border-slate-200 text-slate-700 font-medium rounded-custom text-sm hover:bg-slate-50 transition-colors cursor-pointer"
                    >
                      Hủy bỏ
                    </button>
                  )}
                  <Button type="submit" className="flex-1 text-sm">
                    {editingRole ? 'Cập Nhật' : 'Tạo Mới'}
                  </Button>
                </div>
              </form>
            </div>

            {/* List Roles */}
            <div className="lg:col-span-2 bg-white rounded-xl border border-slate-200/80 shadow-sm overflow-hidden">
              <div className="p-5 border-b border-slate-100 flex justify-between items-center">
                <h2 className="text-lg font-bold font-display text-slate-950">Danh sách các vai trò khả dụng</h2>
                <span className="text-xs font-semibold px-2 py-1 bg-slate-100 text-slate-600 rounded-full">
                  {roles.length} vai trò
                </span>
              </div>
              <div className="divide-y divide-slate-100 max-h-[500px] overflow-y-auto">
                {roles.length === 0 ? (
                  <div className="p-8 text-center text-slate-400 text-sm">
                    Chưa có vai trò nào được định nghĩa trên hệ thống.
                  </div>
                ) : (
                  roles.map((role) => (
                    <div key={role.id} className="p-4 hover:bg-slate-50/50 transition-colors flex items-start justify-between gap-4">
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="font-bold text-slate-900 text-base">{role.name}</span>
                          <span
                            className={`inline-flex items-center px-2 py-0.5 rounded-full text-2xs font-semibold ${
                              role.isActive !== false
                                ? 'bg-emerald-50 text-emerald-700 border border-emerald-100'
                                : 'bg-red-50 text-red-700 border border-red-100'
                            }`}
                          >
                            {role.isActive !== false ? 'Active' : 'Inactive'}
                          </span>
                        </div>
                        <p className="text-slate-500 text-sm mt-1">{role.description || 'Không có mô tả cho vai trò này.'}</p>
                        <div className="text-2xs text-slate-400 mt-2">ID: {role.id}</div>
                      </div>
                      <button
                        onClick={() => startEditRole(role)}
                        className="p-2 border border-slate-200 rounded-lg hover:bg-slate-100 text-slate-600 hover:text-slate-900 transition-colors cursor-pointer"
                        title="Chỉnh sửa thông tin vai trò"
                      >
                        <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                        </svg>
                      </button>
                    </div>
                  ))
                )}
              </div>
            </div>
          </div>
        )}

        {/* TAB 2: PERMISSION MATRIX */}
        {activeTab === 'matrix' && (
          <div className="bg-white rounded-xl border border-slate-200/80 shadow-sm overflow-hidden">
            {/* Header select */}
            <div className="p-5 border-b border-slate-100 bg-slate-50/50 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
              <div className="flex items-center gap-3">
                <label className="text-sm font-semibold text-slate-600 uppercase tracking-wider whitespace-nowrap">
                  Chọn Vai Trò Để Phân Quyền:
                </label>
                <select
                  value={selectedRoleId}
                  onChange={(e) => setSelectedRoleId(e.target.value)}
                  className="bg-white border border-slate-200 rounded-custom px-3 py-1.5 text-sm font-semibold text-slate-800 focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary cursor-pointer min-w-[200px]"
                >
                  {roles.map((r) => (
                    <option key={r.id} value={r.id}>
                      {r.name}
                    </option>
                  ))}
                </select>
              </div>
              <Button
                isLoading={savingMatrix}
                onClick={handleSavePermissions}
                className="w-full sm:w-auto px-6 py-2 shadow-sm text-sm"
              >
                Lưu cấu hình phân quyền
              </Button>
            </div>

            {/* Matrix Table */}
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="border-b border-slate-100 bg-slate-100/40 text-slate-500 font-semibold text-xs tracking-wider uppercase">
                    <th className="p-4 pl-6">Mã Tính Năng</th>
                    <th className="p-4">Tên Tính Năng</th>
                    <th className="p-4 text-center">Truy cập (Read)</th>
                    <th className="p-4 text-center">Tạo mới (Create)</th>
                    <th className="p-4 text-center">Cập nhật (Update)</th>
                    <th className="p-4 text-center">Xóa (Delete)</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-sm">
                  {matrixPermissions.length === 0 ? (
                    <tr>
                      <td colSpan={6} className="p-8 text-center text-slate-400">
                        Chưa có tính năng nào được cấu hình hoặc chọn vai trò trước.
                      </td>
                    </tr>
                  ) : (
                    matrixPermissions.map((permission) => (
                      <tr key={permission.featureId} className="hover:bg-slate-50/40 transition-colors">
                        <td className="p-4 pl-6 font-mono font-medium text-xs text-slate-500">
                          {permission.featureCode}
                        </td>
                        <td className="p-4 font-semibold text-slate-800">
                          {permission.featureName}
                        </td>
                        <td className="p-4 text-center">
                          <input
                            type="checkbox"
                            checked={!!permission.canAccess}
                            onChange={(e) => handlePermissionChange(permission.featureId, 'canAccess', e.target.checked)}
                            className="w-4 h-4 rounded text-primary border-slate-300 focus:ring-primary/20 accent-primary cursor-pointer"
                          />
                        </td>
                        <td className="p-4 text-center">
                          <input
                            type="checkbox"
                            checked={!!permission.canCreate}
                            onChange={(e) => handlePermissionChange(permission.featureId, 'canCreate', e.target.checked)}
                            className="w-4 h-4 rounded text-primary border-slate-300 focus:ring-primary/20 accent-primary cursor-pointer"
                          />
                        </td>
                        <td className="p-4 text-center">
                          <input
                            type="checkbox"
                            checked={!!permission.canUpdate}
                            onChange={(e) => handlePermissionChange(permission.featureId, 'canUpdate', e.target.checked)}
                            className="w-4 h-4 rounded text-primary border-slate-300 focus:ring-primary/20 accent-primary cursor-pointer"
                          />
                        </td>
                        <td className="p-4 text-center">
                          <input
                            type="checkbox"
                            checked={!!permission.canDelete}
                            onChange={(e) => handlePermissionChange(permission.featureId, 'canDelete', e.target.checked)}
                            className="w-4 h-4 rounded text-primary border-slate-300 focus:ring-primary/20 accent-primary cursor-pointer"
                          />
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {/* TAB 3: USER ROLES */}
        {activeTab === 'users' && (
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* User List Panel */}
            <div className="bg-white rounded-xl border border-slate-200/80 shadow-sm overflow-hidden h-fit">
              <div className="p-4 border-b border-slate-100 bg-slate-50/50">
                <h3 className="font-bold text-slate-900 font-display">Chọn Người Dùng</h3>
                <p className="text-slate-400 text-xs">Danh sách tài khoản trong hệ thống</p>
              </div>
              <div className="divide-y divide-slate-100 max-h-[500px] overflow-y-auto">
                {users.length === 0 ? (
                  <div className="p-6 text-center text-slate-400 text-sm">
                    Không tìm thấy người dùng nào.
                  </div>
                ) : (
                  users.map((u) => (
                    <button
                      key={u.id}
                      onClick={() => setSelectedUserId(u.id)}
                      className={`w-full text-left p-4 hover:bg-slate-50 transition-colors flex items-center justify-between gap-3 cursor-pointer ${
                        selectedUserId === u.id ? 'bg-primary/5 hover:bg-primary/5 border-l-4 border-primary pl-3' : ''
                      }`}
                    >
                      <div>
                        <div className="font-bold text-slate-900 text-sm flex items-center gap-1.5">
                          {u.username}
                          {u.isSystemAdmin && (
                            <span className="bg-indigo-50 text-indigo-700 text-3xs font-bold uppercase tracking-wider px-1.5 py-0.5 rounded border border-indigo-100">
                              SysAdmin
                            </span>
                          )}
                        </div>
                        <div className="text-slate-400 text-xs mt-0.5">{u.fullName || u.email}</div>
                      </div>
                      <div className="flex flex-wrap gap-1 justify-end max-w-[120px]">
                        {u.roles && u.roles.length > 0 ? (
                          u.roles.map((rName) => (
                            <span key={rName} className="bg-slate-100 text-slate-700 text-3xs font-semibold px-1 rounded">
                              {rName}
                            </span>
                          ))
                        ) : (
                          <span className="text-3xs text-slate-300">Chưa gán vai trò</span>
                        )}
                      </div>
                    </button>
                  ))
                )}
              </div>
            </div>

            {/* Role Config Panel */}
            <div className="lg:col-span-2 bg-white rounded-xl border border-slate-200/80 shadow-sm overflow-hidden">
              {selectedUserId ? (
                (() => {
                  const selectedUserObj = users.find((u) => u.id === selectedUserId);
                  return (
                    <>
                      <div className="p-5 border-b border-slate-100 bg-slate-50/50 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                        <div>
                          <h3 className="text-lg font-bold font-display text-slate-950">
                            Gán vai trò cho: <span className="text-primary">{selectedUserObj?.username}</span>
                          </h3>
                          <p className="text-slate-400 text-xs mt-0.5">
                            {selectedUserObj?.fullName} ({selectedUserObj?.email || 'Không có email'})
                          </p>
                        </div>
                        <Button
                          isLoading={savingUserRoles}
                          onClick={handleSaveUserRoles}
                          className="w-full sm:w-auto px-6 py-2 shadow-sm text-sm"
                        >
                          Lưu phân vai trò
                        </Button>
                      </div>

                      <div className="p-6">
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                          {roles.map((role) => {
                            const isAssigned = userRoles.includes(role.id);
                            return (
                              <div
                                key={role.id}
                                className={`p-4 border rounded-xl flex items-start gap-3 transition-all ${
                                  isAssigned
                                    ? 'border-primary/30 bg-primary/2'
                                    : 'border-slate-200 hover:border-slate-300'
                                }`}
                              >
                                <input
                                  type="checkbox"
                                  id={`role-check-${role.id}`}
                                  checked={isAssigned}
                                  onChange={(e) => handleUserRoleToggle(role.id, e.target.checked)}
                                  className="w-4.5 h-4.5 rounded text-primary border-slate-300 focus:ring-primary/20 accent-primary cursor-pointer mt-0.5"
                                />
                                <label
                                  htmlFor={`role-check-${role.id}`}
                                  className="flex-1 select-none cursor-pointer"
                                >
                                  <span className="font-bold text-slate-800 text-sm block">
                                    {role.name}
                                  </span>
                                  <span className="text-slate-400 text-xs mt-1 block">
                                    {role.description || 'Không có mô tả.'}
                                  </span>
                                </label>
                              </div>
                            );
                          })}
                        </div>
                      </div>
                    </>
                  );
                })()
              ) : (
                <div className="p-12 text-center text-slate-400 text-sm flex flex-col items-center justify-center min-h-[300px]">
                  <svg className="w-12 h-12 text-slate-300 mb-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                  </svg>
                  Chọn một người dùng từ danh sách bên trái để bắt đầu gán vai trò.
                </div>
              )}
            </div>
          </div>
        )}

        {/* TAB 4: FEATURES */}
        {activeTab === 'features' && (
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* Create or Edit Feature */}
            <div className="bg-white p-6 rounded-xl border border-slate-200/80 shadow-sm h-fit">
              <h2 className="text-lg font-bold mb-4 font-display text-slate-950">
                {editingFeature ? 'Cập Nhật Tính Năng' : 'Thêm Tính Năng Mới'}
              </h2>
              <form onSubmit={editingFeature ? handleUpdateFeature : handleCreateFeature} className="space-y-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                    Chọn trang được viết sẵn
                  </label>
                  <select
                    onChange={(e) => {
                      const selected = PREDEFINED_PAGES.find(p => p.code === e.target.value);
                      if (selected) {
                        if (editingFeature) {
                          setEditFeatureCode(selected.code);
                          setEditFeatureName(selected.name);
                          setEditFeatureDesc(selected.description);
                        } else {
                          setNewFeatureCode(selected.code);
                          setNewFeatureName(selected.name);
                          setNewFeatureDesc(selected.description);
                        }
                      } else if (!editingFeature) {
                        setNewFeatureCode('');
                        setNewFeatureName('');
                        setNewFeatureDesc('');
                      }
                    }}
                    className="w-full px-3.5 py-2 border border-slate-200 rounded-custom text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary bg-white cursor-pointer mb-2"
                  >
                    <option value="">-- Click để chọn nhanh trang --</option>
                    {PREDEFINED_PAGES.map((p) => (
                      <option key={p.code} value={p.code}>
                        {p.name} ({p.code})
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                    Mã Tính Năng (Tự động điền)
                  </label>
                  <input
                    type="text"
                    value={editingFeature ? editFeatureCode : newFeatureCode}
                    onChange={(e) => (editingFeature ? setEditFeatureCode(e.target.value) : setNewFeatureCode(e.target.value))}
                    placeholder="Ví dụ: ContractApproval, ProjectCreate"
                    className="w-full px-3.5 py-2 border border-slate-200 rounded-custom text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                    Tên Tính Năng (Tự động điền)
                  </label>
                  <input
                    type="text"
                    value={editingFeature ? editFeatureName : newFeatureName}
                    onChange={(e) => (editingFeature ? setEditFeatureName(e.target.value) : setNewFeatureName(e.target.value))}
                    placeholder="Ví dụ: Phê duyệt hợp đồng, Tạo dự án mới"
                    className="w-full px-3.5 py-2 border border-slate-200 rounded-custom text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                    Mô Tả (Tự động điền)
                  </label>
                  <textarea
                    rows={3}
                    value={editingFeature ? editFeatureDesc : newFeatureDesc}
                    onChange={(e) => (editingFeature ? setEditFeatureDesc(e.target.value) : setNewFeatureDesc(e.target.value))}
                    placeholder="Mô tả công năng của tính năng này"
                    className="w-full px-3.5 py-2 border border-slate-200 rounded-custom text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary resize-none"
                  />
                </div>

                {editingFeature && (
                  <div className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      id="editFeatureActive"
                      checked={editFeatureActive}
                      onChange={(e) => setEditFeatureActive(e.target.checked)}
                      className="w-4 h-4 rounded text-primary focus:ring-primary/20 accent-primary"
                    />
                    <label htmlFor="editFeatureActive" className="text-sm font-medium text-slate-700 select-none">
                      Kích hoạt hoạt động (Active)
                    </label>
                  </div>
                )}

                <div className="flex gap-2 pt-2">
                  {editingFeature && (
                    <button
                      type="button"
                      onClick={() => setEditingFeature(null)}
                      className="flex-1 py-2 px-4 border border-slate-200 text-slate-700 font-medium rounded-custom text-sm hover:bg-slate-50 transition-colors cursor-pointer"
                    >
                      Hủy bỏ
                    </button>
                  )}
                  <Button type="submit" className="flex-1 text-sm">
                    {editingFeature ? 'Cập Nhật' : 'Tạo Mới'}
                  </Button>
                </div>
              </form>
            </div>

            {/* List Features */}
            <div className="lg:col-span-2 bg-white rounded-xl border border-slate-200/80 shadow-sm overflow-hidden">
              <div className="p-5 border-b border-slate-100 flex justify-between items-center">
                <h2 className="text-lg font-bold font-display text-slate-950">Danh sách các tính năng hệ thống</h2>
                <span className="text-xs font-semibold px-2 py-1 bg-slate-100 text-slate-600 rounded-full">
                  {features.length} tính năng
                </span>
              </div>
              <div className="divide-y divide-slate-100 max-h-[500px] overflow-y-auto">
                {features.length === 0 ? (
                  <div className="p-8 text-center text-slate-400 text-sm">
                    Chưa có tính năng nào được định nghĩa trên hệ thống.
                  </div>
                ) : (
                  features.map((feat) => (
                    <div key={feat.id} className="p-4 hover:bg-slate-50/50 transition-colors flex items-start justify-between gap-4">
                      <div>
                        <div className="flex items-center gap-2 flex-wrap">
                          <span className="font-mono font-semibold text-xs px-2 py-0.5 bg-slate-100 text-slate-700 rounded border border-slate-200">
                            {feat.code}
                          </span>
                          <span className="font-bold text-slate-900 text-base">{feat.name}</span>
                          <span
                            className={`inline-flex items-center px-2 py-0.5 rounded-full text-2xs font-semibold ${
                              feat.isActive !== false
                                ? 'bg-emerald-50 text-emerald-700 border border-emerald-100'
                                : 'bg-red-50 text-red-700 border border-red-100'
                            }`}
                          >
                            {feat.isActive !== false ? 'Active' : 'Inactive'}
                          </span>
                        </div>
                        <p className="text-slate-500 text-sm mt-1">{feat.description || 'Không có mô tả cho tính năng này.'}</p>
                        <div className="text-2xs text-slate-400 mt-2">ID: {feat.id}</div>
                      </div>
                      <div className="flex gap-1.5">
                        <button
                          onClick={() => startEditFeature(feat)}
                          className="p-2 border border-slate-200 rounded-lg hover:bg-slate-100 text-slate-600 hover:text-slate-900 transition-colors cursor-pointer"
                          title="Chỉnh sửa thông tin tính năng"
                        >
                          <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                          </svg>
                        </button>
                        <button
                          onClick={() => handleDeleteFeature(feat.id)}
                          className="p-2 border border-red-100 hover:border-red-200 rounded-lg hover:bg-red-50 text-red-500 hover:text-red-700 transition-colors cursor-pointer"
                          title="Xóa tính năng"
                        >
                          <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                          </svg>
                        </button>
                      </div>
                    </div>
                  ))
                )}
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
