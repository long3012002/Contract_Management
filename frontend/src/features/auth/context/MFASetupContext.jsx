import { createContext, useContext, useState, useCallback, useMemo } from 'react';
import { toast } from 'sonner';
import { useAuthStore } from '../store/authStore';
import useMFANavigation from '../hooks/mfa/useMFANavigation';
import useMFAActivation from '../hooks/mfa/useMFAActivation';
import useMFAFinish from '../hooks/mfa/useMFAFinish';

const MFASetupContext = createContext(null);

export function MFASetupProvider({ children }) {
  const tempUser = useAuthStore((state) => state.tempUser);
  const cancelMfa = useAuthStore((state) => state.cancelMfa);

  const [verifiedUser, setVerifiedUser] = useState(null);

  const { step, setStep, handleNextStep, handlePrevStep } = useMFANavigation();

  const username = tempUser?.username;
  const secretKey = tempUser?.twoFactorSecret || '';
  const qrCodeUrl = tempUser?.qrCodeUrl || '';

  // Setup sao chép khóa bí mật
  const handleCopyKey = useCallback(() => {
    if (secretKey) {
      navigator.clipboard.writeText(secretKey);
      toast.success('Đã sao chép Secret Key vào bộ nhớ tạm.');
    }
  }, [secretKey]);

  // Kích hoạt MFA OTP
  const { otp, setOtp, error, isLoading, handleVerify } = useMFAActivation(username, {
    onSuccess: (data) => {
      setVerifiedUser({
        username: data.username,
        name: data.username,
        accessToken: data.accessToken,
      });
      setStep(5);
    },
  });

  // Hoàn tất MFA
  const { trustDevice, setTrustDevice, handleFinish } = useMFAFinish(verifiedUser);

  const contextValue = {
    step,
    tempUser,
    navigation: {
      next: handleNextStep,
      back: handlePrevStep,
      cancel: cancelMfa,
    },
    qrSetup: {
      secretKey,
      qrCodeUrl,
      copyKey: handleCopyKey,
    },
    otpActivation: {
      otp,
      setOtp,
      error,
      isLoading,
      verify: handleVerify,
    },
    finalization: {
      trustDevice,
      setTrustDevice,
      finish: handleFinish,
    },
  };

  return (
    <MFASetupContext.Provider value={contextValue}>
      {children}
    </MFASetupContext.Provider>
  );
}

export function useMFASetupContext() {
  const context = useContext(MFASetupContext);
  if (!context) {
    throw new Error('useMFASetupContext must be used within an MFASetupProvider');
  }
  return context;
}
