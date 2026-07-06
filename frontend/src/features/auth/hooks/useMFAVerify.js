import { useState, useEffect, useCallback } from 'react';
import { useAuthStore } from '../store/authStore';
import { toast } from 'sonner';

// Mask username: admin -> a***n, newuser -> n*****r
export const maskUsername = (username) => {
  if (!username) return '***';
  if (username.length <= 2) return username;
  return username[0] + '*'.repeat(username.length - 2) + username[username.length - 1];
};

export default function useMFAVerify() {
  const { tempUser, completeMfa, cancelMfa } = useAuthStore();
  const [otp, setOtp] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [secondsLeft, setSecondsLeft] = useState(30);

  // Countdown timer
  useEffect(() => {
    if (secondsLeft <= 0) {
      setSecondsLeft(30);
      return;
    }
    const timer = setTimeout(() => {
      setSecondsLeft((prev) => prev - 1);
    }, 1000);
    return () => clearTimeout(timer);
  }, [secondsLeft]);

  const verifyCode = useCallback((codeToVerify) => {
    setIsLoading(true);
    setError('');

    // Simulate OTP verification API call
    setTimeout(() => {
      if (codeToVerify === '123456') {
        setIsLoading(false);
        toast.success('Xác thực OTP thành công!');
        completeMfa(tempUser);
      } else {
        setIsLoading(false);
        setError('Mã xác thực OTP không chính xác hoặc đã hết hạn.');
        setOtp('');
      }
    }, 1200);
  }, [tempUser, completeMfa]);

  const handleVerify = useCallback((e) => {
    if (e && e.preventDefault) e.preventDefault();
    verifyCode(otp);
  }, [otp, verifyCode]);

  // Auto-verify when otp length reaches 6
  useEffect(() => {
    if (otp.length === 6) {
      verifyCode(otp);
    }
  }, [otp, verifyCode]);

  const handleContactAdmin = useCallback((e) => {
    e.preventDefault();
    alert('Vui lòng liên hệ Phòng Công nghệ thông tin Co-opBank qua hotline nội bộ: 1900-xxxx (Ext: 888) hoặc gửi email yêu cầu reset 2FA tới: itsupport@co-opbank.com.vn');
  }, []);

  return {
    tempUser,
    otp,
    setOtp,
    error,
    setError,
    isLoading,
    secondsLeft,
    handleVerify,
    handleContactAdmin,
    cancelMfa,
  };
}

