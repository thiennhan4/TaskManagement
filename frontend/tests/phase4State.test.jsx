import { beforeEach, describe, expect, it, vi } from 'vitest';
import { act, renderHook, cleanup, render, fireEvent, screen, waitFor } from '@testing-library/react';
import { useShallow } from 'zustand/react/shallow';
import { notificationApi } from '@/api/notificationApi';
import { calendarApi } from '@/api/calendarApi';
import { projectApi } from '@/api/projectApi';
import { timeTrackingApi } from '@/api/timeTrackingApi';
import { taskApi } from '@/api/taskApi';
import useNotificationStore from '@/stores/useNotificationStore';
import { useCalendarStore } from '@/stores/useCalendarStore';
import { useProjectStore } from '@/stores/useProjectStore';
import useTimeTrackingStore from '@/stores/useTimeTrackingStore';
import { useTaskDetailStore } from '@/stores/useTaskDetailStore';
import { resetUserState } from '@/stores/resetUserState';
import { groupCalendarTasks } from '@/utils/calendarGroups';
import { upsertColumns, upsertTask, removeTask, updateTaskCount, mergeBoardPage } from '@/utils/boardState';
import TimeTrackingWidget from '@/components/timetracking/TimeTrackingWidget';
import { projectMemberApi } from '@/api/projectMemberApi';

vi.mock('@/api/notificationApi', () => ({ notificationApi: { getMyNotifications: vi.fn(), getUnreadCount: vi.fn(), markAsRead: vi.fn(), markAllAsRead: vi.fn() } }));
vi.mock('@/api/calendarApi', () => ({ calendarApi: { getCalendarTasks: vi.fn() } }));
vi.mock('@/api/projectApi', () => ({ projectApi: { getPage: vi.fn(), getProjectById: vi.fn(), createProject: vi.fn() } }));
vi.mock('@/api/projectMemberApi', () => ({ projectMemberApi: { getPage: vi.fn(), removeMember: vi.fn() } }));
vi.mock('@/api/timeTrackingApi', () => ({ timeTrackingApi: { getEntriesForTask: vi.fn(), getRunningTimer: vi.fn(), getReport: vi.fn(), getMyEntries: vi.fn(), stopTimer: vi.fn(), startTimer: vi.fn() } }));
vi.mock('@/api/taskApi', () => ({ taskApi: { getTaskById: vi.fn() } }));
vi.mock('@/i18n/translate', () => ({ translate: key => key }));
const response = data => ({ data: { success: true, data } });
const page = (items, number = 1, total = items.length) => ({ items, page: number, pageSize: 20, totalItems: total, totalPages: Math.ceil(total / 20) });
const deferred = () => { let resolve; const promise = new Promise(done => { resolve = done; }); return { promise, resolve }; };
beforeEach(() => { cleanup(); resetUserState(); vi.resetAllMocks(); });

it.each(['applyRead', 'applyReadAll', 'receive'])('%s preserves a pending notification page selection', async action => {
  notificationApi.getMyNotifications.mockResolvedValue(response(page([{ id: 'first' }], 1, 40)));
  notificationApi.getUnreadCount.mockResolvedValue(response(2));
  const store = useNotificationStore.getState();
  await store.fetchNotifications();
  const old = deferred();
  notificationApi.getMyNotifications.mockImplementation(number => number === 2
    ? old.promise : Promise.resolve(response(page([{ id: 'first' }], 1, 40))));
  const selecting = store.fetchNotifications(2);
  notificationApi.getMyNotifications.mockImplementation(number => Promise.resolve(response(page([{ id: 'selected' }], number, 40))));
  const refresh = store[action](action === 'receive' ? { id: 'first', isRead: true } : 'first');
  if (action !== 'receive') await refresh;
  old.resolve(response(page([{ id: 'outdated' }], 2, 40)));
  await Promise.all([refresh, selecting]);
  expect(useNotificationStore.getState().page.page).toBe(2);
  expect(useNotificationStore.getState().notifications[0].id).toBe('selected');
});

