import axiosInstance from '@/api/axiosInstance';

export const timeTrackingApi = {
  startTimer: (data) => axiosInstance.post('/timetracking/start', data),
  stopTimer: (entryId, data = {}) => axiosInstance.post(`/timetracking/${entryId}/stop`, data),
  getRunningTimer: () => axiosInstance.get('/timetracking/running'),
  createManualEntry: (data) => axiosInstance.post('/timetracking/manual', data),
  deleteEntry: (entryId) => axiosInstance.delete(`/timetracking/${entryId}`),
  getEntriesForTask: (taskId, params) => axiosInstance.get(`/v1/timetracking/task/${taskId}`, { params }),
  getMyEntries: (from, to, page = 1) => {
    const params = new URLSearchParams();
    if (from) params.append('from', from.toISOString());
    if (to) params.append('to', to.toISOString());
    params.append('page', page);
    return axiosInstance.get(`/v1/timetracking/my-entries?${params.toString()}`);
  },
  getReport: (from, to, boardId, page = 1) => {
    const params = new URLSearchParams({
      from: from.toISOString(),
      to: to.toISOString(), page
    });
    if (boardId) params.append('boardId', boardId);
    return axiosInstance.get(`/v1/timetracking/report?${params.toString()}`);
  }
};
