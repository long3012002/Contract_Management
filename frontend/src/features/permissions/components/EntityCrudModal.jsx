import React, { useState, useEffect } from 'react';
import { X } from 'lucide-react';
import Button from '@/components/Button/Button';

export default function EntityCrudModal({
  isOpen,
  onClose,
  title,
  initialName = '',
  onSave,
  isLoading
}) {
  const [name, setName] = useState('');
  const [error, setError] = useState('');

  useEffect(() => {
    if (isOpen) {
      setName(initialName);
      setError('');
    }
  }, [isOpen, initialName]);

  if (!isOpen) return null;

  const handleSubmit = (e) => {
    e.preventDefault();
    const trimmed = name.trim();
    if (!trimmed) {
      setError('Tên không được để trống');
      return;
    }
    setError('');
    onSave(trimmed);
  };

  return (
    <div className="fixed inset-0 dialog-overlay flex items-center justify-center p-4 animate-in fade-in duration-200" role="dialog" aria-modal="true" aria-labelledby="crud-modal-title">
      <div className="bg-card border border-border rounded-custom w-full max-w-md overflow-hidden shadow-2xl flex flex-col animate-in zoom-in-95 duration-200">
        
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-border bg-muted/20">
          <h2 id="crud-modal-title" className="font-semibold text-foreground text-base">
            {title}
          </h2>
          <button
            onClick={onClose}
            className="text-muted-foreground hover:text-foreground rounded p-1 hover:bg-muted transition-colors cursor-pointer"
            aria-label="Đóng"
          >
            <X className="w-4.5 h-4.5" />
          </button>
        </div>

        {/* Form Body */}
        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          <div className="space-y-1.5">
            <label htmlFor="entity-name" className="text-sm font-medium text-foreground">
              Tên đối tượng <span className="text-destructive">*</span>
            </label>
            <input
              type="text"
              id="entity-name"
              value={name}
              onChange={(e) => {
                setName(e.target.value);
                if (e.target.value.trim()) setError('');
              }}
              placeholder="Nhập tên..."
              className={`w-full px-3 py-2 text-sm bg-zinc-50 dark:bg-zinc-900 border ${
                error ? 'border-destructive focus:ring-destructive' : 'border-input focus:ring-primary'
              } rounded-md focus:outline-none focus:ring-1 transition-shadow`}
              autoFocus
              disabled={isLoading}
            />
            {error && (
              <p className="text-xs text-destructive mt-1" role="alert">
                {error}
              </p>
            )}
          </div>

          {/* Footer Actions */}
          <div className="flex justify-end gap-3 pt-2">
            <Button
              type="button"
              variant="outline"
              onClick={onClose}
              disabled={isLoading}
              className="w-auto px-4 py-2 cursor-pointer"
            >
              Hủy
            </Button>
            <Button
              type="submit"
              isLoading={isLoading}
              className="w-auto px-4 py-2 cursor-pointer"
            >
              Lưu
            </Button>
          </div>
        </form>

      </div>
    </div>
  );
}
