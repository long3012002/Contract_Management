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
// Tự động đính kèm Access Token vào header Authorization trên mỗi request
axiosClient.interceptors.request.use(
  (config) => {
    const user = useAuthStore.getState().user;
    const accessToken = user?.accessToken;
    if (accessToken) {
      config.headers.Authorization = `Bearer ${accessToken}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// ── Response Interceptor ─────────────────────────────────────────────────────
// Xử lý lỗi 401 (hết hạn token): thực hiện refresh token một lần
// Nếu refresh thất bại, logout người dùng.
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

axiosClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && !originalRequest._retry) {
      // Nếu đang refresh, thêm request vào hàng đợi
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            originalRequest.headers.Authorization = `Bearer ${token}`;
            return axiosClient(originalRequest);
          })
          .catch((err) => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const user = useAuthStore.getState().user;
      const refreshToken = user?.refreshToken;

      if (!refreshToken) {
        useAuthStore.getState().logout();
        return Promise.reject(error);
      }

      try {
        const response = await axios.post(`${BASE_URL}/api/Auth/refresh`, {
          refreshToken,
        });

        const { accessToken: newAccessToken, refreshToken: newRefreshToken } = response.data;

        // Cập nhật token mới vào store
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
        processQueue(refreshError, null);
        useAuthStore.getState().logout();
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);

export default axiosClient;
