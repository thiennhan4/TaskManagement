import { StrictMode } from 'react';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { act, cleanup, fireEvent, render, screen, within } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import AppLayout from '@/components/layout/AppLayout';
import Topbar from '@/components/layout/Topbar';
import useNotificationStore from '@/stores/useNotificationStore';

vi.mock('@/context/authState', () => ({ useAuth: () => ({ user: { fullName: 'Profile' }, logout: vi.fn() }) }));
vi.mock('@/context/LanguageContext', () => ({ useLanguage: () => ({ t: key => key }) }));
vi.mock('@/components/theme/ThemeToggle', () => ({ default: () => <button>Theme</button> }));
vi.mock('@/components/common/LanguageSwitcher', () => ({ default: () => <button>Language</button> }));
beforeEach(() => {
  vi.stubGlobal('matchMedia', () => ({ matches: false, addEventListener: vi.fn(), removeEventListener: vi.fn() }));
  useNotificationStore.setState({ notifications: [], unreadCount: 0 });
});
afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

it('mobile navigation enters/traps focus, closes with Escape and restores its trigger in StrictMode', () => {
  render(<StrictMode><MemoryRouter><AppLayout /></MemoryRouter></StrictMode>);
  const trigger = screen.getByRole('button', { name: 'Open navigation' });
  trigger.focus(); fireEvent.click(trigger);
  const dialog = screen.getByRole('dialog', { name: 'Navigation' });
  expect(dialog.contains(document.activeElement)).toBe(true);
  expect(document.body.style.overflow).toBe('hidden');
  const lastLink = within(dialog).getByRole('link', { name: 'nav.settings' });
  lastLink.focus(); fireEvent.keyDown(document, { key: 'Tab' });
  expect(document.activeElement).toBe(within(dialog).getByRole('button', { name: 'Close dialog' }));
  fireEvent.keyDown(document, { key: 'Escape' });
  expect(screen.queryByRole('dialog')).toBeNull(); expect(document.activeElement).toBe(trigger);
  expect(document.body.style.overflow).not.toBe('hidden');
});

it('selecting a navigation destination closes the drawer and reaches the route', () => {
  render(<MemoryRouter><Routes><Route element={<AppLayout />}><Route path="*" element={<p>Home content</p>} /><Route path="teams" element={<p>Teams destination</p>} /></Route></Routes></MemoryRouter>);
  fireEvent.click(screen.getByRole('button', { name: 'Open navigation' }));
  fireEvent.click(within(screen.getByRole('dialog', { name: 'Navigation' })).getByRole('link', { name: 'Teams' }));
  expect(screen.queryByRole('dialog')).toBeNull(); expect(screen.getByText('Teams destination')).toBeTruthy();
});

it('notifications have native actions, modal focus/Escape and return focus', () => {
  const mark = vi.fn();
  useNotificationStore.setState({ notifications: [{ id: 'notification', title: 'Task assigned', message: 'Review your task', createdAt: '2026-10-01T10:00:00Z', isRead: false }], unreadCount: 1, markAsRead: mark });
  render(<MemoryRouter><Topbar /></MemoryRouter>);
  const trigger = screen.getByRole('button', { name: 'Notifications' });
  trigger.focus(); fireEvent.click(trigger);
  const dialog = screen.getByRole('dialog', { name: 'Notifications' });
  expect(dialog.contains(document.activeElement)).toBe(true);
  expect(within(dialog).getByRole('button', { name: /Task assigned/ })).toBeTruthy();
  fireEvent.keyDown(document, { key: 'Escape' }); expect(document.activeElement).toBe(trigger);
  fireEvent.click(trigger); fireEvent.click(screen.getByRole('button', { name: /Task assigned/ }));
  expect(mark).toHaveBeenCalledWith('notification'); expect(screen.queryByRole('dialog')).toBeNull();
});

it('desktop breakpoint closes an open mobile drawer and restores focus', () => {
  const listeners = new Set(); let desktop = false;
  vi.stubGlobal('matchMedia', () => ({ get matches() { return desktop; }, addEventListener: (_, fn) => listeners.add(fn), removeEventListener: (_, fn) => listeners.delete(fn) }));
  render(<MemoryRouter><AppLayout /></MemoryRouter>);
  const trigger = screen.getByRole('button', { name: 'Open navigation' }); trigger.focus(); fireEvent.click(trigger);
  expect(screen.getByRole('dialog')).toBeTruthy();
  desktop = true;
  const event = new Event('change'); Object.defineProperty(event, 'matches', { value: true });
  act(() => [...listeners].forEach(fn => fn(event)));
  expect(screen.queryByRole('dialog')).toBeNull(); expect(document.activeElement).toBe(trigger);
});

it('profile menu Escape and selection return focus to its summary', () => {
  render(<MemoryRouter><Topbar /></MemoryRouter>);
  const summary = document.querySelector('summary'); const details = summary.parentElement;
  details.open = true; screen.getByRole('button', { name: 'Settings' }).focus();
  fireEvent.keyDown(document, { key: 'Escape' });
  expect(details.open).toBe(false); expect(document.activeElement).toBe(summary);
  details.open = true; fireEvent.click(screen.getByRole('button', { name: 'Settings' }));
  expect(details.open).toBe(false); expect(document.activeElement).toBe(summary);
});
