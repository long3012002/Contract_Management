import { memo } from 'react';
import useMFAVerify from '../hooks/mfa/useMFAVerify';
import { maskUsername } from '../../../utils/formatters';
import Button from '../../../components/Button/Button';
import OTPInput from '../../../components/OTPInput/OTPInput';
import logo from '../../../assets/logo/logo_Co-opBank.png';

// Hoist static JSX ra module level — tránh tạo mới React element mỗi render (rendering-hoist-jsx)
const ShieldCheckIcon = (
  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.8} stroke="currentColor" className="w-12 h-12 text-primary">
    <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" />
  </svg>
);

const ErrorIcon = (
  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" className="w-5 h-5 shrink-0 mt-0.5">
    <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-8-5a.75.75 0 01.75.75v4.5a.75.75 0 01-1.5 0v-4.5A.75.75 0 0110 5zm0 10a1 1 0 100-2 1 1 0 000 2z" clipRule="evenodd" />
  </svg>
);

// Hoist clock SVG ra module level — SVG này không thay đổi giãa các lần render (rendering-hoist-jsx)
const ClockIcon = (
  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.8} stroke="currentColor" className="w-4.5 h-4.5">
    <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" />
  </svg>
);

const MFAVerifyCard = memo(function MFAVerifyCard() {
  const {
    tempUser,
    otp,
    setOtp,
    error,
    isLoading,
    secondsLeft,
    handleVerify,
    handleContactAdmin,
    cancelMfa,
  } = useMFAVerify();

  return (
    <div className="relative z-10 w-full max-w-[400px] bg-white/90 backdrop-blur-md rounded-custom border border-white/30 shadow-2xl overflow-hidden transition-all duration-300 hover:shadow-slate-950/30">
      <div className="p-7">
        {/* Logo & Branding */}
        <div className="flex flex-col items-center mb-6">
          <img
            src={logo}
            alt="Co-opBank Logo"
            className="h-11 object-contain mb-3"
          />
          <h1 className="text-2xl font-bold text-card-foreground text-center font-display tracking-tight">
            XÁC THỰC BẢO MẬT 2FA
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            Ứng dụng Google Authenticator
          </p>
        </div>

        {/* Shield Icon Decoration */}
        <div className="flex justify-center mb-6">
          <div className="p-4 bg-primary/10 rounded-full">
            {ShieldCheckIcon}
          </div>
        </div>

        {/* Form */}
        <form id="otp-form" onSubmit={handleVerify} className="space-y-6">
          <div className="text-center text-sm text-card-foreground space-y-1">
            <div>
              Mã xác thực đã được gửi về thiết bị của tài khoản:
            </div>
            <div className="font-semibold text-primary text-base">
              {maskUsername(tempUser?.username)}
            </div>
          </div>

          {error ? (
            <div className="p-3 bg-red-50 border border-destructive/20 text-destructive text-sm rounded-custom flex items-start gap-2 animate-shake">
              {ErrorIcon}
              <span>{error}</span>
            </div>
          ) : null}

          {/* OTP Input box (6 horizontal inputs) */}
          <div className="space-y-2.5">
            <label className="block text-sm font-semibold uppercase tracking-wider text-muted-foreground text-center">
              Nhập mã xác thực 6 số
            </label>
            <OTPInput
              value={otp}
              onChange={setOtp}
              disabled={isLoading}
              error={!!error}
            />
          </div>

          {/* Countdown timer */}
          <div className="flex justify-center items-center gap-1.5 text-sm text-muted-foreground">
            {ClockIcon}
            <span>Mã OTP tự động đổi sau: </span>
            <span className={`font-semibold ${secondsLeft <= 8 ? 'text-destructive font-bold' : 'text-primary'}`}>
              {secondsLeft}
            </span>
          </div>

          {/* Buttons */}
          <div className="space-y-2.5">
            <Button
              type="submit"
              isLoading={isLoading}
              loadingText="Đang xác minh..."
              disabled={otp.length !== 6}
            >
              Xác minh
            </Button>

            <button
              type="button"
              onClick={cancelMfa}
              disabled={isLoading}
              className="w-full py-2 text-sm font-medium text-muted-foreground hover:text-card-foreground transition-colors focus:outline-none focus:underline cursor-pointer text-center block"
            >
              Quay lại đăng nhập
            </button>
          </div>

          {/* Support separator */}
          <div className="border-t border-border pt-4 text-center">
            <a
              href="#support"
              onClick={handleContactAdmin}
              className="text-sm text-primary hover:underline font-medium"
            >
              Gặp sự cố với thiết bị OTP? Liên hệ IT Support
            </a>
          </div>
        </form>
      </div>
    </div>
  );
});

export default MFAVerifyCard;
