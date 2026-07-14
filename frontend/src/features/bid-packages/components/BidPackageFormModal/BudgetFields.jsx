import CurrencyInput from '@/components/Input/CurrencyInput';

export default function BudgetFields({ form, handleChange }) {
  return (
    <div className="bg-slate-50/50 dark:bg-zinc-900/50 border border-border/80 rounded-lg p-5 space-y-5">
      <div className="grid grid-cols-2 gap-5">
        <div>
          <label className="block text-[11px] font-bold uppercase tracking-wider text-muted-foreground mb-1.5">
            Giá trị dự toán (VND) <span className="text-destructive">*</span>
          </label>
          <CurrencyInput
            required
            value={form.estimatedValue}
            onChange={(val) => handleChange('estimatedValue', val)}
            className="font-mono text-right font-semibold text-base text-primary"
          />
        </div>
        <div>
          <label className="block text-[11px] font-bold uppercase tracking-wider text-muted-foreground mb-1.5">
            Ngưỡng cảnh báo (%) <span className="text-destructive">*</span>
          </label>
          <input
            type="number"
            required
            min="1"
            max="100"
            value={form.warningThresholdPercent}
            onChange={(e) => handleChange('warningThresholdPercent', e.target.value)}
            className="w-full px-3 py-2 text-base font-mono text-right font-semibold bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary tabular-nums [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
          />
        </div>
      </div>

      <div className="pt-2 border-t border-border/50">
        <label className="block text-[11px] font-bold uppercase tracking-wider text-muted-foreground mb-1.5">
          Mô tả
        </label>
        <textarea
          rows={2}
          value={form.description}
          onChange={(e) => handleChange('description', e.target.value)}
          placeholder="Nhập mô tả (nếu có)..."
          className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary placeholder:text-muted-foreground/60 resize-none"
        />
      </div>
    </div>
  );
}
