import Button from '@/components/Button/Button';

export default function FormFooter({ onClose, isEditing }) {
  return (
    <div className="flex items-center justify-end gap-3 pt-2">
      <Button
        type="button"
        variant="outline"
        onClick={onClose}
        className="w-auto px-4 py-2 text-sm cursor-pointer"
      >
        Huỷ
      </Button>
      <Button
        type="submit"
        className="w-auto px-4 py-2 text-sm cursor-pointer"
      >
        {isEditing ? 'Lưu thay đổi' : 'Thêm nguồn vốn'}
      </Button>
    </div>
  );
}
