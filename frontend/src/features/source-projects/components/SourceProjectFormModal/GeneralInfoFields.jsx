import { YEAR_OPTIONS } from '../../constants/mockSourceProjects';

export default function GeneralInfoFields({ form, handleChange }) {
  return (
    <div className="space-y-4">
      {/* Row: Mã nguồn + Năm */}
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <label className="text-sm font-medium text-foreground">
            Mã nguồn <span className="text-destructive">*</span>
          </label>
          <input
            type="text"
            required
            value={form.code}
            onChange={(e) => handleChange('code', e.target.value)}
            placeholder="VD: NV-2026-001"
            className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary placeholder:text-muted-foreground"
          />
        </div>
        <div className="space-y-1.5">
          <label className="text-sm font-medium text-foreground">
            Năm kế hoạch <span className="text-destructive">*</span>
          </label>
          <select
            required
            value={form.year}
            onChange={(e) => handleChange('year', e.target.value)}
            className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary"
          >
            {YEAR_OPTIONS.map((y) => (
              <option key={y} value={y}>
                {y}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Tên nguồn */}
      <div className="space-y-1.5">
        <label className="text-sm font-medium text-foreground">
          Tên nguồn <span className="text-destructive">*</span>
        </label>
        <input
          type="text"
          required
          value={form.name}
          onChange={(e) => handleChange('name', e.target.value)}
          placeholder="VD: Nâng cấp hệ thống Firewall"
          className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary placeholder:text-muted-foreground"
        />
      </div>
    </div>
  );
}
