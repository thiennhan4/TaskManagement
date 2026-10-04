import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import ProjectDetailPage from '@/pages/projects/ProjectDetailPage';
import CreateProjectModal from '@/components/projects/CreateProjectModal';
import { projectApi } from '@/api/projectApi';
import teamApi from '@/api/teamApi';

const mock = vi.hoisted(() => ({ createProject: vi.fn(), inviteMember: vi.fn(), t: key => key }));
vi.mock('@/api/projectApi', () => ({ projectApi: { getProjectById: vi.fn() } }));
vi.mock('@/api/teamApi', () => ({ default: { getTeams: vi.fn() } }));
vi.mock('@/context/authState', () => ({ useAuth: () => ({ user: { id: 'owner' } }) }));
vi.mock('@/context/LanguageContext', () => ({ useLanguage: () => ({ t: mock.t }) }));
vi.mock('@/stores/useProjectStore', () => ({ useProjectStore: selector => selector(mock) }));
vi.mock('@/pages/projects/tabs/ProjectTasksBoard', () => ({ default: () => null }));
vi.mock('@/pages/projects/tabs/ProjectSettingsTab', () => ({ default: () => null }));
afterEach(cleanup);
beforeEach(() => {
  vi.clearAllMocks(); teamApi.getTeams.mockResolvedValue({ data: { data: [{ id: 'team', name: 'Team' }] } });
  mock.createProject.mockResolvedValue({ id: 'created' });
});
for (const [status, text] of [[403, 'permission'], [404, 'not found'], [500, 'connection']]) it('keeps project fetch failure ' + status + ' in place with retry', async () => {
  projectApi.getProjectById.mockRejectedValue({ response: { status } });
  render(<MemoryRouter initialEntries={['/projects/project']}><Routes><Route path="/projects/:id" element={<ProjectDetailPage />} /></Routes></MemoryRouter>);
  expect((await screen.findByRole('alert')).textContent).toContain(text);
  expect(screen.getByRole('button', { name: 'Retry' })).toBeTruthy();
});

it('retains created project and per-recipient outcome after partial invite failure', async () => {
  mock.inviteMember.mockRejectedValue({ response: { data: { message: 'Recipient rejected' } } });
  render(<MemoryRouter><CreateProjectModal isOpen onClose={vi.fn()} /></MemoryRouter>);
  fireEvent.click(screen.getByText('projects.type.team'));
  await screen.findByRole('option', { name: 'Team' });
  fireEvent.change(screen.getByLabelText('projects.fields.name'), { target: { value: 'Created Project' } });
  fireEvent.change(screen.getByLabelText('projects.invite.email'), { target: { value: 'rejected@example.invalid' } });
  fireEvent.click(screen.getByRole('button', { name: 'projects.createModal.submitTeam' }));
  const dialog = await screen.findByRole('dialog', { name: 'Project created: invitation results' });
  expect(dialog.textContent).toContain('rejected@example.invalid: Recipient rejected');
  expect(mock.createProject).toHaveBeenCalledOnce();
  expect(screen.getByRole('button', { name: 'Open project to manage invitations' })).toBeTruthy();
});
