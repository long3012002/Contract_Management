import { cn } from '@/lib/utils';

const statusConfig = {
  Draft: { label: 'Dự thảo', className: 'bg-zinc-100 text-zinc-600 border-zinc-200' },
  Active: { label: 'Đang thực hiện', className: 'bg-emerald-500/10 text-emerald-700 border-emerald-500/20' },
  Expired: { label: 'Hết hạn', className: 'bg-red-500/10 text-red-700 border-red-500/20' },
  Terminated: { label: 'Đã thanh lý', className: 'bg-zinc-200/50 text-zinc-700 border-zinc-300' },
};

export default function ContractStatusBadge({ status }) {
  const config = statusConfig[status] || statusConfig.Draft;

  return (
    <span className={cn('inline-flex items-center px-2.5 py-0.5 text-xs font-medium rounded-full border whitespace-nowrap', config.className)}>
      {config.label}
    </span>
  );
}
