import { useState } from 'react';
import { X } from 'lucide-react';
import GeneralInfoFields from './GeneralInfoFields';
import FinancialFields from './FinancialFields';
import FormFooter from './FormFooter';
import { useSourceProjectsContext } from '../SourceProjectsContext';

const INITIAL_FORM = {
  code: '',
  name: '',
  decision: '',
  decisionDate: '',
  value: '',
  fundType: 'NSNN',
  year: new Date().getFullYear(),
  note: '',
};

export default function SourceProjectFormModal() {
  const { isModalOpen, setIsModalOpen, editingItem, handleSave } = useSourceProjectsContext();
  const isEditing = !!editingItem;
  const [form, setForm] = useState(() =>
    isEditing
      ? {
          code: editingItem.code,
          name: editingItem.name,
          decision: editingItem.decision,
          decisionDate: editingItem.decisionDate,
          value: editingItem.value,
          fundType: editingItem.fundType,
          year: editingItem.year,
          note: editingItem.note || '',
        }
      : { ...INITIAL_FORM }
  );

  if (!isModalOpen) return null;

  const handleChange = (field, value) => {
    setForm((prev) => ({ ...prev, [field]: value }));
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    handleSave({
      ...form,
      value: Number(form.value),
      year: Number(form.year),
    });
    setIsModalOpen(false);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-black/50"
        onClick={() => setIsModalOpen(false)}
        aria-hidden="true"
      />

      {/* Modal */}
      <div className="relative bg-card border border-border rounded-lg shadow-lg w-full max-w-lg mx-4 max-h-[90dvh] overflow-y-auto">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-border">
          <h2 className="text-lg font-semibold text-foreground text-balance">
            {isEditing ? 'Chỉnh sửa nguồn vốn' : 'Thêm nguồn vốn mới'}
          </h2>
          <button
            onClick={() => setIsModalOpen(false)}
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
          />
          
          <FinancialFields
            form={form}
            handleChange={handleChange}
          />

          <FormFooter
            onClose={() => setIsModalOpen(false)}
            isEditing={isEditing}
          />
        </form>
      </div>
    </div>
  );
}
