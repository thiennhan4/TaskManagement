import { afterEach, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import TaskFormModal from '@/components/tasks/TaskFormModal';
import ProjectTasksBoard from '@/pages/projects/tabs/ProjectTasksBoard';
import { projectApi } from '@/api/projectApi';
import { taskApi } from '@/api/taskApi';

const drag = vi.hoisted(() => ({ end: null }));
vi.mock('@/components/ui/Modal', () => ({ default: ({ children }) => <div>{children}</div> }));
vi.mock('@/context/NotificationContext', () => ({ useNotification: () => ({ hubConnection: null, reconnectVersion: 0 }) }));
vi.mock('@/api/projectApi', () => ({ projectApi: { getProjectKanban: vi.fn() } }));
vi.mock('@/api/taskApi', () => ({ taskApi: { moveTask: vi.fn() } }));
vi.mock('@/components/tasks/TaskModal', () => ({ default: () => null }));
vi.mock('@/components/tasks/TaskCard', () => ({ default: ({ task }) => <span>{task.title}</span> }));
vi.mock('@hello-pangea/dnd', () => ({
  DragDropContext: ({ children, onDragEnd }) => { drag.end = onDragEnd; return children; },
  Droppable: ({ children }) => children({ innerRef: () => {}, droppableProps: {}, placeholder: null }, {}),
}));
afterEach(() => { cleanup(); vi.clearAllMocks(); });

it('a title edit omits hidden fields and an unchanged timestamp', async () => {
  const submit = vi.fn().mockResolvedValue();
  render(<TaskFormModal isOpen onClose={() => {}} onSubmit={submit} task={{ id: 'task', title: 'Before', dueDate: '2025-01-01T12:34:56Z', startDate: '2024-12-31T00:00:00Z', progress: 47, assignedToId: 'member' }} />);
  fireEvent.change(screen.getByPlaceholderText('What needs to be done?'), { target: { value: 'After' } });
  fireEvent.click(screen.getByRole('button', { name: /Save Changes/ }));
  await waitFor(() => expect(submit).toHaveBeenCalledOnce());
  const payload = submit.mock.calls[0][0];
  expect(payload.title).toBe('After');
  for (const field of ['startDate', 'dueDate', 'assignedToId', 'progress']) expect(payload).not.toHaveProperty(field);
});

it('failed form submission retains entered state and does not close', async () => {
  const close = vi.fn();
  render(<TaskFormModal isOpen onClose={close} onSubmit={vi.fn().mockRejectedValue({ response: { status: 409 } })} task={{ id: 'task', title: 'Before' }} />);
  fireEvent.change(screen.getByPlaceholderText('What needs to be done?'), { target: { value: 'Unsaved edit' } });
  fireEvent.click(screen.getByRole('button', { name: /Save Changes/ }));
  await waitFor(() => expect(screen.getByRole('button', { name: /Save Changes/ }).disabled).toBe(false));
  expect(screen.getByPlaceholderText('What needs to be done?').value).toBe('Unsaved edit');
  expect(close).not.toHaveBeenCalled();
});

it('a rejected move reloads authoritative order and supplies the previous timestamp', async () => {
  const page = items => ({ items, page: 1, totalPages: 1 });
  const response = { data: { data: { board: { id: 'board' }, canCreateTasks: true, canManageColumns: false, lists: page([
    { id: 'source', name: 'Source', tasks: page([{ id: 'task', title: 'Original card', updatedAt: '2026-09-01T00:00:00Z' }]) },
    { id: 'target', name: 'Target', tasks: page([]) },
  ]) } } };
  projectApi.getProjectKanban.mockResolvedValue(response);
  taskApi.moveTask.mockRejectedValue({ response: { status: 409, data: { message: 'Reload and retry' } } });
  render(<ProjectTasksBoard projectId="project" />);
  await screen.findByText('Original card');
  await drag.end({ source: { droppableId: 'source', index: 0 }, destination: { droppableId: 'target', index: 0 }, draggableId: 'task' });
  await waitFor(() => expect(projectApi.getProjectKanban).toHaveBeenCalledTimes(2));
  expect(taskApi.moveTask).toHaveBeenCalledWith('task', { listId: 'target', position: 0, expectedUpdatedAt: '2026-09-01T00:00:00Z' });
  expect(await screen.findByText('Original card')).toBeTruthy();
});
