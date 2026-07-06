import { useState, useEffect, useCallback } from 'react';
import { useAuthStore } from '../store/authStore';
import { toast } from 'sonner';
import { enable2faApi } from '../api/authApi';

export default function useMFASetup() {
  const { tempUser, completeMfa, cancelMfa } = useAuthStore();
  const [step, setStep] = useState(1);
  const [otp, setOtp] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [trustDevice, setTrustDevice] = useState(false);
  const [verifiedUser, setVerifiedUser] = useState(null);

  const secretKey = tempUser?.twoFactorSecret || '';
  const qrCodeUrl = tempUser?.qrCodeUrl || '';

  const handleNextStep = useCallback(() => {
    setStep((prev) => prev + 1);
  }, []);

  const handlePrevStep = useCallback(() => {
    setStep((prev) => prev - 1);
  }, []);

  const handleCopyKey = useCallback(() => {
    if (secretKey) {
      navigator.clipboard.writeText(secretKey);
      toast.success('Đã sao chép Secret Key vào bộ nhớ tạm.');
    }
  }, [secretKey]);

  const verifyCode = useCallback((codeToVerify) => {
    setIsLoading(true);
    setError('');

    enable2faApi(tempUser?.username, codeToVerify)
      .then((data) => {
        setIsLoading(false);
        toast.success('Mã OTP chính xác. Kích hoạt 2FA thành công!');
        setVerifiedUser({
          username: data.username,
          name: data.username,
          accessToken: data.accessToken,
          refreshToken: data.refreshToken,
        });
        setStep(5);
      })
      .catch((err) => {
        setIsLoading(false);
        const errMsg = err.response?.data?.message || err.message || 'Mã xác thực OTP không chính xác hoặc đã hết hạn.';
        setError(errMsg);
        setOtp('');
      });
  }, [tempUser]);

  const handleVerify = useCallback((e) => {
    if (e && e.preventDefault) e.preventDefault();
    verifyCode(otp);
  }, [otp, verifyCode]);

  // Auto-verify when otp length reaches 6 in Step 4
  useEffect(() => {
    if (step === 4 && otp.length === 6) {
      verifyCode(otp);
    }
  }, [otp, step, verifyCode]);

  const handleFinish = useCallback(() => {
    if (verifiedUser) {
      completeMfa({
        ...verifiedUser,
        mfaSetup: true,
        trustedDevice: trustDevice,
      });
    }
  }, [completeMfa, verifiedUser, trustDevice]);


  return {
    tempUser,
    step,
    otp,
    setOtp,
    error,
    isLoading,
    trustDevice,
    secretKey,
    qrCodeUrl,
    setTrustDevice,
    handleNextStep,
    handlePrevStep,
    handleCopyKey,
    handleVerify,
    handleFinish,
    cancelMfa,
  };
}

