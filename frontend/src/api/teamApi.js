import axiosInstance from './axiosInstance';

const teamApi = {
  getTeams: () => axiosInstance.get('/teams'),
  getTeam: (id) => axiosInstance.get(`/teams/${id}`),
  createTeam: (data) => axiosInstance.post('/teams', data),
  updateTeam: (id, data) => axiosInstance.put(`/teams/${id}`, data),
  deleteTeam: (id) => axiosInstance.delete(`/teams/${id}`),

  getMembers: (teamId) => axiosInstance.get(`/teams/${teamId}/members`),
  addMember: (teamId, data) => axiosInstance.post(`/teams/${teamId}/members`, data),
  removeMember: (teamId, userId) => axiosInstance.delete(`/teams/${teamId}/members/${userId}`),
  changeMemberRole: (teamId, userId, data) => axiosInstance.put(`/teams/${teamId}/members/${userId}/role`, data),
};

export default teamApi;
