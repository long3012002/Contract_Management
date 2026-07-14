import { useState } from 'react';
import { Plus } from 'lucide-react';
import { useBidPackages } from '../hooks/useBidPackages';
import BidPackageListTable from './BidPackageListTable';
import BidPackageFormModal from './BidPackageFormModal';
import Button from '@/components/Button/Button';
import { toast } from 'sonner';

export default function BidPackageByProject({ projectId }) {
  const { getByProjectId, addItem, updateItem, deleteItem } = useBidPackages();
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingItem, setEditingItem] = useState(null);

  const data = getByProjectId(projectId);

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
      updateItem(editingItem.id, formData);
      toast.success('Đã cập nhật gói thầu.');
    } else {
      addItem({ ...formData, projectId });
      toast.success('Đã thêm gói thầu mới vào dự án.');
    }
  };

  const handleDelete = (id) => {
    deleteItem(id);
    toast.success('Đã xoá gói thầu.');
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-base font-semibold text-foreground">Danh sách gói thầu</h3>
        <Button onClick={handleOpenCreate} className="w-auto px-3 py-1.5 text-xs flex items-center gap-1.5 shadow-sm">
          <Plus className="size-3.5" />
          Thêm gói thầu
        </Button>
      </div>

      <BidPackageListTable
        data={data}
        totalItems={data.length}
        totalPages={1}
        currentPage={1}
        onPageChange={() => {}}
        onEdit={handleOpenEdit}
        onDelete={handleDelete}
        hideFilters={true}
      />

      {isModalOpen && (
        <BidPackageFormModal
          isOpen={isModalOpen}
          onClose={() => setIsModalOpen(false)}
          onSave={handleSave}
          editingItem={editingItem}
          fixedProjectId={projectId}
        />
      )}
    </div>
  );
}
