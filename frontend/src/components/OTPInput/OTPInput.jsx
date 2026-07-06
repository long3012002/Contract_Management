import React, { useRef, useEffect, memo } from 'react';

const OTPInput = memo(function OTPInput({
  value = '',
  onChange,
  disabled = false,
  error = false,
}) {
  const numInputs = 6;
  const inputRefs = useRef([]);

  // Convert value string to array of characters
  const otpArray = value.split('').concat(Array(numInputs).fill('')).slice(0, numInputs);

  const handleInputChange = (e, index) => {
    const inputValue = e.target.value;
    const lastChar = inputValue.substring(inputValue.length - 1);
    
    // Only allow numeric inputs
    if (lastChar && !/^[0-9]$/.test(lastChar)) {
      return;
    }

    const newOtpArray = [...otpArray];
    newOtpArray[index] = lastChar;
    const newOtpValue = newOtpArray.join('');
    
    onChange(newOtpValue);

    // Auto-focus next input
    if (lastChar !== '' && index < numInputs - 1) {
      inputRefs.current[index + 1]?.focus();
    }
  };

  const handleKeyDown = (e, index) => {
    if (e.key === 'Backspace') {
      e.preventDefault();
      
      const newOtpArray = [...otpArray];
      
      if (otpArray[index] !== '') {
        // If current box is filled, clear it
        newOtpArray[index] = '';
        onChange(newOtpArray.join(''));
      } else if (index > 0) {
        // If current box is empty, clear the previous box and shift focus there
        newOtpArray[index - 1] = '';
        onChange(newOtpArray.join(''));
        inputRefs.current[index - 1]?.focus();
      }
    } else if (e.key === 'ArrowLeft') {
      e.preventDefault();
      if (index > 0) {
        inputRefs.current[index - 1]?.focus();
      }
    } else if (e.key === 'ArrowRight') {
      e.preventDefault();
      if (index < numInputs - 1) {
        inputRefs.current[index + 1]?.focus();
      }
    }
  };

  const handlePaste = (e) => {
    e.preventDefault();
    if (disabled) return;

    const pastedData = e.clipboardData.getData('text').trim().replace(/[^0-9]/g, '');
    
    if (pastedData.length > 0) {
      // Split the pasted data and fill the inputs
      const newOtpValue = pastedData.slice(0, numInputs);
      onChange(newOtpValue);

      // Focus the appropriate input after paste
      const focusIndex = Math.min(newOtpValue.length, numInputs - 1);
      inputRefs.current[focusIndex]?.focus();
    }
  };

  // Focus the first input on mount
  useEffect(() => {
    if (inputRefs.current[0]) {
      inputRefs.current[0].focus();
    }
  }, []);

  return (
    <div className="flex gap-1.5 sm:gap-2.5 justify-center items-center">
      {Array(numInputs)
        .fill(0)
        .map((_, index) => {
          const hasValue = otpArray[index] !== '';
          return (
            <React.Fragment key={index}>
              <input
                ref={(el) => (inputRefs.current[index] = el)}
                type="text"
                inputMode="numeric"
                maxLength={1}
                value={otpArray[index]}
                disabled={disabled}
                onChange={(e) => handleInputChange(e, index)}
                onKeyDown={(e) => handleKeyDown(e, index)}
                onPaste={handlePaste}
                aria-label={`Mã OTP số ${index + 1}`}
                className={`w-9 h-11 sm:w-11 sm:h-12 text-center text-xl font-bold border-2 rounded-custom transition-all bg-white focus:outline-none focus:scale-105 disabled:opacity-50 ${
                  error
                    ? 'border-destructive focus:border-destructive focus:shadow-[0_0_8px_rgba(228,33,40,0.2)]'
                    : hasValue
                    ? 'border-primary/60 focus:border-primary focus:shadow-[0_0_8px_rgba(33,59,114,0.2)]'
                    : 'border-slate-200 focus:border-primary focus:shadow-[0_0_8px_rgba(33,59,114,0.15)]'
                }`}
              />
              {index === 2 ? (
                <span className="text-slate-300 font-bold mx-0.5 sm:mx-1 select-none text-base">
                  &mdash;
                </span>
              ) : null}
            </React.Fragment>
          );
        })}
    </div>
  );
});

export default OTPInput;
