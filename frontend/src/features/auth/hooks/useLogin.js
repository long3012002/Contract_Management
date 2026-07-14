import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { loginSchema } from '../../../utils/validationSchemas';
import { useAuthStore } from '../store/authStore';
import { useMutation } from '@tanstack/react-query';
import { toast } from 'sonner';
import { loginApi } from '../api/authApi';

import { mockRbacService } from '../../../api/mockRbac';

export default function useLogin() {
  const setMfaPending = useAuthStore((state) => state.setMfaPending);
  const completeMfa = useAuthStore((state) => state.completeMfa);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      username: '',
      password: '',
    },
  });

  const loginMutation = useMutation({
    mutationFn: async ({ username, password }) => {
      return loginApi(username, password);
    },
    onSuccess: (data) => {
      // Normalize response keys một lần — backend có thể trả về camelCase hoặc PascalCase
      // Tránh check cả 2 dạng ở nhiều điểm trong code (code quality)
      const require2FAVerification = data.require2FAVerification ?? data.Require2FAVerification;
      const require2FASetup = data.require2FASetup ?? data.Require2FASetup;
      const qrCodeUrl = data.qrCodeUrl ?? data.QrCodeUrl;
      const twoFactorSecret = data.twoFactorSecret ?? data.TwoFactorSecret;
      const accessToken = data.accessToken ?? data.AccessToken;

      if (require2FAVerification) {
        setMfaPending({ username: data.username }, true);
        toast.info("Xác thực thông tin tài khoản thành công. Vui lòng nhập mã OTP để hoàn tất đăng nhập.");
      } else if (require2FASetup) {
        setMfaPending({
          username: data.username,
          qrCodeUrl,
          twoFactorSecret,
        }, false);
        toast.info("Đăng nhập thành công lần đầu. Vui lòng thiết lập xác thực 2 lớp (2FA).");
      } else if (accessToken) {
        const userPerms = mockRbacService.calculateUserPermissions(data.username);
        const isSystemAdmin = data.isSystemAdmin ?? data.IsSystemAdmin ?? userPerms.isSystemAdmin;
        const permissions = data.permissions ?? data.Permissions ?? userPerms.permissions;

        completeMfa({
          username: data.username,
          name: data.username,
          accessToken,
          isSystemAdmin,
          permissions,
        });
        toast.success("Bypass đăng nhập Admin thành công!");
      } else {
        // Không chấp nhận đăng nhập thẳng bypass 2FA
        toast.error("Hệ thống yêu cầu xác thực 2 lớp. Vui lòng liên hệ Quản trị viên.");
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

  const handleBypassLogin = () => {
    loginMutation.mutate({ username: 'admin', password: 'admin_bypass_dev' });
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
    handleBypassLogin,
  };
}


