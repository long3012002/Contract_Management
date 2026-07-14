export default function ContactFields({ form, handleChange }) {
  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <label className="text-sm font-medium text-foreground">Điện thoại liên hệ</label>
          <input
            type="tel"
            value={form.phone}
            onChange={(e) => handleChange('phone', e.target.value)}
            className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary"
          />
        </div>
        <div className="space-y-1.5">
          <label className="text-sm font-medium text-foreground">Email liên hệ</label>
          <input
            type="email"
            value={form.email}
            onChange={(e) => handleChange('email', e.target.value)}
            className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary"
          />
        </div>
      </div>

      <div className="space-y-1.5">
        <label className="text-sm font-medium text-foreground">Địa chỉ trụ sở</label>
        <textarea
          rows={2}
          value={form.address}
          onChange={(e) => handleChange('address', e.target.value)}
          className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary resize-none"
        />
      </div>

      <div className="border-t border-border pt-4 space-y-3">
        <h4 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Thông tin tài khoản ngân hàng</h4>
        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-1.5">
            <label className="text-sm font-medium text-foreground">Số tài khoản</label>
            <input
              type="text"
              value={form.bankAccount || ''}
              onChange={(e) => handleChange('bankAccount', e.target.value)}
              placeholder="VD: 1200236960"
              className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary placeholder:text-muted-foreground font-mono"
            />
          </div>
          <div className="space-y-1.5">
            <label className="text-sm font-medium text-foreground">Ngân hàng & Chi nhánh</label>
            <input
              type="text"
              value={form.bankName || ''}
              onChange={(e) => handleChange('bankName', e.target.value)}
              placeholder="VD: BIDV - CN Sở Giao dịch 1"
              className="w-full px-3 py-2 text-sm bg-background border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary placeholder:text-muted-foreground"
            />
          </div>
        </div>
      </div>
    </div>
  );
}
