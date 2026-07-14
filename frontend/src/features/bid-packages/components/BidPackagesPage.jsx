import { Package, Plus } from 'lucide-react';
import { BidPackagesProvider, useBidPackagesContext } from './BidPackagesContext';
import BidPackageListTable from './BidPackageListTable';
import BidPackageFormModal from './BidPackageFormModal';
import Button from '@/components/Button/Button';

function BidPackagesPageContent() {
  const { handleOpenCreate, isModalOpen } = useBidPackagesContext();

  return (
    <div className="flex flex-col h-full space-y-6 animate-in fade-in slide-in-from-bottom-4 duration-500 pb-10">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground flex items-center gap-2 text-balance">
            <Package className="size-6 text-primary" />
            Gói thầu
          </h1>
          <p className="text-muted-foreground mt-1 text-sm text-pretty">
            Quản lý các gói thầu thuộc dự án đầu tư.
          </p>
        </div>

        <Button
          onClick={handleOpenCreate}
          className="w-auto px-4 py-2 flex items-center gap-2 text-sm cursor-pointer"
        >
          <Plus className="size-4" />
          Thêm gói thầu
        </Button>
      </div>

      <BidPackageListTable />

      {isModalOpen && (
        <BidPackageFormModal />
      )}
    </div>
  );
}

export default function BidPackagesPage() {
  return (
    <BidPackagesProvider>
      <BidPackagesPageContent />
    </BidPackagesProvider>
  );
}
