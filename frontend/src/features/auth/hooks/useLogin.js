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
      // Save tokens temporarily, or prepare them for after MFA completion
      const user = {
        username: data.username,
        name: data.username,
        accessToken: data.accessToken,
        refreshToken: data.refreshToken,
      };

      // Since they successfully authenticated, they proceed to 2FA verification.
      // We assume MFA is already set up (mfaSetup: true) for the user.
      setMfaPending(user, true);
      toast.info("Xác thực thông tin tài khoản thành công. Vui lòng nhập mã OTP để hoàn tất đăng nhập.");
    },
    onError: (error) => {
      const errorMessage = error.response?.data?.message || error.message || 'Tên đăng nhập hoặc mật khẩu không chính xác.';
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
    error: loginMutation.error?.response?.data?.message || loginMutation.error?.message || '',
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


