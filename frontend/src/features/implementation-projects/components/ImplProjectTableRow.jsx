import { Pencil, Trash2, ChevronRight } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { useConfirm } from '@/components/providers/confirm-dialog-provider';
import ImplProjectStatusBadge from './ImplProjectStatusBadge';
import { formatVND, formatDate } from '@/utils/formatters';
import { useImplProjectsContext } from './ImplProjectsContext';

export default function ImplProjectTableRow({ item }) {
  const navigate = useNavigate();
  const { confirm } = useConfirm();
  const { handleOpenEdit, handleDelete } = useImplProjectsContext();

  const onDeleteClick = async () => {
    const isConfirmed = await confirm({
      title: 'Xoá dự án triển khai?',
      description: (
        <span>
          Bạn có chắc chắn muốn xoá dự án{' '}
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

  return (
    <tr className="hover:bg-muted/20 transition-colors group">
      <td className="py-3 px-4">
        <span className="font-mono text-xs font-medium text-muted-foreground">
          {item.code}
        </span>
      </td>
      <td className="py-3 px-4">
        <button
          onClick={() => navigate(`/projects/${item.id}`)}
          className="text-left font-medium hover:text-primary transition-colors flex items-center gap-1 group-hover:underline underline-offset-4 cursor-pointer"
        >
          <span className="truncate max-w-[240px]">{item.name}</span>
        </button>
      </td>
      <td className="py-3 px-4">
        <span className="text-xs font-medium">{item.unit}</span>
      </td>
      <td className="py-3 px-4 text-center">
        <span className="inline-flex items-center px-2 py-0.5 text-xs font-medium rounded-full bg-secondary border border-border">
          {item.projectType}
        </span>
      </td>
      <td className="py-3 px-4 text-right">
        <span className="font-semibold tabular-nums text-sm">
          {formatVND(item.totalBudget)}
        </span>
      </td>
      <td className="py-3 px-4">
        <div className="flex flex-col gap-0.5 text-xs tabular-nums text-muted-foreground">
          <span>{formatDate(item.startDate)}</span>
          <span>{formatDate(item.endDate)}</span>
        </div>
      </td>
      <td className="py-3 px-4 text-center">
        <ImplProjectStatusBadge status={item.status} />
      </td>
      <td className="py-3 px-4 text-center">
        <div className="flex items-center justify-center gap-1">
          <button
            onClick={() => navigate(`/projects/${item.id}`)}
            className="p-1.5 rounded-md hover:bg-muted text-muted-foreground hover:text-foreground transition-colors cursor-pointer"
            aria-label={`Chi tiết ${item.name}`}
            title="Xem chi tiết"
          >
            <ChevronRight className="size-4" />
          </button>
          {item.status === 'Draft' && (
            <>
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
            </>
          )}
        </div>
      </td>
    </tr>
  );
}
