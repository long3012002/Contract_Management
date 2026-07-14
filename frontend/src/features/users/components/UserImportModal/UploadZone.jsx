import React from 'react';
import { Upload, FileText } from 'lucide-react';

export default function UploadZone({
  selectedFile,
  isDragOver,
  onDragOver,
  onDragLeave,
  onDrop,
  onUploadClick,
  fileInputRef,
  onFileChange
}) {
  return (
    <div
      onDragOver={onDragOver}
      onDragLeave={onDragLeave}
      onDrop={onDrop}
      onClick={onUploadClick}
      className={`border-2 border-dashed rounded-custom p-8 text-center cursor-pointer transition-all flex flex-col items-center justify-center min-h-[200px] ${
        isDragOver
          ? 'border-primary bg-primary/5'
          : 'border-border hover:border-primary/50 hover:bg-muted/20'
      }`}
    >
      <input
        type="file"
        ref={fileInputRef}
        onChange={onFileChange}
        accept=".xlsx, .xls"
        className="hidden"
      />
      {selectedFile ? (
        <>
          <div className="p-3 bg-primary/10 rounded-full text-primary mb-3">
            <FileText className="w-10 h-10" />
          </div>
          <p className="font-semibold text-foreground max-w-md truncate">
            {selectedFile.name}
          </p>
          <p className="text-xs text-muted-foreground mt-1">
            {(selectedFile.size / 1024).toFixed(1)} KB
          </p>
          <p className="text-xs text-primary font-medium mt-3 hover:underline">
            Chọn file khác
          </p>
        </>
      ) : (
        <>
          <div className="p-3 bg-muted rounded-full text-muted-foreground mb-3">
            <Upload className="w-10 h-10" />
          </div>
          <p className="font-semibold text-foreground">
            Kéo thả file Excel vào đây, hoặc click để duyệt
          </p>
          <p className="text-xs text-muted-foreground mt-1">
            Chỉ hỗ trợ file Excel (.xlsx, .xls)
          </p>
        </>
      )}
    </div>
  );
}
