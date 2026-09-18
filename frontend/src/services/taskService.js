import api from '../api/axiosInstance';

export const getTasks = async (filters) => {
  const response = await api.get('/tasks', { params: filters });
  return response.data;
};

export const getTaskById = async (id) => {
  const response = await api.get(`/tasks/${id}`);
  return response.data;
};

export const createTask = async (listId, data) => {
  // Since our backend requires listId in the route: /api/tasks/lists/{listId}/tasks
  const response = await api.post(`/tasks/lists/${listId}/tasks`, data);
  return response.data;
};

export const updateTask = async (id, data) => {
  const response = await api.put(`/tasks/${id}`, data);
  return response.data;
};

export const deleteTask = async (id) => {
  const response = await api.delete(`/tasks/${id}`);
  return response.data;
};

// Status & Assignment
export const changeStatus = async (id, newStatus) => {
  const response = await api.patch(`/tasks/${id}/status`, { newStatus });
  return response.data;
};

export const assignTask = async (id, assignedToUserId) => {
  const response = await api.patch(`/tasks/${id}/assign`, { assignedToUserId });
  return response.data;
};

// Comments
export const getComments = async (id) => {
  const response = await api.get(`/tasks/${id}/comments`);
  return response.data;
};

export const addComment = async (id, content) => {
  const response = await api.post(`/tasks/${id}/comments`, { content });
  return response.data;
};

export const deleteComment = async (taskId, commentId) => {
  const response = await api.delete(`/tasks/${taskId}/comments/${commentId}`);
  return response.data;
};

// Attachments
export const getAttachments = async (id) => {
  const response = await api.get(`/tasks/${id}/attachments`);
  return response.data;
};

export const uploadAttachment = async (id, file) => {
  const formData = new FormData();
  formData.append('file', file);

  const response = await api.post(`/tasks/${id}/attachments`, formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });
  return response.data;
};

export const deleteAttachment = async (taskId, attachmentId) => {
  const response = await api.delete(`/tasks/${taskId}/attachments/${attachmentId}`);
  return response.data;
};

// Activity
export const getActivityLogs = async (id) => {
  const response = await api.get(`/tasks/${id}/activity-logs`);
  return response.data;
};

// Invitations
export const inviteToTask = async (id, email) => {
  const response = await api.post(`/tasks/${id}/invite`, { email });
  return response.data;
};

export const acceptTaskInvite = async (token) => {
  const response = await api.post(`/tasks/accept-invite`, { token });
  return response.data;
};

