import React from 'react';
import FilterBar from '@/components/ui/FilterBar';
import { useUsersContext } from './UsersContext';

export default function UserSearchFilters() {
  const {
    searchTerm,
    setSearchTerm,
    statusFilter,
    setStatusFilter,
    roleFilter,
    setRoleFilter,
    allRoles,
    setCurrentPage,
  } = useUsersContext();

  const handleSearchChange = (val) => {
    setSearchTerm(val);
    setCurrentPage(1);
  };

  const statusOptions = [
    { value: 'active', label: 'Hoạt động' },
    { value: 'locked', label: 'Bị khóa' },
  ];

  const roleOptions = [
    { value: 'admin', label: 'Quản trị hệ thống (Admin)' },
    ...allRoles.map((role) => ({ value: role, label: role })),
  ];

  const filtersConfig = [
    {
      id: 'status-filter',
      value: statusFilter,
      onChange: (val) => {
        setStatusFilter(val);
        setCurrentPage(1);
      },
      options: statusOptions,
      placeholder: 'Tất cả trạng thái',
      widthClass: 'sm:w-36',
    },
    {
      id: 'role-filter',
      value: roleFilter,
      onChange: (val) => {
        setRoleFilter(val);
        setCurrentPage(1);
      },
      options: roleOptions,
      placeholder: 'Tất cả vai trò',
      widthClass: 'sm:w-44',
    },
  ];

  return (
    <FilterBar
      searchTerm={searchTerm}
      onSearchChange={handleSearchChange}
      searchPlaceholder="Tìm theo tên, email, SĐT..."
      filters={filtersConfig}
    />
  );
}

