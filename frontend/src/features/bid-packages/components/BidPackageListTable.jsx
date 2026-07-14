import BidPackageFilters from './BidPackageFilters';
import BidPackageTableRow from './BidPackageTableRow';
import PaginationWithInput from '@/components/common/PaginationWithInput';
import { useBidPackagesContext } from './BidPackagesContext';

export default function BidPackageListTable({
  data,
  totalItems,
  totalPages,
  currentPage,
  onPageChange,
  onEdit,
  onDelete,
  hideFilters = false,
}) {
  const context = useBidPackagesContext();

  const tableData = data ?? context?.data ?? [];
  const tableTotalItems = totalItems ?? context?.totalItems ?? 0;
  const tableTotalPages = totalPages ?? context?.totalPages ?? 1;
  const tableCurrentPage = currentPage ?? context?.currentPage ?? 1;
  const tableOnPageChange = onPageChange ?? context?.setCurrentPage;

  return (
    <div className="space-y-4">
      {!hideFilters && (
        <BidPackageFilters />
      )}

      <div className="bg-card border border-border rounded-custom overflow-hidden shadow-sm relative min-h-[420px] flex flex-col">
        <div className="overflow-x-auto flex-1">
          <table className="w-full text-left text-sm border-collapse">
            <thead>
              <tr className="bg-secondary border-b-2 border-border text-xs uppercase tracking-wider text-secondary-foreground font-semibold">
                <th className="py-3 px-4">Mã gói</th>
                <th className="py-3 px-4 min-w-[200px]">Tên gói thầu</th>
                {!hideFilters && <th className="py-3 px-4">Dự án</th>}
                <th className="py-3 px-4 text-right">Giá trị dự toán</th>
                <th className="py-3 px-4 text-right">Tổng HĐ đã ký</th>
                <th className="py-3 px-4 min-w-[150px]">Tỷ lệ sử dụng</th>
                <th className="py-3 px-4 text-center">Trạng thái</th>
                <th className="py-3 px-4 text-center">Hành động</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border text-foreground">
              {tableData.map((item) => (
                <BidPackageTableRow
                  key={item.id}
                  item={item}
                  onEdit={onEdit}
                  onDelete={onDelete}
                  hideFilters={hideFilters}
                />
              ))}
              {tableData.length === 0 && (
                <tr>
                  <td colSpan={hideFilters ? 7 : 8} className="py-16 text-center text-muted-foreground">
                    <div className="space-y-1">
                      <p className="text-sm">Không tìm thấy gói thầu nào.</p>
                      <p className="text-xs">Thử thay đổi bộ lọc hoặc thêm gói thầu mới.</p>
                    </div>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {tableTotalItems > 0 && !hideFilters && (
        <PaginationWithInput
          totalItems={tableTotalItems}
          totalPages={tableTotalPages}
          currentPage={tableCurrentPage}
          onPageChange={tableOnPageChange}
          itemsPerPage={8}
        />
      )}
    </div>
  );
}
