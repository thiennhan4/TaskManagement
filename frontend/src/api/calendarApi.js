import axiosInstance from './axiosInstance';

export const calendarApi = {
  getCalendarTasks: (start, end, projectId = null, boardId = null) => {
    const params = new URLSearchParams({
      start: start.toISOString(),
      end: end.toISOString()
    });
    if (projectId) params.append('projectId', projectId);
    if (boardId) params.append('boardId', boardId);
    
    return axiosInstance.get(`/tasks/calendar?${params.toString()}`);
  }
};
