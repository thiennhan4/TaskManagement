import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import ProjectTasksBoard from '@/pages/projects/tabs/ProjectTasksBoard';
import { TaskAttachments } from '@/components/tasks/TaskAttachments';
import { projectApi } from '@/api/projectApi';
import { attachmentApi } from '@/api/attachmentApi';

vi.mock('@/api/projectApi', () => ({ projectApi: { getProjectKanban: vi.fn() } }));
vi.mock('@/api/attachmentApi', () => ({ attachmentApi: { list: vi.fn(), download: vi.fn() } }));
vi.mock('@/context/NotificationContext', () => ({ useNotification: () => ({ hubConnection: null, reconnectVersion: 0 }) }));
vi.mock('@/components/tasks/TaskCard', () => ({ default: ({ task }) => <div>{task.title}</div> }));
vi.mock('@/components/tasks/TaskModal', () => ({ default: () => null }));
vi.mock('@/components/tasks/TaskFormModal', () => ({ default: () => null }));
vi.mock('@hello-pangea/dnd', () => ({
  DragDropContext: ({ children }) => children,
  Droppable: ({ children }) => children({ innerRef: () => {}, droppableProps: {}, placeholder: null }, {}),
}));

afterEach(() => { cleanup(); vi.restoreAllMocks(); });
const page = (items) => ({ items, page: 1, pageSize: 20, totalItems: items.length, totalPages: 1 });
const response = (title = 'Persisted card') => ({ data: { data: {
  board: { id: 'board' }, canCreateTasks: false, canManageColumns: false,
  lists: page([{ id: 'list', name: 'Todo', tasks: page([{ id: 'task', title }]) }]),
} } });

describe('Kanban aggregate contract', () => {
  beforeEach(() => { projectApi.getProjectKanban.mockResolvedValue(response()); });
  it('loads persisted columns/cards with one request, including after remount', async () => {
    const view = render(<ProjectTasksBoard projectId="project" requestedBoardId="board" />);
    expect(await screen.findByText('Persisted card')).toBeTruthy();
    expect(screen.getByText('Todo')).toBeTruthy();
    expect(projectApi.getProjectKanban).toHaveBeenCalledExactlyOnceWith('project', { boardId: 'board' });
    view.unmount();
    render(<ProjectTasksBoard projectId="project" requestedBoardId="board" />);
    expect(await screen.findByText('Persisted card')).toBeTruthy();
    expect(projectApi.getProjectKanban).toHaveBeenCalledTimes(2);
  });
  it('shows forbidden as an error, not an empty board', async () => {
    projectApi.getProjectKanban.mockRejectedValue({ response: { status: 403, data: { message: 'Access denied' } } });
    render(<ProjectTasksBoard projectId="project" />);
    expect((await screen.findByRole('alert')).textContent).toContain('Access denied');
    expect(screen.queryByText('No board found')).toBeNull();
  });
  it('searches unloaded cards on the server and restarts from the first page', async () => {
    render(<ProjectTasksBoard projectId="project" requestedBoardId="board" />);
    expect(await screen.findByText('Persisted card')).toBeTruthy();
    projectApi.getProjectKanban.mockResolvedValueOnce(response('Beyond first page'));
    fireEvent.change(screen.getByPlaceholderText('Search tasks...'), { target: { value: 'Needle' } });
    expect(await screen.findByText('Beyond first page')).toBeTruthy();
    expect(projectApi.getProjectKanban).toHaveBeenLastCalledWith('project', { boardId: 'board', searchKeyword: 'Needle' });
    expect(screen.queryByText('Persisted card')).toBeNull();
  });
  it('ignores a stale response after switching projects', async () => {
    let resolveOld;
    projectApi.getProjectKanban.mockImplementationOnce(() => new Promise(resolve => { resolveOld = resolve; }));
    const view = render(<ProjectTasksBoard projectId="old" />);
    await waitFor(() => expect(resolveOld).toBeTypeOf('function'));
    view.rerender(<ProjectTasksBoard projectId="new" />);
    expect(await screen.findByText('Persisted card')).toBeTruthy();
    resolveOld(response('Stale card'));
    await waitFor(() => expect(screen.queryByText('Stale card')).toBeNull());
  });
});

describe('private attachments', () => {
  it('downloads via the authenticated client and honors safe metadata permissions', async () => {
    attachmentApi.list.mockResolvedValue({ success: true, data: page([{ id: 'file', fileName: 'note.txt', uploadedByUserName: 'Member', canDelete: false }]) });
    attachmentApi.download.mockResolvedValue(new Blob(['private']));
    URL.createObjectURL = vi.fn(() => 'blob:private');
    URL.revokeObjectURL = vi.fn();
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {});
    render(<TaskAttachments taskId="task" />);
    fireEvent.click(await screen.findByTitle('Download'));
    await waitFor(() => expect(click).toHaveBeenCalledOnce());
    expect(attachmentApi.download).toHaveBeenCalledWith('task', 'file');
    expect(screen.queryByTitle('Delete')).toBeNull();
    expect(screen.getByText(/By Member/)).toBeTruthy();
  });
  it('does not present access denial as an empty collection', async () => {
    attachmentApi.list.mockRejectedValue({ response: { data: { message: 'Access denied' } } });
    render(<TaskAttachments taskId="task" />);
    expect((await screen.findByRole('alert')).textContent).toContain('Access denied');
    expect(screen.queryByText('No attachments yet')).toBeNull();
  });
  it('distinguishes loading from a successfully loaded empty page', async () => {
    let resolve;
    attachmentApi.list.mockImplementationOnce(() => new Promise(done => { resolve = done; }));
    render(<TaskAttachments taskId="task" />);
    expect(screen.getByRole('status', { name: 'Loading attachments' })).toBeTruthy();
    expect(screen.queryByText('No attachments yet')).toBeNull();
    resolve({ success: true, data: page([]) });
    expect(await screen.findByText('No attachments yet')).toBeTruthy();
  });
  it('reports a malformed collection rather than treating it as empty', async () => {
    attachmentApi.list.mockResolvedValueOnce({ success: true, data: [] });
    render(<TaskAttachments taskId="task" />);
    expect((await screen.findByRole('alert')).textContent).toContain('Invalid attachment response');
    expect(screen.queryByText('No attachments yet')).toBeNull();
  });
  it('uses the attachment total and requests the next page', async () => {
    attachmentApi.list.mockResolvedValueOnce({ success: true, data: { ...page([{ id: 'one', fileName: 'first.txt' }]), totalItems: 21, totalPages: 2 } })
      .mockResolvedValueOnce({ success: true, data: { ...page([{ id: 'two', fileName: 'last.txt' }]), page: 2, totalItems: 21, totalPages: 2 } });
    render(<TaskAttachments taskId="task" />);
    expect(await screen.findByText('first.txt')).toBeTruthy();
    expect(screen.getByText('Attachments (21)')).toBeTruthy();
    fireEvent.click(screen.getByRole('button', { name: 'Next' }));
    expect(await screen.findByText('last.txt')).toBeTruthy();
    expect(attachmentApi.list).toHaveBeenLastCalledWith('task', 2);
    expect(screen.queryByText('first.txt')).toBeNull();
  });
});
