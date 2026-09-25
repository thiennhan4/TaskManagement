import axiosInstance from '@/api/axiosInstance';
import { getCollection } from '@/api/pagedCollection';

export const projectApi = {
  getProjects: () => axiosInstance.get('/projects'),
  getWorkspaceProjects: (workspaceId, includeArchived = false) => 
    axiosInstance.get(`/projects/workspace/${workspaceId}?includeArchived=${includeArchived}`),
  getProjectById: (id) => axiosInstance.get(`/projects/${id}`),
  createProject: (projectData) => axiosInstance.post('/projects', projectData),
  updateProject: (id, projectData) => axiosInstance.patch(`/projects/${id}`, projectData),
  convertToTeam: (id, teamId) => axiosInstance.post(`/v1/projects/${id}/convert-to-team`, { teamId }),
  deleteProject: (id) => axiosInstance.delete(`/projects/${id}`),
  archiveProject: (id) => axiosInstance.post(`/projects/${id}/archive`),
  restoreProject: (id) => axiosInstance.post(`/projects/${id}/restore`),
  getProjectActivity: (id) => axiosInstance.get(`/projects/${id}/activity`),
  getProjectKanban: (id, params) => axiosInstance.get(`/v1/projects/${id}/kanban`, { params }),
  getProjectBoards: (id) => getCollection(`/v1/projects/${id}/boards`),
};
