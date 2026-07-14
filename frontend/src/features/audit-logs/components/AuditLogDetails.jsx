import * as Dialog from '@radix-ui/react-dialog';
import { X } from 'lucide-react';
import { cn } from '@/lib/utils';

export function AuditLogDetails({ log, open, onOpenChange }) {
  if (!log) return null;

  let oldValues = {};
  let newValues = {};
  let changedColumns = [];

  try {
    if (log.oldValues) oldValues = JSON.parse(log.oldValues);
    if (log.newValues) newValues = JSON.parse(log.newValues);
    if (log.changedColumns) changedColumns = JSON.parse(log.changedColumns);
  } catch (e) {
    console.error('Error parsing JSON from audit log', e);
  }

  // Determine which columns to display
  let columnsToDisplay = [];
  if (log.action === 'UPDATE') {
    columnsToDisplay = changedColumns.length > 0 ? changedColumns : Object.keys({ ...oldValues, ...newValues });
  } else if (log.action === 'CREATE') {
    columnsToDisplay = Object.keys(newValues);
  } else if (log.action === 'DELETE') {
    columnsToDisplay = Object.keys(oldValues);
  }

  // Format date
  const dateFormatted = log.timestamp
    ? new Date(log.timestamp).toLocaleString('vi-VN', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
      })
    : '—';

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/40 backdrop-blur-sm z-50 transition-opacity" />
        <Dialog.Content
          aria-describedby={undefined}
          className="fixed right-0 top-0 bottom-0 z-50 w-full max-w-2xl bg-background border-l border-border shadow-2xl p-6 flex flex-col gap-4 overflow-y-auto outline-none data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:slide-out-to-right data-[state=open]:slide-in-from-right duration-200"
        >
          <div className="flex items-center justify-between border-b border-border pb-4">
            <div>
              <Dialog.Title className="text-lg font-semibold text-foreground text-balance">
                Chi tiết nhật ký hoạt động
              </Dialog.Title>
            </div>
            <Dialog.Close className="rounded-full p-2 text-muted-foreground hover:bg-muted/50 hover:text-foreground transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none">
              <X className="w-5 h-5" aria-hidden="true" />
              <span className="sr-only">Đóng</span>
            </Dialog.Close>
          </div>

          <div className="grid grid-cols-2 gap-4 text-sm bg-muted/30 p-4 rounded-lg border border-border">
            <div>
              <span className="text-muted-foreground block mb-1">Thời gian</span>
              <span className="font-medium text-foreground tabular-nums">{dateFormatted}</span>
            </div>
            <div>
              <span className="text-muted-foreground block mb-1">IP Address</span>
              <span className="font-medium text-foreground tabular-nums">{log.ipAddress || '—'}</span>
            </div>
            <div>
              <span className="text-muted-foreground block mb-1">Người thực hiện</span>
              <span className="font-medium text-foreground">{log.username || '—'}</span>
            </div>
            <div>
              <span className="text-muted-foreground block mb-1">Hành động</span>
              <span className={cn('font-medium', 
                log.action === 'CREATE' ? 'text-emerald-600 dark:text-emerald-400' :
                log.action === 'UPDATE' ? 'text-blue-600 dark:text-blue-400' :
                'text-rose-600 dark:text-rose-400'
              )}>
                {log.action}
              </span>
            </div>
            <div>
              <span className="text-muted-foreground block mb-1">Bảng</span>
              <span className="font-medium text-foreground">{log.tableName}</span>
            </div>
            <div>
              <span className="text-muted-foreground block mb-1">Entity ID</span>
              <span className="font-medium text-foreground font-mono tabular-nums">{log.entityId}</span>
            </div>
          </div>

          <div className="flex-1 mt-4">
            <h4 className="font-medium text-foreground mb-4">Chi tiết thay đổi</h4>
            {columnsToDisplay.length > 0 ? (
              <div className="border border-border rounded-lg overflow-hidden">
                <table className="w-full text-sm text-left">
                  <thead className="text-xs uppercase tracking-wider text-secondary-foreground bg-secondary border-b-2 border-border">
                    <tr>
                      <th className="px-4 py-2 font-semibold w-1/4">Trường</th>
                      {(log.action === 'UPDATE' || log.action === 'DELETE') && (
                        <th className="px-4 py-2 font-semibold w-3/8 text-rose-800 dark:text-rose-400">Giá trị cũ</th>
                      )}
                      {(log.action === 'UPDATE' || log.action === 'CREATE') && (
                        <th className="px-4 py-2 font-semibold w-3/8 text-emerald-800 dark:text-emerald-400">Giá trị mới</th>
                      )}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border/50">
                    {columnsToDisplay.map((col) => {
                      const oldVal = oldValues[col];
                      const newVal = newValues[col];
                      // Format objects/arrays beautifully or just strings
                      const displayOld = typeof oldVal === 'object' ? JSON.stringify(oldVal, null, 2) : String(oldVal ?? '—');
                      const displayNew = typeof newVal === 'object' ? JSON.stringify(newVal, null, 2) : String(newVal ?? '—');

                      return (
                        <tr key={col} className="bg-card">
                          <td className="px-4 py-3 font-mono text-xs font-medium text-muted-foreground border-r border-border/50 align-top max-w-[120px] break-all">
                            {col}
                          </td>
                          {(log.action === 'UPDATE' || log.action === 'DELETE') && (
                            <td className="px-4 py-3 align-top bg-rose-50/50 dark:bg-rose-950/20 text-rose-900 dark:text-rose-300 border-r border-border/50 font-mono text-xs whitespace-pre-wrap break-all">
                              {displayOld}
                            </td>
                          )}
                          {(log.action === 'UPDATE' || log.action === 'CREATE') && (
                            <td className="px-4 py-3 align-top bg-emerald-50/50 dark:bg-emerald-950/20 text-emerald-900 dark:text-emerald-300 font-mono text-xs whitespace-pre-wrap break-all">
                              {displayNew}
                            </td>
                          )}
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            ) : (
              <div className="text-sm text-muted-foreground italic text-center p-8 border border-dashed border-border rounded-lg bg-muted/10">
                Không có dữ liệu chi tiết
              </div>
            )}
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
