import axiosInstance from './axiosInstance';
import { format } from 'date-fns';

export const calendarApi = {
  getCalendarTasks: (start, end, projectId = null, boardId = null, page = 1) => {
    const params = new URLSearchParams({
      // SQL DateTime calendar fields have no offset. Match the same wall-time
      // boundaries used for display; converting them to UTC shifts the query.
      start: format(start, "yyyy-MM-dd'T'HH:mm:ss.SSS"),
      end: format(end, "yyyy-MM-dd'T'HH:mm:ss.SSS"), page: String(page), pageSize: '100'
    });
    if (projectId) params.append('projectId', projectId);
    if (boardId) params.append('boardId', boardId);
    
    return axiosInstance.get(`/v1/tasks/calendar?${params.toString()}`);
  }
};
