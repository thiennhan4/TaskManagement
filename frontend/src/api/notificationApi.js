import axiosInstance from './axiosInstance';

export const notificationApi = {
  getMyNotifications: () => axiosInstance.get('/notification'),
  markAsRead: (id) => axiosInstance.put(`/notification/${id}/read`),
  markAllAsRead: () => axiosInstance.put('/notification/read-all'),
};
