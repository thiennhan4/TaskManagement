import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { format } from 'date-fns';
import { execFileSync } from 'node:child_process';
import process from 'node:process';
import { calendarDate, calendarDefaultView, calendarRange, layoutCalendarTasks, navigateCalendar } from '@/utils/calendarLayout';
import CalendarPage from '@/pages/calendar/CalendarPage';
import { useCalendarStore } from '@/stores/useCalendarStore';
import { calendarApi } from '@/api/calendarApi';
import axios from '@/api/axiosInstance';

vi.mock('@/api/axiosInstance', () => ({ default: { get: vi.fn() } }));
vi.mock('@/context/LanguageContext', () => ({ useLanguage: () => ({ t: key => key }) }));
vi.mock('@/components/tasks/CalendarTaskModal', () => ({ default: () => null }));
vi.mock('@/components/tasks/TaskDetailDrawer', () => ({ default: ({ taskId }) => taskId ? <output>Opened {taskId}</output> : null }));
beforeEach(() => {
  useCalendarStore.getState().reset(); vi.clearAllMocks();
  axios.get.mockResolvedValue({ data: { data: { items: [], page: 1, totalPages: 1, totalItems: 0 } } });
});
afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

it('navigates each view by its own interval, including month/year boundaries', () => {
  const date = new Date(2026, 11, 31, 9);
  expect(format(navigateCalendar(date, 'month', 1), 'yyyy-MM-dd')).toBe('2027-01-31');
  expect(format(navigateCalendar(date, 'week', 1), 'yyyy-MM-dd')).toBe('2027-01-07');
  expect(format(navigateCalendar(date, 'day', 1), 'yyyy-MM-dd')).toBe('2027-01-01');
  expect(format(navigateCalendar(new Date(2026, 0, 31), 'month', 1), 'yyyy-MM-dd')).toBe('2026-02-28');
  for (const view of ['day', 'week', 'month']) {
    const range = calendarRange(date, view);
    expect(range.start <= date && range.end >= date).toBe(true);
    expect(range.end - range.start).toBeLessThan(62 * 86400000);
  }
});

it('allocates lanes for more than two overlaps and reuses lanes for touching intervals', () => {
  const tasks = [
    ['a', '09:00', '10:30'], ['b', '09:15', '10:00'], ['c', '09:30', '11:00'], ['d', '10:00', '10:15'],
  ].map(([id, start, end]) => ({ id, startDate: '2026-10-02T' + start, dueDate: '2026-10-02T' + end }));
  const events = layoutCalendarTasks(tasks, calendarRange(new Date(2026, 9, 2), 'day')).get('2026-10-02');
  expect(events).toHaveLength(4); expect(events.every(event => event.lanes === 3)).toBe(true);
  for (let i = 0; i < events.length; i++) for (let j = i + 1; j < events.length; j++) {
    if (events[i].from < events[j].to && events[j].from < events[i].to) expect(events[i].lane).not.toBe(events[j].lane);
  }
  expect(events.find(e => e.task.id === 'b').lane).toBe(events.find(e => e.task.id === 'd').lane);
});

it('splits midnight/year boundaries and keeps deadlines, 00:00 and 23:59 reachable', () => {
  const range = { start: new Date(2026, 11, 31), end: new Date(2027, 0, 1, 23, 59, 59, 999) };
  const grouped = layoutCalendarTasks([
    { id: 'crossing', startDate: '2026-12-31T23:00', dueDate: '2027-01-01T01:00' },
    { id: 'midnight', dueDate: '2027-01-01T00:00' },
    { id: 'late', dueDate: '2027-01-01T23:59' }, { id: 'invalid', dueDate: 'invalid' },
  ], range);
  expect(grouped.get('2026-12-31')[0].to).toBe(1440);
  expect(grouped.get('2027-01-01').map(e => e.task.id)).toEqual(['crossing', 'midnight', 'late']);
  expect(grouped.get('2027-01-01')[0].from).toBe(0);
  expect(calendarDate('invalid')).toBeNull();
});

it('preserves explicit offset instants and local wall dates across DST without 24-hour day arithmetic', () => {
  const result = JSON.parse(execFileSync(process.execPath, ['--input-type=module', '-e', `
    import { calendarRange, calendarDate, layoutCalendarTasks } from './src/utils/calendarLayout.js';
    const range = calendarRange(new Date(2026, 2, 8), 'day');
    const days = layoutCalendarTasks([{id:'cross', startDate:'2026-03-07T23:00', dueDate:'2026-03-08T03:30'}], range);
    console.log(JSON.stringify({hours:(range.end-range.start+1)/3600000, local:calendarDate('2026-03-08T03:30').getHours(), instant:calendarDate('2026-03-08T07:30:00Z').getHours(), end:days.get('2026-03-08')[0].to}));
  `], { env: { ...process.env, TZ: 'America/New_York' }, encoding: 'utf8' }));
  expect(result).toEqual({ hours: 23, local: 3, instant: 3, end: 210 });
});

it('queries local wall boundaries matching offsetless SQL DateTime with bounded paging', async () => {
  const start = new Date(2026, 9, 2), end = new Date(2026, 9, 2, 23, 59, 59, 999);
  await calendarApi.getCalendarTasks(start, end, null, null, 2);
  const params = new URLSearchParams(axios.get.mock.calls[0][0].split('?')[1]);
  expect(params.get('start')).toBe('2026-10-02T00:00:00.000');
  expect(params.get('end')).toBe('2026-10-02T23:59:59.999');
  expect(params.get('page')).toBe('2'); expect(params.get('pageSize')).toBe('100');
});

it('month controls update the label and API range, and event buttons open canonical IDs', async () => {
  useCalendarStore.setState({ currentDate: new Date(2026, 9, 2), tasks: [] });
  render(<CalendarPage />);
  fireEvent.click(screen.getByRole('button', { name: 'Month', exact: true }));
  fireEvent.click(screen.getByRole('button', { name: 'Next month' }));
  expect(screen.getByRole('heading', { name: 'November 2026' })).toBeTruthy();
  await waitFor(() => expect(axios.get.mock.calls.at(-1)[0]).toContain('start=2026-11-01'));
  useCalendarStore.setState({ tasks: [{ id: 'canonical', title: 'Reachable event', startDate: '2026-11-02T09:00' }] });
  fireEvent.click(await screen.findByRole('button', { name: /Reachable event, Nov 2/ }));
  expect(screen.getByText('Opened canonical')).toBeTruthy();
});

it('mobile defaults to Day/Agenda and keeps an explicit user view after resizing', async () => {
  let phone = true;
  const listeners = new Set();
  vi.stubGlobal('matchMedia', () => ({ get matches() { return phone; }, addEventListener: (_, fn) => listeners.add(fn), removeEventListener: (_, fn) => listeners.delete(fn) }));
  expect([360, 768, 1280].map(calendarDefaultView)).toEqual(['day', 'week', 'week']);
  render(<CalendarPage />);
  await screen.findByRole('region', { name: 'Day agenda' });
  expect(screen.getByRole('button', { name: 'Next day' })).toBeTruthy();
  fireEvent.click(screen.getByRole('button', { name: 'Month', exact: true }));
  phone = false;
  // User choice remains authoritative even when a media change arrives.
  const { act } = await import('@testing-library/react');
  act(() => listeners.forEach(fn => fn()));
  expect(screen.getByRole('button', { name: 'Next month' })).toBeTruthy();
});
