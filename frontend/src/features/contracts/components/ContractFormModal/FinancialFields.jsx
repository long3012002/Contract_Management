import PaymentPlanFields from './PaymentPlanFields';
import CurrencyInput from '@/components/Input/CurrencyInput';

export default function FinancialFields({ form, handleChange }) {
  const formatCurrency = (val) => {
    if (!val && val !== 0) return '';
    return Number(val).toLocaleString('vi-VN');
  };

  return (
    <div className="space-y-4">
      <div className="space-y-1.5">
        <label className="text-sm font-medium text-foreground">
          Giá trị hợp đồng (VND) <span className="text-destructive">*</span>
        </label>
        <CurrencyInput
          required
          value={form.value}
          onChange={(val) => handleChange('value', val)}
        />
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <label className="text-sm font-medium text-foreground">Ngày ký HĐ</label>
          <input
            type="date"
            value={form.signedDate}
            onChange={(e) => handleChange('signedDate', e.target.value)}
            className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary"
          />
        </div>
        <div className="space-y-1.5">
          <label className="text-sm font-medium text-foreground">Ngày hết hạn HĐ</label>
          <input
            type="date"
            value={form.expiredDate}
            onChange={(e) => handleChange('expiredDate', e.target.value)}
            className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary"
          />
        </div>
      </div>

      <div className="space-y-1.5">
        <label className="text-sm font-medium text-foreground">Nội dung HĐ</label>
        <textarea
          rows={2}
          value={form.description}
          onChange={(e) => handleChange('description', e.target.value)}
          className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary resize-none"
        />
      </div>

      <div className="pt-4 border-t border-border">
        <PaymentPlanFields
          paymentPlans={form.paymentPlans}
          onChange={(newPlans) => handleChange('paymentPlans', newPlans)}
        />
      </div>
    </div>
  );
}
