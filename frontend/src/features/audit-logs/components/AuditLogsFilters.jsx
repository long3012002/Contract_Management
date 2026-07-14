import { useState } from 'react';
import { Search, FilterX } from 'lucide-react';
import { Button } from '@/components/ui/button';

export function AuditLogsFilters({ filters, onApply, onClear }) {
  const [localFilters, setLocalFilters] = useState({
    username: filters.username || '',
    date: filters.date || '',
    tableName: filters.tableName || '',
  });

  const handleChange = (e) => {
    const { name, value } = e.target;
    setLocalFilters((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    onApply(localFilters);
  };

  const handleClear = () => {
    setLocalFilters({ username: '', date: '', tableName: '' });
    onClear();
  };

  return (
    <form
      onSubmit={handleSubmit}
      className="flex flex-col sm:flex-row gap-4 items-end bg-card p-4 rounded-lg border border-border mb-6 shadow-sm"
    >
      <div className="flex-1 space-y-1.5 w-full">
        <label
          htmlFor="username"
          className="text-sm font-medium text-foreground block"
        >
          Tên đăng nhập
        </label>
        <div className="relative">
          <Search
            className="absolute left-2.5 top-1/2 -translate-y-1/2 text-muted-foreground"
            size={16}
            aria-hidden="true"
          />
          <input
            type="text"
            id="username"
            name="username"
            value={localFilters.username}
            onChange={handleChange}
            placeholder="Tìm theo tên đăng nhập..."
            className="w-full pl-9 pr-3 py-2 text-sm bg-background border border-input rounded-md focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 transition-colors disabled:cursor-not-allowed disabled:opacity-50"
            autoComplete="off"
          />
        </div>
      </div>

      <div className="flex-1 space-y-1.5 w-full">
        <label
          htmlFor="tableName"
          className="text-sm font-medium text-foreground block"
        >
          Bảng dữ liệu
        </label>
        <select
          id="tableName"
          name="tableName"
          value={localFilters.tableName}
          onChange={handleChange}
          className="w-full px-3 py-2 text-sm bg-background border border-input rounded-md focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 transition-colors disabled:cursor-not-allowed disabled:opacity-50"
        >
          <option value="">Tất cả các bảng</option>
          <option value="Projects">Dự án (Projects)</option>
          <option value="Tasks">Công việc (Tasks)</option>
          <option value="Users">Người dùng (Users)</option>
          <option value="Departments">Phòng ban (Departments)</option>
          <option value="Positions">Chức vụ (Positions)</option>
          <option value="UserRoles">Quyền (Roles)</option>
        </select>
      </div>

      <div className="flex-1 space-y-1.5 w-full">
        <label
          htmlFor="date"
          className="text-sm font-medium text-foreground block"
        >
          Ngày thực hiện
        </label>
        <input
          type="date"
          id="date"
          name="date"
          value={localFilters.date}
          onChange={handleChange}
          className="w-full px-3 py-2 text-sm bg-background border border-input rounded-md focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 transition-colors disabled:cursor-not-allowed disabled:opacity-50"
        />
      </div>

      <div className="flex gap-2 w-full sm:w-auto">
        <Button type="submit" className="flex-1 sm:flex-none">
          Áp dụng bộ lọc
        </Button>
        <Button
          type="button"
          variant="outline"
          onClick={handleClear}
          className="flex-none px-3"
          aria-label="Xóa bộ lọc"
          title="Xóa bộ lọc"
        >
          <FilterX size={16} aria-hidden="true" />
        </Button>
      </div>
    </form>
  );
}