it('late member removal for project A cannot change project B roster', async () => {
  projectMemberApi.getPage.mockImplementation(id => Promise.resolve(response(page([{ userId: 'shared', projectId: id }]))));
  const old = deferred();
  projectMemberApi.removeMember.mockReturnValue(old.promise);
  await useProjectStore.getState().fetchMembers('A');
  const removing = useProjectStore.getState().removeMember('A', 'shared');
  await useProjectStore.getState().fetchMembers('B');
  old.resolve(response(null));
  await removing;
  expect(useProjectStore.getState().members).toEqual([{ userId: 'shared', projectId: 'B' }]);
});

it('member removal still updates the current project roster', async () => {
  projectMemberApi.getPage.mockResolvedValue(response(page([{ userId: 'removed' }, { userId: 'kept' }])));
  projectMemberApi.removeMember.mockResolvedValue(response(null));
  await useProjectStore.getState().fetchMembers('A');
  await useProjectStore.getState().removeMember('A', 'removed');
  expect(useProjectStore.getState().members).toEqual([{ userId: 'kept' }]);
});

it('stopping task A timer after switching to task B does not reload task A history', async () => {
  const old = deferred();
  timeTrackingApi.getRunningTimer.mockResolvedValue(response({ id: 'timer', taskId: 'A' }));
  timeTrackingApi.getEntriesForTask.mockImplementation(id => Promise.resolve(response(page([{ id, durationSeconds: 60 }]))));
  timeTrackingApi.stopTimer.mockReturnValue(old.promise);
  const mounted = render(<TimeTrackingWidget taskId="A" />);
  fireEvent.click(await screen.findByRole('button', { name: 'Stop' }));
  mounted.rerender(<TimeTrackingWidget taskId="B" />);
  await waitFor(() => expect(useTimeTrackingStore.getState().taskEntries[0]?.id).toBe('B'));
  await act(async () => old.resolve(response(null)));
  expect(useTimeTrackingStore.getState().taskEntries[0].id).toBe('B');
  expect(timeTrackingApi.getEntriesForTask.mock.calls.map(([id]) => id)).toEqual(['A', 'B']);
  mounted.unmount();
});

it.each([false, true])('timer stop refreshes only a still-mounted task (unmounted: %s)', async unmounted => {
  const old = deferred();
  timeTrackingApi.getRunningTimer.mockResolvedValue(response({ id: 'timer', taskId: 'A' }));
  timeTrackingApi.getEntriesForTask.mockResolvedValue(response(page([])));
  timeTrackingApi.stopTimer.mockReturnValue(old.promise);
  const mounted = render(<TimeTrackingWidget taskId="A" />);
  fireEvent.click(await screen.findByRole('button', { name: 'Stop' }));
  if (unmounted) mounted.unmount();
  await act(async () => old.resolve(response(null)));
  expect(timeTrackingApi.getEntriesForTask).toHaveBeenCalledTimes(unmounted ? 1 : 2);
});

it('late timer start for task A does not clear task B description', async () => {
  const old = deferred();
  timeTrackingApi.getRunningTimer.mockResolvedValue(response(null));
  timeTrackingApi.getEntriesForTask.mockResolvedValue(response(page([])));
  timeTrackingApi.startTimer.mockReturnValue(old.promise);
  const mounted = render(<TimeTrackingWidget taskId="A" />);
  await act(async () => {});
  fireEvent.click(screen.getByRole('button', { name: 'Start' }));
  mounted.rerender(<TimeTrackingWidget taskId="B" />);
  const input = screen.getByPlaceholderText('What are you working on?');
  fireEvent.change(input, { target: { value: 'Task B description' } });
  await act(async () => old.resolve(response({ id: 'timer-A', taskId: 'A' })));
  expect(input.value).toBe('Task B description');
});

it('notification owner uses server totals, dedupes IDs and retains at most one 20-item page after 1000 replayed events', async () => {
  notificationApi.getMyNotifications.mockResolvedValue(response(page(Array.from({ length: 40 }, (_, i) => ({ id: i % 30, isRead: false })), 1, 500)));
  notificationApi.getUnreadCount.mockResolvedValue(response(321));
  await useNotificationStore.getState().fetchNotifications();
  expect(useNotificationStore.getState().unreadCount).toBe(321);
  await Promise.all(Array.from({ length: 1000 }, () => useNotificationStore.getState().receive({ id: 1, isRead: false })));
  expect(useNotificationStore.getState().notifications).toHaveLength(20);
  expect(new Set(useNotificationStore.getState().notifications.map(n => n.id)).size).toBe(20);
  expect(notificationApi.getMyNotifications.mock.calls.length).toBeLessThanOrEqual(3);
  expect(useNotificationStore.getState().page.totalItems).toBe(500);
});

