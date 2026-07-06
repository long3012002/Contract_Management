import axios from 'axios';
import { useAuthStore } from '../features/auth/store/authStore';

// Tập trung base URL theo biến môi trường (tránh hardcode trực tiếp trong hook)
const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:64950';

const axiosClient = axios.create({
  baseURL: BASE_URL,
  timeout: 10_000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// ── Request Interceptor ─────────────────────────────────────────────────────
// Tự động đính kèm Access Token hoặc Temporary Token vào header Authorization trên mỗi request
axiosClient.interceptors.request.use(
  (config) => {
    const state = useAuthStore.getState();
    let accessToken = state.user?.accessToken;
    if (config.url?.includes('/api/Auth/verify-2fa') || config.url?.includes('/api/Auth/enable-2fa')) {
      accessToken = state.tempUser?.accessToken || accessToken;
    } else {
      accessToken = accessToken || state.tempUser?.accessToken;
    }
    console.log('[Axios Interceptor] URL:', config.url, 'Token:', accessToken ? `${accessToken.substring(0, 15)}...` : 'None');
    if (accessToken) {
      config.headers.Authorization = `Bearer ${accessToken}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// ── Response Interceptor ─────────────────────────────────────────────────────
let isRefreshing = false;
let failedQueue = [];

const processQueue = (error, token = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

/**
 * Chuẩn hóa lỗi từ server hoặc mạng để UI không cần dùng chuỗi Optional Chaining (?.data?.message)
 */
const standardizeError = (error) => {
  // Tránh chuẩn hóa lại nếu error đã được xử lý (VD: từ các request trong queue)
  if (error.hasServerMessage !== undefined) {
    return Promise.reject(error);
  }

  const serverMessage = error.response?.data?.message;
  const standardizedError = new Error(serverMessage || 'Đã có lỗi xảy ra. Vui lòng thử lại sau.');
  standardizedError.status = error.response?.status;
  standardizedError.hasServerMessage = !!serverMessage;
  
  return Promise.reject(standardizedError);
};

axiosClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    // Xử lý lỗi 401 Unauthorized (Hết hạn token)
    if (error.response?.status === 401 && !originalRequest._retry) {
      // Nếu đang trong quá trình refresh, đưa request vào hàng chờ
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            originalRequest.headers.Authorization = `Bearer ${token}`;
            return axiosClient(originalRequest);
          })
          .catch((err) => standardizeError(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const refreshToken = useAuthStore.getState().user?.refreshToken;

      // Nếu không có Refresh Token, logout và ném lỗi chuẩn hóa
      if (!refreshToken) {
        useAuthStore.getState().logout();
        return standardizeError(error);
      }

      try {
        // Thực hiện call API refresh token
        const response = await axios.post(`${BASE_URL}/api/Auth/refresh`, { refreshToken });
        const { accessToken: newAccessToken, refreshToken: newRefreshToken } = response.data;

        // Cập nhật lại token mới vào store
        useAuthStore.setState((state) => ({
          user: {
            ...state.user,
            accessToken: newAccessToken,
            refreshToken: newRefreshToken,
          },
        }));

        processQueue(null, newAccessToken);
        originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
        
        return axiosClient(originalRequest);
      } catch (refreshError) {
        // Nếu refresh thất bại (Refresh token hết hạn / lỗi server), logout và reject các request trong queue
        processQueue(refreshError, null);
        useAuthStore.getState().logout();
        return standardizeError(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    // Các lỗi khác (500, 400, Timeout, Network Error, ...)
    return standardizeError(error);
  }
);

export default axiosClient;
