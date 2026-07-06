import { useState, useCallback } from 'react';
import { toast } from 'sonner';
import { enable2faApi } from '../api/authApi';

export default function useMFAActivation(username, { onSuccess }) {
  const [otp, setOtp] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const verifyCode = useCallback((codeToVerify) => {
    if (!username) return;
    setIsLoading(true);
    setError('');

    enable2faApi(username, codeToVerify)
      .then((data) => {
        setIsLoading(false);
        toast.success('Mã OTP chính xác. Kích hoạt 2FA thành công!');
        onSuccess(data);
      })
      .catch((err) => {
        setIsLoading(false);
        const errMsg = err.hasServerMessage ? err.message : 'Mã xác thực OTP không chính xác hoặc đã hết hạn.';
        setError(errMsg);
        setOtp('');
      });
  }, [username, onSuccess]);

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
    isLoading,
    handleVerify,
  };
}
