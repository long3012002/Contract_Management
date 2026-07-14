import React from 'react';
import { Shield, Save } from 'lucide-react';
import Button from '@/components/Button/Button';

export default function PermissionHeader({ isSaving, onSave }) {
  return (
    <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 className="text-2xl font-bold text-foreground flex items-center gap-2 text-balance">
          <Shield className="w-6 h-6 text-primary" />
          Phân quyền hệ thống
        </h1>
        <p className="text-muted-foreground mt-1 text-sm text-pretty">
          Quản lý quyền truy cập chức năng theo Phòng ban và Chức vụ.
        </p>
      </div>
      
      <div className="flex items-center gap-3">
        <Button 
          onClick={onSave} 
          isLoading={isSaving}
          className="w-auto px-6 py-2 shadow-sm flex items-center gap-2 cursor-pointer"
        >
          {!isSaving && <Save className="w-4 h-4" />}
          Lưu thay đổi
        </Button>
      </div>
    </div>
  );
}
