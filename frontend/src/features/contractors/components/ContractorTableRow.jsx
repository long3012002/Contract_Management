import { Pencil, Trash2, Mail, Phone } from 'lucide-react';
import { useConfirm } from '@/components/providers/confirm-dialog-provider';
import ContractorStatusBadge from './ContractorStatusBadge';
import { useContractorsContext } from './ContractorsContext';

export default function ContractorTableRow({ item }) {
  const { confirm } = useConfirm();
  const { handleOpenEdit, handleDelete } = useContractorsContext();

  const onDeleteClick = async () => {
    if (item.contractCount > 0) {
      await confirm({
        title: 'Không thể xoá nhà thầu',
        description: `Nhà thầu ${item.name} đang có ${item.contractCount} hợp đồng liên kết. Bạn cần xoá các hợp đồng này trước.`,
        confirmText: 'Đã hiểu',
        hideCancel: true,
      });
      return;
    }

    const isConfirmed = await confirm({
      title: 'Xoá nhà thầu?',
      description: (
        <span>
          Bạn có chắc chắn muốn xoá nhà thầu{' '}
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

  return (
    <tr className="hover:bg-muted/20 transition-colors group">
      <td className="py-3 px-4">
        <span className="font-mono text-xs font-medium text-muted-foreground">
          {item.code}
        </span>
      </td>
      <td className="py-3 px-4">
        <div className="space-y-0.5">
          <p className="font-medium truncate max-w-[240px]" title={item.name}>
            {item.name}
          </p>
          <p className="text-xs font-mono text-muted-foreground">
            MST: {item.taxCode}
          </p>
        </div>
      </td>
      <td className="py-3 px-4">
        <span className="text-sm">{item.representative}</span>
      </td>
      <td className="py-3 px-4">
        <div className="space-y-1 text-xs text-muted-foreground">
          {item.phone && (
            <div className="flex items-center gap-1.5">
              <Phone className="size-3" />
              <span>{item.phone}</span>
            </div>
          )}
          {item.email && (
            <div className="flex items-center gap-1.5">
              <Mail className="size-3" />
              <span className="truncate max-w-[150px]" title={item.email}>{item.email}</span>
            </div>
          )}
        </div>
      </td>
      <td className="py-3 px-4 text-center">
        <span className="inline-flex items-center justify-center w-6 h-6 rounded-full bg-secondary text-secondary-foreground text-xs font-medium tabular-nums">
          {item.contractCount}
        </span>
      </td>
      <td className="py-3 px-4 text-center">
        <ContractorStatusBadge status={item.status} />
      </td>
      <td className="py-3 px-4 text-center">
        <div className="flex items-center justify-center gap-1">
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
      </td>
    </tr>
  );
}
