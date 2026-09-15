import axiosInstance from './axiosInstance';

export const boardApi = {
  getBoards: () => axiosInstance.get('/boards'),
  getBoardById: (id) => axiosInstance.get(`/boards/${id}`),
  createBoard: (boardData) => axiosInstance.post('/boards', boardData),
  updateBoard: (id, boardData) => axiosInstance.put(`/boards/${id}`, boardData),
  deleteBoard: (id) => axiosInstance.delete(`/boards/${id}`),
};
