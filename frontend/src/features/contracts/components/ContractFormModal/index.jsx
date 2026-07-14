import { useState } from 'react';
import { X } from 'lucide-react';
import GeneralInfoFields from './GeneralInfoFields';
import FinancialFields from './FinancialFields';
import FormFooter from './FormFooter';
import { useContractsContext } from '../ContractsContext';

const INITIAL_FORM = {
  code: '',
  name: '',
  projectId: '',
  contractorId: '',
  signedDate: '',
  expiredDate: '',
  value: '',
  status: 'Active',
  description: '',
  paymentPlans: [],
};

export default function ContractFormModal({ fixedProjectId = null }) {
  const { isModalOpen, setIsModalOpen, editingItem, handleSave } = useContractsContext();
  const isEditing = !!editingItem;
  const [form, setForm] = useState(() =>
    isEditing
      ? {
          code: editingItem.code,
          name: editingItem.name,
          projectId: editingItem.projectId,
          contractorId: editingItem.contractorId,
          signedDate: editingItem.signedDate || '',
          expiredDate: editingItem.expiredDate || '',
          value: editingItem.value,
          status: editingItem.status,
          description: editingItem.description || '',
          paymentPlans: editingItem.paymentPlans || [],
        }
      : { ...INITIAL_FORM, projectId: fixedProjectId || '' }
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
    });
    setIsModalOpen(false);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/50" onClick={() => setIsModalOpen(false)} aria-hidden="true" />

      <div className="relative bg-card border border-border rounded-lg shadow-lg w-full max-w-2xl mx-4 max-h-[90dvh] overflow-y-auto">
        <div className="flex items-center justify-between px-6 py-4 border-b border-border sticky top-0 bg-card z-10">
          <h2 className="text-lg font-semibold text-foreground text-balance">
            {isEditing ? 'Chỉnh sửa hợp đồng' : 'Tạo hợp đồng mới'}
          </h2>
          <button
            onClick={() => setIsModalOpen(false)}
            className="p-1 rounded-md hover:bg-muted text-muted-foreground hover:text-foreground transition-colors cursor-pointer"
            aria-label="Đóng"
          >
            <X className="size-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="px-6 py-5 space-y-4">
          <GeneralInfoFields
            form={form}
            handleChange={handleChange}
            isEditing={isEditing}
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
