import { Landmark, Plus } from 'lucide-react';
import { SourceProjectsProvider, useSourceProjectsContext } from './SourceProjectsContext';
import SourceProjectListTable from './SourceProjectListTable';
import SourceProjectFormModal from './SourceProjectFormModal';
import MergeSelectionBar from './MergeSelectionBar';
import MergeToImplProjectModal from './MergeToImplProjectModal';
import Button from '@/components/Button/Button';

function SourceProjectsPageContent() {
  const { handleOpenCreate, isModalOpen, setIsModalOpen, handleSave, editingItem } = useSourceProjectsContext();

  return (
    <div className="flex flex-col h-full space-y-6 animate-in fade-in slide-in-from-bottom-4 duration-500 pb-10">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground flex items-center gap-2 text-balance">
            <Landmark className="size-6 text-primary" />
            Dự án nguồn
          </h1>
          <p className="text-muted-foreground mt-1 text-sm text-pretty">
            Quản lý các nguồn vốn đầu tư cho dự án triển khai.
          </p>
        </div>

        <Button
          onClick={handleOpenCreate}
          className="w-auto px-4 py-2 flex items-center gap-2 text-sm cursor-pointer"
        >
          <Plus className="size-4" />
          Thêm nguồn vốn
        </Button>
      </div>

      {/* Content */}
      <SourceProjectListTable />

      {/* Form Modal */}
      {isModalOpen && (
        <SourceProjectFormModal
          isOpen={isModalOpen}
          onClose={() => setIsModalOpen(false)}
          onSave={handleSave}
          editingItem={editingItem}
        />
      )}

      {/* Merge UI */}
      <MergeSelectionBar />
      <MergeToImplProjectModal />
    </div>
  );
}

export default function SourceProjectsPage() {
  return (
    <SourceProjectsProvider>
      <SourceProjectsPageContent />
    </SourceProjectsProvider>
  );
}
