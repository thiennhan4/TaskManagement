import { create } from 'zustand';
import { timeTrackingApi } from '../api/timeTrackingApi';

const useTimeTrackingStore = create((set, get) => ({
  runningTimer: null,
  taskEntries: [],
  userEntries: [],
  reportData: null,
  isLoading: false,

  fetchRunningTimer: async () => {
    try {
      const res = await timeTrackingApi.getRunningTimer();
      set({ runningTimer: res.data.data });
    } catch (err) {
      console.error('Failed to fetch running timer:', err);
    }
  },

  startTimer: async (taskId, description = '', isBillable = false) => {
    set({ isLoading: true });
    try {
      const res = await timeTrackingApi.startTimer({ taskId, description, isBillable });
      set({ runningTimer: res.data.data });
    } catch (err) {
      console.error('Failed to start timer:', err);
      throw err;
    } finally {
      set({ isLoading: false });
    }
  },

  stopTimer: async (description) => {
    set({ isLoading: true });
    try {
      const { runningTimer } = get();
      if (!runningTimer) return;
      
      const res = await timeTrackingApi.stopTimer(runningTimer.id, { description });
      set({ runningTimer: null });
      // Update taskEntries if needed, or caller can refetch
    } catch (err) {
      console.error('Failed to stop timer:', err);
      throw err;
    } finally {
      set({ isLoading: false });
    }
  },

  fetchTaskEntries: async (taskId) => {
    set({ isLoading: true });
    try {
      const res = await timeTrackingApi.getEntriesForTask(taskId);
      set({ taskEntries: res.data.data || [] });
    } catch (err) {
      console.error('Failed to fetch task entries:', err);
    } finally {
      set({ isLoading: false });
    }
  },

  fetchUserEntries: async (from, to) => {
    set({ isLoading: true });
    try {
      const res = await timeTrackingApi.getMyEntries(from, to);
      set({ userEntries: res.data.data || [] });
    } catch (err) {
      console.error('Failed to fetch user entries:', err);
    } finally {
      set({ isLoading: false });
    }
  },

  fetchReport: async (from, to, boardId = null) => {
    set({ isLoading: true });
    try {
      const res = await timeTrackingApi.getReport(from, to, boardId);
      set({ reportData: res.data.data });
    } catch (err) {
      console.error('Failed to fetch report:', err);
    } finally {
      set({ isLoading: false });
    }
  }
}));

export default useTimeTrackingStore;
