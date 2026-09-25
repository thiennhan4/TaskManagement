import axiosInstance from '@/api/axiosInstance';
import { getCollection } from '@/api/pagedCollection';

export const projectMemberApi = {
  getMembers: (projectId) => getCollection(`/v1/projects/${projectId}/members`),
  inviteMember: (projectId, inviteData) => axiosInstance.post(`/projects/${projectId}/members/invite`, inviteData),
  acceptInvite: (projectId, token) => axiosInstance.post(`/projects/${projectId}/members/accept-invite`, { token }),
  updateMemberRole: (projectId, memberUserId, roleData) => 
    axiosInstance.patch(`/projects/${projectId}/members/${memberUserId}/role`, roleData),
  removeMember: (projectId, memberUserId) => 
    axiosInstance.delete(`/projects/${projectId}/members/${memberUserId}`),
};
