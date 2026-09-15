import axiosInstance from './axiosInstance';

const commentApi = {
  getComments: (taskId) => axiosInstance.get(`/tasks/${taskId}/comments`),
  addComment: (taskId, data) => axiosInstance.post(`/tasks/${taskId}/comments`, data),
  deleteComment: (commentId) => axiosInstance.delete(`/tasks/comments/${commentId}`),
};

export default commentApi;
