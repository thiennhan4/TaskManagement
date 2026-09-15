import axiosInstance from './axiosInstance';

export const listApi = {
  getListsByBoard: (boardId) => axiosInstance.get(`/boardlists/board/${boardId}`),
  createList: (listData) => axiosInstance.post('/boardlists', listData),
  updateList: (id, listData) => axiosInstance.put(`/boardlists/${id}`, listData),
  deleteList: (id) => axiosInstance.delete(`/boardlists/${id}`),
};
