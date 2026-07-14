import { memo } from 'react';
import Button from '../../../../components/Button/Button';
import ggAuthIcon from '../../../../assets/icon/gg_auth.webp';
import { useMFASetupContext } from '../../context/MFASetupContext';

const MobileIcon = (
  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.8} stroke="currentColor" className="w-12 h-12 text-primary">
    <path strokeLinecap="round" strokeLinejoin="round" d="M10.5 1.5H8.25A2.25 2.25 0 006 3.75v16.5a2.25 2.25 0 002.25 2.25h7.5A2.25 2.25 0 0018 20.25V3.75a2.25 2.25 0 00-2.25-2.25H13.5m-3 0V3h3V1.5m-3 0h3m-3 18.75h3" />
  </svg>
);

const StepDownloadApp = memo(function StepDownloadApp() {
  const { navigation } = useMFASetupContext();

  return (
    <div className="space-y-5 animate-fade-in">
      <div className="flex justify-center p-3 bg-primary/5 rounded-full w-20 h-20 mx-auto items-center">
        {MobileIcon}
      </div>
      <div className="space-y-3">
        <h2 className="text-lg font-bold text-center text-slate-800 font-display">Tải Google Authenticator</h2>
        <p className="text-sm text-muted-foreground leading-relaxed text-center">
          Vui lòng tải xuống ứng dụng xác thực chính thức trên thiết bị di động của bạn để tiếp tục:
        </p>
        <div className="bg-slate-50 border border-border p-4 rounded-custom">
          <div className="flex items-center gap-3.5">
            <img
              src={ggAuthIcon}
              alt="Google Authenticator Logo"
              className="w-10 h-10 shrink-0 object-contain"
            />
            <div className="flex-1">
              <p className="text-sm font-bold text-slate-800">Google Authenticator</p>
              <p className="text-xs text-muted-foreground leading-normal mt-0.5">
                Ứng dụng tạo mã OTP tự động sau mỗi 30 giây, hoạt động ngoại tuyến bảo mật tuyệt đối.
              </p>
            </div>
          </div>
        </div>
      </div>
      <div className="flex gap-3 pt-2">
        <button
          type="button"
          onClick={navigation.back}
          className="flex-1 py-2.5 px-4 bg-muted hover:bg-slate-200 text-muted-foreground font-semibold rounded-custom transition-all text-sm cursor-pointer"
        >
          Quay lại
        </button>
        <Button className="flex-1" onClick={navigation.next}>Tôi đã cài đặt</Button>
      </div>
    </div>
  );
});

export default StepDownloadApp;
