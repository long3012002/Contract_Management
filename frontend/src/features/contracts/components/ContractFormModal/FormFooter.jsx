import Button from '@/components/Button/Button';

export default function FormFooter({ onClose, isEditing }) {
  return (
    <div className="flex items-center justify-end gap-3 pt-4 border-t border-border sticky bottom-0 bg-card">
      <Button type="button" variant="outline" onClick={onClose} className="w-auto px-4 py-2 text-sm cursor-pointer">
        Huỷ
      </Button>
      <Button type="submit" className="w-auto px-4 py-2 text-sm cursor-pointer">
        {isEditing ? 'Lưu thay đổi' : 'Tạo hợp đồng'}
      </Button>
    </div>
  );
}
