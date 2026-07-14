import { useState } from 'react';
import { X } from 'lucide-react';
import GeneralInfoFields from './GeneralInfoFields';
import TimelineFields from './TimelineFields';
import FormFooter from './FormFooter';
import SourceProjectPicker from '../SourceProjectPicker';
import { useImplProjectsContext } from '../ImplProjectsContext';

const INITIAL_FORM = {
  code: '',
  name: '',
  unit: '',
  investor: '',
  projectType: 'Phần mềm',
  startDate: '',
  endDate: '',
  description: '',
  sourceProjects: [],
};

export default function ImplProjectFormModal() {
  const { isModalOpen, setIsModalOpen, editingItem, handleSave } = useImplProjectsContext();
  const isEditing = !!editingItem;
  const [form, setForm] = useState(() =>
    isEditing
      ? {
          code: editingItem.code,
          name: editingItem.name,
          unit: editingItem.unit,
          investor: editingItem.investor,
          projectType: editingItem.projectType,
          startDate: editingItem.startDate,
          endDate: editingItem.endDate,
          description: editingItem.description || '',
          sourceProjects: editingItem.sourceProjects || [],
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
            {isEditing ? 'Chỉnh sửa dự án' : 'Tạo dự án mới'}
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
          />
          
          <TimelineFields
            form={form}
            handleChange={handleChange}
          />

          {/* Source Project Picker */}
          <div className="border-t border-border pt-4">
            <SourceProjectPicker
              selected={form.sourceProjects}
              onChange={(sp) => handleChange('sourceProjects', sp)}
            />
          </div>

          <FormFooter
            onClose={() => setIsModalOpen(false)}
            isEditing={isEditing}
          />
        </form>
      </div>
    </div>
  );
}
