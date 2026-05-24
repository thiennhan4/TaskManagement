import axiosInstance from './axiosInstance';

export const dashboardApi = {
  getStats: () => axiosInstance.get('/dashboard/stats'),
  getRecentTasks: (status = 'all', limit = 10) => 
    axiosInstance.get(`/dashboard/recent-tasks`, { params: { status, limit } }),
  getUpcomingTasks: (days = 7) => 
    axiosInstance.get(`/dashboard/upcoming`, { params: { days } }),
};
