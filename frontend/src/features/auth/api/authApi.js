import axiosClient from '../../../api/axiosClient';

/**
 * Gửi yêu cầu đăng nhập nội bộ (username + password) đến backend.
 * @param {string} username
 * @param {string} password
 * @returns {Promise<import('./types').LoginResponse>}
 */
export const loginApi = async (username, password) => {
  const response = await axiosClient.post('/api/Auth/login', { username, password });
  return response.data;
};

/**
 * Làm mới Access Token bằng Refresh Token.
 * @param {string} refreshToken
 * @returns {Promise<import('./types').LoginResponse>}
 */
export const refreshTokenApi = async (refreshToken) => {
  const response = await axiosClient.post('/api/Auth/refresh', { refreshToken });
  return response.data;
};

/**
 * Xác thực 2FA OTP sau khi đã đăng nhập thành công bước 1.
 * @param {string} username
 * @param {string} code
 * @returns {Promise<any>}
 */
export const verify2faApi = async (username, code) => {
  const response = await axiosClient.post('/api/Auth/verify-2fa', { username, code });
  return response.data;
};

/**
 * Kích hoạt 2FA OTP lần đầu tiên đăng nhập.
 * @param {string} username
 * @param {string} code
 * @returns {Promise<any>}
 */
export const enable2faApi = async (username, code) => {
  const response = await axiosClient.post('/api/Auth/enable-2fa', { username, code });
  return response.data;
};

