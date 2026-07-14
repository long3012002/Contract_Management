import { createContext, useContext, useState } from 'react';
import { useBidPackages } from '../hooks/useBidPackages';
import { toast } from 'sonner';

const BidPackagesContext = createContext(null);

export function BidPackagesProvider({ children }) {
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [projectFilter, setProjectFilter] = useState('all');
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 8;

  const bidPackagesState = useBidPackages({
    search: searchTerm,
    status: statusFilter,
    project: projectFilter,
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
      bidPackagesState.updateItem(editingItem.id, formData);
      toast.success('Đã cập nhật gói thầu thành công.');
    } else {
      bidPackagesState.addItem(formData);
      toast.success('Đã thêm gói thầu mới.');
    }
  };

  const handleDelete = (id) => {
    bidPackagesState.deleteItem(id);
    toast.success('Đã xoá gói thầu.');
  };

  const value = {
    ...bidPackagesState,
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
    <BidPackagesContext.Provider value={value}>
      {children}
    </BidPackagesContext.Provider>
  );
}

export function useBidPackagesContext() {
  const context = useContext(BidPackagesContext);
  return context;
}
