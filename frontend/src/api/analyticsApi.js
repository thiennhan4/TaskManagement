import axiosInstance from './axiosInstance';

export const analyticsApi = {
  getOverview: (days = 30, boardId = null) => {
    const params = new URLSearchParams({ days: days.toString() });
    if (boardId) params.append('boardId', boardId);
    return axiosInstance.get(`/analytics/overview?${params.toString()}`);
  }
};
