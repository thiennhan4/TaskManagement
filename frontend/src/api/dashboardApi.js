import axiosInstance from './axiosInstance';

export const dashboardApi = {
  getStats: (scope = 'Personal', teamId = null) =>
    axiosInstance.get('/dashboard/stats', { params: { scope, ...(teamId ? { teamId } : {}) } }),
  getVelocity: (timeframe = 'SixMonths', scope = 'Personal', teamId = null) =>
    axiosInstance.get('/dashboard/velocity', { params: { timeframe, scope, ...(teamId ? { teamId } : {}) } }),
  getActivity: (scope = 'Personal', teamId = null, page = 1, pageSize = 10) =>
    axiosInstance.get('/dashboard/activity', { params: { scope, ...(teamId ? { teamId } : {}), page, pageSize } }),
  getProjectActivity: (projectId, page = 1, pageSize = 5) =>
    axiosInstance.get(`/v1/projects/${projectId}/activity`, { params: { page, pageSize } }),
  getUpcomingTasks: (scope = 'Personal', teamId = null, page = 1, pageSize = 5) =>
    axiosInstance.get('/dashboard/upcoming', { params: { scope, ...(teamId ? { teamId } : {}), page, pageSize } }),
};
