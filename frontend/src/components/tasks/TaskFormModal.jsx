import { TASK_PRIORITIES as PRIORITIES, TASK_STATUSES as STATUSES } from '@/constants/taskStatus';
import Select from '@/components/ui/Select';
import Textarea from '@/components/ui/Textarea';
import { useId, useState } from 'react';
import { Loader2, Plus, Calendar as CalendarIcon, Tag, AlertCircle } from 'lucide-react';
import Modal from '@/components/ui/Modal';
import Button from '@/components/ui/Button';
import Input from '@/components/ui/Input';
import toast from 'react-hot-toast';



export default function TaskFormModal(props) {
  return props.isOpen ? <TaskForm key={props.task?.id || 'new'} {...props} /> : null;
}

function TaskForm({ isOpen, onClose, onSubmit, task }) {
  const isEdit = Boolean(task);
  const dueDateId = useId();

  const [initialForm] = useState(() => ({
    title: task?.title || '', description: task?.description || '', priority: task?.priority || 'Medium',
    status: task?.status || 'Todo', dueDate: task?.dueDate?.split('T')[0] || '', label: task?.label || '',
  }));
  const [form, setForm] = useState(initialForm);
  const [errors, setErrors] = useState({});
  const [loading, setLoading] = useState(false);



  const validate = () => {
    const errs = {};
    if (!form.title.trim()) errs.title = 'Title is required';
    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (loading || !validate()) return;

    const payload = {
      title: form.title.trim(),
      description: form.description.trim() || null,
      priority: form.priority,
      dueDate: form.dueDate ? new Date(form.dueDate).toISOString() : null,
      label: form.label.trim() || null,
      ...(isEdit && { status: form.status }),
    };
    if (isEdit) {
      for (const field of ['description', 'priority', 'dueDate', 'label', 'status']) {
        if (form[field] === initialForm[field]) delete payload[field];
      }
    }

    try {
      setLoading(true);
      await onSubmit(payload);
      onClose();
    } catch (err) {
      toast.error(err?.response?.data?.message || 'Something went wrong');
    } finally {
      setLoading(false);
    }
  };

  const set = (field) => (value) => setForm(f => ({ ...f, [field]: value }));

  return (
    <Modal isOpen={isOpen} onClose={onClose} closeDisabled={loading} title={isEdit ? 'Update Task' : 'Create New Task'} maxWidth="max-w-xl">
      <form onSubmit={handleSubmit} className="p-4 sm:p-8 space-y-6">

        <Input
          label="Task Title"
          placeholder="What needs to be done?"
          value={form.title}
          onChange={(e) => set('title')(e.target.value)}
          error={errors.title}
          autoFocus
        />

        <div className="flex flex-col gap-1.5">

          <Textarea label="Description"
            className="w-full px-4 py-3 rounded-xl border border-border-subtle bg-surface-0 text-text-main placeholder:text-text-muted focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all duration-200 min-h-[120px]"
            placeholder="Add some details about this task..."
            value={form.description}
            onChange={(e) => set('description')(e.target.value)}
          />
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <fieldset className="min-w-0 flex flex-col gap-1.5">
            <legend className="text-sm font-bold text-text-main ml-1">Priority</legend>
            <div className="flex gap-2">
              {PRIORITIES.map(p => (
                <button
                  key={p}
                  type="button"
                  aria-pressed={form.priority === p}
                  onClick={() => set('priority')(p)}
                  className={`
                    flex-1 py-2 text-xs font-bold rounded-lg border transition-all
                    ${form.priority === p 
                      ? 'bg-primary/10 border-primary text-text-main'
                      : 'bg-surface-0 border-border-subtle text-text-muted hover:border-border-strong'}
                  `}
                >
                  {p}
                </button>
              ))}
            </div>
          </fieldset>

          <div className="flex flex-col gap-1.5">
            <label htmlFor={dueDateId} className="text-sm font-bold text-text-main ml-1 flex items-center gap-2">
              <CalendarIcon size={14} /> Due Date
            </label>
            <input 
              id={dueDateId} type="date"
              className="w-full px-4 py-2 rounded-xl border border-border-subtle bg-surface-0 text-text-main focus:outline-none focus:ring-2 focus:ring-primary/20 transition-all text-sm font-medium"
              value={form.dueDate}
              onChange={(e) => set('dueDate')(e.target.value)}
            />
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <Input
            label="Label"
            placeholder="e.g. Design, Bug"
            value={form.label}
            onChange={(e) => set('label')(e.target.value)}
          />

          {isEdit && (
            <div className="flex flex-col gap-1.5">

              <Select label="Status"
                className="w-full px-4 py-2 rounded-xl border border-border-subtle bg-surface-0 text-text-main focus:outline-none focus:ring-2 focus:ring-primary/20 transition-all text-sm font-medium appearance-none"
                value={form.status}
                onChange={(e) => set('status')(e.target.value)}
              >
                {STATUSES.map(s => <option key={s} value={s}>{s}</option>)}
              </Select>
            </div>
          )}
        </div>

        <div className="sticky bottom-0 z-10 bg-surface-0 flex gap-3 py-3 border-t border-border-subtle">
          <Button type="button" variant="ghost" disabled={loading} onClick={onClose} className="flex-1">Cancel</Button>
          <Button
            type="submit"
            className="flex-1"
            isLoading={loading}
            leftIcon={!loading && (isEdit ? <Save size={18} /> : <Plus size={18} />)}
          >
            {isEdit ? 'Save Changes' : 'Create Task'}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

function Save({ size }) {
  return (
    <svg 
      width={size} 
      height={size} 
      viewBox="0 0 24 24" 
      fill="none" 
      stroke="currentColor" 
      strokeWidth="2" 
      strokeLinecap="round" 
      strokeLinejoin="round"
    >
      <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v13a2 2 0 0 1-2 2z"></path>
      <polyline points="17 21 17 13 7 13 7 21"></polyline>
      <polyline points="7 3 7 8 15 8"></polyline>
    </svg>
  );
}
