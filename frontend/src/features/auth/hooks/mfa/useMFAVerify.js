import { useState, useCallback } from 'react';
import { useMutation } from '@tanstack/react-query';
import { useAuthStore } from '../../store/authStore';
import { toast } from 'sonner';
import { verify2faApi } from '../../api/authApi';
import { useOTPTimer } from '../useOTPTimer';
import { mockRbacService } from '../../../../api/mockRbac';

export default function useMFAVerify() {
  const tempUser = useAuthStore((state) => state.tempUser);
  const completeMfa = useAuthStore((state) => state.completeMfa);
  const cancelMfa = useAuthStore((state) => state.cancelMfa);

  const [otp, setOtp] = useState('');
  const [error, setError] = useState('');

  const { secondsLeft } = useOTPTimer();

  const username = tempUser?.username;

  const verify2faMutation = useMutation({
    mutationFn: async (codeToVerify) => {
      if (!username) throw new Error('Username is required');
      return verify2faApi(username, codeToVerify);
    },
    onSuccess: (data) => {
      toast.success('Xác thực OTP thành công!');
      // Thêm trường phân quyền từ backend hoặc mock để demo giao diện
      const userPerms = mockRbacService.calculateUserPermissions(data.username);
      const isSystemAdmin = data.isSystemAdmin ?? userPerms.isSystemAdmin;
      const permissions = data.permissions ?? userPerms.permissions;

      completeMfa({
        username: data.username,
        name: data.username,
        accessToken: data.accessToken,
        isSystemAdmin,
        permissions,
      });
    },
    onError: (err) => {
      const errMsg = err.hasServerMessage ? err.message : 'Mã xác thực OTP không chính xác hoặc đã hết hạn.';
      setError(errMsg);
      setOtp('');
    },
  });

  const verifyCode = useCallback((codeToVerify) => {
    setError('');
    verify2faMutation.mutate(codeToVerify);
  }, [verify2faMutation]);

  const handleVerify = useCallback((e) => {
    if (e && e.preventDefault) e.preventDefault();
    verifyCode(otp);
  }, [otp, verifyCode]);

  // Auto-verify khi nhập đủ 6 ký tự — đặt logic trong event handler
  // thay vì useEffect để tránh chạy lại không cần thiết (rerender-move-effect-to-event)
  const handleSetOtp = useCallback((newOtp) => {
    setOtp(newOtp);
    if (newOtp.length === 6) {
      verifyCode(newOtp);
    }
  }, [verifyCode]);

  const handleContactAdmin = useCallback((e) => {
    e.preventDefault();
    alert('Vui lòng liên hệ Phòng Công nghệ thông tin Co-opBank qua hotline nội bộ: 1900-xxxx (Ext: 888) hoặc gửi email yêu cầu reset 2FA tới: itsupport@co-opbank.com.vn');
  }, []);

  return {
    tempUser,
    otp,
    setOtp: handleSetOtp,
    error,
    setError,
    isLoading: verify2faMutation.isPending,
    secondsLeft,
    handleVerify,
    handleContactAdmin,
    cancelMfa,
  };
}


