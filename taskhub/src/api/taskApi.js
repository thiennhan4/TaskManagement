import axiosInstance from './axiosInstance';

export const taskApi = {
  getTasksByList: (listId) => axiosInstance.get(`/api/tasks/lists/${listId}/tasks`),
  createTask: (listId, taskData) => axiosInstance.post(`/api/tasks/lists/${listId}/tasks`, taskData),
  updateTask: (id, taskData) => axiosInstance.put(`/api/tasks/${id}`, taskData),
  deleteTask: (id) => axiosInstance.delete(`/api/tasks/${id}`),
  moveTask: (id, moveData) => axiosInstance.patch(`/api/tasks/${id}/move`, moveData),
  updateTaskProgress: (id, progressData) => axiosInstance.patch(`/api/tasks/${id}/progress`, progressData),
};
