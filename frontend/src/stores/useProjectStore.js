import { create } from 'zustand';
import { projectApi } from '@/api/projectApi';
import { projectMemberApi } from '@/api/projectMemberApi';
import { toast } from 'react-hot-toast';
import { translate } from '@/i18n/translate';

let epoch = 0;
const requests = {};

export const useProjectStore = create((set) => ({
  reset: () => { epoch++; set({ projects: [], projectPage: null, currentProject: null, members: [], activityLogs: [], isLoading: false, error: null }); },
  projects: [],
  projectPage: null,
  currentProject: null,
  members: [],
  activityLogs: [],
  isLoading: false,
  error: null,

  fetchProjects: async (params = {}) => {
    const generation = epoch;
    const request = requests.projects = (requests.projects || 0) + 1;
    const current = () => generation === epoch && requests.projects === request;
    set({ isLoading: true });
    try {
      const response = await projectApi.getPage(params);
      if (!current()) return;
      set({ projects: response.data.data.items, projectPage: response.data.data, isLoading: false, error: null });
    } catch (error) {
      if (!current()) return;
      set({ error: error.message, isLoading: false });
      toast.error(error.response?.data?.message || translate('projects.toast.fetchError'));
    }
  },

  fetchWorkspaceProjects: async (workspaceId, includeArchived) => {
    const generation = epoch;
    const request = requests.projects = (requests.projects || 0) + 1;
    const current = () => generation === epoch && requests.projects === request;
    set({ isLoading: true });
    try {
      const response = await projectApi.getPage({ workspaceId, includeArchived });
      if (!current()) return;
      set({ projects: response.data.data.items, projectPage: response.data.data, isLoading: false, error: null });
    } catch (error) {
      if (!current()) return;
      set({ error: error.message, isLoading: false });
      toast.error(error.response?.data?.message || translate('projects.toast.fetchWorkspaceError'));
    }
  },

  fetchProjectById: async (id) => {
    const generation = epoch;
    const request = requests.fetchProjectById = (requests.fetchProjectById || 0) + 1;
    const current = () => generation === epoch && requests.fetchProjectById === request;
    set({ isLoading: true });
    try {
      const response = await projectApi.getProjectById(id);
      if (!current()) return;
      set({ currentProject: response.data?.data, isLoading: false, error: null });
      return response.data?.data;
    } catch (error) {
      if (!current()) return;
      set({ error: error.message, isLoading: false });
      toast.error(error.response?.data?.message || translate('projects.toast.fetchDetailError'));
    }
  },

  createProject: async (projectData, options = {}) => {
    const generation = epoch;
    const request = requests.createProject = (requests.createProject || 0) + 1;
    const current = () => generation === epoch && requests.createProject === request;
    set({ isLoading: true });
    try {
      const response = await projectApi.createProject(projectData);
      if (!current()) return;
      set((state) => ({ 
        projects: [response.data.data, ...state.projects.filter(p => p.id !== response.data.data.id)].slice(0, state.projectPage?.pageSize || 20),
        isLoading: false,
        error: null
      }));
      if (!options.silentSuccessToast) {
        toast.success(translate('projects.toast.createSuccess'));
      }
      return response.data.data;
    } catch (error) {
      if (!current()) return;
      set({ isLoading: false, error: error.response?.data?.message || error.message });
      if (!options.silentErrorToast) {
        toast.error(error.response?.data?.message || translate('projects.toast.createError'));
      }
      throw error;
    }
  },

  updateProject: async (id, projectData) => {
    const generation = epoch;
    const request = requests.updateProject = (requests.updateProject || 0) + 1;
    const current = () => generation === epoch && requests.updateProject === request;
    try {
      const response = await projectApi.updateProject(id, projectData);
      if (!current()) return;
      set((state) => ({
        projects: state.projects.map((p) => (p.id === id ? response.data.data : p)),
        currentProject: state.currentProject?.id === id ? response.data.data : state.currentProject,
      }));
      toast.success(translate('projects.toast.updateSuccess'));
      return response.data.data;
    } catch (error) {
      if (!current()) return;
      toast.error(error.response?.data?.message || translate('projects.toast.updateError'));
      throw error;
    }
  },

  deleteProject: async (id) => {
    const generation = epoch;
    const request = requests.deleteProject = (requests.deleteProject || 0) + 1;
    const current = () => generation === epoch && requests.deleteProject === request;
    try {
      await projectApi.deleteProject(id);
      if (!current()) return;
      set((state) => ({
        projects: state.projects.filter((project) => project.id !== id),
        currentProject: state.currentProject?.id === id ? null : state.currentProject,
      }));
      toast.success(translate('projects.toast.deleteSuccess'));
    } catch (error) {
      if (!current()) return;
      toast.error(error.response?.data?.message || translate('projects.toast.deleteError'));
      throw error;
    }
  },

  archiveProject: async (id) => {
    const generation = epoch;
    const request = requests.archiveProject = (requests.archiveProject || 0) + 1;
    const current = () => generation === epoch && requests.archiveProject === request;
    try {
      const response = await projectApi.archiveProject(id);
      if (!current()) return;
      set((state) => ({
        projects: state.projects.map((p) => (p.id === id ? response.data.data : p)),
        currentProject: state.currentProject?.id === id ? response.data.data : state.currentProject,
      }));
      toast.success(translate('projects.toast.archiveSuccess'));
      return response.data.data;
    } catch (error) {
      if (!current()) return;
      toast.error(error.response?.data?.message || translate('projects.toast.archiveError'));
      throw error;
    }
  },

  restoreProject: async (id) => {
    const generation = epoch;
    const request = requests.restoreProject = (requests.restoreProject || 0) + 1;
    const current = () => generation === epoch && requests.restoreProject === request;
    try {
      const response = await projectApi.restoreProject(id);
      if (!current()) return;
      set((state) => ({
        projects: state.projects.map((p) => (p.id === id ? response.data.data : p)),
        currentProject: state.currentProject?.id === id ? response.data.data : state.currentProject,
      }));
      toast.success(translate('projects.toast.restoreSuccess'));
      return response.data.data;
    } catch (error) {
      if (!current()) return;
      toast.error(error.response?.data?.message || translate('projects.toast.restoreError'));
      throw error;
    }
  },

  fetchMembers: async (projectId) => {
    const generation = epoch;
    const request = requests.fetchMembers = (requests.fetchMembers || 0) + 1;
    const current = () => generation === epoch && requests.fetchMembers === request;
    try {
      const response = await projectMemberApi.getPage(projectId);
      if (!current()) return;
      set({ members: response.data.data.items });
    } catch (error) {
      if (!current()) return;
      toast.error(error.response?.data?.message || translate('projects.toast.fetchMembersError'));
    }
  },

  inviteMember: async (projectId, inviteData) => {
    const generation = epoch;
    const request = requests.inviteMember = (requests.inviteMember || 0) + 1;
    const current = () => generation === epoch && requests.inviteMember === request;
    try {
      await projectMemberApi.inviteMember(projectId, inviteData);
      if (!current()) return;
      toast.success(translate('projects.toast.inviteSuccess'));
    } catch (error) {
      if (!current()) return;
      toast.error(error.response?.data?.message || translate('projects.toast.inviteError'));
      throw error;
    }
  },

  removeMember: async (projectId, memberUserId) => {
    const generation = epoch;
    const rosterRequest = requests.fetchMembers;
    const request = requests.removeMember = (requests.removeMember || 0) + 1;
    const current = () => generation === epoch && requests.removeMember === request && requests.fetchMembers === rosterRequest;
    try {
      await projectMemberApi.removeMember(projectId, memberUserId);
      if (!current()) return;
      set((state) => ({
        members: state.members.filter((m) => m.userId !== memberUserId),
      }));
      toast.success(translate('projects.toast.removeMemberSuccess'));
    } catch (error) {
      if (!current()) return;
      toast.error(error.response?.data?.message || translate('projects.toast.removeMemberError'));
    }
  },

  fetchActivityLogs: async (projectId) => {
    const generation = epoch;
    const request = requests.fetchActivityLogs = (requests.fetchActivityLogs || 0) + 1;
    const current = () => generation === epoch && requests.fetchActivityLogs === request;
    try {
      const response = await projectApi.getActivityPage(projectId);
      if (!current()) return;
      set({ activityLogs: response.data.data.items });
    } catch (error) {
      if (!current()) return;
      console.error('Failed to fetch activity logs', error);
    }
  },
}));
