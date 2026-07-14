import { memo } from 'react';
import Button from '../../../../components/Button/Button';
import { useMFASetupContext } from '../../context/MFASetupContext';

const SecurityIcon = (
  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.8} stroke="currentColor" className="w-12 h-12 text-primary">
    <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" />
  </svg>
);

const StepIntro = memo(function StepIntro() {
  const { navigation } = useMFASetupContext();

  return (
    <div className="space-y-5 animate-fade-in">
      <div className="flex justify-center p-3 bg-primary/5 rounded-full w-20 h-20 mx-auto items-center">
        {SecurityIcon}
      </div>
      <div className="text-center space-y-2">
        <h2 className="text-lg font-bold text-slate-800 font-display">Tăng cường bảo mật tài khoản</h2>
        <p className="text-sm text-muted-foreground leading-relaxed">
          Để bảo vệ thông tin dự án đầu tư Co-opBank và tuân thủ các quy định bảo mật hệ thống ngân hàng, tài khoản của bạn bắt buộc phải thiết lập <strong>Mã xác thực 2 lớp (MFA/2FA)</strong>.
        </p>
      </div>
      <div className="space-y-2.5 pt-2">
        <Button onClick={navigation.next}>Bắt đầu thiết lập</Button>
        <button
          type="button"
          onClick={navigation.cancel}
          className="w-full text-center text-sm font-medium text-muted-foreground hover:text-card-foreground py-1.5 focus:outline-none cursor-pointer"
        >
          Hủy bỏ và quay lại
        </button>
      </div>
    </div>
  );
});

export default StepIntro;
