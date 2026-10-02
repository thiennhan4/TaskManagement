import useNotificationStore from '@/stores/useNotificationStore';
import { useCalendarStore } from '@/stores/useCalendarStore';
import { useProjectStore } from '@/stores/useProjectStore';
import useTimeTrackingStore from '@/stores/useTimeTrackingStore';
import { useTaskDetailStore } from '@/stores/useTaskDetailStore';

export function resetUserState() {
  useNotificationStore.getState().reset();
  useCalendarStore.getState().reset();
  useProjectStore.getState().reset();
  useTimeTrackingStore.getState().reset();
  useTaskDetailStore.getState().reset();
}
