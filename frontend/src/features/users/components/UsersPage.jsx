import { Users, FileDown, Plus } from 'lucide-react';
import { UsersProvider, useUsersContext } from './UsersContext';
import UserListTable from './UserListTable';
import UserImportModal from './UserImportModal';
import Button from '@/components/Button/Button';
import { toast } from 'sonner';

function UsersPageContent() {
  const {
    users,
    loading,
    isImportModalOpen,
    setIsImportModalOpen,
  } = useUsersContext();

  const handleAddMemberClick = () => {
    toast.info('Chức năng thêm thủ công đang phát triển. Vui lòng sử dụng tính năng Nhập Excel!');
  };

  return (
    <div className="flex flex-col h-full space-y-6 animate-in fade-in slide-in-from-bottom-4 duration-500 pb-10">

      {/* Header section */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground flex items-center gap-2 text-balance">
            <Users className="size-6 text-primary" />
            Đội ngũ nhân sự
          </h1>
          <p className="text-muted-foreground mt-1 text-sm text-pretty">
            Quản lý tài khoản người dùng, vai trò và thông tin phòng ban trong hệ thống.
          </p>
        </div>

        <div className="flex items-center gap-2">
          {/* Import button */}
          <Button
            variant="outline"
            onClick={() => setIsImportModalOpen(true)}
            className="w-auto px-4 py-2 flex items-center gap-2 text-sm cursor-pointer"
          >
            <FileDown className="size-4 text-primary" />
            Nhập Excel
          </Button>

          {/* Add user mock / instruction button */}
          <Button
            onClick={handleAddMemberClick}
            className="w-auto px-4 py-2 flex items-center gap-2 text-sm cursor-pointer"
          >
            <Plus className="size-4" />
            Thêm thành viên
          </Button>
        </div>
      </div>

      {/* Main Content Area */}
      {loading && users.length === 0 ? (
        <div className="flex-1 flex flex-col items-center justify-center min-h-[300px] text-muted-foreground">
          <svg className="animate-spin size-8 text-primary mb-2" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <p className="text-sm">Đang tải danh sách người dùng...</p>
        </div>
      ) : (
        <UserListTable />
      )}

      {/* Import Modal */}
      {isImportModalOpen && <UserImportModal />}

    </div>
  );
}

export default function UsersPage() {
  return (
    <UsersProvider>
      <UsersPageContent />
    </UsersProvider>
  );
}
