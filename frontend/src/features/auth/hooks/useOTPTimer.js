import { useState, useEffect } from 'react';

/**
 * Hook quản lý countdown timer 30 giây cho TOTP (Time-based OTP).
 * Dùng setInterval một lần duy nhất với functional setState để tránh
 * re-subscribe mỗi tick (rerender-split-combined-hooks, rerender-derived-state-no-effect).
 *
 * @returns {{ secondsLeft: number }} Số giây còn lại trong chu kỳ OTP hiện tại.
 */
export function useOTPTimer() {
  const [secondsLeft, setSecondsLeft] = useState(30);

  useEffect(() => {
    // setInterval một lần duy nhất — không re-subscribe mỗi khi secondsLeft thay đổi
    // functional setState đảm bảo callback ổn định, không tạo closure stale
    const timer = setInterval(() => {
      setSecondsLeft((prev) => (prev <= 1 ? 30 : prev - 1));
    }, 1000);

    return () => clearInterval(timer);
  }, []); // dependency array rỗng — chạy một lần khi mount

  return { secondsLeft };
}
