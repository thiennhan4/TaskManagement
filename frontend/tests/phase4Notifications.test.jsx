import { StrictMode } from 'react';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { act, cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import NotificationsPage from '@/pages/notifications/NotificationsPage';
import Topbar from '@/components/layout/Topbar';
import { NotificationProvider, useNotification } from '@/context/NotificationContext';
import useNotificationStore from '@/stores/useNotificationStore';
import { notificationApi } from '@/api/notificationApi';
import { setAccessToken } from '@/api/axiosInstance';

const mock = vi.hoisted(() => ({ connections: [], options: [], fail: false, start: null, user: { id: 'A' } }));
vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: class {
    withUrl(_url, options) { mock.options.push(options); return this; }
    withAutomaticReconnect() { return this; }
    build() {
      const handlers = new Map();
      const connection = {
        handlers, on: vi.fn((event, handler) => { if (!handlers.has(event)) handlers.set(event, new Set()); handlers.get(event).add(handler); }),
        off: vi.fn((event, handler) => handlers.get(event)?.delete(handler)),
        onreconnected: vi.fn(), onclose: vi.fn(), invoke: vi.fn().mockResolvedValue(),
        start: vi.fn(() => mock.start ? mock.start() : mock.fail ? Promise.reject(new Error('offline')) : Promise.resolve()),
        stop: vi.fn().mockResolvedValue(),
      };
      mock.connections.push(connection);
      return connection;
    }
  },
}));
vi.mock('@/api/notificationApi', () => ({ notificationApi: {
  getMyNotifications: vi.fn(), getUnreadCount: vi.fn(), markAsRead: vi.fn(), markAllAsRead: vi.fn(),
} }));
vi.mock('@/context/authState', () => ({ useAuth: () => ({ user: mock.user, logout: vi.fn() }) }));
vi.mock('@/context/LanguageContext', () => ({ useLanguage: () => ({ t: key => key }) }));
vi.mock('@/components/theme/ThemeToggle', () => ({ default: () => null }));
vi.mock('@/components/common/LanguageSwitcher', () => ({ default: () => null }));
const response = data => ({ data: { success: true, data } });
const list = (read = false, page = 1) => response({ items: [{ id: page, title: 'Notice ' + page, isRead: read, createdAt: '2026-09-01T00:00:00Z' }],
  page, pageSize: 20, totalPages: 2, totalItems: 30 });
beforeEach(() => {
  vi.clearAllMocks(); useNotificationStore.getState().reset();
  mock.connections.length = 0; mock.options.length = 0; mock.fail = false; mock.start = null; mock.user = { id: 'A' };
  notificationApi.getMyNotifications.mockImplementation(page => Promise.resolve(list(false, page)));
  notificationApi.getUnreadCount.mockResolvedValue(response(29));
});
afterEach(async () => { cleanup(); await act(async () => {}); });
function HubState() {
  const { hubConnection, hubError } = useNotification();
  return <output>{hubError || (hubConnection ? 'connected' : 'waiting')}</output>;
}

it('NotificationsPage mounts, pages and surfaces errors; Topbar shares read-all state', async () => {
  render(<MemoryRouter><Topbar /><NotificationsPage /></MemoryRouter>);
  expect(await screen.findByText('Notice 1')).toBeTruthy();
  expect(screen.getByText('29 new')).toBeTruthy();
  // Existing bell is the only button containing the Bell icon in the header.
  fireEvent.click(document.querySelector('header button:has(svg.lucide-bell)'));
  expect(screen.getAllByText('Notice 1')).toHaveLength(2);
  notificationApi.markAllAsRead.mockImplementation(async () => {
    notificationApi.getMyNotifications.mockImplementation(page => Promise.resolve(list(true, page)));
    notificationApi.getUnreadCount.mockResolvedValue(response(0));
  });
  fireEvent.click(screen.getByText('Mark all read'));
  await waitFor(() => expect(screen.queryByText('29 new')).toBeNull());
  expect(screen.queryByText('Mark all as read')).toBeNull();
  fireEvent.click(screen.getByText('Next'));
  await waitFor(() => expect(screen.getAllByText('Notice 2')).toHaveLength(2));
  notificationApi.getMyNotifications.mockRejectedValueOnce(new Error('offline'));
  fireEvent.click(screen.getByText('Previous'));
  expect(await screen.findByRole('alert')).toBeTruthy();
});

it('StrictMode owns one connection with exactly one handler per event; factory reads refreshed token and cleanup removes same references', async () => {
  setAccessToken('synthetic-first');
  const mounted = render(<StrictMode><NotificationProvider><HubState /></NotificationProvider></StrictMode>);
  await screen.findByText('connected');
  expect(mock.connections).toHaveLength(1);
  const connection = mock.connections[0];
  expect([...connection.handlers.values()].map(set => set.size)).toEqual([1, 1, 1]);
  expect(mock.options[0].accessTokenFactory()).toBe('synthetic-first');
  setAccessToken('synthetic-latest');
  expect(mock.options[0].accessTokenFactory()).toBe('synthetic-latest');
  mounted.unmount();
  await act(async () => {});
  expect([...connection.handlers.values()].every(set => set.size === 0)).toBe(true);
  expect(connection.stop).toHaveBeenCalled();
  render(<NotificationProvider><HubState /></NotificationProvider>);
  await screen.findByText('connected');
  expect(mock.connections).toHaveLength(2);
  expect([...mock.connections[1].handlers.values()].map(set => set.size)).toEqual([1, 1, 1]);
});

it('initial start failure is controlled and removes notification handlers', async () => {
  mock.fail = true;
  render(<NotificationProvider><HubState /></NotificationProvider>);
  expect(await screen.findByText('Live updates unavailable')).toBeTruthy();
  const connection = mock.connections[0];
  expect(connection.stop).toHaveBeenCalled();
  expect([...connection.handlers.values()].every(set => set.size === 0)).toBe(true);
});

it('unmount during slow start prevents stale connection publication and serializes the next start', async () => {
  let finish;
  mock.start = () => new Promise(resolve => { finish = resolve; });
  const first = render(<NotificationProvider><HubState /></NotificationProvider>);
  await waitFor(() => expect(finish).toBeTypeOf('function'));
  first.unmount();
  mock.start = null;
  render(<NotificationProvider><HubState /></NotificationProvider>);
  expect(mock.connections).toHaveLength(1);
  await act(async () => finish());
  await screen.findByText('connected');
  expect(mock.connections).toHaveLength(2);
  expect([...mock.connections[0].handlers.values()].every(set => set.size === 0)).toBe(true);
});
