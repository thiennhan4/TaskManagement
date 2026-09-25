import axiosInstance from '@/api/axiosInstance';
import { getCollection } from '@/api/pagedCollection';

const commentApi = {
  getComments: (taskId) => getCollection(`/v1/tasks/${taskId}/comments`),
  addComment: (taskId, data) => axiosInstance.post(`/tasks/${taskId}/comments`, data),
  deleteComment: (commentId) => axiosInstance.delete(`/tasks/comments/${commentId}`),
};

export default commentApi;
