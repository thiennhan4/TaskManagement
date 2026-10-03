import { useEffect, useRef, useState } from 'react';
import { taskApi } from '@/api/taskApi';
import Modal from '@/components/ui/Modal';
import Select from '@/components/ui/Select';
import Button from '@/components/ui/Button';

export default function MoveTaskModal({ taskId, lists, onClose, onMoved }) {
  const [task, setTask] = useState(null);
  const [destination, setDestination] = useState('');
  const [loading, setLoading] = useState(true);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState('');
  const submitted = useRef(false);
  const mounted = useRef(false);
  useEffect(() => {
    mounted.current = true;
    let active = true;
    taskApi.getTaskById(taskId).then(response => { if (active) setTask(response.data.data); })
      .catch(error => { if (active) setError(error.response?.status === 403 ? 'You do not have permission to move this task.' : 'Task could not be loaded. Close and try again.'); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; mounted.current = false; };
  }, [taskId]);
  const eligible = lists.filter(list => String(list.id) !== String(task?.listId));
  const allowed = task?.capabilities?.canEdit === true;
  const submit = async event => {
    event.preventDefault();
    const target = eligible.find(list => String(list.id) === destination);
    if (submitted.current || !allowed || !target) return;
    submitted.current = true; setPending(true); setError('');
    try {
      const response = await taskApi.moveTask(taskId, { listId: target.id, position: target.tasks?.length || 0, expectedUpdatedAt: task.updatedAt ?? null });
      if (response.data.success === false) throw new Error(response.data.message || 'Move was not completed.');
      if (mounted.current) onMoved(response.data.data, target.name);
    } catch (error) {
      if (mounted.current) setError(error.response?.status === 403 ? 'You do not have permission to move this task.' : error.response?.data?.message || error.message || 'Move failed. Retry after checking the board.');
    } finally { submitted.current = false; if (mounted.current) setPending(false); }
  };
  return <Modal isOpen title="Move task" onClose={onClose} closeDisabled={pending} maxWidth="max-w-md">
    <form onSubmit={submit} className="p-4 sm:p-6 space-y-4">
      {loading && <p role="status">Loading task permissions…</p>}
      {task && <p className="break-words font-bold">{task.title}</p>}
      {task && !allowed && <p role="alert">You do not have permission to move this task.</p>}
      {error && <p role="alert">{error}</p>}
      <Select label="Destination column" required value={destination} onChange={event => setDestination(event.target.value)} disabled={loading || pending || !allowed}>
        <option value="">Choose a column</option>{eligible.map(list => <option key={list.id} value={list.id}>{list.name}</option>)}
      </Select>
      <p className="text-sm text-text-muted">Move to the end of the selected column. Drag and drop remains available for precise ordering.</p>
      <div className="flex gap-3"><Button type="button" variant="ghost" disabled={pending} onClick={onClose}>Cancel</Button><Button type="submit" isLoading={pending} disabled={!allowed || !destination || loading}>Move task</Button></div>
    </form>
  </Modal>;
}
