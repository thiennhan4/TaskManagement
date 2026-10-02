import { StrictMode } from 'react';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import AcceptProjectInvite from '@/pages/projects/AcceptProjectInvite';
import AcceptTaskInvite from '@/pages/tasks/AcceptTaskInvite';
import { taskApi } from '@/api/taskApi';
import { projectMemberApi } from '@/api/projectMemberApi';

vi.mock('@/context/authState', () => ({ useAuth: () => ({ isAuthenticated: true, isLoading: false }) }));
vi.mock('@/api/taskApi', () => ({ taskApi: { acceptTaskInvite: vi.fn() } }));
vi.mock('@/api/projectMemberApi', () => ({ projectMemberApi: { acceptInvite: vi.fn() } }));
afterEach(cleanup); beforeEach(() => vi.clearAllMocks());
for (const [name, Component, action] of [['project', AcceptProjectInvite, () => projectMemberApi.acceptInvite], ['task', AcceptTaskInvite, () => taskApi.acceptTaskInvite]]) {
  it(name + ' invitation accepts once in StrictMode and reports real success', async () => {
    action().mockResolvedValue({ data: { success: true } });
    render(<StrictMode><MemoryRouter initialEntries={['/accept?token=synthetic&projectId=project']}><Component /></MemoryRouter></StrictMode>);
    await screen.findByText('Welcome Aboard!');
    expect(action()).toHaveBeenCalledOnce();
  });
  it(name + ' invitation shows server failure without success', async () => {
    action().mockRejectedValue({ response: { data: { message: 'This invitation belongs to another recipient.' } } });
    render(<MemoryRouter initialEntries={['/accept?token=synthetic&projectId=project']}><Component /></MemoryRouter>);
    await screen.findByText('This invitation belongs to another recipient.');
    expect(screen.queryByText('Welcome Aboard!')).toBeNull();
    await waitFor(() => expect(action()).toHaveBeenCalledOnce());
  });
}
