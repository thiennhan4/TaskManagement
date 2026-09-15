import axiosInstance from './axiosInstance';

export const projectApi = {
  getProjects: () => axiosInstance.get('/projects'),
  getWorkspaceProjects: (workspaceId, includeArchived = false) => 
    axiosInstance.get(`/projects/workspace/${workspaceId}?includeArchived=${includeArchived}`),
  getProjectById: (id) => axiosInstance.get(`/projects/${id}`),
  createProject: (projectData) => axiosInstance.post('/projects', projectData),
  updateProject: (id, projectData) => axiosInstance.patch(`/projects/${id}`, projectData),
  deleteProject: (id) => axiosInstance.delete(`/projects/${id}`),
  archiveProject: (id) => axiosInstance.post(`/projects/${id}/archive`),
  restoreProject: (id) => axiosInstance.post(`/projects/${id}/restore`),
  getProjectActivity: (id) => axiosInstance.get(`/projects/${id}/activity`),
  getProjectBoards: (id) => axiosInstance.get(`/projects/${id}/boards`),
};
