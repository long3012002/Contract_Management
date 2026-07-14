import { createContext, useContext, useState, useMemo, useEffect, useCallback } from 'react';
import { useUsers } from '../hooks/useUsers';
import { toast } from 'sonner';

const UsersContext = createContext(null);

export function UsersProvider({ children }) {
  const usersState = useUsers();
  
  // Custom states that were previously inside UsersPage or UserListTable
  const [isImportModalOpen, setIsImportModalOpen] = useState(false);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [editingUser, setEditingUser] = useState(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('all'); // all, active, locked
  const [roleFilter, setRoleFilter] = useState('all');
  
  // Selection state
  const [selectedIds, setSelectedIds] = useState([]);

  // Pagination state
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 8;

  const users = usersState.usersQuery.data || [];
  const loading = usersState.usersQuery.isLoading || usersState.deleteMutation.isPending || usersState.updateMutation?.isPending;
  const error = usersState.usersQuery.error;
  const { refetch: fetchUsers } = usersState.usersQuery;



  useEffect(() => {
    if (error) {
      toast.error(error.message || String(error));
    }
  }, [error]);

  // Extract all unique roles for the filter dropdown
  const allRoles = useMemo(() => {
    const rolesSet = new Set();
    users.forEach((user) => {
      user.roles?.forEach((role) => rolesSet.add(role));
    });
    return Array.from(rolesSet);
  }, [users]);

  // Filtered and searched users
  const filteredUsers = useMemo(() => {
    return users.filter((user) => {
      const matchesSearch = 
        user.fullName?.toLowerCase().includes(searchTerm.toLowerCase()) ||
        user.username?.toLowerCase().includes(searchTerm.toLowerCase()) ||
        user.email?.toLowerCase().includes(searchTerm.toLowerCase()) ||
        user.phone?.toLowerCase().includes(searchTerm.toLowerCase());

      const matchesStatus = 
        statusFilter === 'all' || 
        (statusFilter === 'active' && user.isActive) || 
        (statusFilter === 'locked' && !user.isActive);

      const matchesRole = 
        roleFilter === 'all' || 
        user.roles?.includes(roleFilter) ||
        (roleFilter === 'admin' && user.isSystemAdmin);

      return matchesSearch && matchesStatus && matchesRole;
    });
  }, [users, searchTerm, statusFilter, roleFilter]);

  // Paginated users
  const paginatedUsers = useMemo(() => {
    const startIndex = (currentPage - 1) * itemsPerPage;
    return filteredUsers.slice(startIndex, startIndex + itemsPerPage);
  }, [filteredUsers, currentPage]);

  const totalPages = Math.ceil(filteredUsers.length / itemsPerPage);

  const value = {
    ...usersState,
    users,
    loading,
    isImportModalOpen,
    setIsImportModalOpen,
    isEditModalOpen,
    setIsEditModalOpen,
    editingUser,
    setEditingUser,
    searchTerm,
    setSearchTerm,
    statusFilter,
    setStatusFilter,
    roleFilter,
    setRoleFilter,
    selectedIds,
    setSelectedIds,
    currentPage,
    setCurrentPage,
    itemsPerPage,
    allRoles,
    filteredUsers,
    paginatedUsers,
    totalPages,
  };

  return (
    <UsersContext.Provider value={value}>
      {children}
    </UsersContext.Provider>
  );
}

export function useUsersContext() {
  const context = useContext(UsersContext);
  if (!context) {
    throw new Error('useUsersContext must be used within a UsersProvider');
  }
  return context;
}
