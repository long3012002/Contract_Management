import { PROJECT_TYPES } from '../../constants/mockImplProjects';

export default function GeneralInfoFields({ form, handleChange }) {
  return (
    <div className="space-y-4">
      {/* Mã + Loại */}
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <label className="text-sm font-medium text-foreground">
            Mã dự án <span className="text-destructive">*</span>
          </label>
          <input
            type="text"
            required
            value={form.code}
            onChange={(e) => handleChange('code', e.target.value)}
            placeholder="VD: DA-2026-001"
            className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary placeholder:text-muted-foreground"
          />
        </div>
        <div className="space-y-1.5">
          <label className="text-sm font-medium text-foreground">
            Loại dự án <span className="text-destructive">*</span>
          </label>
          <select
            required
            value={form.projectType}
            onChange={(e) => handleChange('projectType', e.target.value)}
            className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary"
          >
            {PROJECT_TYPES.map((t) => (
              <option key={t} value={t}>{t}</option>
            ))}
          </select>
        </div>
      </div>

      {/* Tên */}
      <div className="space-y-1.5">
        <label className="text-sm font-medium text-foreground">
          Tên dự án <span className="text-destructive">*</span>
        </label>
        <input
          type="text"
          required
          value={form.name}
          onChange={(e) => handleChange('name', e.target.value)}
          placeholder="VD: Triển khai Core Banking"
          className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary placeholder:text-muted-foreground"
        />
      </div>

      {/* Đơn vị + Chủ đầu tư */}
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <label className="text-sm font-medium text-foreground">
            Đơn vị <span className="text-destructive">*</span>
          </label>
          <input
            type="text"
            required
            value={form.unit}
            onChange={(e) => handleChange('unit', e.target.value)}
            placeholder="VD: Khối CNTT"
            className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary placeholder:text-muted-foreground"
          />
        </div>
        <div className="space-y-1.5">
          <label className="text-sm font-medium text-foreground">
            Chủ đầu tư <span className="text-destructive">*</span>
          </label>
          <input
            type="text"
            required
            value={form.investor}
            onChange={(e) => handleChange('investor', e.target.value)}
            placeholder="VD: Ban Giám đốc"
            className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary"
          />
        </div>
      </div>
    </div>
  );
}
