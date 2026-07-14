import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, FileText, CheckCircle2 } from 'lucide-react';
import Button from '@/components/Button/Button';
import { useContracts } from '../hooks/useContracts';
import ContractStatusBadge from './ContractStatusBadge';
import { cn } from '@/lib/utils';
import { useConfirm } from '@/components/providers/confirm-dialog-provider';
import { toast } from 'sonner';
import ContractParties from './contract-detail/ContractParties';
import ContractTerms from './contract-detail/ContractTerms';
import ContractValueCard from './contract-detail/ContractValueCard';
import ContractPaymentSchedule from './contract-detail/ContractPaymentSchedule';
import ContractAttachments from './contract-detail/ContractAttachments';

const formatVND = (value) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(value);

const formatDate = (dateStr) => {
  if (!dateStr) return '—';
  return new Date(dateStr).toLocaleDateString('vi-VN');
};

export default function ContractDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { getById, updateItem } = useContracts();
  const { confirm } = useConfirm();

  const contract = getById(id);

  if (!contract) {
    return (
      <div className="flex flex-col items-center justify-center h-full space-y-4">
        <p className="text-muted-foreground text-sm">Không tìm thấy hợp đồng.</p>
        <Button variant="outline" onClick={() => navigate('/contracts')} className="w-auto text-sm">
          Quay lại danh sách
        </Button>
      </div>
    );
  }

  const handleTerminate = async () => {
    const isConfirmed = await confirm({
      title: 'Thanh lý hợp đồng?',
      description: 'Bạn có chắc chắn muốn chuyển trạng thái hợp đồng này sang "Đã thanh lý"? Hành động này không thể hoàn tác tự động.',
      confirmText: 'Xác nhận',
      cancelText: 'Huỷ',
    });
    if (isConfirmed) {
      updateItem(contract.id, { status: 'Terminated' });
      toast.success('Hợp đồng đã được thanh lý.');
    }
  };

  const getExpiryStatus = () => {
    if (!contract.expiredDate || contract.status === 'Terminated') return null;
    const expDate = new Date(contract.expiredDate);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const diffDays = Math.ceil((expDate - today) / (1000 * 60 * 60 * 24));
    
    if (diffDays < 0) return { label: `Đã quá hạn ${Math.abs(diffDays)} ngày`, isWarning: true };
    if (diffDays <= 30) return { label: `Sắp hết hạn (còn ${diffDays} ngày)`, isWarning: true };
    return { label: `Còn ${diffDays} ngày`, isWarning: false };
  };

  const expiryStatus = getExpiryStatus();

  return (
    <div className="flex flex-col h-full space-y-4 animate-in fade-in slide-in-from-bottom-4 duration-500 text-foreground overflow-hidden">
      
      {/* Page Header */}
      <div className="space-y-2 shrink-0">
        <button
          onClick={() => navigate('/contracts')}
          className="flex items-center gap-2 text-sm font-medium text-muted-foreground hover:text-foreground transition-colors group w-fit cursor-pointer"
        >
          <ArrowLeft className="size-4 group-hover:-translate-x-0.5 transition-transform" />
          Quay lại danh sách hợp đồng
        </button>
 
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="space-y-1">
            <div className="flex flex-wrap items-center gap-3">
              <h1 className="text-2xl font-bold tracking-tight text-foreground flex items-center gap-2 text-balance">
                <FileText className="size-6 text-primary shrink-0" />
                {contract.name}
              </h1>
              <ContractStatusBadge status={contract.status} />
            </div>
            <p className="text-sm font-mono text-muted-foreground">{contract.code}</p>
          </div>
 
          <div className="flex items-center gap-3 shrink-0">
            {contract.status === 'Active' && (
              <Button onClick={handleTerminate} className="w-auto text-sm py-2 px-4 h-10" variant="outline">
                <CheckCircle2 className="size-4 mr-2 text-emerald-600" />
                Thanh lý hợp đồng
              </Button>
            )}
            <Button onClick={() => navigate(`/projects/${contract.projectId}`)} className="w-auto text-sm py-2 px-4 h-10">
              Xem Dự án
            </Button>
          </div>
        </div>
      </div>
 
      {/* 2-Column Layout */}
      <div className="flex-1 min-h-0 grid grid-cols-1 lg:grid-cols-3 gap-4 md:gap-5 overflow-hidden">
        
        {/* Left Column (2/3 width) - Legal & Parties Info */}
        <div className="lg:col-span-2 space-y-4 md:space-y-5 overflow-y-auto pr-1.5 pb-2">
          <ContractParties 
            contract={contract} 
            onNavigateToContractors={() => navigate('/contractors')} 
          />
          <ContractTerms contract={contract} />
        </div>
 
        {/* Right Column (1/3 width) - Sidebar Widgets & Payment Schedule (Visible in viewport) */}
        <div className="space-y-4 md:space-y-5 overflow-y-auto pr-1.5 pb-2">
          <ContractValueCard 
            contract={contract} 
            expiryStatus={expiryStatus} 
            formatVND={formatVND} 
            formatDate={formatDate} 
          />
          <ContractPaymentSchedule 
            paymentPlans={contract.paymentPlans} 
            formatVND={formatVND} 
            formatDate={formatDate} 
          />
          <ContractAttachments files={contract.files} />
        </div>
      </div>
    </div>
  );
}
