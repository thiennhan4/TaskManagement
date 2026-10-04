import { create } from 'zustand';
import { timeTrackingApi } from '@/api/timeTrackingApi';

let epoch = 0;
const versions = { timer: 0, task: 0, user: 0, report: 0 };
let runningRequest;
const initial = { runningTimer: null, taskEntries: [], taskEntriesPage: null, entriesError: null,
  userEntries: [], userEntriesPage: null, reportData: null, isLoading: false };
const useTimeTrackingStore = create((set, get) => {
  const begin = lane => {
    const generation = epoch, version = ++versions[lane];
    return () => generation === epoch && version === versions[lane];
  };
  return {
    ...initial,
    reset: () => { epoch++; runningRequest = null; set(initial); },
    fetchRunningTimer: () => {
      if (runningRequest) return runningRequest;
      const current = begin('timer');
      const pending = timeTrackingApi.getRunningTimer().then(res => {
        if (current()) set({ runningTimer: res.data.data });
      }).catch(() => { if (current()) set({ entriesError: 'Failed to fetch running timer' }); })
        .finally(() => { if (runningRequest === pending) runningRequest = null; });
      runningRequest = pending;
      return pending;
    },
    startTimer: async (taskId, description = '', isBillable = false) => {
      const current = begin('timer');
      runningRequest = null;
      set({ isLoading: true });
      try {
        const res = await timeTrackingApi.startTimer({ taskId, description, isBillable });
        if (current()) set({ runningTimer: res.data.data });
      } finally { if (current()) set({ isLoading: false }); }
    },
    stopTimer: async description => {
      const current = begin('timer');
      runningRequest = null;
      const timer = get().runningTimer;
      if (!timer) return;
      set({ isLoading: true });
      try {
        await timeTrackingApi.stopTimer(timer.id, { description });
        if (current()) set({ runningTimer: null });
      } finally { if (current()) set({ isLoading: false }); }
    },
    fetchTaskEntries: async (taskId, page = 1) => {
      const current = begin('task');
      set({ isLoading: true, taskEntries: [], taskEntriesPage: null, entriesError: null });
      try {
        const res = await timeTrackingApi.getEntriesForTask(taskId, { page });
        if (current()) {
          const { items, ...metadata } = res.data.data;
          set({ taskEntries: items || [], taskEntriesPage: metadata });
        }
      } catch (err) {
        if (current()) set({ entriesError: err.response?.data?.message || 'Failed to load time entries' });
      } finally { if (current()) set({ isLoading: false }); }
    },
    fetchUserEntries: async (from, to, page = 1) => {
      const current = begin('user');
      set({ isLoading: true, userEntries: [], userEntriesPage: null });
      try {
        const res = await timeTrackingApi.getMyEntries(from, to, page);
        if (current()) {
          const { items, ...metadata } = res.data.data;
          set({ userEntries: items, userEntriesPage: metadata });
        }
      } catch {
        if (current()) set({ entriesError: 'Failed to fetch user entries' });
      } finally { if (current()) set({ isLoading: false }); }
    },
    fetchReport: async (from, to, boardId = null, page = 1) => {
      const current = begin('report');
      set({ isLoading: true, reportData: null });
      try {
        const res = await timeTrackingApi.getReport(from, to, boardId, page);
        if (current()) set({ reportData: res.data.data });
      } catch {
        if (current()) set({ entriesError: 'Failed to fetch report' });
      } finally { if (current()) set({ isLoading: false }); }
    },
  };
});
export default useTimeTrackingStore;
