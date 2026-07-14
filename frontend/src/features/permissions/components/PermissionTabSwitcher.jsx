import React from 'react';
import { Users, Briefcase } from 'lucide-react';

export default function PermissionTabSwitcher({ activeTab, onTabChange }) {
  return (
    <div className="bg-muted p-1 rounded-lg inline-flex w-full relative" role="tablist" aria-label="Loại đối tượng phân quyền">
      <button
        role="tab"
        aria-selected={activeTab === 'department'}
        aria-controls="entity-list-container"
        className={`flex-1 flex items-center justify-center gap-2 py-2 text-sm font-medium z-10 transition-colors cursor-pointer ${
          activeTab === 'department' ? 'text-primary' : 'text-muted-foreground hover:text-foreground'
        }`}
        onClick={() => onTabChange('department')}
      >
        <Users className="w-4 h-4" />
        Phòng ban
      </button>
      <button
        role="tab"
        aria-selected={activeTab === 'position'}
        aria-controls="entity-list-container"
        className={`flex-1 flex items-center justify-center gap-2 py-2 text-sm font-medium z-10 transition-colors cursor-pointer ${
          activeTab === 'position' ? 'text-primary' : 'text-muted-foreground hover:text-foreground'
        }`}
        onClick={() => onTabChange('position')}
      >
        <Briefcase className="w-4 h-4" />
        Chức vụ
      </button>
      
      {/* Tab Background slider */}
      <div
        className={`absolute top-1 bottom-1 w-[calc(50%-0.25rem)] bg-card rounded-md shadow-sm transition-all duration-300 ease-in-out ${
          activeTab === 'department' ? 'left-1' : 'left-[calc(50%+0.125rem)]'
        }`}
      />
    </div>
  );
}
