import axiosInstance from './axiosInstance';

export const boardApi = {
  getBoards: () => axiosInstance.get('/api/Board'),
  getBoardById: (id) => axiosInstance.get(`/api/Board/${id}`),
  createBoard: (boardData) => axiosInstance.post('/api/Board', boardData),
  updateBoard: (id, boardData) => axiosInstance.put(`/api/Board/${id}`, boardData),
  deleteBoard: (id) => axiosInstance.delete(`/api/Board/${id}`),
};
