import axiosInstance from '@/api/axiosInstance';

export const taskApi = {
  getEligibleAssignees: (id, page = 1) => axiosInstance.get(`/v1/tasks/${id}/eligible-assignees`, { params: { page, pageSize: 20 } }),
  getTasks: (params) => axiosInstance.get('/v1/tasks', { params }),
  getTaskById: (id) => axiosInstance.get(`/v1/tasks/${id}`),
  getActivityLogs: (id, page = 1) => axiosInstance.get(`/v1/tasks/${id}/activity-logs`, { params: { page } }),
  assignTask: (id, assignedToUserId) => axiosInstance.patch(`/tasks/${id}/assign`, { assignedToUserId }),
  getMyTasks: (params) => axiosInstance.get(`/v1/tasks/my-tasks`, { params }),
  getSummary: () => axiosInstance.get(`/v1/tasks/summary`),
  getTasksByList: (listId) => axiosInstance.get(`/tasks/lists/${listId}/tasks`),
  createTask: (listId, taskData) => axiosInstance.post(`/tasks/lists/${listId}/tasks`, taskData),
  createPersonalTask: (taskData) => axiosInstance.post(`/tasks`, taskData),
  updateTask: (id, taskData) => axiosInstance.put(`/tasks/${id}`, taskData),
  changeTaskStatus: (id, newStatus) => axiosInstance.patch(`/tasks/${id}/status`, { newStatus }),
  deleteTask: (id) => axiosInstance.delete(`/tasks/${id}`),
  moveTask: (id, moveData) => axiosInstance.patch(`/tasks/${id}/move`, moveData),
  updateTaskProgress: (id, progressData) => axiosInstance.patch(`/tasks/${id}/progress`, progressData),
  inviteTaskMember: (id, inviteData) => axiosInstance.post(`/tasks/${id}/invite`, inviteData),
  acceptTaskInvite: (token) => axiosInstance.post(`/tasks/accept-invite`, { token }),
};
