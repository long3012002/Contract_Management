import { useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { implProjectsApi } from '../api/implProjectsApi';

const STATUS_TO_ENUM = {
  Draft: 1,
  Submitted: 2,
  Approved: 3,
  Implementing: 4,
  Acceptance: 5,
  Payment: 6,
  Settlement: 7,
  Completed: 8,
};

const ENUM_TO_STATUS = {
  1: 'Draft',
  2: 'Submitted',
  3: 'Approved',
  4: 'Implementing',
  5: 'Acceptance',
  6: 'Payment',
  7: 'Settlement',
  8: 'Completed',
};

const NEXT_STATUS = {
  Draft: 'Submitted',
  Submitted: 'Approved',
  Approved: 'Implementing',
  Implementing: 'Acceptance',
  Acceptance: 'Payment',
  Payment: 'Settlement',
  Settlement: 'Completed',
};

export function useImplProjects({ search = '', page = 1, pageSize = 8, status = 'all', type = 'all' } = {}) {
  const queryClient = useQueryClient();

  const { data: pagedData, isLoading, isError } = useQuery({
    queryKey: ['impl-projects', { search, page, pageSize, status, type }],
    queryFn: async () => {
      const result = await implProjectsApi.getAll({
        search: search || undefined,
        page,
        pageSize,
      });

      // Filter by LoaiDuAn = 2 (Implementation Projects)
      const rawItems = result.items || [];
      const implItems = rawItems
        .filter(item => item.loaiDuAn === 2)
        .map(item => ({
          ...item,
          status: ENUM_TO_STATUS[item.trangThai] || 'Draft',
          projectType: item.hinhThucQuanLy === 1 ? 'Phần mềm' : item.hinhThucQuanLy === 2 ? 'Hạ tầng' : item.hinhThucQuanLy === 3 ? 'Tích hợp' : 'Khác',
          totalBudget: item.duToanPheDuyet || 0,
          startDate: item.ngayBatDau ? item.ngayBatDau.split('T')[0] : '',
          endDate: item.ngayKetThuc ? item.ngayKetThuc.split('T')[0] : '',
          sourceProjects: [],
          auditTrail: [],
        }));

      // Clientside filter
      let filtered = implItems;
      if (status !== 'all') {
        filtered = filtered.filter(item => item.status === status);
      }
      if (type !== 'all') {
        filtered = filtered.filter(item => item.projectType === type);
      }

      return {
        items: filtered,
        totalItems: filtered.length,
        totalPages: Math.ceil(filtered.length / pageSize) || 1,
      };
    },
  });

  // Query for all implementation projects (no paging)
  const { data: allItems } = useQuery({
    queryKey: ['impl-projects', 'all'],
    queryFn: async () => {
      const result = await implProjectsApi.getAll({
        page: 1,
        pageSize: 1000,
      });
      return (result.items || [])
        .filter(item => item.loaiDuAn === 2)
        .map(item => ({
          ...item,
          status: ENUM_TO_STATUS[item.trangThai] || 'Draft',
          projectType: item.hinhThucQuanLy === 1 ? 'Phần mềm' : item.hinhThucQuanLy === 2 ? 'Hạ tầng' : item.hinhThucQuanLy === 3 ? 'Tích hợp' : 'Khác',
          totalBudget: item.duToanPheDuyet || 0,
          startDate: item.ngayBatDau ? item.ngayBatDau.split('T')[0] : '',
          endDate: item.ngayKetThuc ? item.ngayKetThuc.split('T')[0] : '',
          sourceProjects: [],
        }));
    },
  });

  const createMutation = useMutation({
    mutationFn: implProjectsApi.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['impl-projects'] });
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }) => implProjectsApi.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['impl-projects'] });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: implProjectsApi.delete,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['impl-projects'] });
    },
  });

  const getById = useCallback((id) => {
    if (!allItems) return null;
    return allItems.find((item) => item.id === id) || null;
  }, [allItems]);

  const addItem = useCallback(async (newItem) => {
    const totalBudget = (newItem.sourceProjects || []).reduce((sum, sp) => sum + sp.value, 0);
    const payload = {
      code: newItem.code,
      name: newItem.name,
      description: newItem.description || '',
      duToanPheDuyet: totalBudget || newItem.totalBudget || 0,
      trangThai: STATUS_TO_ENUM[newItem.status || 'Draft'],
      loaiDuAn: 2,
      hinhThucQuanLy: newItem.projectType === 'Phần mềm' ? 1 : newItem.projectType === 'Hạ tầng' ? 2 : newItem.projectType === 'Tích hợp' ? 3 : 4,
      ngayBatDau: newItem.startDate ? new Date(newItem.startDate).toISOString() : null,
      ngayKetThuc: newItem.endDate ? new Date(newItem.endDate).toISOString() : null,
      isActive: true,
    };
    await createMutation.mutateAsync(payload);
  }, [createMutation]);

  const updateItem = useCallback(async (id, updates) => {
    const current = getById(id);
    if (!current) return;

    const totalBudget = updates.sourceProjects
      ? updates.sourceProjects.reduce((sum, sp) => sum + sp.value, 0)
      : current.totalBudget;

    const payload = {
      name: updates.name,
      description: updates.description || current.description,
      duToanPheDuyet: totalBudget || updates.totalBudget || current.totalBudget,
      trangThai: STATUS_TO_ENUM[updates.status || current.status],
      loaiDuAn: 2,
      hinhThucQuanLy: updates.projectType === 'Phần mềm' ? 1 : updates.projectType === 'Hạ tầng' ? 2 : updates.projectType === 'Tích hợp' ? 3 : 4,
      ngayBatDau: updates.startDate ? new Date(updates.startDate).toISOString() : current.ngayBatDau,
      ngayKetThuc: updates.endDate ? new Date(updates.endDate).toISOString() : current.ngayKetThuc,
      isActive: true,
    };
    await updateMutation.mutateAsync({ id, data: payload });
  }, [updateMutation, getById]);

  const deleteItem = useCallback(async (id) => {
    await deleteMutation.mutateAsync(id);
  }, [deleteMutation]);

  const advanceStatus = useCallback(async (id) => {
    const current = getById(id);
    if (!current) return;
    const nextStatus = NEXT_STATUS[current.status];
    if (!nextStatus) return;

    await updateItem(id, { status: nextStatus });
  }, [getById, updateItem]);

  return {
    data: pagedData?.items || [],
    allData: allItems || [],
    totalItems: pagedData?.totalItems || 0,
    totalPages: pagedData?.totalPages || 0,
    getById,
    addItem,
    updateItem,
    deleteItem,
    advanceStatus,
    isLoading,
    isError,
  };
}
