import { useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { contractsApi } from '../api/contractsApi';
import { useImplProjects } from '../../implementation-projects/hooks/useImplProjects';
import { useContractors } from '../../contractors/hooks/useContractors';
import { useBidPackages } from '../../bid-packages/hooks/useBidPackages';

export function useContracts({ search = '', page = 1, pageSize = 8, status = 'all', project = 'all', fixedProjectId = null } = {}) {
  const queryClient = useQueryClient();

  const { allData: projects } = useImplProjects();
  const { allData: contractors } = useContractors();
  const { getByProjectId: getBidPackagesByProjectId } = useBidPackages();

  // Queries
  const { data: pagedData, isLoading, isError } = useQuery({
    queryKey: ['contracts', { search, page, pageSize, status, project, fixedProjectId }],
    queryFn: async () => {
      const result = await contractsApi.getAll({
        search: search || undefined,
        page,
        pageSize,
      });

      const items = (result.items || []).map(item => {
        const nhaThau = contractors?.find(c => c.id === item.nhaThauId);
        return {
          ...item,
          projectId: item.duAnId || '',
          contractorId: item.nhaThauId || '',
          value: item.giaTriHopDong || 0,
          status: item.isActive ? 'Active' : 'Terminated',
          signedDate: item.ngayHieuLuc ? item.ngayHieuLuc.split('T')[0] : '',
          expiredDate: item.expiredDate ? item.expiredDate.split('T')[0] : '',
          projectName: projects?.find(p => p.id === item.duAnId)?.name || 'Dự án không xác định',
          contractorName: nhaThau?.name || 'Nhà thầu không xác định',
          contractor: nhaThau || null,
          paymentPlans: (item.dotThanhToans || []).map(d => ({
            id: d.id,
            name: d.tenDot,
            ratio: d.tyLeThanhToan,
            value: d.giaTriThanhToan,
          })),
          files: [],
        };
      });

      // Clientside filtering
      let filtered = items;
      if (fixedProjectId) {
        filtered = filtered.filter(item => item.projectId === fixedProjectId);
      } else if (project !== 'all') {
        filtered = filtered.filter(item => item.projectId === project);
      }
      if (status !== 'all') {
        filtered = filtered.filter(item => item.status === status);
      }

      return {
        items: filtered,
        totalItems: filtered.length,
        totalPages: Math.ceil(filtered.length / pageSize) || 1,
      };
    },
  });

  const { data: allItems } = useQuery({
    queryKey: ['contracts', 'all'],
    queryFn: async () => {
      const result = await contractsApi.getAll({
        page: 1,
        pageSize: 1000,
      });
      return (result.items || []).map(item => {
        const nhaThau = contractors?.find(c => c.id === item.nhaThauId);
        return {
          ...item,
          projectId: item.duAnId || '',
          contractorId: item.nhaThauId || '',
          value: item.giaTriHopDong || 0,
          status: item.isActive ? 'Active' : 'Terminated',
          signedDate: item.ngayHieuLuc ? item.ngayHieuLuc.split('T')[0] : '',
          expiredDate: item.expiredDate ? item.expiredDate.split('T')[0] : '',
          projectName: projects?.find(p => p.id === item.duAnId)?.name || 'Dự án không xác định',
          contractorName: nhaThau?.name || 'Nhà thầu không xác định',
          contractor: nhaThau || null,
          paymentPlans: (item.dotThanhToans || []).map(d => ({
            id: d.id,
            name: d.tenDot,
            ratio: d.tyLeThanhToan,
            value: d.giaTriThanhToan,
          })),
        };
      });
    },
  });

  // Warnings queries
  const { data: expiringSoonWarnings } = useQuery({
    queryKey: ['warnings', 'expiring-soon'],
    queryFn: contractsApi.getExpiringSoon,
    staleTime: 60000,
  });

  const { data: expiredWarnings } = useQuery({
    queryKey: ['warnings', 'expired'],
    queryFn: contractsApi.getExpired,
    staleTime: 60000,
  });

  const createMutation = useMutation({
    mutationFn: contractsApi.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contracts'] });
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }) => contractsApi.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contracts'] });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: contractsApi.delete,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contracts'] });
    },
  });

  const getById = useCallback((id) => {
    if (!allItems) return null;
    return allItems.find((item) => item.id === id) || null;
  }, [allItems]);

  const getByProjectId = useCallback((projectId) => {
    if (!allItems) return [];
    return allItems.filter((item) => item.projectId === projectId);
  }, [allItems]);

  const addItem = useCallback(async (newItem) => {
    const bidPackage = getBidPackagesByProjectId(newItem.projectId)?.find(bp => bp.id === newItem.bidPackageId);
    const duAnId = bidPackage ? bidPackage.projectId : newItem.projectId;

    const payload = {
      code: newItem.code,
      name: newItem.name,
      description: newItem.description || '',
      goiThauId: newItem.bidPackageId || null,
      duAnId: duAnId || null,
      nhaThauId: newItem.contractorId || null,
      chuDauTuId: null,
      loaiHopDong: 1,
      thoiHanThucHien: newItem.warrantyPeriod || '',
      diaDiemThucHien: newItem.paymentMethod || '',
      giaTriHopDong: newItem.value || 0,
      hinhThucThanhToan: 1,
      ngayHieuLuc: newItem.signedDate ? new Date(newItem.signedDate).toISOString() : null,
      expiredDate: newItem.expiredDate ? new Date(newItem.expiredDate).toISOString() : null,
      isRenewalRequired: true,
      isActive: true,
      dotThanhToans: (newItem.paymentPlans || []).map(p => ({
        tenDot: p.name,
        tyLeThanhToan: p.ratio || 0,
        giaTriThanhToan: p.value || 0,
      })),
    };
    await createMutation.mutateAsync(payload);
  }, [createMutation, getBidPackagesByProjectId]);

  const updateItem = useCallback(async (id, updates) => {
    const payload = {
      name: updates.name,
      description: updates.description || '',
      goiThauId: updates.bidPackageId || null,
      nhaThauId: updates.contractorId || null,
      giaTriHopDong: updates.value || 0,
      thoiHanThucHien: updates.warrantyPeriod || '',
      diaDiemThucHien: updates.paymentMethod || '',
      ngayHieuLuc: updates.signedDate ? new Date(updates.signedDate).toISOString() : null,
      expiredDate: updates.expiredDate ? new Date(updates.expiredDate).toISOString() : null,
      isActive: updates.status === 'Active',
      isRenewalRequired: true,
    };
    await updateMutation.mutateAsync({ id, data: payload });
  }, [updateMutation]);

  const deleteItem = useCallback(async (id) => {
    await deleteMutation.mutateAsync(id);
  }, [deleteMutation]);

  const getExpiringContracts = useCallback(() => {
    const allWarnings = [...(expiringSoonWarnings || []), ...(expiredWarnings || [])];
    return allWarnings.map(w => ({
      id: w.contractId,
      code: w.contractNumber,
      name: w.title,
      expiredDate: w.expiredDate ? w.expiredDate.split('T')[0] : '',
      diffDays: w.daysRemaining,
      isPast: w.daysRemaining < 0,
    }));
  }, [expiringSoonWarnings, expiredWarnings]);

  const getExpiringPaymentPlans = useCallback(() => {
    return [];
  }, []);

  return {
    data: pagedData?.items || [],
    allData: allItems || [],
    totalItems: pagedData?.totalItems || 0,
    totalPages: pagedData?.totalPages || 0,
    getById,
    getByProjectId,
    addItem,
    updateItem,
    deleteItem,
    getExpiringContracts,
    getExpiringPaymentPlans,
    isLoading,
    isError,
  };
}
