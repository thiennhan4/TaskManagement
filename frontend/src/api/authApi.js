import api, { getSessionVersion, refreshSession } from '@/api/axiosInstance';

export const authApi = {
  login: (data) => api.post('/auth/login', data),
  googleLogin: (credential) => api.post('/auth/google', { credential }),
  register: (data) => api.post('/auth/register', data),
  refresh: refreshSession,
  logout: (token) => api.post('/auth/logout', null, {
    skipAuthRefresh: true,
    _sessionVersion: getSessionVersion(),
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  }),
  getMe: () => api.get('/auth/me'),
};
