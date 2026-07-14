import { useState, useCallback } from 'react';
import { useQuery } from '@tanstack/react-query';
import { getAuditLogs } from '../api/auditLogsApi';

export const useAuditLogs = () => {
  const [filters, setFilters] = useState({
    username: '',
    date: '',
    tableName: '',
    page: 1,
    pageSize: 20,
  });

  const queryParams = {
    username: filters.username || undefined,
    date: filters.date || undefined,
    tableName: filters.tableName || undefined,
    page: filters.page,
    pageSize: filters.pageSize,
  };

  const { data, isPending, isError, error, refetch } = useQuery({
    queryKey: ['audit-logs', queryParams],
    queryFn: () => getAuditLogs(queryParams),
    staleTime: 30000,
  });

  const applyFilters = useCallback((newFilters) => {
    setFilters((prev) => ({
      ...prev,
      ...newFilters,
      page: 1, // Reset to page 1 on filter change
    }));
  }, []);

  const clearFilters = useCallback(() => {
    setFilters((prev) => ({
      username: '',
      date: '',
      tableName: '',
      page: 1,
      pageSize: prev.pageSize,
    }));
  }, []);

  const changePage = useCallback((newPage) => {
    setFilters((prev) => ({ ...prev, page: newPage }));
  }, []);

  const changePageSize = useCallback((newPageSize) => {
    setFilters((prev) => ({ ...prev, pageSize: newPageSize, page: 1 }));
  }, []);

  return {
    data,
    isPending,
    isError,
    error,
    filters,
    applyFilters,
    clearFilters,
    changePage,
    changePageSize,
    refetch,
  };
};
