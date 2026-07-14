import { cn } from '@/lib/utils';

const statusConfig = {
  Draft: {
    label: 'Bản nháp',
    className: 'bg-zinc-100 text-zinc-600 border-zinc-200',
  },
  Available: {
    label: 'Sẵn sàng',
    className: 'bg-emerald-500/10 text-emerald-700 border-emerald-500/20',
  },
  Allocated: {
    label: 'Đã phân bổ',
    className: 'bg-sky-500/10 text-sky-700 border-sky-500/20',
  },
  Closed: {
    label: 'Đã đóng',
    className: 'bg-zinc-100 text-zinc-500 border-zinc-200',
  },
};

export default function SourceProjectStatusBadge({ status }) {
  const config = statusConfig[status] || statusConfig.Draft;

  return (
    <span
      className={cn(
        'inline-flex items-center px-2.5 py-0.5 text-xs font-medium rounded-full border whitespace-nowrap',
        config.className
      )}
    >
      {config.label}
    </span>
  );
}
