import axiosInstance from '@/api/axiosInstance';


const commentApi = {
  getComments: (taskId, page = 1) => axiosInstance.get(`/v1/tasks/${taskId}/comments`, { params: { page } }),
  addComment: (taskId, data) => axiosInstance.post(`/tasks/${taskId}/comments`, data),
  deleteComment: (commentId) => axiosInstance.delete(`/tasks/comments/${commentId}`),
};

export default commentApi;
