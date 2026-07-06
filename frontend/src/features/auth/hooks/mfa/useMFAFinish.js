import { useState, useCallback } from 'react';
import { useAuthStore } from '../../store/authStore';

export default function useMFAFinish(verifiedUser) {
  const completeMfa = useAuthStore((state) => state.completeMfa);
  const [trustDevice, setTrustDevice] = useState(false);

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
    trustDevice,
    setTrustDevice,
    handleFinish,
  };
}
