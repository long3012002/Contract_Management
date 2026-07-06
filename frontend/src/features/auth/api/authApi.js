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
