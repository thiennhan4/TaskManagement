import axios from 'axios';

const api = axios.create({
  baseURL: '/api',
  withCredentials: true,
  headers: { 'Content-Type': 'application/json' },
});

// ── Token management (module-level) ──
let accessToken = null;
let refreshPromise = null;

export const setAccessToken = (token) => {
  accessToken = token;
};

export const getAccessToken = () => accessToken;

// ── Request interceptor: attach Bearer token ──
api.interceptors.request.use((config) => {
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }
  return config;
});

// ── Response interceptor: auto-refresh on 401 ──
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && !originalRequest._retry && !originalRequest.url.includes('/auth/login') && !originalRequest.url.includes('/auth/register') && !originalRequest.url.includes('/auth/refresh') && !originalRequest.url.includes('/auth/google')) {
      originalRequest._retry = true;

      // Prevent multiple simultaneous refresh calls
      if (!refreshPromise) {
        refreshPromise = api
          .post('/auth/refresh')
          .then((res) => {
            if (res.data.success) {
              const newToken = res.data.data.token;
              setAccessToken(newToken);
              return newToken;
            }
            setAccessToken(null);
            return null;
          })
          .catch(() => {
            setAccessToken(null);
            return null;
          })
          .finally(() => {
            refreshPromise = null;
          });
      }

      const newToken = await refreshPromise;
      if (newToken) {
        originalRequest.headers.Authorization = `Bearer ${newToken}`;
        return api(originalRequest);
      }

      // Refresh failed — redirect to login
      window.location.href = '/login';
    }

    return Promise.reject(error);
  }
);

export default api;
