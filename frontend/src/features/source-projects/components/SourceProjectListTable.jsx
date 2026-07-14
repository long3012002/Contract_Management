import SourceProjectFilters from './SourceProjectFilters';
import SourceProjectTableRow from './SourceProjectTableRow';
import PaginationWithInput from '@/components/common/PaginationWithInput';
import { useSourceProjectsContext } from './SourceProjectsContext';

export default function SourceProjectListTable() {
  const {
    data,
    totalItems,
    totalPages,
    currentPage,
    setCurrentPage,
    selectedIds,
    toggleSelectAll,
  } = useSourceProjectsContext();

  const selectableItems = data.filter((item) => item.status === 'Draft' || item.status === 'Available');
  const selectableIds = selectableItems.map((item) => item.id);
  const isAllSelected = selectableIds.length > 0 && selectableIds.every((id) => selectedIds.includes(id));
  const isSomeSelected = selectableIds.some((id) => selectedIds.includes(id)) && !isAllSelected;

  return (
    <div className="space-y-4">
      <SourceProjectFilters />

      {/* Table */}
      <div className="bg-card border border-border rounded-custom overflow-hidden shadow-sm relative min-h-[420px] flex flex-col">
        <div className="overflow-x-auto flex-1">
          <table className="w-full text-left text-sm border-collapse">
            <thead>
              <tr className="bg-secondary border-b-2 border-border text-xs uppercase tracking-wider text-secondary-foreground font-semibold">
                <th className="py-3 px-4 w-12 text-center">
                  <div className="flex justify-center">
                    <input
                      type="checkbox"
                      checked={isAllSelected}
                      ref={(input) => {
                        if (input) input.indeterminate = isSomeSelected;
                      }}
                      onChange={() => toggleSelectAll(selectableIds)}
                      disabled={selectableIds.length === 0}
                      className="size-4 rounded border-border text-primary focus:ring-primary cursor-pointer disabled:cursor-not-allowed disabled:opacity-50"
                      aria-label="Chọn tất cả trang hiện tại"
                    />
                  </div>
                </th>
                <th className="py-3 px-4">Mã nguồn</th>
                <th className="py-3 px-4">Tên nguồn</th>
                <th className="py-3 px-4">Quyết định</th>
                <th className="py-3 px-4 text-right">Giá trị</th>
                <th className="py-3 px-4 text-center">Loại vốn</th>
                <th className="py-3 px-4 text-center">Năm</th>
                <th className="py-3 px-4 text-center">Trạng thái</th>
                <th className="py-3 px-4 text-center">Hành động</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border text-foreground">
              {data.map((item) => (
                <SourceProjectTableRow
                  key={item.id}
                  item={item}
                />
              ))}

              {data.length === 0 && (
                <tr>
                  <td
                    colSpan="9"
                    className="py-16 text-center text-muted-foreground"
                  >
                    <div className="space-y-1">
                      <p className="text-sm">Không tìm thấy nguồn vốn nào.</p>
                      <p className="text-xs">Thử thay đổi bộ lọc hoặc thêm nguồn vốn mới.</p>
                    </div>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Pagination */}
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
