import { beforeEach, afterEach, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import ProfilePage from '@/pages/profile/ProfilePage';
import { settingsApi } from '@/api/settingsApi';
import toast from 'react-hot-toast';

const auth = vi.hoisted(() => ({ user: { id: 'user', fullName: 'Original Name', email: 'user@example.invalid', avatarUrl: '' }, refreshProfile: vi.fn() }));
vi.mock('@/context/authState', () => ({ useAuth: () => auth }));
vi.mock('@/api/settingsApi', () => ({ settingsApi: { updateProfile: vi.fn(), changePassword: vi.fn() } }));
vi.mock('react-hot-toast', () => ({ default: { success: vi.fn(), error: vi.fn() } }));
afterEach(cleanup);
beforeEach(() => { vi.clearAllMocks(); auth.refreshProfile.mockResolvedValue({ fullName: 'Saved Name' }); });

it('saves supported fields through API and waits for current-user refresh before success', async () => {
  let resolve;
  settingsApi.updateProfile.mockReturnValue(new Promise(done => { resolve = done; }));
  render(<ProfilePage />);
  fireEvent.change(screen.getByLabelText('Full name'), { target: { value: 'Saved Name' } });
  fireEvent.click(screen.getByRole('button', { name: 'Save profile' }));
  expect(settingsApi.updateProfile).toHaveBeenCalledWith({ fullName: 'Saved Name', avatarUrl: null });
  expect(screen.getByRole('button', { name: 'Save profile' }).disabled).toBe(true);
  expect(toast.success).not.toHaveBeenCalled();
  resolve({ data: { success: true } });
  await waitFor(() => expect(auth.refreshProfile).toHaveBeenCalledOnce());
  await waitFor(() => expect(toast.success).toHaveBeenCalledOnce());
});

it('shows API failure and never claims success', async () => {
  settingsApi.updateProfile.mockRejectedValue({ response: { data: { message: 'Profile rejected' } } });
  render(<ProfilePage />);
  fireEvent.click(screen.getByRole('button', { name: 'Save profile' }));
  expect((await screen.findByRole('alert')).textContent).toContain('Profile rejected');
  expect(toast.success).not.toHaveBeenCalled(); expect(auth.refreshProfile).not.toHaveBeenCalled();
});

it('reports persisted profile with failed refresh as partial success', async () => {
  settingsApi.updateProfile.mockResolvedValue({ data: { success: true } });
  auth.refreshProfile.mockRejectedValue(new Error('offline'));
  render(<ProfilePage />); fireEvent.click(screen.getByRole('button', { name: 'Save profile' }));
  expect((await screen.findByRole('alert')).textContent).toContain('Profile saved, but');
  expect(toast.success).not.toHaveBeenCalled();
});

it('does not expose invented fields or interactive notification preferences', () => {
  render(<ProfilePage />);
  for (const label of ['Bio', 'Job title', 'Location']) expect(screen.queryByLabelText(label)).toBeNull();
  fireEvent.click(screen.getByRole('button', { name: 'Notifications' }));
  expect(screen.getByText(/Notification preferences are unavailable/)).toBeTruthy();
  expect(screen.queryByRole('switch')).toBeNull();
});

function passwordForm() {
  render(<ProfilePage />); fireEvent.click(screen.getByRole('button', { name: 'Password & Security' }));
  fireEvent.change(screen.getByLabelText('Current password'), { target: { value: 'old-pass' } });
  fireEvent.change(screen.getByLabelText('New password'), { target: { value: 'new-pass' } });
  fireEvent.change(screen.getByLabelText('Confirm new password'), { target: { value: 'new-pass' } });
}

it('validates password confirmation before sending', () => {
  passwordForm(); fireEvent.change(screen.getByLabelText('Confirm new password'), { target: { value: 'different' } });
  fireEvent.click(screen.getByRole('button', { name: 'Update password' }));
  expect(screen.getByRole('alert').textContent).toContain('matching confirmation');
  expect(settingsApi.changePassword).not.toHaveBeenCalled();
});

it('sends real password change and clears fields only on success', async () => {
  settingsApi.changePassword.mockResolvedValue({ data: { success: true } }); passwordForm();
  fireEvent.click(screen.getByRole('button', { name: 'Update password' }));
  await waitFor(() => expect(toast.success).toHaveBeenCalledOnce());
  expect(settingsApi.changePassword).toHaveBeenCalledWith({ currentPassword: 'old-pass', newPassword: 'new-pass' });
  expect(screen.getByLabelText('New password').value).toBe('');
});

it('shows incorrect-current-password errors without success', async () => {
  settingsApi.changePassword.mockRejectedValue({ response: { data: { message: 'Current password is incorrect.' } } }); passwordForm();
  fireEvent.click(screen.getByRole('button', { name: 'Update password' }));
  expect((await screen.findByRole('alert')).textContent).toContain('incorrect');
  expect(toast.success).not.toHaveBeenCalled();
});
