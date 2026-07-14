import React from 'react';
import { Trash2 } from 'lucide-react';
import Button from '@/components/Button/Button';

export default function UserBulkActions({ selectedCount, isDeleting, onDelete }) {
  if (selectedCount === 0) return null;

  return (
    <div className="absolute bottom-0 left-0 right-0 bg-card border-t border-border px-6 py-3 flex items-center justify-between shadow-lg animate-in slide-in-from-bottom-2 z-10">
      <span className="text-sm font-semibold text-foreground">
        Đã chọn <span className="text-primary font-bold">{selectedCount}</span> người dùng
      </span>
      <Button
        variant="destructive"
        isLoading={isDeleting}
        loadingText="Đang xóa..."
        onClick={onDelete}
        className="w-auto px-4 py-1.5 text-xs shadow-sm flex items-center gap-1.5 cursor-pointer"
      >
        <Trash2 className="w-4 h-4" />
        Xóa người dùng đã chọn
      </Button>
    </div>
  );
}
