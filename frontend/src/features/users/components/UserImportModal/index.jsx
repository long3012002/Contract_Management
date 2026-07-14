import React, { useState, useRef } from 'react';
import { Upload, X, AlertTriangle } from 'lucide-react';
import Button from '@/components/Button/Button';
import UploadZone from './UploadZone';
import ImportResults from './ImportResults';
import { useUsersContext } from '../UsersContext';

export default function UserImportModal() {
  const {
    isImportModalOpen,
    setIsImportModalOpen,
    importMutation,
    importResult,
    importError,
    resetImportState,
  } = useUsersContext();

  const [selectedFile, setSelectedFile] = useState(null);
  const [isDragOver, setIsDragOver] = useState(false);
  const fileInputRef = useRef(null);

  if (!isImportModalOpen) return null;

  const handleFileChange = (e) => {
    if (e.target.files && e.target.files[0]) {
      setSelectedFile(e.target.files[0]);
    }
  };

  const handleDragOver = (e) => {
    e.preventDefault();
    setIsDragOver(true);
  };

  const handleDragLeave = () => {
    setIsDragOver(false);
  };

  const handleDrop = (e) => {
    e.preventDefault();
    setIsDragOver(false);
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      setSelectedFile(e.dataTransfer.files[0]);
    }
  };

  const handleUploadClick = () => {
    fileInputRef.current?.click();
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!selectedFile) return;

    try {
      await importMutation.mutateAsync(selectedFile);
    } catch (err) {
      console.error(err);
    }
  };

  const handleClose = () => {
    setSelectedFile(null);
    resetImportState();
    setIsImportModalOpen(false);
  };

  return (
    <div className="fixed inset-0 dialog-overlay flex items-center justify-center p-4 animate-in fade-in duration-200">
      <div className="bg-card border border-border rounded-custom w-full max-w-2xl overflow-hidden shadow-2xl flex flex-col max-h-[90vh] animate-in zoom-in-95 duration-200">
        
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-border bg-muted/20">
          <div>
            <h2 className="font-bold text-lg text-foreground flex items-center gap-2">
              <Upload className="w-5 h-5 text-primary" />
              Nhập người dùng từ Excel
            </h2>
            <p className="text-xs text-muted-foreground mt-0.5">
              Hỗ trợ định dạng .xlsx, .xls
            </p>
          </div>
          <button 
            onClick={handleClose} 
            className="text-muted-foreground hover:text-foreground rounded-full p-1.5 hover:bg-muted transition-colors cursor-pointer"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Content */}
        <div className="p-6 overflow-y-auto flex-1 space-y-4">
          
          {importError && (
            <div className="p-4 bg-destructive/10 border border-destructive/20 rounded-md text-destructive text-sm flex items-start gap-3">
              <AlertTriangle className="w-5 h-5 shrink-0 mt-0.5" />
              <div>
                <h4 className="font-semibold">Lỗi liên kết dữ liệu</h4>
                <p className="mt-1">{importError}</p>
              </div>
            </div>
          )}

          {importResult ? (
            <ImportResults importResult={importResult} />
          ) : (
            <UploadZone
              selectedFile={selectedFile}
              isDragOver={isDragOver}
              onDragOver={handleDragOver}
              onDragLeave={handleDragLeave}
              onDrop={handleDrop}
              onUploadClick={handleUploadClick}
              fileInputRef={fileInputRef}
              onFileChange={handleFileChange}
            />
          )}

          {/* Sample template instruction */}
          {!importResult && (
            <div className="p-4 bg-muted/40 rounded-lg text-xs space-y-2 text-muted-foreground border border-border/50">
              <p className="font-semibold text-foreground">📌 Lưu ý cấu trúc file Excel:</p>
              <ul className="list-disc pl-4 space-y-1">
                <li>Bắt buộc có cột <strong>Username</strong>.</li>
                <li>Cột họ tên có thể đặt tên là <strong>Họ tên</strong> hoặc <strong>FullName</strong>.</li>
                <li>Các cột không bắt buộc khác: <strong>Email</strong>, <strong>Số điện thoại</strong>, <strong>Phòng ban</strong>, <strong>Chức vụ</strong>, <strong>Vai trò</strong>, <strong>Trạng thái</strong>, <strong>Admin</strong>.</li>
              </ul>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="px-6 py-4 border-t border-border bg-muted/20 flex items-center justify-end gap-3">
          {importResult ? (
            <Button onClick={handleClose} className="w-auto px-6 py-2">
              Hoàn tất
            </Button>
          ) : (
            <>
              <Button variant="outline" onClick={handleClose} className="w-auto px-5 py-2">
                Hủy bỏ
              </Button>
              <Button
                disabled={!selectedFile}
                isLoading={importMutation.isPending}
                onClick={handleSubmit}
                className="w-auto px-6 py-2"
              >
                Nhập dữ liệu
              </Button>
            </>
          )}
        </div>

      </div>
    </div>
  );
}
