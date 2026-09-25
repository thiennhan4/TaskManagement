import axiosInstance from '@/api/axiosInstance';
import { getCollection } from '@/api/pagedCollection';

export const boardApi = {
  getBoards: () => getCollection('/v1/boards'),
  getBoardById: (id) => axiosInstance.get(`/boards/${id}`),
  createBoard: (boardData) => axiosInstance.post('/boards', boardData),
  updateBoard: (id, boardData) => axiosInstance.put(`/boards/${id}`, boardData),
  deleteBoard: (id) => axiosInstance.delete(`/boards/${id}`),
};
