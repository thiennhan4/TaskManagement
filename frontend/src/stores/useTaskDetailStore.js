import { create } from 'zustand';
import { taskApi } from '@/api/taskApi';

let version = 0;
let pending;
const empty = { taskId: null, task: null, isLoading: false, error: null };
export const useTaskDetailStore = create((set, get) => ({
  ...empty,
  reset: () => { version++; pending = null; set(empty); },
  load: (taskId) => {
    if (!taskId) return Promise.resolve();
    if (pending?.id === taskId) return pending.promise;
    const request = ++version;
    set({ taskId, task: get().taskId === taskId ? get().task : null, isLoading: true, error: null });
    const promise = taskApi.getTaskById(taskId).then(response => {
      if (request === version) set({ task: response.data.data });
    }).catch(error => {
      if (request === version) set({ error: error.response?.status === 403 ? 'You do not have permission to view this task.' : error.response?.status === 404 ? 'Task not found. It may have been deleted.' : error.response?.data?.message || 'Could not load task. Check your connection and retry.' });
    }).finally(() => {
      if (request === version) set({ isLoading: false });
      if (pending?.promise === promise) pending = null;
    });
    pending = { id: taskId, promise };
    return promise;
  },
  update: task => {
    if (get().taskId === task.id) {
      // Hub cards are summaries. Refetch canonical detail instead of replacing it.
      pending = null;
      return get().load(task.id);
    }
  },
  remove: id => { if (get().taskId === id) { version++; pending = null; set({ task: null, isLoading: false, error: 'Task not found. It may have been deleted.' }); } },
}));
