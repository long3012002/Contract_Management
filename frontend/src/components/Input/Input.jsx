import React, { useState } from 'react';

/**
 * Reusable Input component.
 * Supports labels, left/right icons, custom types (including password toggle), and error messaging.
 */
const Input = React.forwardRef(({
  label,
  id,
  type = 'text',
  placeholder,
  error,
  leftIcon,
  disabled = false,
  className = '',
  ...props
}, ref) => {
  const [showPassword, setShowPassword] = useState(false);
  const isPasswordType = type === 'password';
  const inputType = isPasswordType && showPassword ? 'text' : type;

  return (
    <div className={`space-y-1.5 ${className}`}>
      {label && (
        <label htmlFor={id} className="block text-sm font-medium text-card-foreground">
          {label}
        </label>
      )}
      <div className="relative">
        {leftIcon && (
          <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-muted-foreground">
            {leftIcon}
          </div>
        )}
        
        <input
          id={id}
          type={inputType}
          ref={ref}
          placeholder={placeholder}
          disabled={disabled}
          className={`block w-full text-sm bg-white border border-input rounded-custom placeholder-muted-foreground focus:outline-none focus:border-ring focus:ring-2 focus:ring-ring/20 disabled:opacity-50 disabled:bg-muted transition-all ${
            leftIcon ? 'pl-11' : 'px-4'
          } ${isPasswordType ? 'pr-11' : 'pr-4'} ${
            error ? 'border-destructive focus:border-destructive focus:ring-destructive/20' : ''
          } py-2.5`}
          {...props}
        />

        {isPasswordType && (
          <button
            type="button"
            onClick={() => setShowPassword(prev => !prev)}
            disabled={disabled}
            className="absolute inset-y-0 right-0 pr-3.5 flex items-center text-muted-foreground hover:text-card-foreground focus:outline-none cursor-pointer"
          >
            {showPassword ? (
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.8} stroke="currentColor" className="w-5 h-5">
                <path strokeLinecap="round" strokeLinejoin="round" d="M3.98 8.223A10.477 10.477 0 001.934 12C3.226 16.338 7.244 19.5 12 19.5c.993 0 1.953-.138 2.863-.395M6.228 6.228A10.45 10.45 0 0112 4.5c4.756 0 8.773 3.162 10.065 7.498a10.523 10.523 0 01-4.293 5.774M6.228 6.228L3 3m3.228 3.228l3.65 3.65m7.894 7.894L21 21m-3.228-3.228l-3.65-3.65m0 0a3 3 0 10-4.243-4.243m4.242 4.242L9.88 9.88" />
              </svg>
            ) : (
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.8} stroke="currentColor" className="w-5 h-5">
                <path strokeLinecap="round" strokeLinejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z" />
                <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
              </svg>
            )}
          </button>
        )}
      </div>
      {/* Dùng ternary thay vì && để tránh render số 0 nếu error là falsy number (rendering-conditional-render) */}
      {error ? (
        <span className="block text-xs text-destructive animate-fade-in">{error}</span>
      ) : null}

    </div>
  );
});

Input.displayName = 'Input';

export default Input;
