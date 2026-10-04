import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { act, cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import MoveTaskModal from '@/components/boards/MoveTaskModal';
import ProjectTasksBoard from '@/pages/projects/tabs/ProjectTasksBoard';
import BoardDetailPage from '@/pages/boards/BoardDetailPage';
import { taskApi } from '@/api/taskApi';
import { projectApi } from '@/api/projectApi';
import { boardApi } from '@/api/boardApi';

vi.mock('@/api/taskApi', () => ({ taskApi: { getTaskById: vi.fn(), moveTask: vi.fn() } }));
vi.mock('@/api/projectApi', () => ({ projectApi: { getProjectKanban: vi.fn() } }));
vi.mock('@/api/boardApi', () => ({ boardApi: { getBoardById: vi.fn() } }));
vi.mock('@/context/LanguageContext', () => ({ useLanguage: () => ({ t: key => key }) }));
vi.mock('@/context/NotificationContext', () => ({ useNotification: () => ({ hubConnection: null, reconnectVersion: 0 }) }));
vi.mock('@/components/tasks/TaskModal', () => ({ default: () => null }));
vi.mock('@hello-pangea/dnd', () => ({ DragDropContext: ({ children }) => children, Droppable: ({ children }) => children({ innerRef: () => {}, droppableProps: {}, placeholder: null }, {}), Draggable: ({ children }) => children({ innerRef: () => {}, draggableProps: {}, dragHandleProps: {} }, {}) }));
const task = { id: 'task', listId: 'todo', title: 'Move this task', updatedAt: '2026-10-02T09:00:00', status: 'Todo', capabilities: { canEdit: true } };
const lists = [{ id: 'todo', name: 'Todo', tasks: [task] }, { id: 'done', name: 'Done', tasks: [] }];
const response = data => ({ data: { success: true, data } });
beforeEach(() => { vi.clearAllMocks(); taskApi.getTaskById.mockResolvedValue(response(task)); taskApi.moveTask.mockResolvedValue(response({ ...task, listId: 'done' })); });
afterEach(cleanup);

it('uses fresh permissions, selected destination, append ordering and version; blocks duplicate submit', async () => {
  let resolve;
  taskApi.moveTask.mockReturnValue(new Promise(done => { resolve = done; }));
  const moved = vi.fn(); const close = vi.fn();
  render(<MoveTaskModal taskId="task" lists={lists} onClose={close} onMoved={moved} />);
  await screen.findByText(task.title);
  expect(screen.queryByRole('option', { name: 'Todo' })).toBeNull();
  fireEvent.change(screen.getByLabelText('Destination column'), { target: { value: 'done' } });
  fireEvent.click(screen.getByRole('button', { name: 'Move task' }));
  fireEvent.submit(document.querySelector('form'));
  expect(taskApi.moveTask).toHaveBeenCalledTimes(1);
  expect(taskApi.moveTask).toHaveBeenCalledWith('task', { listId: 'done', position: 0, expectedUpdatedAt: task.updatedAt });
  fireEvent.keyDown(document, { key: 'Escape' }); expect(close).not.toHaveBeenCalled();
  await act(async () => resolve(response({ ...task, listId: 'done' })));
  expect(moved).toHaveBeenCalledWith(expect.objectContaining({ listId: 'done' }), 'Done');
});

it('permission denied disables movement before mutation', async () => {
  taskApi.getTaskById.mockResolvedValue(response({ ...task, capabilities: { canEdit: false } }));
  render(<MoveTaskModal taskId="task" lists={lists} onClose={vi.fn()} onMoved={vi.fn()} />);
  await screen.findByRole('alert'); expect(screen.getByLabelText('Destination column').disabled).toBe(true);
  fireEvent.submit(document.querySelector('form')); expect(taskApi.moveTask).not.toHaveBeenCalled();
});

it('server permission/conflict errors remain visible without success', async () => {
  taskApi.moveTask.mockRejectedValue({ response: { status: 403 } }); const moved = vi.fn();
  render(<MoveTaskModal taskId="task" lists={lists} onClose={vi.fn()} onMoved={moved} />);
  await screen.findByText(task.title);
  fireEvent.change(screen.getByLabelText('Destination column'), { target: { value: 'done' } });
  fireEvent.click(screen.getByRole('button', { name: 'Move task' }));
  expect((await screen.findByRole('alert')).textContent).toContain('permission'); expect(moved).not.toHaveBeenCalled();
});

it('late move completion cannot update an unmounted board', async () => {
  let resolve; taskApi.moveTask.mockReturnValue(new Promise(done => { resolve = done; }));
  const moved = vi.fn();
  const mounted = render(<MoveTaskModal taskId="task" lists={lists} onClose={vi.fn()} onMoved={moved} />);
  await screen.findByText(task.title);
  fireEvent.change(screen.getByLabelText('Destination column'), { target: { value: 'done' } });
  fireEvent.click(screen.getByRole('button', { name: 'Move task' }));
  mounted.unmount(); await act(async () => resolve(response({ ...task, listId: 'done' })));
  expect(moved).not.toHaveBeenCalled();
});

it('board Move is reachable, announces destination, keeps one card and focuses the board after success', async () => {
  const page = items => ({ items, page: 1, totalPages: 1, totalItems: items.length, pageSize: 20 });
  const board = { board: { id: 'board', name: 'Board' }, lists: page(lists.map(list => ({ ...list, tasks: page(list.tasks) }))), canCreateTasks: true, canManageColumns: true };
  projectApi.getProjectKanban.mockResolvedValue(response(board));
  render(<MemoryRouter><ProjectTasksBoard projectId="project" /></MemoryRouter>);
  fireEvent.click(await screen.findByRole('button', { name: 'Move', exact: true }));
  await screen.findByLabelText('Destination column');
  await waitFor(() => expect(screen.getByLabelText('Destination column').disabled).toBe(false));
  projectApi.getProjectKanban.mockResolvedValue(response({ ...board, lists: page(lists.map(list => ({ ...list, tasks: page(list.id === 'done' ? [{ ...task, listId: 'done' }] : []) }))) }));
  fireEvent.change(screen.getByLabelText('Destination column'), { target: { value: 'done' } });
  fireEvent.click(screen.getByRole('button', { name: 'Move task' }));
  await screen.findByText('Move this task moved to Done.');
  expect(screen.getAllByRole('button', { name: task.title, exact: true })).toHaveLength(1);
  await waitFor(() => expect(document.activeElement).toBe(screen.getByLabelText('Task board')));
});

it('standalone board uses its owner-only read contract and offers the same Move flow', async () => {
  const page = items => ({ items, page: 1, totalPages: 1, totalItems: items.length, pageSize: 20 });
  const board = { id: 'board', name: 'Standalone', projectId: null, listPage: page(lists.map(list => ({ ...list, tasks: page(list.tasks) }))) };
  boardApi.getBoardById.mockResolvedValue(response(board));
  render(<MemoryRouter initialEntries={['/boards/board']}><Routes><Route path="/boards/:id" element={<BoardDetailPage />} /></Routes></MemoryRouter>);
  fireEvent.click(await screen.findByRole('button', { name: 'Move', exact: true }));
  await waitFor(() => expect(screen.getByLabelText('Destination column').disabled).toBe(false));
  boardApi.getBoardById.mockResolvedValue(response({ ...board, listPage: page(lists.map(list => ({ ...list, tasks: page(list.id === 'done' ? [{ ...task, listId: 'done' }] : []) }))) }));
  fireEvent.change(screen.getByLabelText('Destination column'), { target: { value: 'done' } });
  fireEvent.click(screen.getByRole('button', { name: 'Move task' }));
  await screen.findByText('Move this task moved to Done.');
  expect(screen.getAllByRole('button', { name: task.title, exact: true })).toHaveLength(1);
});
