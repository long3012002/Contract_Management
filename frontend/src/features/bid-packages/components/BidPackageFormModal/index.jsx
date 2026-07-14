import { useState } from 'react';
import { X } from 'lucide-react';
import GeneralInfoFields from './GeneralInfoFields';
import BudgetFields from './BudgetFields';
import FormFooter from './FormFooter';
import { useBidPackagesContext } from '../BidPackagesContext';

const INITIAL_FORM = {
  code: '',
  name: '',
  projectId: '',
  estimatedValue: '',
  warningThresholdPercent: 90,
  description: '',
};

export default function BidPackageFormModal({
  isOpen,
  onClose,
  onSave,
  editingItem = null,
  fixedProjectId = null,
}) {
  const context = useBidPackagesContext();

  const isModalOpen = isOpen ?? context?.isModalOpen ?? false;
  const handleCloseModal = onClose ?? (() => context?.setIsModalOpen(false));
  const handleSaveItem = onSave ?? context?.handleSave;
  const currentEditingItem = editingItem ?? context?.editingItem ?? null;

  const isEditing = !!currentEditingItem;
  const [form, setForm] = useState(() =>
    isEditing
      ? {
          code: currentEditingItem.code,
          name: currentEditingItem.name,
          projectId: currentEditingItem.projectId,
          estimatedValue: currentEditingItem.estimatedValue,
          warningThresholdPercent: currentEditingItem.warningThresholdPercent,
          description: currentEditingItem.description || '',
        }
      : { ...INITIAL_FORM, projectId: fixedProjectId || '' }
  );

  if (!isModalOpen) return null;

  const handleChange = (field, value) => {
    setForm((prev) => ({ ...prev, [field]: value }));
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    handleSaveItem({
      ...form,
      estimatedValue: Number(form.estimatedValue),
      warningThresholdPercent: Number(form.warningThresholdPercent),
    });
    handleCloseModal();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/50" onClick={handleCloseModal} aria-hidden="true" />

      <div className="relative bg-card border border-border rounded-lg shadow-lg w-full max-w-lg mx-4 max-h-[90dvh] overflow-y-auto">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-border">
          <h2 className="text-lg font-semibold text-foreground text-balance">
            {isEditing ? 'Chỉnh sửa gói thầu' : 'Thêm gói thầu mới'}
          </h2>
          <button
            onClick={handleCloseModal}
            className="p-1 rounded-md hover:bg-muted text-muted-foreground hover:text-foreground transition-colors cursor-pointer"
            aria-label="Đóng"
          >
            <X className="size-5" />
          </button>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} className="px-6 py-5 space-y-4">
          <GeneralInfoFields
            form={form}
            handleChange={handleChange}
            fixedProjectId={fixedProjectId}
          />
          
          <BudgetFields
            form={form}
            handleChange={handleChange}
          />

          <FormFooter
            onClose={handleCloseModal}
            isEditing={isEditing}
          />
        </form>
      </div>
    </div>
  );
}
