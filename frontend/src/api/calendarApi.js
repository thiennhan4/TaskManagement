import axiosInstance from './axiosInstance';

export const calendarApi = {
  getCalendarTasks: (start, end, projectId = null, boardId = null, page = 1) => {
    const params = new URLSearchParams({
      start: start.toISOString(),
      end: end.toISOString(), page: String(page), pageSize: '100'
    });
    if (projectId) params.append('projectId', projectId);
    if (boardId) params.append('boardId', boardId);
    
    return axiosInstance.get(`/v1/tasks/calendar?${params.toString()}`);
  }
};
