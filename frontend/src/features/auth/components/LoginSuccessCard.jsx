import { memo } from 'react';
import Button from '../../../components/Button/Button';

const SuccessIcon = (
  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2.5} stroke="currentColor" className="w-8 h-8">
    <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
  </svg>
);

const LoginSuccessCard = memo(function LoginSuccessCard({ onBack }) {
  return (
    <div className="relative z-10 w-full max-w-[390px] bg-white/90 backdrop-blur-md p-6 rounded-custom border border-white/30 shadow-2xl text-center animate-fade-in">
      <div className="w-16 h-16 bg-emerald-100/80 text-emerald-600 rounded-full flex items-center justify-center mx-auto mb-4">
        {SuccessIcon}
      </div>
      <h2 className="text-2xl font-bold text-slate-900 mb-2 font-display">Đăng nhập thành công!</h2>
      <p className="text-slate-600 text-sm mb-6">Chào mừng quay trở lại hệ thống quản lý dự án đầu tư Co-opBank.</p>
      <Button onClick={onBack}>
        Quay lại trang Đăng nhập
      </Button>
    </div>
  );
});

export default LoginSuccessCard;
