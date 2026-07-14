import { memo } from 'react';
import Button from '../../../../components/Button/Button';
import { useMFASetupContext } from '../../context/MFASetupContext';

const SuccessIcon = (
  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2.5} stroke="currentColor" className="w-8 h-8">
    <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
  </svg>
);

const StepSuccess = memo(function StepSuccess() {
  const { finalization } = useMFASetupContext();

  return (
    <div className="space-y-5 text-center animate-fade-in">
      <div className="w-16 h-16 bg-emerald-100/80 text-emerald-600 rounded-full flex items-center justify-center mx-auto">
        {SuccessIcon}
      </div>
      <div className="space-y-2">
        <h2 className="text-xl font-bold text-slate-900 font-display">Bật bảo mật 2 lớp thành công!</h2>
        <p className="text-sm text-muted-foreground leading-relaxed px-2">
          Tài khoản của bạn đã được kết nối an toàn với ứng dụng Authenticator. Từ những lần đăng nhập sau, bạn sẽ dùng mã này để truy cập.
        </p>
      </div>

      <div className="bg-slate-50 border border-border p-3.5 rounded-custom flex items-center justify-center">
        <label className="flex items-center gap-2 cursor-pointer select-none">
          <input
            type="checkbox"
            checked={finalization.trustDevice}
            onChange={(e) => finalization.setTrustDevice(e.target.checked)}
            className="rounded border-input text-primary focus:ring-primary w-4.5 h-4.5"
          />
          <span className="text-sm text-slate-700 font-medium">Tin cậy thiết bị này trong 30 ngày</span>
        </label>
      </div>

      <Button onClick={finalization.finish}>
        Vào Dashboard
      </Button>
    </div>
  );
});

export default StepSuccess;
