import { useEffect, useState } from 'react';
import { useTaskDetail } from '@/hooks/useTaskDetail';
import { taskApi } from '@/api/taskApi';
import Modal from '@/components/ui/Modal';
import Button from '@/components/ui/Button';
import Select from '@/components/ui/Select';
import PageControls from '@/components/ui/PageControls';
import TaskFormModal from '@/components/tasks/TaskFormModal';
import TaskComments from '@/components/tasks/TaskComments';
import TimeTrackingWidget from '@/components/timetracking/TimeTrackingWidget';
import { TaskActivity } from '@/components/tasks/TaskActivity';
import { TaskAttachments } from '@/components/tasks/TaskAttachments';
import { StatusBadge } from '@/components/tasks/StatusBadge';
import { PriorityBadge } from '@/components/tasks/PriorityBadge';

export default function TaskModal(props) {
  return props.isOpen ? <TaskDetail key={props.taskId} {...props} /> : null;
}

function TaskDetail({ isOpen, taskId, onClose, onDelete, readOnly = false, onChanged }) {
  const { task, error, isLoading, load } = useTaskDetail(taskId);
  const [pending, setPending] = useState(false);
  const [actionError, setActionError] = useState('');
  const [editing, setEditing] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [tab, setTab] = useState('comments');
  const capabilities = task?.capabilities || {};
  const run = async action => {
    if (pending) return;
    setPending(true); setActionError('');
    try { await action(); await load(taskId); onChanged?.(); }
    catch (err) { setActionError(err.response?.data?.message || err.message || 'Action failed. Retry when your connection is restored.'); }
    finally { setPending(false); }
  };
  const remove = async () => {
    if (!confirmDelete) { setConfirmDelete(true); return; }
    if (pending) return;
    setPending(true); setActionError('');
    try {
      if (onDelete) await onDelete(taskId); else await taskApi.deleteTask(taskId);
      onChanged?.(); onClose();
    } catch (err) { setActionError(err.response?.data?.message || err.message || 'Task could not be deleted.'); }
    finally { setPending(false); }
  };
  return <>
    <Modal isOpen={isOpen} onClose={onClose} closeDisabled={pending} title={task?.title || 'Task details'}>
      <div className="p-4 sm:p-6 space-y-5 break-words">
        {error ? <div role="alert"><p>{error}</p><Button variant="outline" onClick={() => load(taskId)}>Retry</Button></div> : !task ? <p role="status">Loading task details…</p> : <>
          {isLoading && <p role="status">Refreshing task details…</p>}
          <div className="flex gap-2"><StatusBadge status={task.status} /><PriorityBadge priority={task.priority} /></div>
          <p className="whitespace-pre-wrap text-text-main">{task.description || 'No description.'}</p>
          <p className="text-text-muted">Assignee: {task.assignedToName || 'Unassigned'}</p>
          <p className="text-text-muted">Due: {task.dueDate ? new Date(task.dueDate).toLocaleDateString() : 'No deadline'}</p>
          <div className="flex flex-wrap gap-3">
            {!readOnly && capabilities.canChangeStatus && task.status !== 'Done' && <Button isLoading={pending} onClick={() => run(() => taskApi.changeTaskStatus(taskId, 'Done'))}>Mark done</Button>}
            {!readOnly && capabilities.canEdit && <Button variant="outline" disabled={pending} onClick={() => setEditing(true)}>Edit task</Button>}
            {!readOnly && capabilities.canDelete && <Button variant="danger" isLoading={pending} onClick={remove}>{confirmDelete ? 'Confirm delete' : 'Delete task'}</Button>}
          </div>
          {confirmDelete && <p>Delete permanently removes this task from active views. This is not archive.</p>}
          <Assignee key={task.id + ':' + task.updatedAt} task={task} disabled={pending || readOnly || !capabilities.canAssign} allowed={!readOnly && capabilities.canAssign} onAssign={id => run(() => taskApi.assignTask(taskId, id || null))} />
          {actionError && <p role="alert" className="text-danger">{actionError}</p>}
          <nav aria-label="Task sections" className="flex flex-wrap gap-2">{['comments', 'time', 'attachments', 'activity'].map(name => <Button key={name} variant={tab === name ? 'primary' : 'outline'} aria-pressed={tab === name} onClick={() => setTab(name)}>{name}</Button>)}</nav>
          {tab === 'comments' && <TaskComments taskId={taskId} readOnly={readOnly} />}
          {tab === 'time' && <TimeTrackingWidget taskId={taskId} />}
          {tab === 'attachments' && <TaskAttachments taskId={taskId} readOnly />}
          {tab === 'activity' && <TaskActivity taskId={taskId} />}
        </>}
      </div>
    </Modal>
    {editing && task && <TaskFormModal isOpen task={task} onClose={() => setEditing(false)} onSubmit={async payload => { const response = await taskApi.updateTask(taskId, payload); if (!response.data.success) throw new Error(response.data.message); await load(taskId); onChanged?.(); }} />}
  </>;
}

function Assignee({ task, allowed, disabled, onAssign }) {
  const [page, setPage] = useState(1);
  const [result, setResult] = useState(null);
  const [error, setError] = useState('');
  const [loadedPage, setLoadedPage] = useState(null);
  useEffect(() => {
    if (!allowed) return;
    let active = true;
    taskApi.getEligibleAssignees(task.id, page).then(({ data }) => {
      if (!data.success) throw new Error(data.message || 'Eligible members could not be loaded.');
      if (active) { setResult(data.data); setError(''); setLoadedPage(page); }
    }).catch(err => { if (active) { setError(err.response?.data?.message || err.message || 'Eligible members could not be loaded.'); setLoadedPage(page); } });
    return () => { active = false; };
  }, [task.id, page, allowed]);
  if (!allowed) return <Select label="Assignee" disabled value={task.assignedToId || ''}><option value={task.assignedToId || ''}>{task.assignedToName || 'Unassigned'} (assignment unavailable)</option></Select>;
  const loading = loadedPage !== page;
  return <div className="space-y-2">
    {loading && <p role="status">Loading eligible members…</p>}
    {error && <p role="alert">{error}</p>}
    <Select label="Assignee" disabled={disabled || loading || !!error} value={task.assignedToId || ''} onChange={event => onAssign(event.target.value)}>
      <option value="">Unassigned</option>
      {task.assignedToId && !result?.items.some(member => member.id === task.assignedToId) && <option value={task.assignedToId}>{task.assignedToName} (current assignee)</option>}
      {result?.items.map(member => <option key={member.id} value={member.id}>{member.fullName}</option>)}
    </Select>
    <PageControls page={result} loading={loading} onPage={setPage} />
  </div>;
}