it('read/read-all events and repeated HTTP mutations use authoritative count without double decrements', async () => {
  let read = false, count = 87;
  notificationApi.getMyNotifications.mockImplementation(() => Promise.resolve(response(page([{ id: 1, isRead: read }], 1, 90))));
  notificationApi.getUnreadCount.mockImplementation(() => Promise.resolve(response(count)));
  notificationApi.markAsRead.mockImplementation(async () => { read = true; count = 86; });
  notificationApi.markAllAsRead.mockImplementation(async () => { read = true; count = 0; });
  const store = useNotificationStore.getState();
  await store.fetchNotifications();
  await Promise.all([store.markAsRead(1), store.markAsRead(1)]);
  await store.markAsRead(1);
  await store.applyRead(1);
  expect(notificationApi.markAsRead).toHaveBeenCalledTimes(1);
  expect(useNotificationStore.getState().unreadCount).toBe(86);
  await store.markAllAsRead();
  await store.applyReadAll();
  expect(useNotificationStore.getState().unreadCount).toBe(0);
  expect(useNotificationStore.getState().notifications[0].isRead).toBe(true);
});

it('notification page and count reads cannot restore account A after reset/login B', async () => {
  const old = deferred(), oldCount = deferred();
  notificationApi.getMyNotifications.mockReturnValueOnce(old.promise).mockResolvedValue(response(page([{ id: 'B' }])));
  notificationApi.getUnreadCount.mockReturnValueOnce(oldCount.promise).mockResolvedValue(response(2));
  const a = useNotificationStore.getState().fetchNotifications();
  resetUserState();
  await useNotificationStore.getState().fetchNotifications();
  old.resolve(response(page([{ id: 'A' }]))); oldCount.resolve(response(90));
  await a;
  expect(useNotificationStore.getState().notifications.map(n => n.id)).toEqual(['B']);
  expect(useNotificationStore.getState().unreadCount).toBe(2);
});

describe.each([
  ['calendar', useCalendarStore, calendarApi, 'getCalendarTasks', 'fetchCalendarTasks', [], state => state.tasks, items => page(items)],
  ['projects', useProjectStore, projectApi, 'getPage', 'fetchProjects', [], state => state.projects, items => page(items)],
  ['project detail', useProjectStore, projectApi, 'getProjectById', 'fetchProjectById', ['project'], state => [state.currentProject], items => items[0]],
  ['time history', useTimeTrackingStore, timeTrackingApi, 'getEntriesForTask', 'fetchTaskEntries', ['task'], state => state.taskEntries, items => page(items)],
  ['time user entries', useTimeTrackingStore, timeTrackingApi, 'getMyEntries', 'fetchUserEntries', [], state => state.userEntries, items => page(items)],
  ['time report', useTimeTrackingStore, timeTrackingApi, 'getReport', 'fetchReport', [], state => state.reportData.items, items => page(items)],
  ['task detail', useTaskDetailStore, taskApi, 'getTaskById', 'load', ['task'], state => [state.task], items => items[0]],
])('%s request ownership', (_name, store, api, method, action, args, select, shape) => {
  it('late A cannot replace B after reset, or overwrite the loading/error state', async () => {
    const old = deferred();
    api[method].mockReturnValueOnce(old.promise).mockResolvedValueOnce(response(shape([{ id: 'B' }])));
    const a = store.getState()[action](...args);
    resetUserState();
    await store.getState()[action](...args);
    old.resolve(response(shape([{ id: 'A' }])));
    await a;
    expect(select(store.getState()).map(item => item.id)).toEqual(['B']);
    expect(store.getState().isLoading).toBe(false);
  });
  it('latest resource request wins within one account', async () => {
    const old = deferred();
    api[method].mockReturnValueOnce(old.promise).mockResolvedValueOnce(response(shape([{ id: 'B' }])));
    const a = store.getState()[action](...args);
    await store.getState()[action](...(action === 'load' ? ['other-task'] : args));
    old.resolve(response(shape([{ id: 'A' }])));
    await a;
    expect(select(store.getState()).map(item => item.id)).toEqual(['B']);
  });
});

