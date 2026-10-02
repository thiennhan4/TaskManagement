import axiosInstance from '@/api/axiosInstance';

export const taskApi = {
  getMyTasks: () => axiosInstance.get(`/tasks/my-tasks`),
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
