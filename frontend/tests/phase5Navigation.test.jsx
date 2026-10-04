import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Routes, Route, useLocation } from 'react-router-dom';
import Topbar from '@/components/layout/Topbar';
import Sidebar from '@/components/layout/Sidebar';
import PrivateRoute from '@/routes/PrivateRoute';
import LoginPage from '@/pages/auth/LoginPage';
import TeamActionModal from '@/components/teams/TeamActionModal';

const auth = vi.hoisted(() => ({ user: { fullName: 'Example User' }, isAuthenticated: false, isLoading: false, login: vi.fn(), googleLogin: vi.fn(), logout: vi.fn() }));
vi.mock('@/context/authState', () => ({ useAuth: () => auth }));
vi.mock('@/context/LanguageContext', () => ({ useLanguage: () => ({ t: key => key }) }));
vi.mock('@/components/theme/ThemeToggle', () => ({ default: () => null }));
vi.mock('@/components/common/LanguageSwitcher', () => ({ default: () => null }));
vi.mock('@react-oauth/google', () => ({ GoogleLogin: () => null }));
afterEach(cleanup);
beforeEach(() => { vi.clearAllMocks(); auth.login.mockResolvedValue({}); });
function Location() { return <output aria-label="Route">{useLocation().pathname}</output>; }

it('Topbar profile/settings controls navigate by click and search is explicitly unavailable', () => {
  render(<MemoryRouter><Topbar /><Location /></MemoryRouter>);
  expect(screen.getByLabelText('Global search unavailable').disabled).toBe(true);
  fireEvent.click(screen.getByText('Profile'));
  expect(screen.getByLabelText('Route').textContent).toBe('/profile');
  fireEvent.click(screen.getByText('Settings'));
  expect(screen.getByLabelText('Route').textContent).toBe('/settings');
});

it('Sidebar exposes Teams navigation and unavailable billing', () => {
  render(<MemoryRouter><Sidebar isCollapsed={false} setIsCollapsed={vi.fn()} /><Location /></MemoryRouter>);
  fireEvent.click(screen.getByRole('link', { name: 'Teams' }));
  expect(screen.getByLabelText('Route').textContent).toBe('/teams');
  expect(screen.getByRole('button', { name: 'Billing unavailable' }).disabled).toBe(true);
});

it('protected deep link returns through login to the original local route', async () => {
  render(<MemoryRouter initialEntries={['/tasks/task?tab=time']}><Routes><Route path="/tasks/:id" element={<PrivateRoute><Location /></PrivateRoute>} /><Route path="/login" element={<LoginPage />} /></Routes><Location /></MemoryRouter>);
  await screen.findByRole('button', { name: 'auth.login.submit' });
  fireEvent.change(screen.getByLabelText('common.email'), { target: { value: 'user@example.invalid' } });
  fireEvent.change(screen.getByLabelText('common.password'), { target: { value: 'password' } });
  auth.isAuthenticated = true;
  fireEvent.click(screen.getByRole('button', { name: 'auth.login.submit' }));
  await waitFor(() => expect(screen.getAllByLabelText('Route')[0].textContent).toBe('/tasks/task'));
  auth.isAuthenticated = false;
});

it('validated team form submits with pending/error states and labelled fields', async () => {
  let reject;
  const submit = vi.fn(() => new Promise((_, fail) => { reject = fail; }));
  const close = vi.fn();
  render(<TeamActionModal title="Create team" fields={[{ name: 'name', label: 'Team name', required: true }]} onSubmit={submit} onClose={close} />);
  fireEvent.change(screen.getByLabelText('Team name'), { target: { value: ' Team ' } });
  fireEvent.click(screen.getByRole('button', { name: 'Confirm' }));
  expect(submit).toHaveBeenCalledWith({ name: 'Team' });
  expect(screen.getByRole('button', { name: 'Confirm' }).disabled).toBe(true);
  reject(new Error('Team name already exists'));
  expect((await screen.findByRole('alert')).textContent).toContain('already exists');
  expect(close).not.toHaveBeenCalled();
});

it('login rejects an external query return URL', async () => {
  render(<MemoryRouter initialEntries={['/login?redirect=https%3A%2F%2Fevil.invalid']}><Routes><Route path="/login" element={<LoginPage />} /><Route path="/dashboard" element={<Location />} /></Routes></MemoryRouter>);
  fireEvent.change(screen.getByLabelText('common.email'), { target: { value: 'user@example.invalid' } });
  fireEvent.change(screen.getByLabelText('common.password'), { target: { value: 'password' } });
  fireEvent.click(screen.getByRole('button', { name: 'auth.login.submit' }));
  await waitFor(() => expect(screen.getByLabelText('Route').textContent).toBe('/dashboard'));
});
