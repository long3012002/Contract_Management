import { Search, Filter } from 'lucide-react';
import { STATUS_OPTIONS, YEAR_OPTIONS } from '../constants/mockSourceProjects';
import { useSourceProjectsContext } from './SourceProjectsContext';

export default function SourceProjectFilters() {
  const {
    searchTerm,
    setSearchTerm,
    statusFilter,
    setStatusFilter,
    yearFilter,
    setYearFilter,
    setCurrentPage,
  } = useSourceProjectsContext();

  const handleSearchChange = (val) => {
    setSearchTerm(val);
    setCurrentPage(1);
  };

  const handleStatusChange = (val) => {
    setStatusFilter(val);
    setCurrentPage(1);
  };

  const handleYearChange = (val) => {
    setYearFilter(val);
    setCurrentPage(1);
  };

  return (
    <div className="flex flex-col md:flex-row gap-3 items-center justify-between bg-card border border-border p-4 rounded-custom shadow-sm">
      {/* Search */}
      <div className="relative w-full md:w-80">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
        <input
          type="text"
          placeholder="Tìm theo tên hoặc mã nguồn..."
          value={searchTerm}
          onChange={(e) => handleSearchChange(e.target.value)}
          className="w-full pl-9 pr-4 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary transition-all placeholder:text-muted-foreground text-foreground"
        />
      </div>

      {/* Filters */}
      <div className="flex flex-wrap w-full md:w-auto items-center gap-3">
        <div className="flex items-center gap-2 w-full sm:w-auto">
          <Filter className="size-4 text-muted-foreground shrink-0" />
          <select
            value={statusFilter}
            onChange={(e) => handleStatusChange(e.target.value)}
            className="w-full sm:w-40 px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary text-foreground"
          >
            <option value="all">Tất cả trạng thái</option>
            {STATUS_OPTIONS.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
        </div>

        <select
          value={yearFilter}
          onChange={(e) => handleYearChange(e.target.value)}
          className="w-full sm:w-32 px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary text-foreground"
        >
          <option value="all">Tất cả năm</option>
          {YEAR_OPTIONS.map((year) => (
            <option key={year} value={year}>
              {year}
            </option>
          ))}
        </select>
      </div>
    </div>
  );
}
