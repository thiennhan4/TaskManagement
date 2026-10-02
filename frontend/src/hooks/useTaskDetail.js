import { useEffect } from 'react';
import { useShallow } from 'zustand/react/shallow';
import { useTaskDetailStore } from '@/stores/useTaskDetailStore';

export function useTaskDetail(taskId) {
  const state = useTaskDetailStore(useShallow(s => ({
    task: s.taskId === taskId ? s.task : null,
    isLoading: s.taskId !== taskId || s.isLoading,
    error: s.taskId === taskId ? s.error : null,
    load: s.load,
  })));
  const load = state.load;
  useEffect(() => {
    load(taskId);
    return () => {
      if (useTaskDetailStore.getState().taskId === taskId) useTaskDetailStore.getState().reset();
    };
  }, [taskId, load]);
  return state;
}
