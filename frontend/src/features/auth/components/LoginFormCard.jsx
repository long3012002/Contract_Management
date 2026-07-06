import { memo } from 'react';
import logo from '../../../assets/logo/logo_Co-opBank.png';
import Input from '../../../components/Input/Input';
import Button from '../../../components/Button/Button';
import Checkbox from '../../../components/Checkbox/Checkbox';

const ErrorIcon = (
  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" className="w-5 h-5 shrink-0 mt-0.5">
    <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-8-5a.75.75 0 01.75.75v4.5a.75.75 0 01-1.5 0v-4.5A.75.75 0 0110 5zm0 10a1 1 0 100-2 1 1 0 000 2z" clipRule="evenodd" />
  </svg>
);

const UserIcon = (
  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.8} stroke="currentColor" className="w-5 h-5">
    <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z" />
  </svg>
);

const LockIcon = (
  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.8} stroke="currentColor" className="w-5 h-5">
    <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" />
  </svg>
);

const LoginFormCard = memo(function LoginFormCard({
  register,
  handleSubmit,
  errors,
  error,
  isLoading,
  onSubmit,
  handleForgotPassword,
}) {
  return (
    <div className="relative z-10 w-full max-w-[390px] bg-white/90 backdrop-blur-md rounded-custom border border-white/30 shadow-2xl overflow-hidden transition-all duration-300 hover:shadow-slate-950/30">
      <div className="p-6">
        {/* Logo & Branding */}
        <div className="flex flex-col items-center mb-8">
          <img
            src={logo}
            alt="Co-opBank Logo"
            className="h-10 object-contain mb-3"
          />
          <h1 className="text-xl font-semibold text-card-foreground text-center font-display tracking-tight">
            ĐĂNG NHẬP HỆ THỐNG
          </h1>
          <p className="text-xs text-muted-foreground mt-1">
            Quản lý Dự án Đầu tư Co-opBank
          </p>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          {error ? (
            <div className="p-3 bg-red-50 border border-destructive/20 text-destructive text-sm rounded-custom flex items-start gap-2 animate-shake">
              {ErrorIcon}
              <span>{error}</span>
            </div>
          ) : null}

          {/* Username Field */}
          <Input
            id="username"
            type="text"
            label="Tên đăng nhập / Mã nhân viên"
            placeholder="Nhập tên đăng nhập hoặc mã"
            disabled={isLoading}
            error={errors.username?.message}
            autoFocus
            leftIcon={UserIcon}
            {...register('username')}
          />

          {/* Password Field */}
          <Input
            id="password"
            type="password"
            label="Mật khẩu"
            placeholder="Nhập mật khẩu"
            disabled={isLoading}
            error={errors.password?.message}
            leftIcon={LockIcon}
            {...register('password')}
          />

          {/* Remember Me & Forgot Password */}
          <div className="flex items-center justify-between text-sm">
            <Checkbox
              id="rememberMe"
              label="Ghi nhớ đăng nhập"
              disabled={isLoading}
              {...register('rememberMe')}
            />
            <a
              href="#forgot"
              className="text-primary hover:underline font-medium text-xs md:text-sm"
              onClick={handleForgotPassword}
            >
              Quên mật khẩu?
            </a>
          </div>

          {/* Submit Button */}
          <Button
            type="submit"
            isLoading={isLoading}
            loadingText="Đang đăng nhập..."
          >
            Đăng nhập
          </Button>
        </form>
      </div>
    </div>
  );
});

export default LoginFormCard;
