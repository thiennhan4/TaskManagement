import { beforeEach, afterEach, expect, it, vi } from 'vitest';
import { act, cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import BoardDetailPage from '@/pages/boards/BoardDetailPage';
import ProjectTasksBoard from '@/pages/projects/tabs/ProjectTasksBoard';
import { boardApi } from '@/api/boardApi';
import { projectApi } from '@/api/projectApi';
import { taskApi } from '@/api/taskApi';
import { listApi } from '@/api/listApi';
const hub = vi.hoisted(() => ({ handlers: new Map(), on: vi.fn(), off: vi.fn(), invoke: vi.fn().mockResolvedValue() }));
const translate = vi.hoisted(() => key => key);
vi.mock('@/context/LanguageContext', () => ({ useLanguage: () => ({ t: translate }) }));
vi.mock('@/context/NotificationContext', () => ({ useNotification: () => ({ hubConnection: hub, reconnectVersion: 0 }) }));
vi.mock('@/api/boardApi', () => ({ boardApi: { getBoardById: vi.fn(), getColumns: vi.fn() } }));
vi.mock('@/api/projectApi', () => ({ projectApi: { getProjectKanban: vi.fn() } }));
vi.mock('@/api/taskApi', () => ({ taskApi: { createTask: vi.fn(), updateTask: vi.fn() } }));
vi.mock('@/api/listApi', () => ({ listApi: { createList: vi.fn() } }));
vi.mock('@/components/tasks/TaskModal', () => ({ default: ({ isOpen, taskId, onEdit }) => isOpen ? <button onClick={() => onEdit({ id: taskId })}>Edit task</button> : null }));
vi.mock('@/components/tasks/TaskCard', () => ({ default: ({ task, onClick }) => <output onClick={() => onClick(task)}>{task.title}:{task.commentsCount || 0}</output> }));
vi.mock('@/components/tasks/TaskFormModal', () => ({ default: ({ isOpen, onSubmit, task }) => isOpen ? <button onClick={() => onSubmit({ title: task ? 'Updated card' : 'New card' })}>Submit task</button> : null }));
vi.mock('@hello-pangea/dnd', () => ({
  DragDropContext: ({ children }) => children,
  Droppable: ({ children }) => children({ innerRef: () => {}, droppableProps: {}, placeholder: null }, {}),
}));
const response = data => ({ data: { data } });
const page = items => ({ items, page: 1, pageSize: 20, totalItems: items.length, totalPages: 1 });
const deferred = () => { let resolve; const promise = new Promise(done => { resolve = done; }); return { promise, resolve }; };
beforeEach(() => {
  vi.clearAllMocks(); hub.handlers.clear();
  hub.on.mockImplementation((event, handler) => { if (!hub.handlers.has(event)) hub.handlers.set(event, new Set()); hub.handlers.get(event).add(handler); });
  hub.off.mockImplementation((event, handler) => hub.handlers.get(event)?.delete(handler));
  const lists = page([{ id: 'list', name: 'Todo', boardId: 'board', tasks: page([]) }]);
  boardApi.getBoardById.mockResolvedValue(response({ id: 'board', name: 'Board', listPage: lists }));
  projectApi.getProjectKanban.mockResolvedValue(response({ board: { id: 'board', name: 'Board' }, lists, canCreateTasks: true, canManageColumns: true }));
});
afterEach(cleanup);
const emit = (event, data) => act(() => hub.handlers.get(event)?.forEach(handler => handler(data)));
function mount(surface) {
  return render(<MemoryRouter initialEntries={['/boards/board']}><Routes><Route path="/boards/:id" element={surface === 'standalone' ? <BoardDetailPage /> : <ProjectTasksBoard projectId="project" />} /></Routes></MemoryRouter>);
}
for (const surface of ['standalone', 'project']) {
  for (const order of ['HTTP first', 'hub first']) {
    it(surface + ' task update ' + order + ' renders one updated card', async () => {
      const card = { id: 'existing', listId: 'list', boardId: 'board', title: 'Old card' };
      const boardResponse = task => response({ id: 'board', name: 'Board', listPage: page([{ id: 'list', name: 'Todo', tasks: page([task]) }]) });
      const projectResponse = task => response({ board: { id: 'board', name: 'Board' }, lists: page([{ id: 'list', name: 'Todo', tasks: page([task]) }]), canCreateTasks: true, canManageColumns: true });
      boardApi.getBoardById.mockResolvedValue(boardResponse(card));
      projectApi.getProjectKanban.mockResolvedValue(projectResponse(card));
      const http = deferred();
      taskApi.updateTask.mockReturnValue(http.promise);
      const mounted = mount(surface);
      fireEvent.click(await screen.findByText('Old card:0'));
      fireEvent.click(screen.getByText('Edit task'));
      fireEvent.click(screen.getByText('Submit task'));
      const updated = { ...card, title: 'Updated card' };
      boardApi.getBoardById.mockResolvedValue(boardResponse(updated));
      projectApi.getProjectKanban.mockResolvedValue(projectResponse(updated));
      if (order === 'hub first') emit('TaskUpdated', updated);
      await act(async () => http.resolve(response(updated)));
      await screen.findByText('Updated card:0');
      if (order === 'HTTP first') emit('TaskUpdated', updated);
      emit('TaskUpdated', updated);
      expect(screen.getAllByText('Updated card:0')).toHaveLength(1);
      expect(screen.queryByText('Old card:0')).toBeNull();
      expect([...hub.handlers.values()].every(handlers => handlers.size === 1)).toBe(true);
      mounted.unmount();
    });
    it(surface + ' task create ' + order + ' renders one card', async () => {
      const http = deferred();
      taskApi.createTask.mockReturnValue(http.promise);
      mount(surface);
      await screen.findByRole('heading', { name: 'Todo' });
      fireEvent.click(screen.getByTitle(surface === 'project' ? 'Add task' : 'board.addTask'));
      fireEvent.click(screen.getByText('Submit task'));
      const card = { id: 'new', listId: 'list', boardId: 'board', title: 'New card' };
      if (order === 'hub first') emit('TaskCreated', card);
      await act(async () => http.resolve(response(card)));
      if (order === 'HTTP first') emit('TaskCreated', card);
      expect(screen.getAllByText('New card:0')).toHaveLength(1);
      emit('TaskCommentsCountUpdated', { taskId: 'new', count: 7 });
      expect(screen.getAllByText('New card:7')).toHaveLength(1);
      emit('TaskDeleted', 'new');
      expect(screen.queryByText('New card:7')).toBeNull();
    });
    it(surface + ' column create ' + order + ' renders one column', async () => {
      const http = deferred();
      listApi.createList.mockReturnValue(http.promise);
      mount(surface);
      await screen.findByRole('heading', { name: 'Todo' });
      fireEvent.click(screen.getAllByRole('button', { name: surface === 'project' ? 'Add Column' : 'board.addColumn' })[0]);
      const form = document.querySelector('form');
      fireEvent.change(form.querySelector('input[type="text"]'), { target: { value: 'Created column' } });
      fireEvent.submit(form);
      await waitFor(() => expect(listApi.createList).toHaveBeenCalledTimes(1));
      const column = { id: 'new-column', boardId: 'board', name: 'Created column', position: 1 };
      if (order === 'hub first') emit('BoardListCreated', column);
      await act(async () => http.resolve(response(column)));
      if (order === 'HTTP first') emit('BoardListCreated', column);
      expect(screen.getAllByRole('heading', { name: 'Created column' })).toHaveLength(1);
      emit('BoardListUpdated', { ...column, name: 'Renamed column' });
      expect(screen.getByRole('heading', { name: 'Renamed column' })).toBeTruthy();
      emit('BoardListDeleted', column.id);
      expect(screen.queryByRole('heading', { name: 'Renamed column' })).toBeNull();
    });
  }
  it(surface + ' handlers cover the same eight events and clean up on unmount', async () => {
    const mounted = mount(surface);
    await screen.findByRole('heading', { name: 'Todo' });
    expect([...hub.handlers.keys()].sort()).toEqual(['BoardListCreated', 'BoardListDeleted', 'BoardListUpdated', 'TaskCommentsCountUpdated', 'TaskCreated', 'TaskDeleted', 'TaskUpdated', 'UpdateBoardPresence']);
    expect([...hub.handlers.values()].every(handlers => handlers.size === 1)).toBe(true);
    mounted.unmount();
    expect([...hub.handlers.values()].every(handlers => handlers.size === 0)).toBe(true);
  });
}
