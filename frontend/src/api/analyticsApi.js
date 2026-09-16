import axiosInstance from './axiosInstance';

export const analyticsApi = {
  getOverview: (days = 30, boardId = null, scope = 'Personal', teamId = null, timeframe = 'SixMonths') => {
    return axiosInstance.get('/analytics/overview', {
      params: { days, scope, timeframe, ...(boardId ? { boardId } : {}), ...(teamId ? { teamId } : {}) },
    });
  }
};
