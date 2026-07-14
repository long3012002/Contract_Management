import { FolderKanban, Plus } from 'lucide-react';
import { ImplProjectsProvider, useImplProjectsContext } from './ImplProjectsContext';
import ImplProjectListTable from './ImplProjectListTable';
import ImplProjectFormModal from './ImplProjectFormModal';
import Button from '@/components/Button/Button';

function ImplProjectsPageContent() {
  const { handleOpenCreate, isModalOpen, setIsModalOpen, handleSave, editingItem } = useImplProjectsContext();

  return (
    <div className="flex flex-col h-full space-y-6 animate-in fade-in slide-in-from-bottom-4 duration-500 pb-10">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground flex items-center gap-2 text-balance">
            <FolderKanban className="size-6 text-primary" />
            Dự án triển khai
          </h1>
          <p className="text-muted-foreground mt-1 text-sm text-pretty">
            Quản lý toàn bộ vòng đời dự án đầu tư CNTT.
          </p>
        </div>

        <Button
          onClick={handleOpenCreate}
          className="w-auto px-4 py-2 flex items-center gap-2 text-sm cursor-pointer"
        >
          <Plus className="size-4" />
          Tạo dự án
        </Button>
      </div>

      {/* Table */}
      <ImplProjectListTable />

      {/* Modal Form */}
      {isModalOpen && (
        <ImplProjectFormModal />
      )}
    </div>
  );
}

export default function ImplProjectsPage() {
  return (
    <ImplProjectsProvider>
      <ImplProjectsPageContent />
    </ImplProjectsProvider>
  );
}
