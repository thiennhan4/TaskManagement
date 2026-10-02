import { beforeEach, afterEach, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import TaskModal from '@/components/tasks/TaskModal';
import TaskDetailDrawer from '@/components/tasks/TaskDetailDrawer';
import TaskDetailPage from '@/pages/tasks/TaskDetailPage';
import { taskApi } from '@/api/taskApi';
import { useTaskDetailStore } from '@/stores/useTaskDetailStore';

vi.mock('@/api/taskApi', () => ({ taskApi: { getTaskById: vi.fn(), getEligibleAssignees: vi.fn(), changeTaskStatus: vi.fn(), assignTask: vi.fn(), deleteTask: vi.fn(), updateTask: vi.fn() } }));
vi.mock('@/components/tasks/TaskComments', () => ({ default: ({ taskId }) => <p>Comments for {taskId}</p> }));
vi.mock('@/components/timetracking/TimeTrackingWidget', () => ({ default: ({ taskId }) => <p>Time for {taskId}</p> }));
vi.mock('@/components/tasks/TaskActivity', () => ({ TaskActivity: ({ taskId }) => <p>Activity for {taskId}</p> }));
vi.mock('@/components/tasks/TaskAttachments', () => ({ TaskAttachments: ({ taskId, readOnly }) => <p>Attachments for {taskId}, read only {String(readOnly)}</p> }));
const task = { id: 'task', title: 'Canonical title', status: 'Review', priority: 'Critical', capabilities: { canEdit: true, canDelete: true, canAssign: true, canChangeStatus: true } };
beforeEach(() => {
  vi.clearAllMocks(); useTaskDetailStore.getState().reset();
  taskApi.getTaskById.mockResolvedValue({ data: { success: true, data: task } });
  taskApi.getEligibleAssignees.mockResolvedValue({ data: { success: true, data: { items: [{ id: 'member', fullName: 'Eligible Member' }], totalItems: 1, page: 1, pageSize: 20, totalPages: 1 } } });
  taskApi.changeTaskStatus.mockResolvedValue({ data: { success: true } });
  taskApi.assignTask.mockResolvedValue({ data: { success: true } });
});
afterEach(cleanup);

it('calendar drawer uses canonical fetched detail, functional completion and sections', async () => {
  render(<TaskDetailDrawer isOpen taskId="task" onClose={vi.fn()} />);
  await screen.findByRole('dialog', { name: 'Canonical title' });
  expect(screen.getByText('Review')).toBeTruthy(); expect(screen.getByText('Critical')).toBeTruthy();
  expect(screen.getByText('Comments for task')).toBeTruthy();
  fireEvent.click(screen.getByRole('button', { name: 'Mark done' }));
  await waitFor(() => expect(taskApi.changeTaskStatus).toHaveBeenCalledWith('task', 'Done'));
  await waitFor(() => expect(taskApi.getTaskById).toHaveBeenCalledTimes(2));
  fireEvent.click(screen.getByRole('button', { name: 'time' })); expect(screen.getByText('Time for task')).toBeTruthy();
  fireEvent.click(screen.getByRole('button', { name: 'attachments' })); expect(screen.getByText('Attachments for task, read only true')).toBeTruthy();
  fireEvent.click(screen.getByRole('button', { name: 'activity' })); expect(screen.getByText('Activity for task')).toBeTruthy();
});

it('loads eligible members only when assignment capability permits', async () => {
  taskApi.getTaskById.mockResolvedValue({ data: { success: true, data: { ...task, capabilities: {} } } });
  render(<TaskModal isOpen taskId="task" onClose={vi.fn()} />);
  await screen.findByRole('dialog', { name: 'Canonical title' });
  expect(screen.getByLabelText('Assignee').disabled).toBe(true);
  expect(taskApi.getEligibleAssignees).not.toHaveBeenCalled();
  expect(screen.queryByRole('button', { name: 'Mark done' })).toBeNull();
});

it('assigns using eligible API options and refreshes detail', async () => {
  render(<TaskModal isOpen taskId="task" onClose={vi.fn()} />);
  await screen.findByRole('option', { name: 'Eligible Member' });
  fireEvent.change(screen.getByLabelText('Assignee'), { target: { value: 'member' } });
  await waitFor(() => expect(taskApi.assignTask).toHaveBeenCalledWith('task', 'member'));
});

it('keeps detail and error visible when deletion fails', async () => {
  const close = vi.fn(); taskApi.deleteTask.mockRejectedValue(new Error('Delete failed'));
  render(<TaskModal isOpen taskId="task" onClose={close} />);
  fireEvent.click(await screen.findByRole('button', { name: 'Delete task' }));
  fireEvent.click(screen.getByRole('button', { name: 'Confirm delete' }));
  expect((await screen.findByRole('alert')).textContent).toContain('Delete failed');
  expect(close).not.toHaveBeenCalled();
});

for (const [status, message] of [[403, 'permission'], [404, 'not found']]) it('explicit task error ' + status, async () => {
  taskApi.getTaskById.mockRejectedValue({ response: { status } });
  render(<TaskModal isOpen taskId="task" onClose={vi.fn()} />);
  expect((await screen.findByRole('alert')).textContent).toContain(message);
});

it('deep link loads by route ID without previous page state', async () => {
  render(<MemoryRouter initialEntries={['/tasks/task']}><Routes><Route path="/tasks/:taskId" element={<TaskDetailPage />} /></Routes></MemoryRouter>);
  await screen.findByRole('dialog', { name: 'Canonical title' });
  expect(taskApi.getTaskById).toHaveBeenCalledWith('task');
});
