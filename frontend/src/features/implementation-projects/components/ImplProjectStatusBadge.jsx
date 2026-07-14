import { cn } from '@/lib/utils';

const statusConfig = {
  Draft:        { label: 'Bản nháp',       className: 'bg-zinc-100 text-zinc-600 border-zinc-200' },
  Submitted:    { label: 'Đã trình',       className: 'bg-amber-500/10 text-amber-700 border-amber-500/20' },
  Approved:     { label: 'Đã duyệt',       className: 'bg-sky-500/10 text-sky-700 border-sky-500/20' },
  Implementing: { label: 'Đang triển khai', className: 'bg-blue-500/10 text-blue-700 border-blue-500/20' },
  Acceptance:   { label: 'Nghiệm thu',     className: 'bg-violet-500/10 text-violet-700 border-violet-500/20' },
  Payment:      { label: 'Thanh toán',      className: 'bg-orange-500/10 text-orange-700 border-orange-500/20' },
  Settlement:   { label: 'Quyết toán',      className: 'bg-emerald-500/10 text-emerald-700 border-emerald-500/20' },
  Completed:    { label: 'Hoàn thành',      className: 'bg-green-500/10 text-green-700 border-green-500/20' },
};

export default function ImplProjectStatusBadge({ status }) {
  const config = statusConfig[status] || statusConfig.Draft;
  return (
    <span className={cn('inline-flex items-center px-2.5 py-0.5 text-xs font-medium rounded-full border whitespace-nowrap', config.className)}>
      {config.label}
    </span>
  );
}

export { statusConfig };
