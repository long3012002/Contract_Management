import { Pencil, Trash2, Zap } from 'lucide-react';
import { useConfirm } from '@/components/providers/confirm-dialog-provider';
import SourceProjectStatusBadge from './SourceProjectStatusBadge';
import { cn } from '@/lib/utils';
import { formatVND } from '@/utils/formatters';
import { useSourceProjectsContext } from './SourceProjectsContext';

const fundTypeBadge = {
  NSNN: 'bg-blue-500/10 text-blue-700 border-blue-500/20',
  'Tự có': 'bg-amber-500/10 text-amber-700 border-amber-500/20',
  Vay: 'bg-violet-500/10 text-violet-700 border-violet-500/20',
  Khác: 'bg-zinc-100 text-zinc-600 border-zinc-200',
};

export default function SourceProjectTableRow({ item }) {
  const { confirm } = useConfirm();
  const { handleOpenEdit, handleDelete, handleActivate, selectedIds, toggleSelect } = useSourceProjectsContext();

  const isSelected = selectedIds.includes(item.id);

  const onDeleteClick = async () => {
    const isConfirmed = await confirm({
      title: 'Xoá nguồn vốn?',
      description: (
        <span>
          Bạn có chắc chắn muốn xoá nguồn vốn{' '}
          <strong className="text-foreground font-semibold">{item.name}</strong>?
          <br />
          <span className="text-xs text-muted-foreground mt-1 inline-block">
            Hành động này không thể hoàn tác.
          </span>
        </span>
      ),
      confirmText: 'Xoá',
      cancelText: 'Huỷ',
      variant: 'destructive',
    });
    if (isConfirmed) {
      handleDelete(item.id);
    }
  };

  const onActivateClick = async () => {
    const isConfirmed = await confirm({
      title: 'Kích hoạt nguồn vốn?',
      description: (
        <span>
          Chuyển nguồn vốn{' '}
          <strong className="text-foreground font-semibold">{item.name}</strong>{' '}
          sang trạng thái <strong className="text-emerald-600">Sẵn sàng</strong>?
        </span>
      ),
      confirmText: 'Kích hoạt',
      cancelText: 'Huỷ',
    });
    if (isConfirmed) {
      handleActivate(item.id);
    }
  };

  const canModify = (status) => status === 'Draft' || status === 'Available';

  return (
    <tr className={cn(
      "transition-colors",
      isSelected ? "bg-primary/5 hover:bg-primary/10" : "hover:bg-muted/20"
    )}>
      <td className="py-3 px-4">
        <div className="flex justify-center">
          <input
            type="checkbox"
            checked={isSelected}
            onChange={() => toggleSelect(item.id)}
            disabled={!canModify(item.status)}
            className="size-4 rounded border-border text-primary focus:ring-primary cursor-pointer disabled:cursor-not-allowed disabled:opacity-50"
            aria-label={`Chọn nguồn vốn ${item.name}`}
            title={!canModify(item.status) ? "Không thể chọn nguồn vốn đã phân bổ hoặc đã đóng" : undefined}
          />
        </div>
      </td>
      <td className="py-3 px-4">
        <span className="font-mono text-xs font-medium text-muted-foreground">
          {item.code}
        </span>
      </td>
      <td className="py-3 px-4">
        <div className="min-w-0">
          <p className="font-medium truncate max-w-[240px]">
            {item.name}
          </p>
          {item.allocatedProjectName && (
            <p className="text-xs text-muted-foreground mt-0.5 truncate max-w-[240px]">
              → {item.allocatedProjectName}
            </p>
          )}
        </div>
      </td>
      <td className="py-3 px-4">
        <span className="text-xs font-medium text-muted-foreground">{item.decision}</span>
      </td>
      <td className="py-3 px-4 text-right">
        <span className="font-semibold tabular-nums text-sm">
          {formatVND(item.value)}
        </span>
      </td>
      <td className="py-3 px-4 text-center">
        <span
          className={cn(
            'inline-flex items-center px-2 py-0.5 text-xs font-medium rounded-full border whitespace-nowrap',
            fundTypeBadge[item.fundType] || fundTypeBadge['Khác']
          )}
        >
          {item.fundType}
        </span>
      </td>
      <td className="py-3 px-4 text-center">
        <span className="tabular-nums text-sm text-muted-foreground">{item.year}</span>
      </td>
      <td className="py-3 px-4 text-center">
        <SourceProjectStatusBadge status={item.status} />
      </td>
      <td className="py-3 px-4 text-center">
        {canModify(item.status) ? (
          <div className="flex items-center justify-center gap-1">
            {item.status === 'Draft' && (
              <button
                onClick={onActivateClick}
                className="p-1.5 rounded-md hover:bg-emerald-500/10 text-muted-foreground hover:text-emerald-600 transition-colors cursor-pointer"
                aria-label={`Kích hoạt ${item.name}`}
                title="Kích hoạt"
              >
                <Zap className="size-4" />
              </button>
            )}
            <button
              onClick={() => handleOpenEdit(item)}
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
        ) : (
          <span className="text-xs text-muted-foreground">—</span>
        )}
      </td>
    </tr>
  );
}
