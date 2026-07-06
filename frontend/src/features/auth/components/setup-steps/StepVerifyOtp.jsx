import { memo } from 'react';
import Button from '../../../../components/Button/Button';
import OTPInput from '../../../../components/OTPInput/OTPInput';
import { useMFASetupContext } from '../../context/MFASetupContext';

const QrIcon = (
  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.8} stroke="currentColor" className="w-12 h-12 text-primary">
    <path strokeLinecap="round" strokeLinejoin="round" d="M3.75 4.875c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v2.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125v-2.25zM3.75 14.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v2.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125v-2.25zM14.625 3.75a1.125 1.125 0 00-1.125 1.125v2.25c0 .621.504 1.125 1.125 1.125h2.25c.621 0 1.125-.504 1.125-1.125v-2.25a1.125 1.125 0 00-1.125-1.125h-2.25zM14.625 14.625v3.75a1.125 1.125 0 01-1.125 1.125h-3.75a1.125 1.125 0 01-1.125-1.125v-3.75a1.125 1.125 0 011.125-1.125h3.75a1.125 1.125 0 011.125 1.125z" />
  </svg>
);

const StepVerifyOtp = memo(() => {
  const { otpActivation, navigation } = useMFASetupContext();

  return (
    <div className="space-y-5 animate-fade-in">
      <div className="flex justify-center p-3 bg-primary/5 rounded-full w-20 h-20 mx-auto items-center">
        {QrIcon}
      </div>
      <div className="space-y-3">
        <h2 className="text-lg font-bold text-center text-slate-800 font-display">Nhập mã xác nhận kích hoạt</h2>
        <p className="text-sm text-muted-foreground text-center">
          Nhập mã OTP 6 số hiển thị trên ứng dụng xác thực để kiểm tra thiết lập chính xác:
        </p>
      </div>

      <form id="wizard-otp-form" onSubmit={otpActivation.verify} className="space-y-5">
        {otpActivation.error ? (
          <div className="p-3 bg-red-50 border border-destructive/20 text-destructive text-sm rounded-custom flex items-center gap-2 animate-shake">
            <span className="shrink-0">{otpActivation.error}</span>
          </div>
        ) : null}

        {/* 6 horizontal inputs */}
        <OTPInput
          value={otpActivation.otp}
          onChange={otpActivation.setOtp}
          disabled={otpActivation.isLoading}
          error={!!otpActivation.error}
        />

        <div className="flex gap-3 pt-2">
          <button
            type="button"
            onClick={navigation.back}
            disabled={otpActivation.isLoading}
            className="flex-1 py-2.5 px-4 bg-muted hover:bg-slate-200 text-muted-foreground font-semibold rounded-custom transition-all text-sm cursor-pointer"
          >
            Quay lại
          </button>
          <Button
            type="submit"
            className="flex-1"
            isLoading={otpActivation.isLoading}
            loadingText="Đang kích hoạt..."
            disabled={otpActivation.otp.length !== 6}
          >
            Kích hoạt
          </Button>
        </div>
      </form>
    </div>
  );
});

export default StepVerifyOtp;
