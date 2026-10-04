import axiosInstance from '@/api/axiosInstance';
import { getCollection } from '@/api/pagedCollection';

const teamApi = {
  getTeams: () => getCollection('/v1/teams'),
  getPage: (params) => axiosInstance.get('/v1/teams', { params }),
  getTeam: (id) => axiosInstance.get(`/v1/teams/${id}`),
  createTeam: (data) => axiosInstance.post('/teams', data),
  updateTeam: (id, data) => axiosInstance.put(`/teams/${id}`, data),
  deleteTeam: (id) => axiosInstance.delete(`/teams/${id}`),

  getMembers: (teamId, page = 1) => axiosInstance.get(`/v1/teams/${teamId}/members`, { params: { page } }),
  getAllMembers: (teamId) => getCollection(`/v1/teams/${teamId}/members`),
  addMember: (teamId, data) => axiosInstance.post(`/teams/${teamId}/members`, data),
  removeMember: (teamId, userId) => axiosInstance.delete(`/teams/${teamId}/members/${userId}`),
  changeMemberRole: (teamId, userId, data) => axiosInstance.put(`/teams/${teamId}/members/${userId}/role`, data),
};

export default teamApi;
