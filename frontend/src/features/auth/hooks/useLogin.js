import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { loginSchema } from '../../../utils/validationSchemas';
import { useAuthStore } from '../store/authStore';
import { useMutation } from '@tanstack/react-query';
import { toast } from 'sonner';
import { loginApi } from '../api/authApi';

export default function useLogin() {
  const setMfaPending = useAuthStore((state) => state.setMfaPending);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      username: '',
      password: '',
      rememberMe: false,
    },
  });

  const loginMutation = useMutation({
    mutationFn: async ({ username, password }) => {
      return loginApi(username, password);
    },
    onSuccess: (data) => {
      // Backend now returns require2FAVerification or require2FASetup
      if (data.require2FAVerification || data.Require2FAVerification) {
        setMfaPending({
          username: data.username || data.Username,
          accessToken: data.accessToken || data.AccessToken
        }, true);
        toast.info("Xác thực thông tin tài khoản thành công. Vui lòng nhập mã OTP để hoàn tất đăng nhập.");
      } else if (require2FASetup) {
        setMfaPending({
          username: data.username || data.Username,
          qrCodeUrl: data.qrCodeUrl || data.QrCodeUrl,
          twoFactorSecret: data.twoFactorSecret || data.TwoFactorSecret,
          accessToken: data.accessToken || data.AccessToken
        }, false);
        toast.info("Đăng nhập thành công lần đầu. Vui lòng thiết lập xác thực 2 lớp (2FA).");
      } else {
        // Fallback if 2FA is bypassed/not required
        const user = {
          username: data.username || data.Username,
          name: data.username || data.Username,
          accessToken: data.accessToken || data.AccessToken,
          refreshToken: data.refreshToken || data.RefreshToken,
        };
        setMfaPending(user, true);
        toast.info("Đăng nhập thành công.");
      }
    },
    onError: (error) => {
      const errorMessage = error.hasServerMessage ? error.message : 'Tên đăng nhập hoặc mật khẩu không chính xác.';
      toast.error(errorMessage);
    },
  });

  const onSubmit = (data) => {
    loginMutation.mutate(data);
  };

  return {
    register,
    handleSubmit,
    errors,
    error: loginMutation.error ? (loginMutation.error.hasServerMessage ? loginMutation.error.message : 'Tên đăng nhập hoặc mật khẩu không chính xác.') : '',
    isLoading: loginMutation.isPending,
    isSuccess: loginMutation.isSuccess,
    setIsSuccess: (status) => {
      if (!status) {
        loginMutation.reset();
      }
    },
    onSubmit,
  };
}


