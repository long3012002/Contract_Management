import { FileSearch } from 'lucide-react';
import { cn } from '@/lib/utils';

export function AuditLogsTable({ logs, onRowClick }) {
  if (!logs || logs.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-16 px-4 border border-dashed border-border rounded-lg bg-zinc-50 dark:bg-zinc-900/50">
        <div className="bg-background p-3 rounded-full shadow-sm border border-border mb-4">
          <FileSearch className="w-6 h-6 text-muted-foreground" aria-hidden="true" />
        </div>
        <h3 className="text-base font-semibold text-foreground mb-1 text-balance text-center">
          Không tìm thấy lịch sử hoạt động
        </h3>
        <p className="text-sm text-muted-foreground text-center text-pretty max-w-sm mb-4">
          Hệ thống chưa ghi nhận bất kỳ thay đổi nào khớp với bộ lọc hiện tại của bạn.
        </p>
        <p className="text-sm text-muted-foreground">
          Thử xóa bộ lọc để xem toàn bộ danh sách.
        </p>
      </div>
    );
  }

  const getActionColor = (action) => {
    switch (action?.toUpperCase()) {
      case 'CREATE':
        return 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-400 border-emerald-200 dark:border-emerald-800';
      case 'UPDATE':
        return 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-400 border-blue-200 dark:border-blue-800';
      case 'DELETE':
        return 'bg-rose-100 text-rose-800 dark:bg-rose-900/30 dark:text-rose-400 border-rose-200 dark:border-rose-800';
      default:
        return 'bg-zinc-100 text-zinc-800 dark:bg-zinc-800 dark:text-zinc-400 border-zinc-200 dark:border-zinc-700';
    }
  };

  const formatTime = (isoString) => {
    if (!isoString) return '—';
    const date = new Date(isoString);
    return date.toLocaleTimeString('vi-VN', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    });
  };

  const formatDateOnly = (isoString) => {
    if (!isoString) return '—';
    const date = new Date(isoString);
    const today = new Date();
    const yesterday = new Date();
    yesterday.setDate(today.getDate() - 1);

    if (date.toDateString() === today.toDateString()) {
      return { text: 'Hôm nay', isSpecial: true, className: 'text-emerald-600 dark:text-emerald-400 font-medium' };
    }
    if (date.toDateString() === yesterday.toDateString()) {
      return { text: 'Hôm qua', isSpecial: true, className: 'text-amber-600 dark:text-amber-400 font-medium' };
    }

    return {
      text: date.toLocaleDateString('vi-VN', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
      }),
      isSpecial: false,
      className: 'text-muted-foreground'
    };
  };

  return (
    <div className="w-full overflow-x-auto border border-border rounded-lg shadow-sm">
      <table className="w-full text-sm text-left whitespace-nowrap">
        <thead className="text-xs uppercase tracking-wider text-secondary-foreground bg-secondary border-b-2 border-border">
          <tr>
            <th scope="col" className="px-4 py-3 font-semibold">Thời gian</th>
            <th scope="col" className="px-4 py-3 font-semibold">Người dùng</th>
            <th scope="col" className="px-4 py-3 font-semibold">Hành động</th>
            <th scope="col" className="px-4 py-3 font-semibold">Bảng dữ liệu</th>
            <th scope="col" className="px-4 py-3 font-semibold max-w-[150px]">Mã ID</th>
            <th scope="col" className="px-4 py-3 font-semibold text-right">Hành động</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {logs.map((log) => (
            <tr
              key={log.id}
              className="bg-card hover:bg-muted/30 transition-colors cursor-pointer group"
              onClick={() => onRowClick(log)}
              tabIndex={0}
              role="button"
              aria-label={`Xem chi tiết nhật ký lúc ${formatTime(log.timestamp)}`}
              onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                  e.preventDefault();
                  onRowClick(log);
                }
              }}
            >
              <td className="px-4 py-3 tabular-nums">
                <div className="flex flex-col">
                  <span className="font-semibold text-foreground text-sm">
                    {formatTime(log.timestamp)}
                  </span>
                  {(() => {
                    const dateInfo = formatDateOnly(log.timestamp);
                    return (
                      <span className={cn("text-xs", dateInfo.className)}>
                        {dateInfo.text}
                      </span>
                    );
                  })()}
                </div>
              </td>
              <td className="px-4 py-3">
                <div className="flex flex-col min-w-0">
                  <span className="font-medium text-foreground truncate max-w-[180px]">
                    {log.username || '—'}
                  </span>
                  <span className="text-xs text-muted-foreground truncate max-w-[180px]" title={log.userId}>
                    {log.userId || '—'}
                  </span>
                </div>
              </td>
              <td className="px-4 py-3">
                <span
                  className={cn(
                    'px-2.5 py-0.5 rounded-full text-xs font-medium border',
                    getActionColor(log.action)
                  )}
                >
                  {log.action}
                </span>
              </td>
              <td className="px-4 py-3 font-medium text-foreground">
                {log.tableName || '—'}
              </td>
              <td className="px-4 py-3">
                <div className="text-muted-foreground truncate max-w-[150px] font-mono text-xs tabular-nums" title={log.entityId}>
                  {log.entityId || '—'}
                </div>
              </td>
              <td className="px-4 py-3 text-right">
                <span className="text-xs font-medium text-primary opacity-0 group-hover:opacity-100 transition-opacity focus-visible:opacity-100">
                  Xem chi tiết
                </span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
