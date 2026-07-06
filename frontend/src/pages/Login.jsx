import { useCallback } from 'react';
import logoBg from '../assets/logo/logo1.png';
import useLogin from '../features/auth/hooks/useLogin';
import LoginSuccessCard from '../features/auth/components/LoginSuccessCard';
import LoginFormCard from '../features/auth/components/LoginFormCard';
import MFAVerifyCard from '../features/auth/components/MFAVerifyCard';
import MFASetupWizard from '../features/auth/components/MFASetupWizard';
import { useAuthStore } from '../features/auth/store/authStore';

export default function Login() {
  const {
    register,
    handleSubmit,
    errors,
    error,
    isLoading,
    onSubmit,
  } = useLogin();

  const { isAuthenticated, mfaPending, isMfaSetup, logout } = useAuthStore();

  const handleForgotPassword = useCallback((e) => {
    e.preventDefault();
    alert('Vui lòng liên hệ Quản trị viên hệ thống Co-opBank để được cấp lại mật khẩu.');
  }, []);

  return (
    <div
      style={{ backgroundImage: `url(${logoBg})` }}
      className="min-h-screen flex items-center justify-center bg-cover bg-center bg-no-repeat p-4 font-sans relative overflow-hidden"
    >
      <div className="absolute inset-0 bg-gradient-to-tr from-primary/85 to-slate-950/85 z-0"></div>

      {/* Subtle background visual rings */}
      <div className="absolute top-[10%] right-[15%] w-72 h-72 rounded-full border border-white/5 pointer-events-none z-0"></div>
      <div className="absolute bottom-[15%] left-[10%] w-96 h-96 rounded-full border border-white/5 pointer-events-none z-0"></div>

      {isAuthenticated ? (
        <LoginSuccessCard onBack={logout} />
      ) : mfaPending ? (
        isMfaSetup ? (
          <MFAVerifyCard />
        ) : (
          <MFASetupWizard />
        )
      ) : (
        <LoginFormCard
          register={register}
          handleSubmit={handleSubmit}
          errors={errors}
          error={error}
          isLoading={isLoading}
          onSubmit={onSubmit}
          handleForgotPassword={handleForgotPassword}
        />
      )}
    </div>
  );
}

