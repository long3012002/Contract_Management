import { PROJECT_OPTIONS } from '../../constants/mockBidPackages';

export default function GeneralInfoFields({ form, handleChange, fixedProjectId }) {
  return (
    <div className="space-y-4">
      <div className="space-y-1.5">
        <label className="text-sm font-medium text-foreground">
          Mã gói thầu <span className="text-destructive">*</span>
        </label>
        <input
          type="text"
          required
          value={form.code}
          onChange={(e) => handleChange('code', e.target.value)}
          placeholder="VD: GT-2026-001"
          className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary placeholder:text-muted-foreground"
        />
      </div>

      <div className="space-y-1.5">
        <label className="text-sm font-medium text-foreground">
          Tên gói thầu <span className="text-destructive">*</span>
        </label>
        <input
          type="text"
          required
          value={form.name}
          onChange={(e) => handleChange('name', e.target.value)}
          placeholder="VD: Mua sắm thiết bị..."
          className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary placeholder:text-muted-foreground"
        />
      </div>

      <div className="space-y-1.5">
        <label className="text-sm font-medium text-foreground">
          Dự án triển khai <span className="text-destructive">*</span>
        </label>
        <select
          required
          disabled={!!fixedProjectId}
          value={form.projectId}
          onChange={(e) => handleChange('projectId', e.target.value)}
          className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary disabled:opacity-50 disabled:cursor-not-allowed"
        >
          <option value="" disabled>Chọn dự án...</option>
          {PROJECT_OPTIONS.map((p) => (
            <option key={p.value} value={p.value}>{p.label}</option>
          ))}
        </select>
      </div>
    </div>
  );
}
