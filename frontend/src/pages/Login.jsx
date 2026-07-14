import { useCallback, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import logoBg from '../assets/logo/logo1.png';
import useLogin from '../features/auth/hooks/useLogin';
import LoginSuccessCard from '../features/auth/components/LoginSuccessCard';
import LoginFormCard from '../features/auth/components/LoginFormCard';
import MFAVerifyCard from '../features/auth/components/MFAVerifyCard';
import MFASetupWizard from '../features/auth/components/MFASetupWizard';
import { useAuthStore } from '../features/auth/store/authStore';

export default function Login() {
  const navigate = useNavigate();
  const {
    register,
    handleSubmit,
    errors,
    error,
    isLoading,
    onSubmit,
    handleBypassLogin,
  } = useLogin();

  // Granular Zustand subscriptions — mỗi selector độc lập
  // Component chỉ re-render khi đúng field đó thay đổi (rerender-defer-reads)
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);

  useEffect(() => {
    if (isAuthenticated) {
      navigate('/dashboard');
    }
  }, [isAuthenticated, navigate]);

  const mfaPending = useAuthStore((state) => state.mfaPending);
  const isMfaSetup = useAuthStore((state) => state.isMfaSetup);
  const logout = useAuthStore((state) => state.logout);


  return (
    <div
      style={{ backgroundImage: `url(${logoBg})` }}
      className="min-h-dvh flex items-center justify-center bg-cover bg-center bg-no-repeat p-4 font-sans relative overflow-hidden"
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
          handleBypassLogin={handleBypassLogin}
        />
      )}
    </div>
  );
}

