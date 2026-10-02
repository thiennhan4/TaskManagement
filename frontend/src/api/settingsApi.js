import api from '@/api/axiosInstance';

export const settingsApi = {
  updateProfile: data => api.put('/settings/profile', data),
  changePassword: data => api.put('/settings/password', data),
};
