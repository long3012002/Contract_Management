import ContractorFilters from './ContractorFilters';
import ContractorTableRow from './ContractorTableRow';
import PaginationWithInput from '@/components/common/PaginationWithInput';
import { useContractorsContext } from './ContractorsContext';

export default function ContractorListTable() {
  const {
    data,
    totalItems,
    totalPages,
    currentPage,
    setCurrentPage,
  } = useContractorsContext();

  return (
    <div className="space-y-4">
      <ContractorFilters />

      <div className="bg-card border border-border rounded-custom overflow-hidden shadow-sm relative min-h-[420px] flex flex-col">
        <div className="overflow-x-auto flex-1">
          <table className="w-full text-left text-sm border-collapse">
            <thead>
              <tr className="bg-secondary border-b-2 border-border text-xs uppercase tracking-wider text-secondary-foreground font-semibold">
                <th className="py-3 px-4">Mã NT</th>
                <th className="py-3 px-4 min-w-[240px]">Tên nhà thầu / MST</th>
                <th className="py-3 px-4">Đại diện</th>
                <th className="py-3 px-4">Liên hệ</th>
                <th className="py-3 px-4 text-center">Số HĐ</th>
                <th className="py-3 px-4 text-center">Trạng thái</th>
                <th className="py-3 px-4 text-center">Hành động</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border text-foreground">
              {data.map((item) => (
                <ContractorTableRow key={item.id} item={item} />
              ))}
              {data.length === 0 && (
                <tr>
                  <td colSpan="7" className="py-16 text-center text-muted-foreground">
                    <div className="space-y-1">
                      <p className="text-sm">Không tìm thấy nhà thầu nào.</p>
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
