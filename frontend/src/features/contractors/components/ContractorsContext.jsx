import { createContext, useContext, useState } from 'react';
import { useContractors } from '../hooks/useContractors';
import { toast } from 'sonner';

const ContractorsContext = createContext(null);

export function ContractorsProvider({ children }) {
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 8;

  const contractorsState = useContractors({
    search: searchTerm,
    status: statusFilter,
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
      contractorsState.updateItem(editingItem.id, formData);
      toast.success('Đã cập nhật thông tin nhà thầu.');
    } else {
      contractorsState.addItem(formData);
      toast.success('Đã thêm nhà thầu mới.');
    }
  };

  const handleDelete = (id) => {
    contractorsState.deleteItem(id);
    toast.success('Đã xoá nhà thầu.');
  };

  const value = {
    ...contractorsState,
    searchTerm,
    setSearchTerm,
    statusFilter,
    setStatusFilter,
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
    <ContractorsContext.Provider value={value}>
      {children}
    </ContractorsContext.Provider>
  );
}

export function useContractorsContext() {
  const context = useContext(ContractorsContext);
  if (!context) {
    throw new Error('useContractorsContext must be used within a ContractorsProvider');
  }
  return context;
}
