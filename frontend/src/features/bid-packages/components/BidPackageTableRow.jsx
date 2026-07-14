import { Pencil, Trash2, AlertTriangle } from 'lucide-react';
import { useConfirm } from '@/components/providers/confirm-dialog-provider';
import BidPackageStatusBadge from './BidPackageStatusBadge';
import { cn } from '@/lib/utils';
import { formatVND } from '@/utils/formatters';
import { useBidPackagesContext } from './BidPackagesContext';

export default function BidPackageTableRow({
  item,
  onEdit,
  onDelete,
  hideFilters = false,
}) {
  const { confirm } = useConfirm();
  const context = useBidPackagesContext();

  const handleEdit = onEdit ?? context?.handleOpenEdit;
  const handleDeleteItem = onDelete ?? context?.handleDelete;

  const onDeleteClick = async () => {
    const isConfirmed = await confirm({
      title: 'Xoá gói thầu?',
      description: (
        <span>
          Bạn có chắc chắn muốn xoá gói thầu{' '}
          <strong className="text-foreground font-semibold">{item.name}</strong>?
        </span>
      ),
      confirmText: 'Xoá',
      cancelText: 'Huỷ',
      variant: 'destructive',
    });
    if (isConfirmed) {
      handleDeleteItem(item.id);
    }
  };

  const percent = item.estimatedValue > 0 
    ? (item.totalContractValue / item.estimatedValue) * 100 
    : 0;
  
  const isWarning = percent >= item.warningThresholdPercent;
  const progressColor = isWarning ? 'bg-destructive' : 'bg-primary';

  return (
    <tr className="hover:bg-muted/20 transition-colors group">
      <td className="py-3 px-4">
        <span className="font-mono text-xs font-medium text-muted-foreground">
          {item.code}
        </span>
      </td>
      <td className="py-3 px-4">
        <p className="font-medium truncate max-w-[200px]" title={item.name}>{item.name}</p>
      </td>
      {!hideFilters && (
        <td className="py-3 px-4">
          <span className="text-xs truncate max-w-[150px] inline-block text-muted-foreground" title={item.projectName}>
            {item.projectName}
          </span>
        </td>
      )}
      <td className="py-3 px-4 text-right">
        <span className="font-semibold tabular-nums text-sm text-muted-foreground">
          {formatVND(item.estimatedValue)}
        </span>
      </td>
      <td className="py-3 px-4 text-right">
        <span className="font-semibold tabular-nums text-sm">
          {formatVND(item.totalContractValue)}
        </span>
      </td>
      <td className="py-3 px-4">
        <div className="flex items-center gap-2">
          <div className="flex-1 h-2 bg-secondary rounded-full overflow-hidden">
            <div
              className={cn('h-full rounded-full transition-all', progressColor)}
              style={{ width: `${Math.min(percent, 100)}%` }}
            />
          </div>
          <span className="text-xs font-medium tabular-nums w-12 text-right shrink-0">
            {percent.toFixed(1)}%
          </span>
          {isWarning && (
            <AlertTriangle className="size-4 text-destructive shrink-0" title={`Vượt ngưỡng cảnh báo ${item.warningThresholdPercent}%`} />
          )}
        </div>
      </td>
      <td className="py-3 px-4 text-center">
        <BidPackageStatusBadge status={item.status} />
      </td>
      <td className="py-3 px-4 text-center">
        <div className="flex items-center justify-center gap-1">
          <button
            onClick={() => handleEdit(item)}
            className="p-1.5 rounded-md hover:bg-primary/10 text-muted-foreground hover:text-primary transition-colors cursor-pointer"
            aria-label={`Sửa ${item.name}`}
            title="Chỉnh sửa"
          >
            <Pencil className="size-4" />
          </button>
          <button
            onClick={onDeleteClick}
            className="p-1.5 rounded-md hover:bg-destructive/10 text-muted-foreground hover:text-destructive transition-colors cursor-pointer"
            aria-label={`Xoá ${item.name}`}
            title="Xoá"
          >
            <Trash2 className="size-4" />
          </button>
        </div>
      </td>
    </tr>
  );
}
