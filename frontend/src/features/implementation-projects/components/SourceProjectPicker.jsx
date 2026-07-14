import { MOCK_SOURCE_PROJECTS } from '@/features/source-projects/constants/mockSourceProjects';
import Checkbox from '@/components/Checkbox/Checkbox';

const formatVND = (value) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(value);

export default function SourceProjectPicker({ selected = [], onChange }) {
  const available = MOCK_SOURCE_PROJECTS.filter((sp) => sp.status === 'Available');
  const totalValue = selected.reduce((sum, sp) => sum + sp.value, 0);

  const handleToggle = (sp, checked) => {
    if (checked) {
      onChange([...selected, { id: sp.id, code: sp.code, name: sp.name, value: sp.value }]);
    } else {
      onChange(selected.filter((item) => item.id !== sp.id));
    }
  };

  const isSelected = (id) => selected.some((item) => item.id === id);

  return (
    <div className="space-y-3">
      <label className="text-sm font-medium text-foreground">Chọn nguồn vốn</label>

      {available.length === 0 ? (
        <p className="text-sm text-muted-foreground py-3">
          Không có nguồn vốn nào ở trạng thái sẵn sàng.
        </p>
      ) : (
        <div className="border border-border rounded-md max-h-48 overflow-y-auto divide-y divide-border">
          {available.map((sp) => (
            <label
              key={sp.id}
              className="flex items-center gap-3 px-3 py-2.5 hover:bg-muted/30 transition-colors cursor-pointer"
            >
              <Checkbox
                id={`pick-${sp.id}`}
                checked={isSelected(sp.id)}
                onChange={(e) => handleToggle(sp, e.target.checked)}
              />
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium truncate">{sp.name}</p>
                <p className="text-xs text-muted-foreground font-mono">{sp.code}</p>
              </div>
              <span className="text-sm font-semibold tabular-nums text-right shrink-0">
                {formatVND(sp.value)}
              </span>
            </label>
          ))}
        </div>
      )}

      {/* Summary */}
      {selected.length > 0 && (
        <div className="bg-secondary/50 border border-border rounded-md p-3 space-y-2">
          <p className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
            Nguồn vốn đã chọn ({selected.length})
          </p>
          <div className="space-y-1">
            {selected.map((sp) => (
              <div key={sp.id} className="flex items-center justify-between text-sm">
                <span className="truncate max-w-[60%]">{sp.name}</span>
                <span className="tabular-nums font-medium">{formatVND(sp.value)}</span>
              </div>
            ))}
          </div>
          <div className="border-t border-border pt-2 flex items-center justify-between">
            <span className="text-sm font-semibold">Tổng vốn</span>
            <span className="text-sm font-bold text-primary tabular-nums">
              {formatVND(totalValue)}
            </span>
          </div>
        </div>
      )}
    </div>
  );
}
