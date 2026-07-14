import { createContext, useContext, useState } from 'react';
import { useSourceProjects } from '../hooks/useSourceProjects';
import { toast } from 'sonner';

const SourceProjectsContext = createContext(null);

export function SourceProjectsProvider({ children }) {
  const { query, filter } = useSourceProjects();
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingItem, setEditingItem] = useState(null);

  // Selection & Merge state
  const [selectedIds, setSelectedIds] = useState([]);
  const [isMergeModalOpen, setIsMergeModalOpen] = useState(false);

  const handleOpenCreate = () => {
    setEditingItem(null);
    setIsModalOpen(true);
  };

  const handleOpenEdit = (item) => {
    setEditingItem(item);
    setIsModalOpen(true);
  };

  const handleSave = async (formData) => {
    const promise = editingItem
      ? query.updateItem(editingItem.id, formData)
      : query.addItem(formData);

    toast.promise(promise, {
      loading: editingItem ? 'Đang cập nhật...' : 'Đang thêm nguồn vốn...',
      success: editingItem ? 'Đã cập nhật nguồn vốn thành công.' : 'Đã thêm nguồn vốn mới.',
      error: (err) => err.message || 'Thao tác thất bại.',
    });
  };

  const handleDelete = async (id) => {
    toast.promise(query.deleteItem(id), {
      loading: 'Đang xoá nguồn vốn...',
      success: 'Đã xoá nguồn vốn thành công.',
      error: (err) => err.message || 'Xoá nguồn vốn thất bại.',
    });
  };

  const handleActivate = async (id) => {
    toast.promise(query.activateItem(id), {
      loading: 'Đang kích hoạt nguồn vốn...',
      success: 'Đã kích hoạt nguồn vốn thành công.',
      error: (err) => err.message || 'Kích hoạt nguồn vốn thất bại.',
    });
  };

  const toggleSelect = (id) => {
    setSelectedIds((prev) =>
      prev.includes(id) ? prev.filter((item) => item !== id) : [...prev, id]
    );
  };

  const toggleSelectAll = (visibleIds) => {
    const allSelected = visibleIds.every((id) => selectedIds.includes(id));
    if (allSelected) {
      setSelectedIds((prev) => prev.filter((id) => !visibleIds.includes(id)));
    } else {
      const toAdd = visibleIds.filter((id) => !selectedIds.includes(id));
      setSelectedIds((prev) => [...prev, ...toAdd]);
    }
  };

  const clearSelection = () => setSelectedIds([]);

  const handleOpenMerge = () => setIsMergeModalOpen(true);

  const value = {
    // Data & mutations (from query hook)
    ...query,
    // Filter & pagination (from filter hook)
    ...filter,
    // Modal state
    isModalOpen,
    setIsModalOpen,
    editingItem,
    handleOpenCreate,
    handleOpenEdit,
    handleSave,
    handleDelete,
    handleActivate,
    // Selection state
    selectedIds,
    toggleSelect,
    toggleSelectAll,
    clearSelection,
    isMergeModalOpen,
    setIsMergeModalOpen,
    handleOpenMerge,
  };

  return (
    <SourceProjectsContext.Provider value={value}>
      {children}
    </SourceProjectsContext.Provider>
  );
}

export function useSourceProjectsContext() {
  const context = useContext(SourceProjectsContext);
  if (!context) {
    throw new Error('useSourceProjectsContext must be used within a SourceProjectsProvider');
  }
  return context;
}
