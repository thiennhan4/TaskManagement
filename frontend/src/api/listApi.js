import axiosInstance from '@/api/axiosInstance';
import { getCollection } from '@/api/pagedCollection';

export const listApi = {
  // Selectors need column names, not every card on the board.
  getListChoices: async (boardId) => {
    const response = await getCollection(`/v1/boardlists/board/${boardId}`, { taskPageSize: 1 });
    return { ...response, data: { ...response.data, data: response.data.data.map(list => ({
      id: list.id, name: list.name, position: list.position, color: list.color,
    })) } };
  },
  getListsByBoard: async (boardId) => {
    const url = `/v1/boardlists/board/${boardId}`;
    const response = await getCollection(url, { taskPageSize: 100 });
    for (const list of response.data.data) {
      const tasks = [...list.tasks.items];
      for (let taskPage = 2; taskPage <= list.tasks.totalPages; taskPage++) {
        const next = await axiosInstance.get(url, { params: { listId: list.id, taskPage, taskPageSize: 100 } });
        tasks.push(...next.data.data.items[0].tasks.items);
      }
      list.tasks = tasks;
    }
    return response;
  },
  createList: (listData) => axiosInstance.post('/boardlists', listData),
  updateList: (id, listData) => axiosInstance.put(`/boardlists/${id}`, listData),
  deleteList: (id) => axiosInstance.delete(`/boardlists/${id}`),
};
