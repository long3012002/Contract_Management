import { cn } from '@/lib/utils';

const statusConfig = {
  Draft: { label: 'Bản nháp', className: 'bg-zinc-100 text-zinc-600 border-zinc-200' },
  Active: { label: 'Đang hoạt động', className: 'bg-blue-500/10 text-blue-700 border-blue-500/20' },
  Completed: { label: 'Đã hoàn thành', className: 'bg-green-500/10 text-green-700 border-green-500/20' },
};

export default function BidPackageStatusBadge({ status }) {
  const config = statusConfig[status] || statusConfig.Draft;

  return (
    <span className={cn('inline-flex items-center px-2.5 py-0.5 text-xs font-medium rounded-full border whitespace-nowrap', config.className)}>
      {config.label}
    </span>
  );
}
