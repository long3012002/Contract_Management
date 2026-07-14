import { cn } from '@/lib/utils';

const statusConfig = {
  Active: { label: 'Đang hoạt động', className: 'bg-green-500/10 text-green-700 border-green-500/20' },
  Inactive: { label: 'Ngừng giao dịch', className: 'bg-zinc-100 text-zinc-600 border-zinc-200' },
};

export default function ContractorStatusBadge({ status }) {
  const config = statusConfig[status] || statusConfig.Active;

  return (
    <span className={cn('inline-flex items-center px-2.5 py-0.5 text-xs font-medium rounded-full border whitespace-nowrap', config.className)}>
      {config.label}
    </span>
  );
}
