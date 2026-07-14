import { useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { bidPackagesApi } from '../api/bidPackagesApi';
import { useImplProjects } from '../../implementation-projects/hooks/useImplProjects';

export function useBidPackages({ search = '', page = 1, pageSize = 8, status = 'all', project = 'all' } = {}) {
  const queryClient = useQueryClient();
  const { allData: projects } = useImplProjects();

  const getProjectName = useCallback((projectId) => {
    const proj = projects?.find(p => p.id === projectId);
    return proj ? proj.name : 'Dự án không xác định';
  }, [projects]);

  const { data: pagedData, isLoading, isError } = useQuery({
    queryKey: ['bid-packages', { search, page, pageSize, status, project }],
    queryFn: async () => {
      const result = await bidPackagesApi.getAll({
        search: search || undefined,
        page,
        pageSize,
      });

      const items = (result.items || []).map(item => ({
        ...item,
        projectId: item.duAnId || '',
        estimatedValue: item.giaTriGoiThau || 0,
        warningThresholdPercent: item.nguongCanhBaoPercent || 100,
        status: item.isActive ? 'Active' : 'Draft',
        projectName: getProjectName(item.duAnId),
        totalContractValue: 0,
      }));

      // Clientside filtering
      let filtered = items;
      if (status !== 'all') {
        filtered = filtered.filter(item => item.status === status);
      }
      if (project !== 'all') {
        filtered = filtered.filter(item => item.projectId === project);
      }

      return {
        items: filtered,
        totalItems: filtered.length,
        totalPages: Math.ceil(filtered.length / pageSize) || 1,
      };
    },
  });

  const { data: allItems } = useQuery({
    queryKey: ['bid-packages', 'all'],
    queryFn: async () => {
      const result = await bidPackagesApi.getAll({
        page: 1,
        pageSize: 1000,
      });
      return (result.items || []).map(item => ({
        ...item,
        projectId: item.duAnId || '',
        estimatedValue: item.giaTriGoiThau || 0,
        warningThresholdPercent: item.nguongCanhBaoPercent || 100,
        status: item.isActive ? 'Active' : 'Draft',
        projectName: getProjectName(item.duAnId),
      }));
    },
  });

  const createMutation = useMutation({
    mutationFn: bidPackagesApi.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['bid-packages'] });
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }) => bidPackagesApi.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['bid-packages'] });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: bidPackagesApi.delete,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['bid-packages'] });
    },
  });

  const getByProjectId = useCallback((projectId) => {
    if (!allItems) return [];
    return allItems.filter(item => item.projectId === projectId);
  }, [allItems]);

  const addItem = useCallback(async (newItem) => {
    const payload = {
      code: newItem.code,
      name: newItem.name,
      description: newItem.description || '',
      duAnId: newItem.projectId || null,
      giaTriGoiThau: newItem.estimatedValue || 0,
      nguongCanhBaoPercent: newItem.warningThresholdPercent || 100,
      isActive: newItem.status !== 'Draft',
    };
    await createMutation.mutateAsync(payload);
  }, [createMutation]);

  const updateItem = useCallback(async (id, updates) => {
    const payload = {
      name: updates.name,
      description: updates.description || '',
      duAnId: updates.projectId || null,
      giaTriGoiThau: updates.estimatedValue || 0,
      nguongCanhBaoPercent: updates.warningThresholdPercent || 100,
      isActive: updates.status !== 'Draft',
    };
    await updateMutation.mutateAsync({ id, data: payload });
  }, [updateMutation]);

  const deleteItem = useCallback(async (id) => {
    await deleteMutation.mutateAsync(id);
  }, [deleteMutation]);

  return {
    data: pagedData?.items || [],
    totalItems: pagedData?.totalItems || 0,
    totalPages: pagedData?.totalPages || 0,
    getByProjectId,
    addItem,
    updateItem,
    deleteItem,
    isLoading,
    isError,
  };
}
