import axiosInstance from '@/api/axiosInstance';

export const notificationApi = {
  getMyNotifications: (page = 1, pageSize = 20) => axiosInstance.get('/v1/notifications', { params: { page, pageSize } }),
  getUnreadCount: () => axiosInstance.get('/notification/unread-count'),
  markAsRead: (id) => axiosInstance.put(`/notification/${id}/read`),
  markAllAsRead: () => axiosInstance.put('/notification/read-all'),
};
