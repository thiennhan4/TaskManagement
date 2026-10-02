import axios from 'axios';

const api = axios.create({
  baseURL: '/api',
  withCredentials: true,
  headers: { 'Content-Type': 'application/json' },
});

// ── Token management (module-level) ──
let accessToken = null;
let refreshPromise = null;
let sessionVersion = 0;
const sessionListeners = new Set();
export const onSessionCleared = (listener) => {
  sessionListeners.add(listener);
  return () => sessionListeners.delete(listener);
};
export const getSessionVersion = () => sessionVersion;

export const setAccessToken = (token) => {
  sessionVersion += 1;
  accessToken = token;
  refreshPromise = null;
  if (!token) sessionListeners.forEach(listener => listener());
};

export const getAccessToken = () => accessToken;

// Bootstrap and intercepted 401s share the same in-tab request. Web Locks serialize
// cookie rotation across tabs where available; SQL consumption remains authoritative.
export const refreshSession = () => {
  if (!refreshPromise) {
    const version = sessionVersion;
    const rotate = () => {
      if (version !== sessionVersion) throw new axios.CanceledError('Session changed');
      return api.post('/auth/refresh');
    };
    const request = globalThis.navigator?.locks?.request
      ? globalThis.navigator.locks.request('taskhub-refresh', rotate)
      : rotate();
    const pending = request.then((response) => {
      if (version !== sessionVersion) throw new axios.CanceledError('Session changed');
      if (!response.data.success) throw new Error('Session refresh failed');
      accessToken = response.data.data.token;
      return response;
    }).catch((error) => {
      if (version === sessionVersion) setAccessToken(null);
      throw error;
    }).finally(() => { if (refreshPromise === pending) refreshPromise = null; });
    refreshPromise = pending;
  }
  return refreshPromise;
};

// ── Request interceptor: attach Bearer token ──
api.interceptors.request.use((config) => {
  config._sessionVersion ??= sessionVersion;
  if (config._sessionVersion !== sessionVersion) throw new axios.CanceledError('Session changed');
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }
  return config;
});

// ── Response interceptor: auto-refresh on 401 ──
api.interceptors.response.use(
  (response) => {
    if (response.config?._sessionVersion !== undefined && response.config._sessionVersion !== sessionVersion)
      throw new axios.CanceledError('Session changed');
    return response;
  },
  async (error) => {
    const originalRequest = error.config;
    if (!originalRequest || (originalRequest._sessionVersion !== undefined && originalRequest._sessionVersion !== sessionVersion))
      return Promise.reject(error);

    if (error.response?.status === 401 && !originalRequest.skipAuthRefresh && !originalRequest._retry && !originalRequest.url.includes('/auth/login') && !originalRequest.url.includes('/auth/register') && !originalRequest.url.includes('/auth/refresh') && !originalRequest.url.includes('/auth/google')) {
      originalRequest._retry = true;

      let newToken = null;
      const versionBeforeRefresh = sessionVersion;
      // A late 401 can belong to the token another request already replaced.
      try {
        newToken = accessToken && originalRequest.headers.Authorization !== `Bearer ${accessToken}`
          ? accessToken : (await refreshSession()).data.data.token;
      } catch {
        // Another login/logout superseded this refresh; do not redirect its UI.
        if (sessionVersion !== versionBeforeRefresh && accessToken) return Promise.reject(error);
      }
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
