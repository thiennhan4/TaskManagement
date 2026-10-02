import axiosInstance from '@/api/axiosInstance';
import { getCollection } from '@/api/pagedCollection';

export const boardApi = {
  getColumns: (id, params) => axiosInstance.get(`/v1/boardlists/board/${id}`, { params }),
  getBoards: () => getCollection('/v1/boards'),
  getBoardById: (id, params) => axiosInstance.get(`/boards/${id}`, { params }),
  createBoard: (boardData) => axiosInstance.post('/boards', boardData),
  updateBoard: (id, boardData) => axiosInstance.put(`/boards/${id}`, boardData),
  deleteBoard: (id) => axiosInstance.delete(`/boards/${id}`),
};
