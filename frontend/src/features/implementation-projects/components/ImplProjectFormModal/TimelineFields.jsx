export default function TimelineFields({ form, handleChange }) {
  return (
    <div className="space-y-4">
      {/* Ngày bắt đầu + kết thúc */}
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <label className="text-sm font-medium text-foreground">
            Ngày bắt đầu <span className="text-destructive">*</span>
          </label>
          <input
            type="date"
            required
            value={form.startDate}
            onChange={(e) => handleChange('startDate', e.target.value)}
            className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary"
          />
        </div>
        <div className="space-y-1.5">
          <label className="text-sm font-medium text-foreground">
            Ngày kết thúc <span className="text-destructive">*</span>
          </label>
          <input
            type="date"
            required
            value={form.endDate}
            onChange={(e) => handleChange('endDate', e.target.value)}
            className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary"
          />
        </div>
      </div>

      {/* Mô tả */}
      <div className="space-y-1.5">
        <label className="text-sm font-medium text-foreground">Mô tả</label>
        <textarea
          rows={3}
          value={form.description}
          onChange={(e) => handleChange('description', e.target.value)}
          placeholder="Mô tả chi tiết dự án..."
          className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary placeholder:text-muted-foreground resize-none"
        />
      </div>
    </div>
  );
}
