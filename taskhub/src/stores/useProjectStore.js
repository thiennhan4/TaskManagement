import { create } from 'zustand';
import { projectApi } from '../api/projectApi';
import { projectMemberApi } from '../api/projectMemberApi';
import { toast } from 'react-hot-toast';
import { translate } from '../i18n/translate';

export const useProjectStore = create((set, get) => ({
  projects: [],
  currentProject: null,
  members: [],
  activityLogs: [],
  isLoading: false,
  error: null,

  fetchProjects: async () => {
    if (get().isLoading) return;
    set({ isLoading: true });
    try {
      const response = await projectApi.getProjects();
      set({ projects: response.data?.data || [], isLoading: false, error: null });
    } catch (error) {
      set({ error: error.message, isLoading: false });
      toast.error(error.response?.data?.message || translate('projects.toast.fetchError'));
    }
  },

  fetchWorkspaceProjects: async (workspaceId, includeArchived) => {
    if (get().isLoading) return;
    set({ isLoading: true });
    try {
      const response = await projectApi.getWorkspaceProjects(workspaceId, includeArchived);
      set({ projects: response.data?.data || [], isLoading: false, error: null });
    } catch (error) {
      set({ error: error.message, isLoading: false });
      toast.error(error.response?.data?.message || translate('projects.toast.fetchWorkspaceError'));
    }
  },

  fetchProjectById: async (id) => {
    if (get().isLoading) return;
    set({ isLoading: true });
    try {
      const response = await projectApi.getProjectById(id);
      set({ currentProject: response.data?.data, isLoading: false, error: null });
      return response.data?.data;
    } catch (error) {
      set({ error: error.message, isLoading: false });
      toast.error(error.response?.data?.message || translate('projects.toast.fetchDetailError'));
    }
  },

  createProject: async (projectData, options = {}) => {
    set({ isLoading: true });
    try {
      const response = await projectApi.createProject(projectData);
      set((state) => ({ 
        projects: [response.data.data, ...state.projects],
        isLoading: false,
        error: null
      }));
      if (!options.silentSuccessToast) {
        toast.success(translate('projects.toast.createSuccess'));
      }
      return response.data.data;
    } catch (error) {
      set({ isLoading: false, error: error.response?.data?.message || error.message });
      if (!options.silentErrorToast) {
        toast.error(error.response?.data?.message || translate('projects.toast.createError'));
      }
      throw error;
    }
  },

  updateProject: async (id, projectData) => {
    try {
      const response = await projectApi.updateProject(id, projectData);
      set((state) => ({
        projects: state.projects.map((p) => (p.id === id ? response.data.data : p)),
        currentProject: state.currentProject?.id === id ? response.data.data : state.currentProject,
      }));
      toast.success(translate('projects.toast.updateSuccess'));
      return response.data.data;
    } catch (error) {
      toast.error(error.response?.data?.message || translate('projects.toast.updateError'));
      throw error;
    }
  },

  deleteProject: async (id) => {
    try {
      await projectApi.deleteProject(id);
      set((state) => ({
        projects: state.projects.filter((project) => project.id !== id),
        currentProject: state.currentProject?.id === id ? null : state.currentProject,
      }));
      toast.success(translate('projects.toast.deleteSuccess'));
    } catch (error) {
      toast.error(error.response?.data?.message || translate('projects.toast.deleteError'));
      throw error;
    }
  },

  archiveProject: async (id) => {
    try {
      const response = await projectApi.archiveProject(id);
      set((state) => ({
        projects: state.projects.map((p) => (p.id === id ? response.data.data : p)),
        currentProject: state.currentProject?.id === id ? response.data.data : state.currentProject,
      }));
      toast.success(translate('projects.toast.archiveSuccess'));
      return response.data.data;
    } catch (error) {
      toast.error(error.response?.data?.message || translate('projects.toast.archiveError'));
      throw error;
    }
  },

  restoreProject: async (id) => {
    try {
      const response = await projectApi.restoreProject(id);
      set((state) => ({
        projects: state.projects.map((p) => (p.id === id ? response.data.data : p)),
        currentProject: state.currentProject?.id === id ? response.data.data : state.currentProject,
      }));
      toast.success(translate('projects.toast.restoreSuccess'));
      return response.data.data;
    } catch (error) {
      toast.error(error.response?.data?.message || translate('projects.toast.restoreError'));
      throw error;
    }
  },

  fetchMembers: async (projectId) => {
    try {
      const response = await projectMemberApi.getMembers(projectId);
      set({ members: response.data.data });
    } catch (error) {
      toast.error(error.response?.data?.message || translate('projects.toast.fetchMembersError'));
    }
  },

  inviteMember: async (projectId, inviteData) => {
    try {
      await projectMemberApi.inviteMember(projectId, inviteData);
      toast.success(translate('projects.toast.inviteSuccess'));
    } catch (error) {
      toast.error(error.response?.data?.message || translate('projects.toast.inviteError'));
      throw error;
    }
  },

  removeMember: async (projectId, memberUserId) => {
    try {
      await projectMemberApi.removeMember(projectId, memberUserId);
      set((state) => ({
        members: state.members.filter((m) => m.userId !== memberUserId),
      }));
      toast.success(translate('projects.toast.removeMemberSuccess'));
    } catch (error) {
      toast.error(error.response?.data?.message || translate('projects.toast.removeMemberError'));
    }
  },

  fetchActivityLogs: async (projectId) => {
    try {
      const response = await projectApi.getProjectActivity(projectId);
      set({ activityLogs: response.data.data });
    } catch (error) {
      console.error('Failed to fetch activity logs', error);
    }
  },
}));
