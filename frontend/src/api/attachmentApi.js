import api from '@/api/axiosInstance';

const path = (taskId) => `/tasks/${taskId}/attachments`;
export const attachmentApi = {
  list: async (taskId) => (await api.get(path(taskId))).data,
  upload: async (taskId, file) => {
    const data = new FormData();
    data.append('file', file);
    return (await api.post(path(taskId), data, { headers: { 'Content-Type': 'multipart/form-data' } })).data;
  },
  remove: async (taskId, id) => (await api.delete(`${path(taskId)}/${id}`)).data,
  // Use the authenticated client: opening a URL directly cannot send the in-memory bearer token.
  download: async (taskId, id) => (await api.get(`/v1/tasks/${taskId}/attachments/${id}/download`, { responseType: 'blob' })).data,
};
