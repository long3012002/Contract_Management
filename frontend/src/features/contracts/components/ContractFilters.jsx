import { Search, Filter } from 'lucide-react';
import { CONTRACT_STATUSES, PROJECT_OPTIONS } from '../constants/mockContracts';
import { useContractsContext } from './ContractsContext';

export default function ContractFilters({ hideProjectFilter = false }) {
  const {
    searchTerm,
    setSearchTerm,
    statusFilter,
    setStatusFilter,
    projectFilter,
    setProjectFilter,
    setCurrentPage,
  } = useContractsContext();

  const handleSearchChange = (val) => {
    setSearchTerm(val);
    setCurrentPage(1);
  };

  const handleStatusChange = (val) => {
    setStatusFilter(val);
    setCurrentPage(1);
  };

  const handleProjectChange = (val) => {
    setProjectFilter(val);
    setCurrentPage(1);
  };

  return (
    <div className="flex flex-col md:flex-row gap-3 items-center justify-between bg-card border border-border p-4 rounded-custom shadow-sm">
      <div className="relative w-full md:w-80">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
        <input
          type="text"
          placeholder="Tìm theo tên HĐ hoặc Số HĐ..."
          value={searchTerm}
          onChange={(e) => handleSearchChange(e.target.value)}
          className="w-full pl-9 pr-4 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary transition-all placeholder:text-muted-foreground text-foreground"
        />
      </div>

      <div className="flex flex-wrap w-full md:w-auto items-center gap-3">
        <div className="flex items-center gap-2 w-full sm:w-auto">
          <Filter className="size-4 text-muted-foreground shrink-0" />
          <select
            value={statusFilter}
            onChange={(e) => handleStatusChange(e.target.value)}
            className="w-full sm:w-40 px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary text-foreground"
          >
            <option value="all">Tất cả trạng thái</option>
            {CONTRACT_STATUSES.map((s) => (
              <option key={s.value} value={s.value}>{s.label}</option>
            ))}
          </select>
        </div>

        {!hideProjectFilter && (
          <select
            value={projectFilter}
            onChange={(e) => handleProjectChange(e.target.value)}
            className="w-full sm:w-64 px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary text-foreground"
          >
            <option value="all">Tất cả dự án</option>
            {PROJECT_OPTIONS.map((p) => (
              <option key={p.value} value={p.value}>{p.label}</option>
            ))}
          </select>
        )}
      </div>
    </div>
  );
}
