import { Building2, Plus } from 'lucide-react';
import { ContractorsProvider, useContractorsContext } from './ContractorsContext';
import ContractorListTable from './ContractorListTable';
import ContractorFormModal from './ContractorFormModal';
import Button from '@/components/Button/Button';

function ContractorsPageContent() {
  const { handleOpenCreate, isModalOpen } = useContractorsContext();

  return (
    <div className="flex flex-col h-full space-y-6 animate-in fade-in slide-in-from-bottom-4 duration-500 pb-10">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground flex items-center gap-2 text-balance">
            <Building2 className="size-6 text-primary" />
            Nhà thầu / Đối tác
          </h1>
          <p className="text-muted-foreground mt-1 text-sm text-pretty">
            Quản lý danh sách nhà thầu, nhà cung cấp và đối tác.
          </p>
        </div>

        <Button
          onClick={handleOpenCreate}
          className="w-auto px-4 py-2 flex items-center gap-2 text-sm cursor-pointer"
        >
          <Plus className="size-4" />
          Thêm nhà thầu
        </Button>
      </div>

      <ContractorListTable />

      {isModalOpen && (
        <ContractorFormModal />
      )}
    </div>
  );
}

export default function ContractorsPage() {
  return (
    <ContractorsProvider>
      <ContractorsPageContent />
    </ContractorsProvider>
  );
}
