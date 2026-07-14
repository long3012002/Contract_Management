import React from 'react';
import { CheckCircle, AlertTriangle } from 'lucide-react';

export default function ImportResults({ importResult }) {
  if (!importResult) return null;

  return (
    <div className="space-y-4">
      {/* Summary Results */}
      <div className="grid grid-cols-3 gap-4 text-center">
        <div className="p-4 rounded-lg bg-green-500/10 border border-green-500/20">
          <p className="text-2xl font-bold text-green-600 dark:text-green-400">{importResult.addedCount}</p>
          <p className="text-xs text-muted-foreground mt-1 font-medium">Thêm mới thành công</p>
        </div>
        <div className="p-4 rounded-lg bg-blue-500/10 border border-blue-500/20">
          <p className="text-2xl font-bold text-blue-600 dark:text-blue-400">{importResult.updatedCount}</p>
          <p className="text-xs text-muted-foreground mt-1 font-medium">Cập nhật thành công</p>
        </div>
        <div className="p-4 rounded-lg bg-destructive/10 border border-destructive/20">
          <p className="text-2xl font-bold text-destructive">{importResult.errorCount}</p>
          <p className="text-xs text-muted-foreground mt-1 font-medium">Hàng lỗi</p>
        </div>
      </div>

      {/* Success Notification */}
      {importResult.errorCount === 0 && (
        <div className="p-4 bg-green-500/15 border border-green-500/30 rounded-md text-green-700 dark:text-green-400 text-sm flex items-center gap-3">
          <CheckCircle className="w-5 h-5 shrink-0" />
          <span className="font-medium">Nhập danh sách người dùng thành công hoàn toàn!</span>
        </div>
      )}

      {/* Errors Breakdown */}
      {importResult.errorCount > 0 && (
        <div className="space-y-2">
          <h4 className="font-semibold text-sm text-foreground flex items-center gap-2">
            <AlertTriangle className="w-4 h-4 text-destructive" />
            Chi tiết các dòng bị lỗi ({importResult.errorCount})
          </h4>
          <div className="border border-border rounded-lg overflow-hidden max-h-[250px] overflow-y-auto">
            <table className="w-full text-left text-sm border-collapse">
              <thead>
                <tr className="bg-secondary text-secondary-foreground text-xs uppercase tracking-wider font-semibold border-b-2 border-border">
                  <th className="py-2 px-3 w-16 text-center">Dòng</th>
                  <th className="py-2 px-3 w-40">Username</th>
                  <th className="py-2 px-3">Chi tiết lỗi</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border bg-card">
                {importResult.errors?.map((err, idx) => (
                  <tr key={idx} className="hover:bg-muted/30">
                    <td className="py-2 px-3 text-center text-muted-foreground font-medium">{err.rowIndex}</td>
                    <td className="py-2 px-3 font-semibold text-foreground">{err.username || '(Trống)'}</td>
                    <td className="py-2 px-3 text-destructive space-y-1">
                      {err.errorMessages.map((msg, mIdx) => (
                        <p key={mIdx}>• {msg}</p>
                      ))}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
