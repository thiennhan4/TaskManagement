import { afterEach, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { AuthProvider } from '@/context/AuthContext';
import { useAuth } from '@/context/authState';
import { authApi } from '@/api/authApi';

vi.mock('@/api/authApi', () => ({ authApi: { refresh: vi.fn(), getMe: vi.fn() } }));
afterEach(cleanup);
function Profile() { const { user, refreshProfile } = useAuth(); return <><output>{user?.fullName || 'Loading'}</output><button onClick={refreshProfile}>Refresh profile</button></>; }
it('refreshes current user from me and reflects persisted values after remount', async () => {
  authApi.refresh.mockResolvedValue({ data: { success: true, data: { token: 'synthetic-profile-token', user: { id: 'owner', fullName: 'Old Name' } } } });
  authApi.getMe.mockResolvedValue({ data: { success: true, data: { id: 'owner', fullName: 'Saved Name', avatarUrl: 'https://example.invalid/avatar.png' } } });
  const mounted = render(<AuthProvider><Profile /></AuthProvider>);
  await screen.findByText('Old Name');
  fireEvent.click(screen.getByRole('button', { name: 'Refresh profile' }));
  await screen.findByText('Saved Name');
  expect(authApi.getMe).toHaveBeenCalledOnce();
  mounted.unmount();
  authApi.refresh.mockResolvedValue({ data: { success: true, data: { token: 'synthetic-reload-token', user: { id: 'owner', fullName: 'Saved Name' } } } });
  render(<AuthProvider><Profile /></AuthProvider>);
  await waitFor(() => expect(screen.getByText('Saved Name')).toBeTruthy());
});
