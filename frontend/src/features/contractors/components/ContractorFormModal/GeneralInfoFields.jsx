import { CONTRACTOR_STATUSES } from '../../constants/mockContractors';

export default function GeneralInfoFields({ form, handleChange, isEditing }) {
  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <label className="text-sm font-medium text-foreground">
            Mã nhà thầu <span className="text-destructive">*</span>
          </label>
          <input
            type="text"
            required
            value={form.code}
            onChange={(e) => handleChange('code', e.target.value)}
            placeholder="VD: NT-2026-001"
            className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary placeholder:text-muted-foreground"
          />
        </div>
        {isEditing && (
          <div className="space-y-1.5">
            <label className="text-sm font-medium text-foreground">
              Trạng thái hoạt động
            </label>
            <select
              value={form.status}
              onChange={(e) => handleChange('status', e.target.value)}
              className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary"
            >
              {CONTRACTOR_STATUSES.map((s) => (
                <option key={s.value} value={s.value}>{s.label}</option>
              ))}
            </select>
          </div>
        )}
      </div>

      <div className="space-y-1.5">
        <label className="text-sm font-medium text-foreground">
          Tên nhà thầu <span className="text-destructive">*</span>
        </label>
        <input
          type="text"
          required
          value={form.name}
          onChange={(e) => handleChange('name', e.target.value)}
          placeholder="Tên doanh nghiệp đầy đủ"
          className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary placeholder:text-muted-foreground"
        />
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <label className="text-sm font-medium text-foreground">
            Mã số thuế <span className="text-destructive">*</span>
          </label>
          <input
            type="text"
            required
            value={form.taxCode}
            onChange={(e) => handleChange('taxCode', e.target.value)}
            className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary placeholder:text-muted-foreground font-mono"
          />
        </div>
        <div className="space-y-1.5">
          <label className="text-sm font-medium text-foreground">
            Liên danh (Nếu có)
          </label>
          <input
            type="text"
            value={form.jointVenture}
            onChange={(e) => handleChange('jointVenture', e.target.value)}
            placeholder="VD: Liên danh ABC - XYZ"
            className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary placeholder:text-muted-foreground"
          />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <label className="text-sm font-medium text-foreground">
            Người đại diện <span className="text-destructive">*</span>
          </label>
          <input
            type="text"
            required
            value={form.representative}
            onChange={(e) => handleChange('representative', e.target.value)}
            className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary"
          />
        </div>
        <div className="space-y-1.5">
          <label className="text-sm font-medium text-foreground">
            Chức vụ đại diện
          </label>
          <input
            type="text"
            value={form.representativePosition}
            onChange={(e) => handleChange('representativePosition', e.target.value)}
            placeholder="VD: Tổng Giám đốc"
            className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary placeholder:text-muted-foreground"
          />
        </div>
      </div>
    </div>
  );
}
