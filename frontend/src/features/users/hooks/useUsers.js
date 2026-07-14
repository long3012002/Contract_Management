import { useState, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { usersApi } from '../api/usersApi';
import { MOCK_USERS } from '../constants/mockUsers';

export function useUsers() {
  const queryClient = useQueryClient();
  const [importResult, setImportResult] = useState(null);
  const [importError, setImportError] = useState(null);

  // Fetch Users Query
  const usersQuery = useQuery({
    queryKey: ['users'],
    queryFn: async () => {
      // Simulate API delay
      await new Promise(resolve => setTimeout(resolve, 600));
      return MOCK_USERS;
    },
  });

  // Delete Users Mutation
  const deleteMutation = useMutation({
    mutationFn: async (ids) => {
      return usersApi.bulkDelete(ids);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
  });

  // Import Users Mutation
  const importMutation = useMutation({
    mutationFn: async (file) => {
      return usersApi.importExcel(file);
    },
    onSuccess: (data) => {
      setImportResult(data);
      setImportError(null);
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
    onError: (err) => {
      if (err.response?.data) {
        setImportResult(err.response.data);
        setImportError(null);
      } else {
        setImportError(err.message || 'Lỗi khi nhập file Excel');
        setImportResult(null);
      }
    },
  });

  // Update User Mutation
  const updateMutation = useMutation({
    mutationFn: async (updatedUser) => {
      // Try calling the API if integrated, or simulate in-memory update for mock data
      try {
        // Since we are currently using MOCK_USERS in this query, we also mutate MOCK_USERS in-place
        const index = MOCK_USERS.findIndex((u) => u.id === updatedUser.id);
        if (index !== -1) {
          MOCK_USERS[index] = { ...MOCK_USERS[index], ...updatedUser };
        }
        
        // Also call usersApi.updateUser to match actual API behavior if server is running
        // await usersApi.updateUser(updatedUser.id, updatedUser);
      } catch (err) {
        console.warn('API call failed or not running, falling back to local memory update:', err);
      }
      return updatedUser;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
  });

  const deleteUsers = useCallback(async (ids) => {
    return deleteMutation.mutateAsync(ids);
  }, [deleteMutation]);

  const importUsers = useCallback(async (file) => {
    return importMutation.mutateAsync(file);
  }, [importMutation]);

  const resetImportState = useCallback(() => {
    setImportResult(null);
    setImportError(null);
    importMutation.reset();
  }, [importMutation]);

  return {
    usersQuery,
    deleteMutation,
    importMutation,
    updateMutation,
    importResult,
    importError,
    resetImportState,
  };
}

