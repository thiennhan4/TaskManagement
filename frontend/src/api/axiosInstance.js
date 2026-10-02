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

// Bootstrap and intercepted 401s share the same in-tab request. Web Locks serialize
// cookie rotation across tabs where available; SQL consumption remains authoritative.
export const refreshSession = () => {
  if (!refreshPromise) {
    const rotate = () => api.post('/auth/refresh');
    const request = globalThis.navigator?.locks?.request
      ? globalThis.navigator.locks.request('taskhub-refresh', rotate)
      : rotate();
    refreshPromise = request.then((response) => {
      if (!response.data.success) throw new Error('Session refresh failed');
      setAccessToken(response.data.data.token);
      return response;
    }).catch((error) => {
      setAccessToken(null);
      throw error;
    }).finally(() => { refreshPromise = null; });
  }
  return refreshPromise;
};

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

      let newToken = null;
      try { newToken = (await refreshSession()).data.data.token; } catch { /* controlled session failure */ }
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
