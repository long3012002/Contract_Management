import { useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { contractorsApi } from '../api/contractorsApi';

export function useContractors({ search = '', page = 1, pageSize = 8, status = 'all' } = {}) {
  const queryClient = useQueryClient();

  // Query for paginated data
  const { data: pagedData, isLoading, isError } = useQuery({
    queryKey: ['contractors', { search, page, pageSize, status }],
    queryFn: async () => {
      const result = await contractorsApi.getAll({
        search: search || undefined,
        page,
        pageSize,
      });

      // Map backend fields to frontend expected fields
      const items = (result.items || []).map(item => ({
        ...item,
        status: item.isActive ? 'Active' : 'Inactive',
        taxCode: item.taxCode || '',
        phone: item.phone || '',
        email: item.email || '',
        address: item.address || '',
      }));

      // Clientside filter by status
      const filtered = status === 'all' 
        ? items 
        : items.filter(item => item.status === status);

      return {
        items: filtered,
        totalItems: result.totalItems || 0,
        totalPages: result.totalPages || 0,
      };
    },
  });

  // Query for all data (used in picker, no paging)
  const { data: allItems } = useQuery({
    queryKey: ['contractors', 'all'],
    queryFn: async () => {
      const result = await contractorsApi.getAll({
        page: 1,
        pageSize: 1000,
      });
      return (result.items || []).map(item => ({
        ...item,
        status: item.isActive ? 'Active' : 'Inactive',
        taxCode: item.taxCode || '',
        phone: item.phone || '',
        email: item.email || '',
        address: item.address || '',
      }));
    },
  });

  const createMutation = useMutation({
    mutationFn: contractorsApi.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contractors'] });
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }) => contractorsApi.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contractors'] });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: contractorsApi.delete,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contractors'] });
    },
  });

  const getById = useCallback((id) => {
    if (!allItems) return null;
    return allItems.find((item) => item.id === id) || null;
  }, [allItems]);

  const addItem = useCallback(async (newItem) => {
    const payload = {
      code: newItem.code,
      name: newItem.name,
      taxCode: newItem.taxCode || '',
      phone: newItem.phone || '',
      email: newItem.email || '',
      address: newItem.address || '',
      description: newItem.description || '',
      isActive: newItem.status !== 'Inactive',
    };
    await createMutation.mutateAsync(payload);
  }, [createMutation]);

  const updateItem = useCallback(async (id, updates) => {
    const payload = {
      name: updates.name,
      taxCode: updates.taxCode || '',
      phone: updates.phone || '',
      email: updates.email || '',
      address: updates.address || '',
      description: updates.description || '',
      isActive: updates.status !== 'Inactive',
    };
    await updateMutation.mutateAsync({ id, data: payload });
  }, [updateMutation]);

  const deleteItem = useCallback(async (id) => {
    await deleteMutation.mutateAsync(id);
  }, [deleteMutation]);

  return {
    data: pagedData?.items || [],
    allData: allItems || [],
    totalItems: pagedData?.totalItems || 0,
    totalPages: pagedData?.totalPages || 0,
    isLoading,
    isError,
    getById,
    addItem,
    updateItem,
    deleteItem,
  };
}
