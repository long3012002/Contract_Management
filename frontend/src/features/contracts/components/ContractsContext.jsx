import { createContext, useContext, useState } from 'react';
import { useContracts } from '../hooks/useContracts';
import { toast } from 'sonner';

const ContractsContext = createContext(null);

export function ContractsProvider({ children, fixedProjectId }) {
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [projectFilter, setProjectFilter] = useState(fixedProjectId || 'all');
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 8;

  const contractsState = useContracts({
    search: searchTerm,
    status: statusFilter,
    project: projectFilter,
    page: currentPage,
    pageSize: itemsPerPage,
    fixedProjectId,
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
      contractsState.updateItem(editingItem.id, formData);
      toast.success('Đã cập nhật thông tin hợp đồng.');
    } else {
      contractsState.addItem(formData);
      toast.success('Đã tạo hợp đồng mới.');
    }
  };

  const handleDelete = (id) => {
    contractsState.deleteItem(id);
    toast.success('Đã xoá hợp đồng.');
  };

  const value = {
    ...contractsState,
    searchTerm,
    setSearchTerm,
    statusFilter,
    setStatusFilter,
    projectFilter,
    setProjectFilter,
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
    <ContractsContext.Provider value={value}>
      {children}
    </ContractsContext.Provider>
  );
}

export function useContractsContext() {
  const context = useContext(ContractsContext);
  if (!context) {
    throw new Error('useContractsContext must be used within a ContractsProvider');
  }
  return context;
}
