import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { act, cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import DashboardPage from '@/pages/dashboard/DashboardPage';
import { dashboardApi } from '@/api/dashboardApi';
import { boardApi } from '@/api/boardApi';
import { projectApi } from '@/api/projectApi';
const hub = vi.hoisted(() => ({ handlers: new Map(), on: vi.fn(), off: vi.fn(), invoke: vi.fn().mockResolvedValue() }));
const translate = vi.hoisted(() => key => key);
vi.mock('@/context/authState', () => ({ useAuth: () => ({ user: { id: 'user' } }) }));
vi.mock('@/context/LanguageContext', () => ({ useLanguage: () => ({ t: translate }) }));
vi.mock('@/context/NotificationContext', () => ({ useNotification: () => ({ hubConnection: hub, reconnectVersion: 0 }) }));
vi.mock('@/api/dashboardApi', () => ({ dashboardApi: { getStats: vi.fn(), getVelocity: vi.fn(), getActivity: vi.fn(), getUpcomingTasks: vi.fn() } }));
vi.mock('@/api/boardApi', () => ({ boardApi: { getBoards: vi.fn() } }));
vi.mock('@/api/projectApi', () => ({ projectApi: { getProjects: vi.fn() } }));
vi.mock('@/api/teamApi', () => ({ default: { getTeams: vi.fn().mockResolvedValue({ data: { data: [{ id: 'team', name: 'Test team' }] } }) } }));
vi.mock('@/components/dashboard/DashboardHeader', () => ({ default: () => null }));
vi.mock('@/components/dashboard/DashboardStats', () => ({ default: ({ stats }) => <output data-testid="stats">{stats.total}</output> }));
vi.mock('@/components/dashboard/TaskActivityChart', () => ({ default: ({ points, onTimeframeChange }) => <><output data-testid="velocity">{JSON.stringify(points)}</output><button onClick={() => onTimeframeChange('Week')}>Week</button><button onClick={() => onTimeframeChange('Year')}>Year</button></> }));
vi.mock('@/components/dashboard/RecentActivity', () => ({ default: ({ activities }) => <output data-testid="activity">{JSON.stringify(activities)}</output> }));
vi.mock('@/components/dashboard/UpcomingTasks', () => ({ default: () => null }));
vi.mock('@/components/dashboard/RecentProjects', () => ({ default: () => null }));
vi.mock('@/components/dashboard/RecentBoards', () => ({ default: () => null }));
const response = data => ({ data: { data } });
const deferred = () => { let resolve; const promise = new Promise(done => { resolve = done; }); return { promise, resolve }; };
beforeEach(() => {
  vi.clearAllMocks();
  hub.handlers.clear();
  hub.on.mockImplementation((event, handler) => hub.handlers.set(event, handler));
  hub.off.mockImplementation((event, handler) => { if (hub.handlers.get(event) === handler) hub.handlers.delete(event); });
  dashboardApi.getStats.mockResolvedValue(response({ total: 5 }));
  dashboardApi.getVelocity.mockResolvedValue(response(['initial']));
  dashboardApi.getActivity.mockResolvedValue(response({ items: ['initial'] }));
  dashboardApi.getUpcomingTasks.mockResolvedValue(response({ items: [] }));
  boardApi.getBoards.mockResolvedValue(response([]));
  projectApi.getProjects.mockResolvedValue(response([{ id: 'project', projectType: 'Personal', ownerId: 'user' }]));
});
afterEach(cleanup);
const mount = () => render(<MemoryRouter><DashboardPage /></MemoryRouter>);
const loaded = async () => { await waitFor(() => expect(screen.getByTestId('stats').textContent).toBe('5')); await waitFor(() => expect(hub.invoke).toHaveBeenCalledWith('JoinProject', 'project')); };

it('timeframe changes request velocity only and ignore earlier velocity response', async () => {
  mount(); await loaded();
  const counts = [dashboardApi.getStats, dashboardApi.getActivity, dashboardApi.getUpcomingTasks, boardApi.getBoards, projectApi.getProjects].map(mock => mock.mock.calls.length);
  const old = deferred();
  dashboardApi.getVelocity.mockReturnValueOnce(old.promise).mockResolvedValueOnce(response(['latest']));
  fireEvent.click(screen.getByText('Week'));
  await waitFor(() => expect(dashboardApi.getVelocity).toHaveBeenCalledTimes(2));
  fireEvent.click(screen.getByText('Year'));
  await waitFor(() => expect(screen.getByTestId('velocity').textContent).toContain('latest'));
  await act(async () => old.resolve(response(['stale'])));
  expect(screen.getByTestId('velocity').textContent).toContain('latest');
  expect([dashboardApi.getStats, dashboardApi.getActivity, dashboardApi.getUpcomingTasks, boardApi.getBoards, projectApi.getProjects].map(mock => mock.mock.calls.length)).toEqual(counts);
  console.info('PHASE4_PROFILE', JSON.stringify({ component: 'DashboardPage', timeframeChanges: 2, velocityRequests: dashboardApi.getVelocity.mock.calls.length - 1, unrelatedWidgetRequests: 0, initialWidgetRequests: counts }));
});

it('comment bursts refresh only activity once, ignore unrelated projects, and clean up handlers/timer', async () => {
  const mounted = mount(); await loaded();
  await act(async () => {
    for (let i = 0; i < 20; i++) hub.handlers.get('ProjectActivity')({ projectId: 'project', eventType: 'CommentAdded' });
    hub.handlers.get('ProjectActivity')({ projectId: 'other', eventType: 'TaskCreated' });
  });
  await waitFor(() => expect(dashboardApi.getActivity).toHaveBeenCalledTimes(2));
  expect(dashboardApi.getStats).toHaveBeenCalledTimes(1);
  expect(dashboardApi.getVelocity).toHaveBeenCalledTimes(1);
  expect(dashboardApi.getUpcomingTasks).toHaveBeenCalledTimes(1);
  expect(boardApi.getBoards).toHaveBeenCalledTimes(1);
  expect(projectApi.getProjects).toHaveBeenCalledTimes(1);
  act(() => hub.handlers.get('ProjectActivity')({ projectId: 'project', eventType: 'CommentAdded' }));
  mounted.unmount();
  await act(async () => new Promise(resolve => setTimeout(resolve, 130)));
  expect(dashboardApi.getActivity).toHaveBeenCalledTimes(2);
  expect(hub.handlers.size).toBe(0);
  console.info('PHASE4_PROFILE', JSON.stringify({ component: 'DashboardPage', commentEvents: 20, activityRefreshRequests: dashboardApi.getActivity.mock.calls.length - 1, unrelatedWidgetRequests: 0, listenersAfterUnmount: hub.handlers.size }));
});

it('old personal scope cannot overwrite the selected team', async () => {
  const old = deferred();
  dashboardApi.getStats.mockReturnValueOnce(old.promise).mockResolvedValueOnce(response({ total: 77 }));
  mount();
  await waitFor(() => expect(dashboardApi.getStats).toHaveBeenCalledTimes(1));
  fireEvent.change(screen.getByLabelText('Dashboard scope'), { target: { value: 'Team' } });
  fireEvent.change(screen.getByLabelText('Team'), { target: { value: 'team' } });
  await waitFor(() => expect(screen.getByTestId('stats').textContent).toBe('77'));
  await act(async () => old.resolve(response({ total: 2 })));
  expect(screen.getByTestId('stats').textContent).toBe('77');
});
