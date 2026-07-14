export default function ImplProjectAuditTrail({ auditTrail = [] }) {
  if (auditTrail.length === 0) {
    return (
      <div className="py-12 text-center text-muted-foreground">
        <p className="text-sm">Chưa có lịch sử thay đổi nào.</p>
      </div>
    );
  }

  const formatDate = (timestamp) => {
    return new Date(timestamp).toLocaleString('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-left text-sm border-collapse">
        <thead>
          <tr className="bg-secondary border-b-2 border-border text-xs uppercase tracking-wider text-secondary-foreground font-semibold">
            <th className="py-3 px-4 w-44">Thời gian</th>
            <th className="py-3 px-4">Người thực hiện</th>
            <th className="py-3 px-4">Hành động</th>
            <th className="py-3 px-4">Chi tiết</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border text-foreground">
          {[...auditTrail].reverse().map((log) => (
            <tr key={log.id} className="hover:bg-muted/20 transition-colors">
              <td className="py-3 px-4">
                <span className="text-xs font-mono text-muted-foreground tabular-nums">
                  {formatDate(log.timestamp)}
                </span>
              </td>
              <td className="py-3 px-4">
                <span className="text-sm font-medium">{log.user}</span>
              </td>
              <td className="py-3 px-4">
                <span className="text-sm">{log.action}</span>
              </td>
              <td className="py-3 px-4">
                {log.changes ? (
                  <span className="text-xs text-muted-foreground">
                    <span className="line-through text-destructive/70">{log.changes.oldValue}</span>
                    {' → '}
                    <span className="font-medium text-foreground">{log.changes.newValue}</span>
                  </span>
                ) : (
                  <span className="text-xs text-muted-foreground">—</span>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
