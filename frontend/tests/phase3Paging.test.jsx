import { afterEach, expect, it, vi } from 'vitest';
import { act, cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import MyTasksPage from '@/pages/tasks/MyTasksPage';
import BoardDetailPage from '@/pages/boards/BoardDetailPage';
import { taskApi } from '@/api/taskApi';
import { boardApi } from '@/api/boardApi';
import { calendarApi } from '@/api/calendarApi';
import { useCalendarStore } from '@/stores/useCalendarStore';

const drag = vi.hoisted(() => ({ end: null }));
const translate = vi.hoisted(() => key => key);
vi.mock('@/api/taskApi', () => ({ taskApi: { getMyTasks: vi.fn(), getSummary: vi.fn(), moveTask: vi.fn() } }));
vi.mock('@/api/boardApi', () => ({ boardApi: { getBoardById: vi.fn(), getColumns: vi.fn() } }));
vi.mock('@/api/calendarApi', () => ({ calendarApi: { getCalendarTasks: vi.fn() } }));
vi.mock('@/context/LanguageContext', () => ({ useLanguage: () => ({ t: translate }) }));
vi.mock('@/context/NotificationContext', () => ({ useNotification: () => ({ hubConnection: null, reconnectVersion: 0 }) }));
vi.mock('@/components/tasks/TaskCard', () => ({ default: ({ task }) => <span>{task.title}</span> }));
vi.mock('@/components/tasks/TaskModal', () => ({ default: () => null }));
vi.mock('@/components/tasks/TaskFormModal', () => ({ default: () => null }));
vi.mock('@hello-pangea/dnd', () => ({
  DragDropContext: ({ children, onDragEnd }) => { drag.end = onDragEnd; return children; },
  Droppable: ({ children }) => children({ innerRef: () => {}, droppableProps: {}, placeholder: null }, {}),
}));
afterEach(() => { cleanup(); vi.clearAllMocks(); });
const page = (items, current = 1, totalItems = 21, pageSize = 20) => ({ items, page: current, pageSize, totalItems, totalPages: Math.ceil(totalItems / pageSize) });
const response = data => ({ data: { success: true, data } });

it('My Tasks uses server pages and independent totals', async () => {
  taskApi.getMyTasks.mockResolvedValueOnce(response(page([{ id: 'one', title: 'First page task', status: 'Todo' }])))
    .mockResolvedValueOnce(response(page([{ id: 'two', title: 'Second page task', status: 'Todo' }], 2)));
  taskApi.getSummary.mockResolvedValue(response({ total: 123, done: 90, inProgress: 20, review: 3 }));
  render(<MemoryRouter><MyTasksPage /></MemoryRouter>);
  expect(await screen.findByText('First page task')).toBeTruthy();
  expect(screen.getByText('123')).toBeTruthy();
  expect(screen.getByText(/21 matching tasks/)).toBeTruthy();
  fireEvent.click(screen.getByRole('button', { name: 'Next' }));
  expect(await screen.findByText('Second page task')).toBeTruthy();
  expect(screen.queryByText('First page task')).toBeNull();
  expect(taskApi.getMyTasks).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2, pageSize: 20 }));
  taskApi.getMyTasks.mockResolvedValueOnce(response(page([{ id: 'three', title: 'Server search result' }], 1, 1)));
  fireEvent.change(screen.getByPlaceholderText('Search tasks...'), { target: { value: 'Server' } });
  expect(await screen.findByText('Server search result')).toBeTruthy();
  expect(taskApi.getMyTasks).toHaveBeenLastCalledWith(expect.objectContaining({ page: 1, searchKeyword: 'Server' }));
});

it('standalone boards show partial data and prevent reorder until loaded', async () => {
  const first = { id: 'list', name: 'Todo', tasks: page([{ id: 'one', title: 'Loaded card' }], 1, 2, 1) };
  boardApi.getBoardById.mockResolvedValue(response({ id: 'board', name: 'Standalone', listPage: page([first], 1, 1) }));
  boardApi.getColumns.mockResolvedValue(response(page([{ ...first, tasks: page([{ id: 'two', title: 'Remaining card' }], 2, 2, 1) }], 1, 1)));
  render(<MemoryRouter initialEntries={['/boards/board']}><Routes><Route path="/boards/:id" element={<BoardDetailPage />} /></Routes></MemoryRouter>);
  expect(await screen.findByText('Loaded card')).toBeTruthy();
  expect(screen.getByRole('status').textContent).toContain('Showing part');
  await drag.end({ draggableId: 'one', source: { droppableId: 'list', index: 0 }, destination: { droppableId: 'list', index: 1 } });
  expect(taskApi.moveTask).not.toHaveBeenCalled();
  fireEvent.click(screen.getByRole('button', { name: 'Load more cards: Todo' }));
  expect(await screen.findByText('Remaining card')).toBeTruthy();
  await waitFor(() => expect(screen.queryByRole('status')).toBeNull());
});

