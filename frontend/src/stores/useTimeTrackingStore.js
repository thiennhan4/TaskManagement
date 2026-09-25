import { create } from 'zustand';
import { timeTrackingApi } from '@/api/timeTrackingApi';

let entriesRequest = 0;

const useTimeTrackingStore = create((set, get) => ({
  runningTimer: null,
  taskEntries: [],
  taskEntriesPage: null,
  entriesError: null,
  userEntries: [],
  reportData: null,
  isLoading: false,

  fetchRunningTimer: async () => {
    try {
      const res = await timeTrackingApi.getRunningTimer();
      set({ runningTimer: res.data.data });
    } catch {
      console.error('Failed to fetch running timer');
    }
  },

  startTimer: async (taskId, description = '', isBillable = false) => {
    set({ isLoading: true });
    try {
      const res = await timeTrackingApi.startTimer({ taskId, description, isBillable });
      set({ runningTimer: res.data.data });
    } finally {
      set({ isLoading: false });
    }
  },

  stopTimer: async (description) => {
    set({ isLoading: true });
    try {
      const { runningTimer } = get();
      if (!runningTimer) return;
      
      await timeTrackingApi.stopTimer(runningTimer.id, { description });
      set({ runningTimer: null });
      // Update taskEntries if needed, or caller can refetch
    } finally {
      set({ isLoading: false });
    }
  },

  fetchTaskEntries: async (taskId, page = 1) => {
    const request = ++entriesRequest;
    set({ isLoading: true, taskEntries: [], taskEntriesPage: null, entriesError: null });
    try {
      const res = await timeTrackingApi.getEntriesForTask(taskId, { page });
      if (request !== entriesRequest) return;
      set({ taskEntries: res.data.data.items || [], taskEntriesPage: res.data.data, entriesError: null });
    } catch (err) {
      if (request !== entriesRequest) return;
      set({ taskEntries: [], taskEntriesPage: null, entriesError: err.response?.data?.message || 'Failed to load time entries' });
    } finally {
      if (request === entriesRequest) set({ isLoading: false });
    }
  },

  fetchUserEntries: async (from, to) => {
    set({ isLoading: true });
    try {
      const res = await timeTrackingApi.getMyEntries(from, to);
      set({ userEntries: res.data.data || [] });
    } catch {
      console.error('Failed to fetch user entries');
    } finally {
      set({ isLoading: false });
    }
  },

  fetchReport: async (from, to, boardId = null) => {
    set({ isLoading: true });
    try {
      const res = await timeTrackingApi.getReport(from, to, boardId);
      set({ reportData: res.data.data });
    } catch {
      console.error('Failed to fetch report');
    } finally {
      set({ isLoading: false });
    }
  }
}));

export default useTimeTrackingStore;
