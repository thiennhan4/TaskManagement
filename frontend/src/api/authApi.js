import api, { refreshSession } from '@/api/axiosInstance';

export const authApi = {
  login: (data) => api.post('/auth/login', data),
  googleLogin: (credential) => api.post('/auth/google', { credential }),
  register: (data) => api.post('/auth/register', data),
  refresh: refreshSession,
  logout: () => api.post('/auth/logout'),
  getMe: () => api.get('/auth/me'),
};
