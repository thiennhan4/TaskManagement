import { Profiler } from 'react';
import { act, cleanup, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import CalendarPage from '@/pages/calendar/CalendarPage';
import TimeTrackingWidget from '@/components/timetracking/TimeTrackingWidget';
import { useCalendarStore } from '@/stores/useCalendarStore';
import useTimeTrackingStore from '@/stores/useTimeTrackingStore';
import { resetUserState } from '@/stores/resetUserState';
import { groupCalendarTasks } from '@/utils/calendarGroups';
vi.mock('@/utils/calendarGroups', async importOriginal => {
  const actual = await importOriginal();
  return { ...actual, groupCalendarTasks: vi.fn(actual.groupCalendarTasks) };
});
vi.mock('@/context/LanguageContext', () => ({ useLanguage: () => ({ t: key => key }) }));
vi.mock('@/components/tasks/CalendarTaskModal', () => ({ default: () => null }));
vi.mock('@/components/tasks/TaskDetailDrawer', () => ({ default: () => null }));
beforeEach(() => { resetUserState(); vi.clearAllMocks(); });
afterEach(cleanup);

it('CalendarPage groups once per changed task array, across 168 slots and unrelated renders', () => {
  const date = new Date('2026-09-01T08:10:00');
  const task = { id: 'task', title: 'Grouped task', startDate: '2026-09-01T08:10:00' };
  const fetch = vi.fn();
  useCalendarStore.setState({ currentDate: date, tasks: [task], fetchCalendarTasks: fetch });
  render(<CalendarPage />);
  expect(screen.getByText('Grouped task')).toBeTruthy();
  expect(groupCalendarTasks).toHaveBeenCalledTimes(1);
  act(() => useCalendarStore.setState({ selectedDate: date }));
  expect(groupCalendarTasks).toHaveBeenCalledTimes(1);
  act(() => useCalendarStore.setState({ tasks: [task, { ...task, id: 'second', title: 'Second grouped task' }] }));
  expect(groupCalendarTasks).toHaveBeenCalledTimes(2);
  expect(screen.getByText('Second grouped task')).toBeTruthy();
  console.info('PHASE4_PROFILE', JSON.stringify({ component: 'CalendarPage', weekSlots: 168, initialGroupingCalls: 1, callsAfterSelection: 1, callsAfterTaskChange: groupCalendarTasks.mock.calls.length }));
});

it('TimeTrackingWidget skips unrelated user-history updates and renders changed task history', () => {
  useTimeTrackingStore.setState({ fetchRunningTimer: vi.fn(), fetchTaskEntries: vi.fn() });
  let commits = 0;
  render(<Profiler id="timer" onRender={() => { commits++; }}><TimeTrackingWidget taskId="task" /></Profiler>);
  const initial = commits;
  act(() => useTimeTrackingStore.setState({ userEntries: [{ id: 'other' }] }));
  expect(commits).toBe(initial);
  act(() => useTimeTrackingStore.setState({ taskEntries: [{ id: 'entry', durationSeconds: 3600, startTime: '2026-09-01T08:00:00' }] }));
  expect(commits).toBe(initial + 1);
  expect(screen.getByText('Page total: 1h 0m')).toBeTruthy();
  console.info('PHASE4_PROFILE', JSON.stringify({ component: 'TimeTrackingWidget', initialCommits: initial, unrelatedUpdateCommits: 0, taskHistoryUpdateCommits: commits - initial }));
});
