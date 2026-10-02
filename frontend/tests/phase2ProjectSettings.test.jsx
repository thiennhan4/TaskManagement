import { afterEach, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import ProjectSettingsTab from '@/pages/projects/tabs/ProjectSettingsTab';
import { projectApi } from '@/api/projectApi';

const navigation = vi.hoisted(() => vi.fn());
vi.mock('react-router-dom', () => ({ useNavigate: () => navigation }));
vi.mock('@/context/AuthContext', () => ({ useAuth: () => ({ user: { id: 'owner' } }) }));
vi.mock('@/api/projectApi', () => ({ projectApi: { deleteProject: vi.fn(), archiveProject: vi.fn() } }));
afterEach(() => { cleanup(); vi.restoreAllMocks(); vi.clearAllMocks(); });

for (const [label, action, pending] of [['Delete', 'deleteProject', 'Deleting…'], ['Archive', 'archiveProject', 'Archiving…']]) {
  it(`${label} disables duplicate actions and preserves edits on failure`, async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    let reject;
    projectApi[action].mockImplementation(() => new Promise((_, fail) => { reject = fail; }));
    render(<ProjectSettingsTab project={{ id: 'project', name: 'Before', projectType: 'Team', ownerId: 'owner' }} />);
    const name = screen.getByDisplayValue('Before');
    fireEvent.change(name, { target: { value: 'Unsaved name' } });
    fireEvent.click(screen.getByRole('button', { name: label }));
    expect(screen.getByRole('button', { name: pending }).disabled).toBe(true);
    expect(screen.getByRole('button', { name: label === 'Delete' ? 'Archive' : 'Delete' }).disabled).toBe(true);
    reject({ response: { status: 409, data: { message: 'Reload and retry' } } });
    await waitFor(() => expect(screen.getByRole('button', { name: label }).disabled).toBe(false));
    expect(name.value).toBe('Unsaved name');
    expect(navigation).not.toHaveBeenCalled();
    expect(projectApi[action]).toHaveBeenCalledOnce();
  });
}
