import { FUND_TYPE_OPTIONS } from '../../constants/mockSourceProjects';
import CurrencyInput from '@/components/Input/CurrencyInput';

export default function FinancialFields({ form, handleChange }) {
  return (
    <div className="bg-slate-50/50 dark:bg-zinc-900/50 border border-border/80 rounded-lg p-5 space-y-5">
      {/* Row: Quyết định + Ngày */}
      <div className="grid grid-cols-2 gap-5">
        <div>
          <label className="block text-[11px] font-bold uppercase tracking-wider text-muted-foreground mb-1.5">
            Số quyết định <span className="text-destructive">*</span>
          </label>
          <input
            type="text"
            required
            value={form.decision}
            onChange={(e) => handleChange('decision', e.target.value)}
            placeholder="VD: QĐ-123/2026"
            className="w-full px-3 py-2 text-sm font-mono bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary placeholder:text-muted-foreground/60 placeholder:font-sans"
          />
        </div>
        <div>
          <label className="block text-[11px] font-bold uppercase tracking-wider text-muted-foreground mb-1.5">
            Ngày quyết định <span className="text-destructive">*</span>
          </label>
          <input
            type="date"
            required
            value={form.decisionDate}
            onChange={(e) => handleChange('decisionDate', e.target.value)}
            className="w-full px-3 py-2 text-sm font-mono bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary"
          />
        </div>
      </div>

      {/* Row: Giá trị + Loại nguồn vốn */}
      <div className="grid grid-cols-2 gap-5 pt-2 border-t border-border/50">
        <div>
          <label className="block text-[11px] font-bold uppercase tracking-wider text-muted-foreground mb-1.5">
            Giá trị (VND) <span className="text-destructive">*</span>
          </label>
          <CurrencyInput
            required
            value={form.value}
            onChange={(val) => handleChange('value', val)}
            className="font-mono text-right font-semibold text-base text-primary"
          />
        </div>
        <div>
          <label className="block text-[11px] font-bold uppercase tracking-wider text-muted-foreground mb-1.5">
            Loại nguồn vốn <span className="text-destructive">*</span>
          </label>
          <select
            required
            value={form.fundType}
            onChange={(e) => handleChange('fundType', e.target.value)}
            className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary"
          >
            {FUND_TYPE_OPTIONS.map((ft) => (
              <option key={ft} value={ft}>
                {ft}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Ghi chú */}
      <div className="pt-2 border-t border-border/50">
        <label className="block text-[11px] font-bold uppercase tracking-wider text-muted-foreground mb-1.5">
          Ghi chú
        </label>
        <textarea
          rows={2}
          value={form.note}
          onChange={(e) => handleChange('note', e.target.value)}
          placeholder="Nhập ghi chú (nếu có)..."
          className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary placeholder:text-muted-foreground/60 resize-none"
        />
      </div>
    </div>
  );
}

