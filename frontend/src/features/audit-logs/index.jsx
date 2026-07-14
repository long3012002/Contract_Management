import { useState, useEffect } from 'react';
import { useAuditLogs } from './hooks/useAuditLogs';
import { ClipboardList } from 'lucide-react';
import { AuditLogsFilters } from './components/AuditLogsFilters';
import { AuditLogsTable } from './components/AuditLogsTable';
import { AuditLogDetails } from './components/AuditLogDetails';
import { Loader2 } from 'lucide-react';
import {
  Pagination,
  PaginationContent,
  PaginationEllipsis,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
} from '@/components/ui/pagination';

export default function AuditLogsPage() {
  const {
    data,
    isPending,
    isError,
    filters,
    applyFilters,
    clearFilters,
    changePage,
    changePageSize
  } = useAuditLogs();

  const totalPages = data?.totalItems ? Math.ceil(data.totalItems / filters.pageSize) : 1;

  const [selectedLog, setSelectedLog] = useState(null);
  const [pageInput, setPageInput] = useState('1');

  useEffect(() => {
    setPageInput(filters.page.toString());
  }, [filters.page]);

  const handlePageInputChange = (e) => {
    const val = e.target.value;
    setPageInput(val);
    const num = parseInt(val, 10);
    if (!isNaN(num) && num >= 1 && num <= totalPages) {
      changePage(num);
    }
  };

  return (
    <div className="flex-1 flex flex-col h-full bg-background pb-10">
      <main className="flex-1 p-6 overflow-y-auto">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
          <div>
            <h1 className="text-2xl font-bold text-foreground flex items-center gap-2 text-balance">
              <ClipboardList className="size-6 text-primary" />
              Lịch sử hoạt động
            </h1>
            <p className="text-muted-foreground mt-1 text-sm text-pretty">
              Theo dõi và kiểm tra các thay đổi dữ liệu trong hệ thống.
            </p>
          </div>
        </div>

        <AuditLogsFilters
          filters={filters}
          onApply={applyFilters}
          onClear={clearFilters}
        />

        {isError && (
          <div className="p-4 mb-6 text-sm text-rose-800 bg-rose-50 border border-rose-200 rounded-lg dark:bg-rose-900/30 dark:text-rose-400 dark:border-rose-900" role="alert" aria-live="assertive">
            <span className="font-medium">Lỗi!</span> Không thể tải dữ liệu lịch sử hoạt động. Vui lòng thử lại sau.
          </div>
        )}

        <div className="bg-card rounded-lg border border-border shadow-sm flex flex-col relative min-h-[400px]">
          {isPending ? (
            <div className="absolute inset-0 flex items-center justify-center bg-background/50 backdrop-blur-sm z-10" aria-busy="true" aria-live="polite">
              <Loader2 className="w-8 h-8 text-primary animate-spin" />
              <span className="sr-only">Đang tải dữ liệu...</span>
            </div>
          ) : (
            <>
              <div className="flex-1 overflow-x-auto" aria-live="polite">
                <AuditLogsTable logs={data?.items || []} onRowClick={setSelectedLog} />
              </div>

              {data?.items && data.items.length > 0 && (
                <div className="p-4 border-t border-border flex items-center justify-between">
                  <div className="text-sm text-muted-foreground">
                    Hiển thị <span className="font-medium tabular-nums">{data.items.length}</span> / <span className="font-medium tabular-nums">{data.totalItems}</span> bản ghi
                  </div>
                  
                  <div className="flex items-center gap-4">
                    <div className="flex items-center gap-2">
                      <label htmlFor="pageSize" className="text-sm text-muted-foreground whitespace-nowrap">Số dòng:</label>
                      <select
                        id="pageSize"
                        value={filters.pageSize}
                        onChange={(e) => changePageSize(Number(e.target.value))}
                        className="text-sm bg-background border border-input rounded-md px-2 py-1 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                      >
                        <option value="10">10</option>
                        <option value="20">20</option>
                        <option value="50">50</option>
                        <option value="100">100</option>
                      </select>
                    </div>

                    {totalPages > 1 && (
                      <div className="flex items-center gap-4">
                        <Pagination className="w-auto mx-0">
                          <PaginationContent>
                            <PaginationItem>
                              <PaginationPrevious 
                                onClick={() => changePage(Math.max(filters.page - 1, 1))}
                                disabled={filters.page === 1}
                                className={`cursor-pointer ${filters.page === 1 ? 'pointer-events-none opacity-50' : ''}`}
                              />
                            </PaginationItem>
                            
                            <PaginationItem>
                              <PaginationLink isActive className="pointer-events-none">
                                {filters.page}
                              </PaginationLink>
                            </PaginationItem>

                            {filters.page < totalPages && (
                              <PaginationItem>
                                <PaginationLink className="cursor-pointer" onClick={() => changePage(filters.page + 1)}>
                                  {filters.page + 1}
                                </PaginationLink>
                              </PaginationItem>
                            )}
                            
                            {filters.page < totalPages - 1 && (
                              <PaginationItem>
                                <PaginationEllipsis />
                              </PaginationItem>
                            )}

                            <PaginationItem>
                              <PaginationNext 
                                onClick={() => changePage(Math.min(filters.page + 1, totalPages))}
                                disabled={filters.page === totalPages}
                                className={`cursor-pointer ${filters.page === totalPages ? 'pointer-events-none opacity-50' : ''}`}
                              />
                            </PaginationItem>
                          </PaginationContent>
                        </Pagination>
                        <div className="flex items-center gap-1.5 text-xs md:text-sm text-muted-foreground">
                          <span className="whitespace-nowrap">Đến trang:</span>
                          <input
                            type="number"
                            min={1}
                            max={totalPages}
                            value={pageInput}
                            onChange={handlePageInputChange}
                            className="w-12 text-center bg-background border border-input rounded-md py-1 text-xs md:text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:border-ring [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
                          />
                          <span className="whitespace-nowrap">/ {totalPages}</span>
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      </main>

      <AuditLogDetails
        log={selectedLog}
        open={!!selectedLog}
        onOpenChange={(isOpen) => !isOpen && setSelectedLog(null)}
      />
    </div>
  );
}
