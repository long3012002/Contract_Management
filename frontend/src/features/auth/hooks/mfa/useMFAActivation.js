import { useState, useCallback } from 'react';
import { useMutation } from '@tanstack/react-query';
import { toast } from 'sonner';
import { enable2faApi } from '../../api/authApi';

export default function useMFAActivation(username, { onSuccess }) {
  const [otp, setOtp] = useState('');
  const [error, setError] = useState('');

  const enable2faMutation = useMutation({
    mutationFn: async (codeToVerify) => {
      if (!username) throw new Error('Username is required');
      return enable2faApi(username, codeToVerify);
    },
    onSuccess: (data) => {
      toast.success('Mã OTP chính xác. Kích hoạt 2FA thành công!');
      onSuccess(data);
    },
    onError: (err) => {
      const errMsg = err.hasServerMessage ? err.message : 'Mã xác thực OTP không chính xác hoặc đã hết hạn.';
      setError(errMsg);
      setOtp('');
    },
  });

  const verifyCode = useCallback((codeToVerify) => {
    setError('');
    enable2faMutation.mutate(codeToVerify);
  }, [enable2faMutation]);

  const handleVerify = useCallback((e) => {
    if (e && e.preventDefault) e.preventDefault();
    verifyCode(otp);
  }, [otp, verifyCode]);

  const handleSetOtp = useCallback((newOtp) => {
    setOtp(newOtp);
    if (newOtp.length === 6) {
      verifyCode(newOtp);
    }
  }, [verifyCode]);

  return {
    otp,
    setOtp: handleSetOtp,
    error,
    isLoading: enable2faMutation.isPending,
    handleVerify,
  };
}

