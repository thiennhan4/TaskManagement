import { StrictMode } from 'react';
import { AxiosError } from 'axios';
import { beforeEach, afterEach, expect, it, vi } from 'vitest';
import { act, cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { AuthProvider } from '@/context/AuthContext';
import { useAuth } from '@/context/authState';
import api, { getAccessToken, refreshSession, setAccessToken } from '@/api/axiosInstance';
import { useCalendarStore } from '@/stores/useCalendarStore';
import { useProjectStore } from '@/stores/useProjectStore';
import useNotificationStore from '@/stores/useNotificationStore';
import useTimeTrackingStore from '@/stores/useTimeTrackingStore';
import { useTaskDetailStore } from '@/stores/useTaskDetailStore';
import { resetUserState } from '@/stores/resetUserState';
import { NotificationProvider, useNotification } from '@/context/NotificationContext';
const hub = vi.hoisted(() => ({ connections: [] }));
vi.mock('@microsoft/signalr', () => ({ HubConnectionBuilder: class {
  withUrl() { return this; }
  withAutomaticReconnect() { return this; }
  build() {
    const handlers = new Map();
    const connection = {
      handlers, on: (name, handler) => handlers.set(name, handler),
      off: (name, handler) => { if (handlers.get(name) === handler) handlers.delete(name); },
      onreconnected: vi.fn(), onclose: vi.fn(), start: vi.fn().mockResolvedValue(), stop: vi.fn().mockResolvedValue(),
    };
    hub.connections.push(connection);
    return connection;
  }
} }));
vi.mock('@/api/notificationApi', () => ({ notificationApi: {
  getMyNotifications: vi.fn().mockResolvedValue({ data: { data: { items: [], page: 1, pageSize: 20, totalItems: 0, totalPages: 0 } } }),
  getUnreadCount: vi.fn().mockResolvedValue({ data: { data: 0 } }),
} }));
function HubStatus() {
  const { hubConnection } = useNotification();
  return <output>{hubConnection ? 'connected' : 'waiting'}</output>;
}
const response = (data, config) => ({ data: { success: true, data }, status: 200, config });
function Session() {
  const { user, isAuthenticated, isLoading, logout, login } = useAuth();
  return <><output>{user?.id || 'anonymous'}</output><output>{`auth:${isAuthenticated};loading:${isLoading}`}</output><button onClick={logout}>logout</button><button onClick={() => login('b@example.test', 'test')}>login B</button></>;
}
const originalAdapter = api.defaults.adapter;
beforeEach(() => { setAccessToken(null); resetUserState(); hub.connections.length = 0; });
afterEach(async () => { cleanup(); await act(async () => {}); api.defaults.adapter = originalAdapter; });
it('StrictMode bootstrap has one effective refresh; logout resets user stores immediately before slow HTTP completion', async () => {
  let refreshes = 0, completeLogout;
  api.defaults.adapter = async config => {
    if (config.url === '/auth/refresh') { refreshes++; return response({ token: 'synthetic-a', user: { id: 'A' } }, config); }
    if (config.url === '/auth/logout') { await new Promise(resolve => { completeLogout = resolve; }); return response(null, config); }
    return response({ token: 'synthetic-b', user: { id: 'B' } }, config);
  };
  render(<StrictMode><AuthProvider><NotificationProvider><Session /><HubStatus /></NotificationProvider></AuthProvider></StrictMode>);
  expect(await screen.findByText('A')).toBeTruthy();
  await screen.findByText('connected');
  expect(hub.connections).toHaveLength(1);
  const connectionA = hub.connections[0];
  expect(connectionA.handlers.size).toBe(3);
  expect(refreshes).toBe(1);
  act(() => {
    useCalendarStore.setState({ tasks: [{ id: 'A' }] });
    useProjectStore.setState({ projects: [{ id: 'A' }] });
    useNotificationStore.setState({ notifications: [{ id: 'A' }], unreadCount: 10 });
    useTimeTrackingStore.setState({ runningTimer: { id: 'A' } });
  });
  fireEvent.click(screen.getByText('logout'));
  expect(screen.getByText('anonymous')).toBeTruthy();
  expect(getAccessToken()).toBeNull();
  expect(useCalendarStore.getState().tasks).toEqual([]);
  expect(useProjectStore.getState().projects).toEqual([]);
  expect(useNotificationStore.getState().notifications).toEqual([]);
  expect(useTimeTrackingStore.getState().runningTimer).toBeNull();
  expect(connectionA.stop).toHaveBeenCalledTimes(1);
  expect(connectionA.handlers.size).toBe(0);
  await waitFor(() => expect(completeLogout).toBeTypeOf('function'));
  fireEvent.click(screen.getByText('login B'));
  expect(await screen.findByText('B')).toBeTruthy();
  await screen.findByText('connected');
  expect(hub.connections).toHaveLength(2);
  expect(hub.connections[1].handlers.size).toBe(3);
  await act(async () => completeLogout());
  expect(screen.getByText('B')).toBeTruthy();
  expect(getAccessToken()).toBe('synthetic-b');
});

it('late bootstrap cannot restore an account after logout', async () => {
  let finish;
  api.defaults.adapter = async config => {
    if (config.url === '/auth/refresh') {
      await new Promise(resolve => { finish = resolve; });
      return response({ token: 'synthetic-a', user: { id: 'A' } }, config);
    }
    return response(null, config);
  };
  render(<AuthProvider><Session /></AuthProvider>);
  await waitFor(() => expect(finish).toBeTypeOf('function'));
  fireEvent.click(screen.getByText('logout'));
  await act(async () => finish());
  expect(screen.getByText('anonymous')).toBeTruthy();
  expect(getAccessToken()).toBeNull();
});

function seedUserStores() {
  act(() => {
    useCalendarStore.setState({ tasks: [{ id: 'A' }] });
    useProjectStore.setState({ projects: [{ id: 'A' }] });
    useNotificationStore.setState({ notifications: [{ id: 'A' }], unreadCount: 10 });
    useTimeTrackingStore.setState({ runningTimer: { id: 'A' } });
    useTaskDetailStore.setState({ taskId: 'A', task: { id: 'A' } });
  });
}

function expectLoggedOut() {
  expect(screen.getByText('anonymous')).toBeTruthy();
  expect(screen.getByText('auth:false;loading:false')).toBeTruthy();
  expect(getAccessToken()).toBeNull();
  expect(useCalendarStore.getState().tasks).toEqual([]);
  expect(useProjectStore.getState().projects).toEqual([]);
  expect(useNotificationStore.getState().notifications).toEqual([]);
  expect(useNotificationStore.getState().unreadCount).toBe(0);
  expect(useTimeTrackingStore.getState().runningTimer).toBeNull();
  expect(useTaskDetailStore.getState().taskId).toBeNull();
  expect(useTaskDetailStore.getState().task).toBeNull();
}

for (const outcome of [200, 401, 500, 'network']) {
  it(`logout ${outcome} sends the captured bearer once, never refreshes, and finally clears auth and user stores`, async () => {
    const requests = [];
    let finishLogout;
    api.defaults.adapter = async config => {
      requests.push({ url: config.url, bearer: config.headers.Authorization });
      if (config.url === '/auth/refresh')
        return response({ token: 'synthetic-a', user: { id: 'A' } }, config);
      if (config.url !== '/auth/logout') throw new Error('Unexpected request');
      await new Promise(resolve => { finishLogout = resolve; });
      if (outcome === 'network') throw new AxiosError('Network Error', 'ERR_NETWORK', config);
      // Enforce the actual backend authorization contract, even in the 200 case.
      const status = config.headers.Authorization === 'Bearer synthetic-a' ? outcome : 401;
      if (status !== 200) throw new AxiosError('Logout rejected', 'ERR_BAD_REQUEST', config, null, { status, config });
      return response(null, config);
    };
    render(<AuthProvider><Session /></AuthProvider>);
    await screen.findByText('A');
    seedUserStores();
    requests.length = 0; // Exclude the intentional bootstrap refresh.
    fireEvent.click(screen.getByText('logout'));
    expectLoggedOut(); // Retain Phase 4's immediate reset while HTTP is pending.
    await waitFor(() => expect(finishLogout).toBeTypeOf('function'));
    await act(async () => finishLogout());
    expect(requests).toEqual([{ url: '/auth/logout', bearer: 'Bearer synthetic-a' }]);
    expectLoggedOut();
  });
}

it('logout with no access token and an expired refresh cookie rejects 401 without rotation or retry', async () => {
  const requests = [];
  api.defaults.adapter = async config => {
    requests.push(config.url);
    throw new AxiosError('Expired session', 'ERR_BAD_REQUEST', config, null, { status: 401, config });
  };
  render(<AuthProvider><Session /></AuthProvider>);
  await screen.findByText('auth:false;loading:false');
  requests.length = 0;
  fireEvent.click(screen.getByText('logout'));
  await act(async () => {});
  expect(requests).toEqual(['/auth/logout']);
  expectLoggedOut();
});

it('ordinary concurrent protected 401s still share one refresh and retry with the fresh bearer', async () => {
  let refreshes = 0, finishRefresh;
  const protectedRequests = [];
  api.defaults.adapter = async config => {
    if (config.url === '/auth/refresh') {
      refreshes++;
      if (refreshes > 1) await new Promise(resolve => { finishRefresh = resolve; });
      return response({ token: refreshes === 1 ? 'synthetic-a' : 'synthetic-fresh', user: { id: 'A' } }, config);
    }
    protectedRequests.push({ url: config.url, bearer: config.headers.Authorization });
    if (config.headers.Authorization !== 'Bearer synthetic-fresh')
      throw new AxiosError('Expired access token', 'ERR_BAD_REQUEST', config, null, { status: 401, config });
    return response({ authorized: true }, config);
  };
  render(<AuthProvider><Session /></AuthProvider>);
  await screen.findByText('A');
  const first = api.get('/protected/first');
  const second = api.get('/protected/second');
  await waitFor(() => expect(finishRefresh).toBeTypeOf('function'));
  await act(async () => { finishRefresh(); await Promise.all([first, second]); });
  expect(refreshes).toBe(2); // One bootstrap, one shared interceptor refresh.
  expect(protectedRequests).toEqual([
    { url: '/protected/first', bearer: 'Bearer synthetic-a' },
    { url: '/protected/second', bearer: 'Bearer synthetic-a' },
    { url: '/protected/first', bearer: 'Bearer synthetic-fresh' },
    { url: '/protected/second', bearer: 'Bearer synthetic-fresh' },
  ]);
  expect(getAccessToken()).toBe('synthetic-fresh');
  expect(screen.getByText('auth:true;loading:false')).toBeTruthy();
});

it('a refresh already pending before logout cannot restore a token after logout completes', async () => {
  let refreshes = 0, finishRefresh;
  api.defaults.adapter = async config => {
    if (config.url === '/auth/refresh') {
      refreshes++;
      if (refreshes > 1) await new Promise(resolve => { finishRefresh = resolve; });
      return response({ token: 'synthetic-a', user: { id: 'A' } }, config);
    }
    expect(config.url).toBe('/auth/logout');
    return response(null, config);
  };
  render(<AuthProvider><Session /></AuthProvider>);
  await screen.findByText('A');
  const pendingRefresh = refreshSession().catch(error => error);
  await waitFor(() => expect(finishRefresh).toBeTypeOf('function'));
  fireEvent.click(screen.getByText('logout'));
  await act(async () => {});
  expectLoggedOut();
  let error;
  await act(async () => { finishRefresh(); error = await pendingRefresh; });
  expect(error.code).toBe('ERR_CANCELED');
  expect(refreshes).toBe(2);
  expectLoggedOut();
});

for (const timing of ['before', 'after']) {
  it(`logout finalization clears or invalidates a concurrent refresh settling ${timing} logout completion`, async () => {
    let refreshes = 0, finishRefresh, finishLogout;
    api.defaults.adapter = async config => {
      if (config.url === '/auth/refresh') {
        refreshes++;
        if (refreshes > 1) await new Promise(resolve => { finishRefresh = resolve; });
        return response({ token: 'synthetic-a', user: { id: 'A' } }, config);
      }
      expect(config.url).toBe('/auth/logout');
      await new Promise(resolve => { finishLogout = resolve; });
      return response(null, config);
    };
    render(<AuthProvider><Session /></AuthProvider>);
    await screen.findByText('A');
    fireEvent.click(screen.getByText('logout'));
    await waitFor(() => expect(finishLogout).toBeTypeOf('function'));
    // Explicit concurrent refresh, not one initiated by logout's interceptor.
    const pendingRefresh = refreshSession().then(() => 'resolved', error => error.code);
    await waitFor(() => expect(finishRefresh).toBeTypeOf('function'));
    if (timing === 'before') {
      await act(async () => { finishRefresh(); expect(await pendingRefresh).toBe('resolved'); });
      expect(getAccessToken()).toBe('synthetic-a');
    }
    await act(async () => finishLogout());
    expectLoggedOut();
    if (timing === 'after') {
      await act(async () => { finishRefresh(); expect(await pendingRefresh).toBe('ERR_CANCELED'); });
    }
    expect(refreshes).toBe(2);
    expectLoggedOut();
  });
}

it('a late failed logout cannot clear the superseding account B session', async () => {
  let finishLogout;
  const requests = [];
  api.defaults.adapter = async config => {
    requests.push(config.url);
    if (config.url === '/auth/refresh') return response({ token: 'synthetic-a', user: { id: 'A' } }, config);
    if (config.url === '/auth/login') return response({ token: 'synthetic-b', user: { id: 'B' } }, config);
    expect(config.url).toBe('/auth/logout');
    expect(config.headers.Authorization).toBe('Bearer synthetic-a');
    await new Promise(resolve => { finishLogout = resolve; });
    throw new AxiosError('Logout rejected', 'ERR_BAD_REQUEST', config, null, { status: 401, config });
  };
  render(<AuthProvider><Session /></AuthProvider>);
  await screen.findByText('A');
  requests.length = 0;
  fireEvent.click(screen.getByText('logout'));
  await waitFor(() => expect(finishLogout).toBeTypeOf('function'));
  fireEvent.click(screen.getByText('login B'));
  await screen.findByText('B');
  await act(async () => finishLogout());
  expect(requests).toEqual(['/auth/logout', '/auth/login']);
  expect(getAccessToken()).toBe('synthetic-b');
  expect(screen.getByText('auth:true;loading:false')).toBeTruthy();
});
