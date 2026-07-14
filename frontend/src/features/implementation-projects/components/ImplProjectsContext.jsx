import { createContext, useContext, useState } from 'react';
import { useImplProjects } from '../hooks/useImplProjects';
import { toast } from 'sonner';

const ImplProjectsContext = createContext(null);

export function ImplProjectsProvider({ children }) {
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [typeFilter, setTypeFilter] = useState('all');
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 8;

  const implProjectsState = useImplProjects({
    search: searchTerm,
    status: statusFilter,
    type: typeFilter,
    page: currentPage,
    pageSize: itemsPerPage,
  });

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingItem, setEditingItem] = useState(null);

  const handleOpenCreate = () => {
    setEditingItem(null);
    setIsModalOpen(true);
  };

  const handleOpenEdit = (item) => {
    setEditingItem(item);
    setIsModalOpen(true);
  };

  const handleSave = (formData) => {
    if (editingItem) {
      implProjectsState.updateItem(editingItem.id, formData);
      toast.success('Đã cập nhật dự án thành công.');
    } else {
      implProjectsState.addItem(formData);
      toast.success('Đã tạo dự án mới.');
    }
  };

  const handleDelete = (id) => {
    implProjectsState.deleteItem(id);
    toast.success('Đã xoá dự án.');
  };

  const value = {
    ...implProjectsState,
    searchTerm,
    setSearchTerm,
    statusFilter,
    setStatusFilter,
    typeFilter,
    setTypeFilter,
    currentPage,
    setCurrentPage,
    isModalOpen,
    setIsModalOpen,
    editingItem,
    handleOpenCreate,
    handleOpenEdit,
    handleSave,
    handleDelete,
  };

  return (
    <ImplProjectsContext.Provider value={value}>
      {children}
    </ImplProjectsContext.Provider>
  );
}

export function useImplProjectsContext() {
  const context = useContext(ImplProjectsContext);
  if (!context) {
    throw new Error('useImplProjectsContext must be used within an ImplProjectsProvider');
  }
  return context;
}
