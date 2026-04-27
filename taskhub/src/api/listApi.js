import axiosInstance from './axiosInstance';

export const listApi = {
  getListsByBoard: (boardId) => axiosInstance.get(`/api/BoardList/board/${boardId}`),
  createList: (listData) => axiosInstance.post('/api/BoardList', listData),
  updateList: (id, listData) => axiosInstance.put(`/api/BoardList/${id}`, listData),
  deleteList: (id) => axiosInstance.delete(`/api/BoardList/${id}`),
};
