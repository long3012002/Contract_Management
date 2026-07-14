import ImplProjectFilters from './ImplProjectFilters';
import ImplProjectTableRow from './ImplProjectTableRow';
import PaginationWithInput from '@/components/common/PaginationWithInput';
import { useImplProjectsContext } from './ImplProjectsContext';

export default function ImplProjectListTable() {
  const {
    data,
    totalItems,
    totalPages,
    currentPage,
    setCurrentPage,
  } = useImplProjectsContext();

  return (
    <div className="space-y-4">
      <ImplProjectFilters />

      <div className="bg-card border border-border rounded-custom overflow-hidden shadow-sm relative min-h-[420px] flex flex-col">
        <div className="overflow-x-auto flex-1">
          <table className="w-full text-left text-sm border-collapse">
            <thead>
              <tr className="bg-secondary border-b-2 border-border text-xs uppercase tracking-wider text-secondary-foreground font-semibold">
                <th className="py-3 px-4">Mã dự án</th>
                <th className="py-3 px-4 min-w-[240px]">Tên dự án</th>
                <th className="py-3 px-4">Đơn vị</th>
                <th className="py-3 px-4 text-center">Loại</th>
                <th className="py-3 px-4 text-right">Tổng vốn</th>
                <th className="py-3 px-4">Thời gian</th>
                <th className="py-3 px-4 text-center">Trạng thái</th>
                <th className="py-3 px-4 text-center">Hành động</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border text-foreground">
              {data.map((item) => (
                <ImplProjectTableRow
                  key={item.id}
                  item={item}
                />
              ))}
              {data.length === 0 && (
                <tr>
                  <td colSpan="8" className="py-16 text-center text-muted-foreground">
                    <div className="space-y-1">
                      <p className="text-sm">Không tìm thấy dự án nào.</p>
                      <p className="text-xs">Thử thay đổi bộ lọc hoặc tạo dự án mới.</p>
                    </div>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      <PaginationWithInput
        totalItems={totalItems}
        totalPages={totalPages}
        currentPage={currentPage}
        onPageChange={setCurrentPage}
        itemsPerPage={8}
      />
    </div>
  );
}
