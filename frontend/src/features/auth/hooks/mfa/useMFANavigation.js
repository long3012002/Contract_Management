import { useState, useCallback } from 'react';

export default function useMFANavigation() {
  const [step, setStep] = useState(1);

  const handleNextStep = useCallback(() => {
    setStep((prev) => prev + 1);
  }, []);

  const handlePrevStep = useCallback(() => {
    setStep((prev) => prev - 1);
  }, []);

  return {
    step,
    setStep,
    handleNextStep,
    handlePrevStep,
  };
}
