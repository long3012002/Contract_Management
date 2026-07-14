import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, CheckCircle2, ChevronRight, Settings2 } from 'lucide-react';
import Button from '@/components/Button/Button';
import { useImplProjects } from '../hooks/useImplProjects';
import ImplProjectStatusBadge from './ImplProjectStatusBadge';
import ImplProjectAuditTrail from './ImplProjectAuditTrail';
import ImplProjectGeneralInfo from './ImplProjectGeneralInfo';
import { NEXT_STATUS } from '../constants/mockImplProjects';
import { cn } from '@/lib/utils';
import { useConfirm } from '@/components/providers/confirm-dialog-provider';
import { toast } from 'sonner';
import { formatVND } from '@/utils/formatters';

import BidPackageByProject from '@/features/bid-packages/components/BidPackageByProject';
import ContractByProject from '@/features/contracts/components/ContractByProject';
import ProgressTab from '@/features/progress/components/ProgressTab';
import FinanceTab from '@/features/finance/components/FinanceTab';

const TABS = [
  { id: 'info', label: 'Thông tin chung' },
  { id: 'bidding', label: 'Gói thầu' },
  { id: 'contract', label: 'Hợp đồng' },
  { id: 'progress', label: 'Tiến độ' },
  { id: 'finance', label: 'Nghiệm thu & Giải ngân' },
  { id: 'audit', label: 'Nhật ký thay đổi' },
];

export default function ImplProjectDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { getById, advanceStatus } = useImplProjects();
  const { confirm } = useConfirm();
  const [activeTab, setActiveTab] = useState('info');

  const project = getById(id);

  if (!project) {
    return (
      <div className="flex flex-col items-center justify-center h-full space-y-4">
        <p className="text-muted-foreground">Không tìm thấy dự án.</p>
        <Button variant="outline" onClick={() => navigate('/projects')} className="w-auto">
          Quay lại danh sách
        </Button>
      </div>
    );
  }

  const nextStatus = NEXT_STATUS[project.status];

  const handleAdvanceStatus = async () => {
    if (!nextStatus) return;
    const isConfirmed = await confirm({
      title: 'Chuyển trạng thái dự án?',
      description: (
        <span>
          Bạn có muốn chuyển dự án sang trạng thái tiếp theo là{' '}
          <strong className="text-primary">{nextStatus}</strong>?
        </span>
      ),
      confirmText: 'Đồng ý',
      cancelText: 'Huỷ',
    });
    if (isConfirmed) {
      advanceStatus(project.id);
      toast.success('Đã cập nhật trạng thái dự án.');
    }
  };

  const handleCloseProject = async () => {
    // Tạm thời hiển thị dialog confirm
    await confirm({
      title: 'Đóng dự án?',
      description: 'Dự án đã quyết toán có thể được đóng lại.',
      confirmText: 'Đóng dự án',
      cancelText: 'Huỷ',
      variant: 'destructive',
    });
  };

  return (
    <div className="flex flex-col h-full space-y-6 animate-in fade-in slide-in-from-bottom-4 duration-500 pb-10">
      {/* Header & Breadcrumb */}
      <div className="space-y-4">
        <button
          onClick={() => navigate('/projects')}
          className="flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors group w-fit"
        >
          <ArrowLeft className="size-4 group-hover:-translate-x-1 transition-transform" />
          Quay lại danh sách
        </button>

        <div className="flex flex-col md:flex-row md:items-start justify-between gap-4">
          <div className="space-y-1">
            <div className="flex items-center gap-3">
              <h1 className="text-2xl font-bold text-foreground text-balance">
                {project.name}
              </h1>
              <ImplProjectStatusBadge status={project.status} />
            </div>
            <div className="flex items-center gap-4 text-sm text-muted-foreground font-mono">
              <span>{project.code}</span>
              <span>•</span>
              <span>{project.projectType}</span>
            </div>
          </div>

          <div className="flex items-center gap-3 shrink-0">
            {nextStatus && (
              <Button onClick={handleAdvanceStatus} className="w-auto flex items-center gap-2 shadow-sm">
                Chuyển sang {nextStatus}
                <ChevronRight className="size-4" />
              </Button>
            )}
            {project.status === 'Settlement' && (
              <Button variant="destructive" onClick={handleCloseProject} className="w-auto shadow-sm">
                Đóng dự án
              </Button>
            )}
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div className="border-b border-border">
        <nav className="flex space-x-1 overflow-x-auto pb-px scrollbar-hide" aria-label="Tabs">
          {TABS.map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={cn(
                'whitespace-nowrap py-3 px-4 text-sm font-medium border-b-2 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-1 rounded-t-md',
                activeTab === tab.id
                  ? 'border-primary text-primary bg-primary/5'
                  : 'border-transparent text-muted-foreground hover:text-foreground hover:border-muted hover:bg-muted/30'
              )}
            >
              {tab.label}
            </button>
          ))}
        </nav>
      </div>

      {/* Tab Content */}
      <div className="flex-1 min-h-[400px]">
        {/* TAB: Thông tin chung */}
        {activeTab === 'info' && (
          <ImplProjectGeneralInfo project={project} />
        )}


        {/* TAB: Gói thầu */}
        {activeTab === 'bidding' && (
          <div className="bg-card border border-border rounded-custom p-5 shadow-sm">
            <BidPackageByProject projectId={project.id} />
          </div>
        )}

        {/* TAB: Hợp đồng */}
        {activeTab === 'contract' && (
          <ContractByProject projectId={project.id} />
        )}

        {/* TAB: Tiến độ */}
        {activeTab === 'progress' && (
          <ProgressTab projectId={project.id} />
        )}
        
        {/* TAB: Nghiệm thu & Giải ngân */}
        {activeTab === 'finance' && (
          <FinanceTab projectId={project.id} />
        )}

        {/* TAB: Nhật ký thay đổi */}
        {activeTab === 'audit' && (
          <div className="bg-card border border-border rounded-custom shadow-sm overflow-hidden">
            <ImplProjectAuditTrail auditTrail={project.auditTrail} />
          </div>
        )}
      </div>
    </div>
  );
}
