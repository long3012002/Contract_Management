import React from 'react';
import { Plus, Edit2, Trash2, Shield } from 'lucide-react';

export default function PermissionEntityList({
  activeTab,
  entities = [],
  selectedEntityId,
  onSelectEntity,
  onAddClick,
  onEditClick,
  onDeleteClick
}) {
  const title = activeTab === 'department' ? 'Danh sách Phòng ban' : 'Danh sách Chức vụ';
  const label = activeTab === 'department' ? 'phòng ban mới' : 'chức vụ mới';

  return (
    <div className="bg-card border border-border rounded-custom overflow-hidden shadow-sm" id="entity-list-container">
      <div className="p-4 border-b border-border bg-muted/30 flex items-center justify-between">
        <h3 className="font-semibold text-sm text-foreground text-balance">
          {title}
        </h3>
        <button
          onClick={onAddClick}
          className="flex items-center gap-1.5 px-2.5 py-1.5 text-xs font-semibold text-primary bg-primary/10 hover:bg-primary/20 rounded-md transition-colors cursor-pointer"
          aria-label={`Thêm ${label}`}
        >
          <Plus className="w-3.5 h-3.5" />
          <span>Thêm</span>
        </button>
      </div>
      <ul className="max-h-[500px] overflow-y-auto p-2 list-none m-0">
        {entities.map(entity => {
          const name = entity.tenPhongBan || entity.tenChucVu || entity.name;
          const isSelected = selectedEntityId === entity.id;
          
          return (
            <li key={entity.id} className="group relative mb-1">
              <button
                onClick={() => onSelectEntity(entity.id)}
                className={`w-full text-left pl-3 pr-14 py-2.5 rounded-md text-sm transition-all flex items-center gap-2 ${
                  isSelected 
                    ? 'bg-primary/10 text-primary font-medium' 
                    : 'text-foreground hover:bg-muted'
                }`}
              >
                <div className="w-1.5 h-1.5 flex items-center justify-center shrink-0" aria-hidden="true">
                  {isSelected && (
                    <div className="w-1.5 h-1.5 rounded-full bg-primary" />
                  )}
                </div>
                <span className="truncate">{name}</span>
              </button>
              
              {/* Hover actions */}
              <div className={`absolute right-2 top-1/2 -translate-y-1/2 hidden group-hover:flex items-center gap-0.5 p-0.5 rounded-md border border-border/40 shadow-sm ${
                isSelected ? 'bg-sky-50 dark:bg-zinc-900' : 'bg-white dark:bg-zinc-800'
              }`}>
                <button
                  onClick={(e) => {
                    e.stopPropagation();
                    onEditClick(entity);
                  }}
                  className="p-1 text-muted-foreground hover:text-foreground hover:bg-zinc-100 dark:hover:bg-zinc-700 rounded transition-colors cursor-pointer"
                  aria-label="Sửa"
                >
                  <Edit2 className="w-3 h-3" />
                </button>
                <button
                  onClick={(e) => {
                    e.stopPropagation();
                    onDeleteClick(entity);
                  }}
                  className="p-1 text-muted-foreground hover:text-destructive hover:bg-zinc-100 dark:hover:bg-zinc-700 rounded transition-colors cursor-pointer"
                  aria-label="Xóa"
                >
                  <Trash2 className="w-3 h-3" />
                </button>
              </div>
            </li>
          );
        })}
        
        {entities.length === 0 && (
          <li className="p-8 text-center text-sm text-muted-foreground flex flex-col items-center justify-center gap-2">
            <Shield className="w-8 h-8 opacity-20" aria-hidden="true" />
            <span>Không có dữ liệu</span>
          </li>
        )}
      </ul>
    </div>
  );
}
