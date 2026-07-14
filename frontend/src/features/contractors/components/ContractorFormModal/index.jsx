import { useState } from 'react';
import { X } from 'lucide-react';
import GeneralInfoFields from './GeneralInfoFields';
import ContactFields from './ContactFields';
import FormFooter from './FormFooter';
import { useContractorsContext } from '../ContractorsContext';

const INITIAL_FORM = {
  code: '',
  name: '',
  taxCode: '',
  representative: '',
  representativePosition: '',
  bankAccount: '',
  bankName: '',
  jointVenture: '',
  phone: '',
  email: '',
  address: '',
  status: 'Active',
};

export default function ContractorFormModal() {
  const { isModalOpen, setIsModalOpen, editingItem, handleSave } = useContractorsContext();
  const isEditing = !!editingItem;
  const [form, setForm] = useState(() =>
    isEditing
      ? {
          code: editingItem.code,
          name: editingItem.name,
          taxCode: editingItem.taxCode,
          representative: editingItem.representative,
          representativePosition: editingItem.representativePosition || '',
          bankAccount: editingItem.bankAccount || '',
          bankName: editingItem.bankName || '',
          jointVenture: editingItem.jointVenture || '',
          phone: editingItem.phone || '',
          email: editingItem.email || '',
          address: editingItem.address || '',
          status: editingItem.status,
        }
      : { ...INITIAL_FORM }
  );

  if (!isModalOpen) return null;

  const handleChange = (field, value) => {
    setForm((prev) => ({ ...prev, [field]: value }));
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    handleSave(form);
    setIsModalOpen(false);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/50" onClick={() => setIsModalOpen(false)} aria-hidden="true" />

      <div className="relative bg-card border border-border rounded-lg shadow-lg w-full max-w-2xl mx-4 max-h-[90dvh] overflow-y-auto">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-border">
          <h2 className="text-lg font-semibold text-foreground text-balance">
            {isEditing ? 'Chỉnh sửa nhà thầu' : 'Thêm nhà thầu mới'}
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
            isEditing={isEditing}
          />
          
          <ContactFields
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