it('reset clears selections, timers and all retained user state', () => {
  useCalendarStore.setState({ tasks: [{ id: 'A' }], selectedTask: 'A', selectedDate: new Date(), isTaskDrawerOpen: true });
  useProjectStore.setState({ projects: [{ id: 'A' }], currentProject: { id: 'A' }, members: [{ id: 'A' }], activityLogs: [{ id: 'A' }] });
  useTimeTrackingStore.setState({ runningTimer: { id: 'A' }, userEntries: [{ id: 'A' }], reportData: { id: 'A' } });
  resetUserState();
  expect(useCalendarStore.getState()).toMatchObject({ tasks: [], selectedTask: null, isTaskDrawerOpen: false });
  expect(useProjectStore.getState()).toMatchObject({ projects: [], currentProject: null, members: [], activityLogs: [] });
  expect(useTimeTrackingStore.getState()).toMatchObject({ runningTimer: null, userEntries: [], reportData: null });
});

it('deduplicates simultaneous running timer and same-task detail requests', async () => {
  timeTrackingApi.getRunningTimer.mockResolvedValue(response(null));
  taskApi.getTaskById.mockResolvedValue(response({ id: 'task' }));
  await Promise.all([useTimeTrackingStore.getState().fetchRunningTimer(), useTimeTrackingStore.getState().fetchRunningTimer(),
    useTaskDetailStore.getState().load('task'), useTaskDetailStore.getState().load('task')]);
  expect(timeTrackingApi.getRunningTimer).toHaveBeenCalledTimes(1);
  expect(taskApi.getTaskById).toHaveBeenCalledTimes(1);
});

it('calendar indexes preserve local day/hour, start-date precedence, order and missing-date handling', () => {
  const tasks = [{ id: 1, startDate: '2026-09-01T08:10:00', dueDate: '2026-09-02T09:00:00' },
    { id: 2, dueDate: '2026-09-01T08:40:00' }, { id: 3 }, { id: 4, startDate: '2026-09-02T00:00:00' }];
  const groups = groupCalendarTasks(tasks);
  expect(groups.hours.get('2026-09-01:8')).toEqual(tasks.slice(0, 2));
  expect(groups.days.get('2026-09-02')).toEqual([tasks[3]]);
  expect(groups.days.size).toBe(2);
});

it('selective subscriptions ignore unrelated project store writes', () => {
  let renders = 0;
  renderHook(() => { renders++; return useProjectStore(useShallow(s => ({ projects: s.projects, fetchProjects: s.fetchProjects }))); });
  const before = renders;
  act(() => useProjectStore.setState({ members: [{ id: 'member' }] }));
  expect(renders).toBe(before);
});

it.each(['HTTP then hub', 'hub then HTTP'])('%s creates one task/column and preserves untouched columns', () => {
  const other = { id: 'other', tasks: [{ id: 'unrelated' }] };
  const columns = [{ id: 'list', tasks: [] }, other];
  const task = { id: 'new', listId: 'list', title: 'Created' };
  const once = upsertTask(columns, task);
  const twice = upsertTask(once, { ...task });
  expect(twice).toBe(once);
  expect(twice[0].tasks).toHaveLength(1);
  expect(twice[1]).toBe(other);
  const created = upsertColumns(columns, [{ id: 'new-column', name: 'New' }]);
  expect(upsertColumns(created, [{ id: 'new-column', name: 'New' }])).toBe(created);
  expect(removeTask(twice, 'new')[1]).toBe(other);
  expect(updateTaskCount(twice, { taskId: 'new', count: 5 })[1]).toBe(other);
  expect(mergeBoardPage(twice, [{ id: 'list', tasks: [task], taskPage: { page: 2 } }], 'list')[0].tasks).toHaveLength(1);
});

it('realtime replay cannot expand board collections past loaded capacity', () => {
  let columns = [{ id: 'list', tasks: [] }];
  for (let i = 0; i < 1000; i++) columns = upsertTask(columns, { id: i, listId: 'list' });
  expect(columns[0].tasks).toHaveLength(50);
  expect(columns[0].refreshRequired).toBe(true);
  for (let i = 0; i < 1000; i++) columns = upsertColumns(columns, [{ id: 'column-' + i }]);
  expect(columns).toHaveLength(20);
});
