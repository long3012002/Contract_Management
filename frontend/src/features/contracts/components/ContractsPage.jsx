import { FileText, Plus } from 'lucide-react';
import { ContractsProvider, useContractsContext } from './ContractsContext';
import ContractListTable from './ContractListTable';
import ContractFormModal from './ContractFormModal';
import ContractExpiryWarning from './ContractExpiryWarning';
import Button from '@/components/Button/Button';

function ContractsPageContent() {
  const {
    isModalOpen,
    setIsModalOpen,
    handleSave,
    editingItem,
    handleOpenCreate,
    getExpiringContracts,
    getExpiringPaymentPlans,
  } = useContractsContext();

  const expiringContracts = getExpiringContracts();
  const expiringPaymentPlans = getExpiringPaymentPlans();

  return (
    <div className="flex flex-col h-full space-y-6 pb-10">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground flex items-center gap-3 text-balance">
            <FileText className="size-6 text-primary" />
            <span>Hợp đồng</span>
            <ContractExpiryWarning 
              expiringContracts={expiringContracts} 
              expiringPaymentPlans={expiringPaymentPlans} 
            />
          </h1>
          <p className="text-muted-foreground mt-1 text-sm text-pretty">
            Quản lý hợp đồng với nhà thầu, đối tác.
          </p>
        </div>

        <Button
          onClick={handleOpenCreate}
          className="w-auto px-4 py-2 flex items-center gap-2 text-sm cursor-pointer"
        >
          <Plus className="size-4" />
          Tạo hợp đồng
        </Button>
      </div>

      <ContractListTable />

      {isModalOpen && (
        <ContractFormModal />
      )}
    </div>
  );
}

export default function ContractsPage() {
  return (
    <ContractsProvider>
      <ContractsPageContent />
    </ContractsProvider>
  );
}
