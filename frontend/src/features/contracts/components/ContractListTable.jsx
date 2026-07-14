import ContractFilters from './ContractFilters';
import ContractTableRow from './ContractTableRow';
import PaginationWithInput from '@/components/common/PaginationWithInput';
import { useContractsContext } from './ContractsContext';

export default function ContractListTable({ hideProjectFilter = false }) {
  const {
    data,
    totalItems,
    totalPages,
    currentPage,
    setCurrentPage,
  } = useContractsContext();

  return (
    <div className="space-y-4">
      <ContractFilters hideProjectFilter={hideProjectFilter} />

      <div className="bg-card border border-border rounded-custom overflow-hidden shadow-sm relative min-h-[420px] flex flex-col">
        <div className="overflow-x-auto flex-1">
          <table className="w-full text-left text-sm border-collapse">
            <thead>
              <tr className="bg-secondary border-b-2 border-border text-xs uppercase tracking-wider text-secondary-foreground font-semibold">
                <th className="py-3 px-4">Số HĐ</th>
                <th className="py-3 px-4 min-w-[200px]">Tên Hợp đồng</th>
                <th className="py-3 px-4 min-w-[150px]">Nhà thầu</th>
                <th className="py-3 px-4 text-right">Giá trị HĐ</th>
                <th className="py-3 px-4">Ngày ký</th>
                <th className="py-3 px-4">Ngày hết hạn</th>
                <th className="py-3 px-4 text-center">Trạng thái</th>
                <th className="py-3 px-4 text-center">Hành động</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border text-foreground">
              {data.map((item) => (
                <ContractTableRow
                  key={item.id}
                  item={item}
                />
              ))}
              {data.length === 0 && (
                <tr>
                  <td colSpan="8" className="py-16 text-center text-muted-foreground">
                    <div className="space-y-1">
                      <p className="text-sm">Không tìm thấy hợp đồng nào.</p>
                      <p className="text-xs">Thử thay đổi bộ lọc hoặc thêm mới.</p>
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
