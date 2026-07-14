import { Pencil, Trash2, AlertTriangle, ChevronRight } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { useConfirm } from '@/components/providers/confirm-dialog-provider';
import ContractStatusBadge from './ContractStatusBadge';
import { cn } from '@/lib/utils';
import { formatVND, formatDate } from '@/utils/formatters';
import { useContractsContext } from './ContractsContext';

export default function ContractTableRow({ item }) {
  const navigate = useNavigate();
  const { confirm } = useConfirm();
  const { handleOpenEdit, handleDelete } = useContractsContext();

  const onDeleteClick = async () => {
    const isConfirmed = await confirm({
      title: 'Xoá hợp đồng?',
      description: (
        <span>
          Bạn có chắc chắn muốn xoá hợp đồng{' '}
          <strong className="text-foreground font-semibold">{item.name}</strong>?
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

  const getExpiryWarning = (dateStr, status) => {
    if (!dateStr || status === 'Terminated') return false;
    const expDate = new Date(dateStr);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const diffTime = expDate - today;
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    return diffDays <= 30;
  };

  const isExpiringWarning = getExpiryWarning(item.expiredDate, item.status);

  return (
    <tr className="hover:bg-muted/20 transition-colors group">
      <td className="py-3 px-4">
        <div className="flex items-center gap-1.5">
          {isExpiringWarning && <AlertTriangle className="size-3.5 text-amber-500" title="Sắp hết hạn" />}
          <span className="font-mono text-xs font-medium text-muted-foreground">
            {item.code}
          </span>
        </div>
      </td>
      <td className="py-3 px-4">
        <button
          onClick={() => navigate(`/contracts/${item.id}`)}
          className="text-left font-medium hover:text-primary transition-colors flex items-center gap-1 group-hover:underline underline-offset-4 truncate max-w-[200px] cursor-pointer"
          title={item.name}
        >
          {item.name}
        </button>
      </td>
      <td className="py-3 px-4">
        <p className="text-xs font-medium truncate max-w-[150px]" title={item.contractorName}>
          {item.contractorName}
        </p>
      </td>
      <td className="py-3 px-4 text-right">
        <span className="font-semibold tabular-nums text-sm">
          {formatVND(item.value)}
        </span>
      </td>
      <td className="py-3 px-4">
        <span className="text-xs text-muted-foreground tabular-nums">
          {formatDate(item.signedDate)}
        </span>
      </td>
      <td className="py-3 px-4">
        <span className={cn(
          "text-xs font-medium tabular-nums px-2 py-0.5 rounded-sm",
          isExpiringWarning ? "bg-red-500/10 text-red-700" : "text-muted-foreground"
        )}>
          {formatDate(item.expiredDate)}
        </span>
      </td>
      <td className="py-3 px-4 text-center">
        <ContractStatusBadge status={item.status} />
      </td>
      <td className="py-3 px-4 text-center">
        <div className="flex items-center justify-center gap-1">
          <button
            onClick={() => navigate(`/contracts/${item.id}`)}
            className="p-1.5 rounded-md hover:bg-muted text-muted-foreground hover:text-foreground transition-colors cursor-pointer"
            aria-label="Chi tiết"
            title="Xem chi tiết"
          >
            <ChevronRight className="size-4" />
          </button>
          <button
            onClick={() => handleOpenEdit(item)}
            className="p-1.5 rounded-md hover:bg-primary/10 text-muted-foreground hover:text-primary transition-colors cursor-pointer"
            title="Chỉnh sửa"
          >
            <Pencil className="size-4" />
          </button>
          <button
            onClick={onDeleteClick}
            className="p-1.5 rounded-md hover:bg-destructive/10 text-muted-foreground hover:text-destructive transition-colors cursor-pointer"
            title="Xoá"
          >
            <Trash2 className="size-4" />
          </button>
        </div>
      </td>
    </tr>
  );
}
