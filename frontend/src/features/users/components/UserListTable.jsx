import Checkbox from '@/components/Checkbox/Checkbox';
import { useConfirm } from '@/components/providers/confirm-dialog-provider';
import UserSearchFilters from './UserSearchFilters';
import UserTableRow from './UserTableRow';
import UserBulkActions from './UserBulkActions';
import UserEditModal from './UserEditModal';
import PaginationWithInput from '@/components/common/PaginationWithInput';
import { useUsersContext } from './UsersContext';

export default function UserListTable() {
  const { confirm } = useConfirm();
  const {
    loading,
    selectedIds,
    setSelectedIds,
    currentPage,
    setCurrentPage,
    filteredUsers,
    paginatedUsers,
    totalPages,
    deleteMutation,
  } = useUsersContext();

  const handleSelectAll = (e) => {
    if (e.target.checked) {
      setSelectedIds(paginatedUsers.map((u) => u.id));
    } else {
      setSelectedIds([]);
    }
  };

  const handleSelectItem = (id, checked) => {
    if (checked) {
      setSelectedIds((prev) => [...prev, id]);
    } else {
      setSelectedIds((prev) => prev.filter((item) => item !== id));
    }
  };

  const handleDeleteClick = async () => {
    if (selectedIds.length === 0) return;
    
    const isConfirmed = await confirm({
      title: "Xóa người dùng?",
      description: (
        <span>
          Bạn có chắc chắn muốn xóa <strong className="text-foreground font-semibold">{selectedIds.length}</strong> người dùng đã chọn không?
          <br />
          <span className="text-xs text-muted-foreground mt-1 inline-block">
            Hành động này không thể hoàn tác.
          </span>
        </span>
      ),
      confirmText: "Xóa",
      cancelText: "Hủy",
      variant: "destructive"
    });

    if (isConfirmed) {
      try {
        await deleteMutation.mutateAsync(selectedIds);
        setSelectedIds([]);
      } catch (err) {
        console.error(err);
      }
    }
  };

  return (
    <div className="space-y-4">
      {/* Search and Filters */}
      <UserSearchFilters />

      {/* Main Table Card */}
      <div className="card-container min-h-[420px]">
        <div className="overflow-x-auto flex-1">
          <table className="w-full text-left text-sm border-collapse table-fixed">
            <thead>
              <tr className="table-header-row">
                <th className="py-3 px-4 w-12 text-center">
                  <Checkbox
                    id="select-all-header"
                    onChange={handleSelectAll}
                    checked={paginatedUsers.length > 0 && selectedIds.length === paginatedUsers.length}
                    disabled={paginatedUsers.length === 0}
                  />
                </th>
                <th className="py-3 px-4 w-[25%]">Họ tên / Username</th>
                <th className="py-3 px-4 w-[22%]">Liên hệ</th>
                <th className="py-3 px-4 w-[22%]">Vai trò</th>
                <th className="py-3 px-4 w-28 text-center">Loại tài khoản</th>
                <th className="py-3 px-4 w-28 text-center">Trạng thái</th>
                <th className="py-3 px-4 w-24 text-center">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border text-foreground">
              {paginatedUsers.map((user) => (
                <UserTableRow
                   key={user.id}
                   user={user}
                   isSelected={selectedIds.includes(user.id)}
                   onSelectChange={handleSelectItem}
                />
              ))}
              
              {filteredUsers.length === 0 && (
                <tr>
                  <td colSpan="7" className="py-12 text-center text-muted-foreground">
                    Không tìm thấy người dùng nào phù hợp.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        {/* Selected Action Floating Bar */}
        <UserBulkActions
          selectedCount={selectedIds.length}
          isDeleting={loading}
          onDelete={handleDeleteClick}
        />
      </div>

      {/* Pagination controls */}
      <PaginationWithInput
        totalItems={filteredUsers.length}
        totalPages={totalPages}
        currentPage={currentPage}
        onPageChange={setCurrentPage}
        itemsPerPage={8}
      />

      {/* Edit User Modal */}
      <UserEditModal />
    </div>
  );
}
