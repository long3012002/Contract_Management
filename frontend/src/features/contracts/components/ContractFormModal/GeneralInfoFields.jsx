import { PROJECT_OPTIONS, CONTRACTOR_OPTIONS, CONTRACT_STATUSES } from '../../constants/mockContracts';

export default function GeneralInfoFields({ form, handleChange, isEditing }) {
  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <label className="text-sm font-medium text-foreground">
            Số hợp đồng <span className="text-destructive">*</span>
          </label>
          <input
            type="text"
            required
            value={form.code}
            onChange={(e) => handleChange('code', e.target.value)}
            placeholder="VD: HD-2026-001"
            className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary placeholder:text-muted-foreground"
          />
        </div>
        {isEditing && (
          <div className="space-y-1.5">
            <label className="text-sm font-medium text-foreground">Trạng thái</label>
            <select
              value={form.status}
              onChange={(e) => handleChange('status', e.target.value)}
              className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary"
            >
              {CONTRACT_STATUSES.map((s) => (
                <option key={s.value} value={s.value}>{s.label}</option>
              ))}
            </select>
          </div>
        )}
      </div>

      <div className="space-y-1.5">
        <label className="text-sm font-medium text-foreground">
          Tên hợp đồng <span className="text-destructive">*</span>
        </label>
        <input
          type="text"
          required
          value={form.name}
          onChange={(e) => handleChange('name', e.target.value)}
          className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary"
        />
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <label className="text-sm font-medium text-foreground">
            Dự án <span className="text-destructive">*</span>
          </label>
          <select
            required
            value={form.projectId}
            onChange={(e) => handleChange('projectId', e.target.value)}
            className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary"
          >
            <option value="" disabled>Chọn dự án...</option>
            {PROJECT_OPTIONS.map((p) => (
              <option key={p.value} value={p.value}>{p.label}</option>
            ))}
          </select>
        </div>
        <div className="space-y-1.5">
          <label className="text-sm font-medium text-foreground">
            Nhà thầu / Đối tác <span className="text-destructive">*</span>
          </label>
          <select
            required
            value={form.contractorId}
            onChange={(e) => handleChange('contractorId', e.target.value)}
            className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary"
          >
            <option value="" disabled>Chọn nhà thầu...</option>
            {CONTRACTOR_OPTIONS.map((c) => (
              <option key={c.value} value={c.value}>{c.label}</option>
            ))}
          </select>
        </div>
      </div>
    </div>
  );
}
