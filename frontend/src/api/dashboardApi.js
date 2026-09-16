import axiosInstance from './axiosInstance';

export const dashboardApi = {
  getStats: () => axiosInstance.get('/dashboard/stats'),
  getVelocity: (timeframe = 'SixMonths', scope = 'Personal', teamId = null) =>
    axiosInstance.get('/dashboard/velocity', { params: { timeframe, scope, ...(teamId ? { teamId } : {}) } }),
  getRecentTasks: (status = 'all', limit = 10) => 
    axiosInstance.get(`/dashboard/recent-tasks`, { params: { status, limit } }),
  getUpcomingTasks: (days = 7) => 
    axiosInstance.get(`/dashboard/upcoming`, { params: { days } }),
};
