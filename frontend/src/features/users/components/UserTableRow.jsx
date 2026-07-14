import React from 'react';
import { Shield, UserCheck, UserX, Edit2 } from 'lucide-react';
import Checkbox from '@/components/Checkbox/Checkbox';
import { useUsersContext } from './UsersContext';

const getInitials = (name) => {
  if (!name) return 'U';
  return name.split(' ').map((n) => n[0]).join('').substring(0, 2).toUpperCase();
};

export default function UserTableRow({ user }) {
  const { selectedIds, setSelectedIds, setEditingUser, setIsEditModalOpen } = useUsersContext();
  const isSelected = selectedIds.includes(user.id);

  const handleSelectItem = (id, checked) => {
    if (checked) {
      setSelectedIds((prev) => [...prev, id]);
    } else {
      setSelectedIds((prev) => prev.filter((item) => item !== id));
    }
  };

  const handleEditClick = () => {
    setEditingUser(user);
    setIsEditModalOpen(true);
  };


  return (
    <tr 
      className={`hover:bg-muted/20 ${isSelected ? 'bg-primary/5 hover:bg-primary/10' : ''}`}
    >
      <td className="py-3 px-4 text-center">
        <Checkbox
          id={`select-user-${user.id}`}
          checked={isSelected}
          onChange={(e) => handleSelectItem(user.id, e.target.checked)}
        />
      </td>
      <td className="py-3 px-4">
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-full bg-primary/10 text-primary font-bold flex items-center justify-center shrink-0 text-xs">
            {getInitials(user.fullName)}
          </div>
          <div className="min-w-0">
            <p className="font-semibold truncate max-w-[180px]">{user.fullName || '(Chưa cập nhật)'}</p>
            <p className="text-xs text-muted-foreground font-mono">@{user.username}</p>
          </div>
        </div>
      </td>
      <td className="py-3 px-4">
        <div className="space-y-0.5">
          <p className="truncate text-xs font-medium">{user.email || '—'}</p>
          <p className="text-xs text-muted-foreground font-mono">{user.phone || '—'}</p>
        </div>
      </td>
      <td className="py-3 px-4">
        <div className="flex flex-wrap gap-1">
          {user.roles && user.roles.length > 0 ? (
            user.roles.map((role, idx) => (
              <span 
                key={idx} 
                className="px-2 py-0.5 text-xs font-medium rounded-full bg-secondary text-secondary-foreground border border-secondary"
              >
                {role}
              </span>
            ))
          ) : (
            <span className="text-xs text-muted-foreground">Không có vai trò</span>
          )}
        </div>
      </td>
      <td className="py-3 px-4 text-center">
        {user.isSystemAdmin ? (
          <span className="badge-primary">
            <Shield className="w-3.5 h-3.5" />
            System Admin
          </span>
        ) : (
          <span className="text-xs text-muted-foreground">Thành viên</span>
        )}
      </td>
      <td className="py-3 px-4 text-center">
        {user.isActive ? (
          <span className="badge-success">
            <UserCheck className="w-3.5 h-3.5" />
            Hoạt động
          </span>
        ) : (
          <span className="badge-destructive">
            <UserX className="w-3.5 h-3.5" />
            Bị khóa
          </span>
        )}
      </td>
      <td className="py-3 px-4 text-center">
        <button
          onClick={handleEditClick}
          className="text-muted-foreground hover:text-primary hover:bg-primary-10 rounded-lg p-1.5 cursor-pointer inline-flex items-center justify-center"
          title="Chỉnh sửa thông tin"
        >
          <Edit2 className="w-4 h-4" />
        </button>
      </td>
    </tr>
  );
}