it('standalone board search requests a server filter and resets card pages', async () => {
  const board = title => response({ id: 'board', name: 'Standalone', listPage: page([{ id: 'list', name: 'Todo', tasks: page([{ id: title, title }], 1, 1) }], 1, 1) });
  boardApi.getBoardById.mockResolvedValueOnce(board('Original card')).mockResolvedValueOnce(board('Unloaded match'));
  render(<MemoryRouter initialEntries={['/boards/board']}><Routes><Route path="/boards/:id" element={<BoardDetailPage />} /></Routes></MemoryRouter>);
  expect(await screen.findByText('Original card')).toBeTruthy();
  fireEvent.change(screen.getByPlaceholderText('board.searchPlaceholder'), { target: { value: 'Needle' } });
  expect(await screen.findByText('Unloaded match')).toBeTruthy();
  expect(boardApi.getBoardById).toHaveBeenLastCalledWith('board', { searchKeyword: 'Needle' });
  expect(screen.queryByText('Original card')).toBeNull();
});

it('My Tasks ignores a late page response after changing the filter', async () => {
  let resolveOld;
  taskApi.getMyTasks.mockResolvedValueOnce(response(page([{ id: 'one', title: 'Original task' }])))
    .mockImplementationOnce(() => new Promise(resolve => { resolveOld = resolve; }))
    .mockResolvedValueOnce(response(page([{ id: 'filtered', title: 'Filtered task' }], 1, 1)));
  taskApi.getSummary.mockResolvedValue(response({ total: 21 }));
  render(<MemoryRouter><MyTasksPage /></MemoryRouter>);
  expect(await screen.findByText('Original task')).toBeTruthy();
  fireEvent.click(screen.getByRole('button', { name: 'Next' }));
  await waitFor(() => expect(resolveOld).toBeTypeOf('function'));
  fireEvent.change(screen.getByPlaceholderText('Search tasks...'), { target: { value: 'Filtered' } });
  expect(await screen.findByText('Filtered task')).toBeTruthy();
  await act(async () => resolveOld(response(page([{ id: 'old', title: 'Stale task' }], 2))));
  expect(screen.queryByText('Stale task')).toBeNull();
  expect(screen.getByText('Filtered task')).toBeTruthy();
});

it('calendar appends pages and exposes errors instead of implying completeness', async () => {
  useCalendarStore.setState({ tasks: [], taskPage: null });
  calendarApi.getCalendarTasks.mockResolvedValueOnce(response(page([{ id: 'one' }], 1, 101, 100)))
    .mockResolvedValueOnce(response(page([{ id: 'two' }], 2, 101, 100)));
  await useCalendarStore.getState().fetchCalendarTasks();
  expect(useCalendarStore.getState().taskPage.totalPages).toBe(2);
  await useCalendarStore.getState().fetchCalendarTasks(2);
  expect(useCalendarStore.getState().tasks.map(t => t.id)).toEqual(['one', 'two']);
  calendarApi.getCalendarTasks.mockRejectedValueOnce(new Error('denied'));
  await useCalendarStore.getState().fetchCalendarTasks();
  expect(useCalendarStore.getState().loadError).toBeTruthy();
});

it('calendar ignores a late failure from the previous date window', async () => {
  let rejectOld;
  useCalendarStore.setState({ currentDate: new Date(2026,0,1), tasks: [], taskPage: null });
  calendarApi.getCalendarTasks.mockImplementationOnce(() => new Promise((resolve,reject) => { rejectOld=reject; }))
    .mockResolvedValueOnce(response(page([{ id: 'current' }],1,1)));
  const old = useCalendarStore.getState().fetchCalendarTasks();
  useCalendarStore.getState().setCurrentDate(new Date(2026,1,1));
  await useCalendarStore.getState().fetchCalendarTasks();
  rejectOld(new Error('Old window failed'));
  await old;
  expect(useCalendarStore.getState().tasks.map(t=>t.id)).toEqual(['current']);
  expect(useCalendarStore.getState().loadError).toBeNull();
  expect(useCalendarStore.getState().isLoading).toBe(false);
});

it('My Tasks does not display an empty-state claim after access denial', async () => {
  taskApi.getMyTasks.mockRejectedValueOnce({ response: { data: { message: 'Access denied' } } });
  taskApi.getSummary.mockResolvedValue(response({ total: 0 }));
  render(<MemoryRouter><MyTasksPage /></MemoryRouter>);
  expect((await screen.findByRole('alert')).textContent).toContain('Access denied');
  expect(screen.queryByText('No tasks assigned to you')).toBeNull();
});
