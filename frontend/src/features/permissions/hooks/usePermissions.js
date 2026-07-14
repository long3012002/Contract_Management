import { useState, useEffect, useCallback, useContext } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { permissionsApi } from '../api/permissionsApi';
import { toast } from 'sonner';
import { PermissionsContext } from '../context/PermissionsContext';

// Hook contains all stateful logic (separated from Context Provider for Clean Architecture & easy testing)
export const usePermissionsLogic = () => {
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState('department'); // 'department' | 'position'
  const [selectedEntityId, setSelectedEntityId] = useState('');
  const [matrixState, setMatrixState] = useState({});

  // Query Features
  const featuresQuery = useQuery({
    queryKey: ['features'],
    queryFn: async () => {
      const res = await permissionsApi.getFeatures();
      return res.data;
    },
  });

  // Query Departments
  const departmentsQuery = useQuery({
    queryKey: ['departments'],
    queryFn: async () => {
      const res = await permissionsApi.getDepartments();
      return res.data;
    },
  });

  // Query Positions
  const positionsQuery = useQuery({
    queryKey: ['positions'],
    queryFn: async () => {
      const res = await permissionsApi.getPositions();
      return res.data;
    },
  });

  const features = featuresQuery.data || [];
  const departments = departmentsQuery.data || [];
  const positions = positionsQuery.data || [];

  // Auto select first department when departments load
  useEffect(() => {
    if (!selectedEntityId && departments.length > 0) {
      setSelectedEntityId(departments[0].id);
    }
  }, [departments, selectedEntityId]);

  // Query Permissions for selected entity
  const permissionsQuery = useQuery({
    queryKey: ['permissions', activeTab, selectedEntityId],
    queryFn: async () => {
      if (!selectedEntityId) return [];
      const res = activeTab === 'department'
        ? await permissionsApi.getDepartmentPermissions(selectedEntityId)
        : await permissionsApi.getPositionPermissions(selectedEntityId);
      return res.data;
    },
    enabled: !!selectedEntityId,
  });

  // Sync permissions to matrixState local state
  useEffect(() => {
    if (permissionsQuery.data && features.length > 0) {
      const newMatrix = {};
      permissionsQuery.data.forEach(item => {
        const permsArray = item.permissions ? item.permissions.split(';') : [];
        newMatrix[item.featureId] = {
          canAccess: item.canAccess,
          create: permsArray.includes('create'),
          update: permsArray.includes('update'),
          delete: permsArray.includes('delete')
        };
      });

      features.forEach(f => {
        if (!newMatrix[f.id]) {
          newMatrix[f.id] = { canAccess: false, create: false, update: false, delete: false };
        }
      });

      setMatrixState(newMatrix);
    }
  }, [permissionsQuery.data, features]);

  // Event handler for tab change
  const handleTabChange = useCallback((tab) => {
    setActiveTab(tab);
    if (tab === 'department' && departments.length > 0) {
      setSelectedEntityId(departments[0].id);
    } else if (tab === 'position' && positions.length > 0) {
      setSelectedEntityId(positions[0].id);
    }
  }, [departments, positions]);

  // Handle Checkbox Change
  const handlePermissionChange = useCallback((featureId, action, checked) => {
    setMatrixState(prev => {
      const currentPerm = prev[featureId] || { canAccess: false, create: false, update: false, delete: false };
      const updatedPerm = { ...currentPerm, [action]: checked };
      
      // Specific logic: if canAccess is unchecked, all others must be false
      if (action === 'canAccess' && !checked) {
        updatedPerm.create = false;
        updatedPerm.update = false;
        updatedPerm.delete = false;
      }
      // If any action (create, update, delete) is checked, canAccess MUST be true
      else if ((action === 'create' || action === 'update' || action === 'delete') && checked) {
        updatedPerm.canAccess = true;
      }
      
      return { ...prev, [featureId]: updatedPerm };
    });
  }, []);

  // Save changes Mutation
  const saveMutation = useMutation({
    mutationFn: async (payload) => {
      if (activeTab === 'department') {
        return permissionsApi.updateDepartmentPermissions(selectedEntityId, payload);
      } else {
        return permissionsApi.updatePositionPermissions(selectedEntityId, payload);
      }
    },
    onSuccess: () => {
      toast.success('Lưu phân quyền thành công!');
      queryClient.invalidateQueries({ queryKey: ['permissions', activeTab, selectedEntityId] });
    },
    onError: () => {
      toast.error('Lỗi khi lưu phân quyền');
    }
  });

  // Department Mutations
  const createDepartmentMutation = useMutation({
    mutationFn: async (name) => {
      return permissionsApi.createDepartment({ TenPhongBan: name });
    },
    onSuccess: (res) => {
      toast.success('Thêm phòng ban thành công!');
      queryClient.invalidateQueries({ queryKey: ['departments'] });
      if (res.data && res.data.id) {
        setSelectedEntityId(res.data.id);
      }
    },
    onError: (err) => {
      toast.error(err.response?.data?.message || err.response?.data?.Message || 'Lỗi khi thêm phòng ban');
    }
  });

  const updateDepartmentMutation = useMutation({
    mutationFn: async ({ id, name }) => {
      return permissionsApi.updateDepartment(id, { TenPhongBan: name });
    },
    onSuccess: () => {
      toast.success('Cập nhật phòng ban thành công!');
      queryClient.invalidateQueries({ queryKey: ['departments'] });
    },
    onError: (err) => {
      toast.error(err.response?.data?.message || err.response?.data?.Message || 'Lỗi khi cập nhật phòng ban');
    }
  });

  const deleteDepartmentMutation = useMutation({
    mutationFn: async (id) => {
      return permissionsApi.deleteDepartment(id);
    },
    onSuccess: (_, deletedId) => {
      toast.success('Xóa phòng ban thành công!');
      queryClient.invalidateQueries({ queryKey: ['departments'] });
      if (selectedEntityId === deletedId) {
        setSelectedEntityId('');
      }
    },
    onError: (err) => {
      toast.error(err.response?.data?.message || err.response?.data?.Message || 'Lỗi khi xóa phòng ban');
    }
  });

  // Position Mutations
  const createPositionMutation = useMutation({
    mutationFn: async (name) => {
      return permissionsApi.createPosition({ TenChucVu: name });
    },
    onSuccess: (res) => {
      toast.success('Thêm chức vụ thành công!');
      queryClient.invalidateQueries({ queryKey: ['positions'] });
      if (res.data && res.data.id) {
        setSelectedEntityId(res.data.id);
      }
    },
    onError: (err) => {
      toast.error(err.response?.data?.message || err.response?.data?.Message || 'Lỗi khi thêm chức vụ');
    }
  });

  const updatePositionMutation = useMutation({
    mutationFn: async ({ id, name }) => {
      return permissionsApi.updatePosition(id, { TenChucVu: name });
    },
    onSuccess: () => {
      toast.success('Cập nhật chức vụ thành công!');
      queryClient.invalidateQueries({ queryKey: ['positions'] });
    },
    onError: (err) => {
      toast.error(err.response?.data?.message || err.response?.data?.Message || 'Lỗi khi cập nhật chức vụ');
    }
  });

  const deletePositionMutation = useMutation({
    mutationFn: async (id) => {
      return permissionsApi.deletePosition(id);
    },
    onSuccess: (_, deletedId) => {
      toast.success('Xóa chức vụ thành công!');
      queryClient.invalidateQueries({ queryKey: ['positions'] });
      if (selectedEntityId === deletedId) {
        setSelectedEntityId('');
      }
    },
    onError: (err) => {
      toast.error(err.response?.data?.message || err.response?.data?.Message || 'Lỗi khi xóa chức vụ');
    }
  });

  // Save changes
  const handleSave = useCallback(async () => {
    if (!selectedEntityId) return;
    
    // Convert matrixState back to backend payload
    const payload = Object.keys(matrixState).map(featureId => {
      const state = matrixState[featureId];
      const perms = [];
      if (state.create) perms.push('create');
      if (state.update) perms.push('update');
      if (state.delete) perms.push('delete');
      
      return {
        featureId,
        canAccess: state.canAccess,
        permissions: perms.join(';')
      };
    });
    
    saveMutation.mutate(payload);
  }, [selectedEntityId, matrixState, saveMutation]);

  const isLoading = featuresQuery.isLoading || departmentsQuery.isLoading || positionsQuery.isLoading || permissionsQuery.isLoading;

  return {
    activeTab,
    setActiveTab: handleTabChange,
    selectedEntityId,
    setSelectedEntityId,
    featuresQuery,
    departmentsQuery,
    positionsQuery,
    permissionsQuery,
    matrixState,
    handlePermissionChange,
    handleSave,
    saveMutation,
    
    // CRUD Mutations
    createDepartmentMutation,
    updateDepartmentMutation,
    deleteDepartmentMutation,
    createPositionMutation,
    updatePositionMutation,
    deletePositionMutation,
  };
};

// Custom Hook to consume the Context
export const usePermissions = () => {
  const context = useContext(PermissionsContext);
  if (!context) {
    throw new Error('usePermissions must be used within a PermissionsProvider');
  }
  return context;
};
