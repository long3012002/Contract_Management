import { useState, useEffect, useCallback } from 'react';
import { useAuthStore } from '../store/authStore';
import { toast } from 'sonner';

export const secretKey = 'COOP BANK PROJ SECR KEY 2026';

export default function useMFASetup() {
  const { tempUser, completeMfa, cancelMfa } = useAuthStore();
  const [step, setStep] = useState(1);
  const [otp, setOtp] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [trustDevice, setTrustDevice] = useState(false);

  const handleNextStep = useCallback(() => {
    setStep((prev) => prev + 1);
  }, []);

  const handlePrevStep = useCallback(() => {
    setStep((prev) => prev - 1);
  }, []);

  const handleCopyKey = useCallback(() => {
    navigator.clipboard.writeText(secretKey);
    toast.success('Đã sao chép Secret Key vào bộ nhớ tạm.');
  }, []);

  const verifyCode = useCallback((codeToVerify) => {
    setIsLoading(true);
    setError('');

    // Simulate OTP verification API call
    setTimeout(() => {
      if (codeToVerify === '123456') {
        setIsLoading(false);
        toast.success('Mã OTP chính xác. Kích hoạt 2FA thành công!');
        setStep(5);
      } else {
        setIsLoading(false);
        setError('Mã xác thực OTP không chính xác hoặc đã hết hạn.');
        setOtp('');
      }
    }, 1200);
  }, []);

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
    completeMfa({
      ...tempUser,
      mfaSetup: true,
      trustedDevice: trustDevice,
    });
  }, [completeMfa, tempUser, trustDevice]);

  return {
    tempUser,
    step,
    otp,
    setOtp,
    error,
    isLoading,
    trustDevice,
    secretKey,
    setTrustDevice,
    handleNextStep,
    handlePrevStep,
    handleCopyKey,
    handleVerify,
    handleFinish,
    cancelMfa,
  };
}

