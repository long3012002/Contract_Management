import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { loginSchema } from '../../../utils/validationSchemas';
import { useAuthStore } from '../store/authStore';
import { useMutation } from '@tanstack/react-query';
import { toast } from 'sonner';

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
      // Simulate API call
      return new Promise((resolve, reject) => {
        setTimeout(() => {
          if (username === 'admin' && password === 'admin123') {
            resolve({ username, name: 'Quản trị viên', mfaSetup: true });
          } else if (username === 'newuser' && password === 'user123') {
            resolve({ username, name: 'Nhân viên Mới', mfaSetup: false });
          } else {
            reject(new Error('Tên đăng nhập hoặc mật khẩu không chính xác.'));
          }
        }, 1500);
      });
    },
    onSuccess: (user) => {
      setMfaPending(user, user.mfaSetup);
      if (user.mfaSetup) {
        toast.info("Xác thực thành công. Vui lòng nhập mã xác thực OTP.");
      } else {
        toast.info("Lần đầu đăng nhập. Vui lòng thiết lập bảo mật 2 lớp (2FA).");
      }
    },
    onError: (error) => {
      toast.error(error.message);
    },
  });

  const onSubmit = (data) => {
    loginMutation.mutate(data);
  };

  return {
    register,
    handleSubmit,
    errors,
    error: loginMutation.error?.message || '',
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

