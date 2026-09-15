import { useState, useEffect, useMemo } from 'react';
import { useAuth } from '@/context/AuthContext';
import { taskApi } from '@/api/taskApi';
import {
  CheckCircle2, Circle, Clock, Layout, Search, Filter,
  ExternalLink, Plus, ChevronDown, AlertCircle, Timer,
  BarChart2, Inbox, ArrowUpRight, MoreHorizontal, X,
} from 'lucide-react';
import { Link } from 'react-router-dom';
import toast from 'react-hot-toast';
import TaskModal from '@/components/tasks/TaskModal';
import TaskFormModal from '@/components/tasks/TaskFormModal';
import Button from '@/components/ui/Button';

// ── Status config ──
const STATUS_CONFIG = {
  Todo:        { label: 'Todo',        color: '#64748b', bg: 'bg-surface-2', text: 'text-text-muted', dot: 'bg-text-subtle', icon: Circle },
  InProgress:  { label: 'In Progress', color: '#6366f1', bg: 'bg-violet-50 dark:bg-violet-900/20', text: 'text-violet-700 dark:text-violet-300', dot: 'bg-violet-500', icon: Timer },
  Done:        { label: 'Done',        color: '#22c55e', bg: 'bg-emerald-50 dark:bg-emerald-900/20', text: 'text-emerald-700 dark:text-emerald-300', dot: 'bg-emerald-500', icon: CheckCircle2 },
  Blocked:     { label: 'Blocked',     color: '#ef4444', bg: 'bg-red-50 dark:bg-red-900/20', text: 'text-red-700 dark:text-red-300', dot: 'bg-red-500', icon: AlertCircle },
};

const PRIORITY_CONFIG = {
  High:   { label: 'High',   color: 'text-red-500',    bg: 'bg-red-50 dark:bg-red-900/20',    border: 'border-red-200 dark:border-red-800' },
  Medium: { label: 'Medium', color: 'text-amber-500',  bg: 'bg-amber-50 dark:bg-amber-900/20', border: 'border-amber-200 dark:border-amber-800' },
  Low:    { label: 'Low',    color: 'text-emerald-500', bg: 'bg-emerald-50 dark:bg-emerald-900/20', border: 'border-emerald-200 dark:border-emerald-800' },
};

function StatusBadge({ status }) {
  const cfg = STATUS_CONFIG[status] || STATUS_CONFIG.Todo;
  const Icon = cfg.icon;
  return (
    <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-bold ${cfg.bg} ${cfg.text}`}>
      <span className={`w-1.5 h-1.5 rounded-full ${cfg.dot}`} />
      {cfg.label}
    </span>
  );
}

function PriorityBadge({ priority }) {
  const cfg = PRIORITY_CONFIG[priority];
  if (!cfg) return <span className="text-xs text-text-muted">-</span>;
  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded-md text-[11px] font-black border ${cfg.bg} ${cfg.color} ${cfg.border} uppercase tracking-wider`}>
      {cfg.label}
    </span>
  );
}

export default function MyTasks() {
  const { user } = useAuth();
  const [tasks, setTasks]             = useState([]);
  const [loading, setLoading]         = useState(true);
  const [selectedTask, setSelectedTask] = useState(null);
  const [editingTask, setEditingTask] = useState(null);
  const [isCreatingTask, setIsCreatingTask] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [filterStatus, setFilterStatus] = useState('All');
  const [filterPriority, setFilterPriority] = useState('All');
  const [groupBy, setGroupBy] = useState('status'); // 'status' | 'priority' | 'none'
  const [showFilters, setShowFilters] = useState(false);

  useEffect(() => { fetchMyTasks(); }, []);

  const fetchMyTasks = async () => {
    try {
      setLoading(true);
      const res = await taskApi.getMyTasks();
      setTasks(res.data.data || []);
    } catch (err) {
      console.error('Failed to fetch tasks', err);
      toast.error('Failed to load tasks');
    } finally {
      setLoading(false);
    }
  };

  const toggleTaskStatus = async (task) => {
    const cycle = { Todo: 'InProgress', InProgress: 'Done', Done: 'Todo', Blocked: 'Todo' };
    const newStatus = cycle[task.status] || 'Done';
    try {
      await taskApi.updateTask(task.id, { ...task, status: newStatus });
      setTasks(prev => prev.map(t => t.id === task.id ? { ...t, status: newStatus } : t));
      toast.success(`Status → ${newStatus}`);
    } catch (err) {
      toast.error('Failed to update task');
    }
  };

  const handleEditTask = async (formData) => {
    if (!editingTask) return;
    try {
      await taskApi.updateTask(editingTask.id, formData);
      toast.success('Task updated!');
      setEditingTask(null);
      fetchMyTasks();
    } catch { toast.error('Failed to update task'); }
  };

  const handleCreateTask = async (formData) => {
    try {
      await taskApi.createPersonalTask(formData);
      toast.success('Task created!');
      setIsCreatingTask(false);
      fetchMyTasks();
    } catch { toast.error('Failed to create task'); }
  };

  const handleDeleteTask = async (taskId) => {
    try {
      await taskApi.deleteTask(taskId);
      toast.success('Task deleted!');
      setTasks(t => t.filter(task => task.id !== taskId));
      setSelectedTask(null);
    } catch { toast.error('Failed to delete task'); }
  };

  // ── Filtering ──
  const filtered = useMemo(() => tasks.filter(t => {
    const q = searchQuery.toLowerCase();
    const matchesSearch = !q || t.title?.toLowerCase().includes(q) || t.description?.toLowerCase().includes(q);
    const matchesStatus = filterStatus === 'All' || t.status === filterStatus;
    const matchesPriority = filterPriority === 'All' || t.priority === filterPriority;
    return matchesSearch && matchesStatus && matchesPriority;
  }), [tasks, searchQuery, filterStatus, filterPriority]);

  // ── Stats ──
  const stats = useMemo(() => ({
    total: tasks.length,
    done: tasks.filter(t => t.status === 'Done').length,
    inProgress: tasks.filter(t => t.status === 'InProgress').length,
    blocked: tasks.filter(t => t.status === 'Blocked').length,
  }), [tasks]);

  // ── Grouped tasks ──
  const grouped = useMemo(() => {
    if (groupBy === 'none') return { 'All Tasks': filtered };
    const key = groupBy === 'status' ? 'status' : 'priority';
    return filtered.reduce((acc, task) => {
      const group = task[key] || 'None';
      if (!acc[group]) acc[group] = [];
      acc[group].push(task);
      return acc;
    }, {});
  }, [filtered, groupBy]);

  const isOverdue = (dueDate) => dueDate && new Date(dueDate) < new Date() && true;

  if (loading) {
    return (
      <div className="space-y-4">
        <div className="h-28 rounded-2xl bg-surface-2 animate-pulse" />
        {[1,2,3,4].map(i => <div key={i} className="h-16 rounded-xl bg-surface-2 animate-pulse" />)}
      </div>
    );
  }

  return (
    <div className="space-y-6 pb-10">
      {/* ── Header ── */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-black text-text-main tracking-tight flex items-center gap-3">
            <div className="w-10 h-10 bg-primary/10 rounded-xl flex items-center justify-center">
              <Inbox size={20} className="text-primary" />
            </div>
            My Tasks
          </h1>
          <p className="text-text-muted mt-1 font-medium">
            Keep track of everything you're working on across all projects.
          </p>
        </div>
        <Button variant="primary" leftIcon={<Plus size={16} />} onClick={() => setIsCreatingTask(true)}>
          Create Task
        </Button>
      </div>

      {/* ── Stats Cards ── */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { label: 'Total', value: stats.total, color: 'text-text-muted', bg: 'bg-surface-2', icon: BarChart2 },
          { label: 'In Progress', value: stats.inProgress, color: 'text-violet-600', bg: 'bg-violet-50 dark:bg-violet-900/20', icon: Timer },
          { label: 'Completed', value: stats.done, color: 'text-emerald-600', bg: 'bg-emerald-50 dark:bg-emerald-900/20', icon: CheckCircle2 },
          { label: 'Blocked', value: stats.blocked, color: 'text-red-600', bg: 'bg-red-50 dark:bg-red-900/20', icon: AlertCircle },
        ].map(({ label, value, color, bg, icon: Icon }) => (
          <div key={label} className={`${bg} rounded-2xl p-4 border border-border-subtle`}>
            <div className="flex items-center justify-between mb-2">
              <span className="text-xs font-bold text-text-muted uppercase tracking-wider">{label}</span>
              <Icon size={16} className={color} />
            </div>
            <p className={`text-3xl font-black ${color}`}>{value}</p>
          </div>
        ))}
      </div>

      {/* ── Toolbar ── */}
      <div className="flex flex-col gap-3">
        <div className="flex items-center gap-3 flex-wrap">
          {/* Search */}
          <div className="relative flex-1 min-w-48">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-text-muted" size={15} />
            <input
              type="text"
              placeholder="Search tasks..."
              value={searchQuery}
              onChange={e => setSearchQuery(e.target.value)}
              className="w-full pl-9 pr-10 py-2.5 bg-surface-0 border border-border-subtle rounded-xl text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none transition-all"
            />
            {searchQuery && (
              <button onClick={() => setSearchQuery('')} className="absolute right-3 top-1/2 -translate-y-1/2 text-text-subtle hover:text-text-muted">
                <X size={14} />
              </button>
            )}
          </div>

          {/* Filter toggle */}
          <button
            onClick={() => setShowFilters(f => !f)}
            className={`flex items-center gap-2 px-4 py-2.5 border rounded-xl font-bold text-sm transition-all ${
              showFilters || filterStatus !== 'All' || filterPriority !== 'All'
                ? 'border-primary bg-primary/5 text-primary'
                : 'border-border-subtle text-text-muted hover:border-primary/50 hover:text-primary'
            }`}
          >
            <Filter size={15} />
            Filter
            {(filterStatus !== 'All' || filterPriority !== 'All') && (
              <span className="w-2 h-2 bg-primary rounded-full" />
            )}
          </button>

          {/* Group by */}
          <div className="relative">
            <select
              value={groupBy}
              onChange={e => setGroupBy(e.target.value)}
              className="appearance-none pl-4 pr-8 py-2.5 bg-surface-0 border border-border-subtle rounded-xl text-sm font-bold text-text-muted focus:ring-2 focus:ring-primary/20 outline-none cursor-pointer hover:border-primary/50 transition-all"
            >
              <option value="status">Group: Status</option>
              <option value="priority">Group: Priority</option>
              <option value="none">No Grouping</option>
            </select>
            <ChevronDown size={14} className="absolute right-3 top-1/2 -translate-y-1/2 text-text-muted pointer-events-none" />
          </div>
        </div>

        {/* Expanded filters */}
        {showFilters && (
          <div className="flex items-center gap-3 flex-wrap p-3 bg-surface-0 border border-border-subtle rounded-xl">
            <span className="text-xs font-bold text-text-muted uppercase tracking-wider">Status:</span>
            {['All', 'Todo', 'InProgress', 'Done', 'Blocked'].map(s => (
              <button key={s}
                onClick={() => setFilterStatus(s)}
                className={`px-3 py-1.5 rounded-lg text-xs font-bold border transition-all ${
                  filterStatus === s
                    ? 'bg-primary text-white border-primary'
                    : 'border-border-subtle text-text-muted hover:border-primary/40 hover:text-primary'
                }`}
              >
                {s === 'All' ? 'All' : STATUS_CONFIG[s]?.label || s}
              </button>
            ))}
            <div className="w-px h-4 bg-border-subtle mx-1" />
            <span className="text-xs font-bold text-text-muted uppercase tracking-wider">Priority:</span>
            {['All', 'High', 'Medium', 'Low'].map(p => (
              <button key={p}
                onClick={() => setFilterPriority(p)}
                className={`px-3 py-1.5 rounded-lg text-xs font-bold border transition-all ${
                  filterPriority === p
                    ? 'bg-primary text-white border-primary'
                    : 'border-border-subtle text-text-muted hover:border-primary/40 hover:text-primary'
                }`}
              >
                {p}
              </button>
            ))}
          </div>
        )}
      </div>

      {/* ── Task List ── */}
      {filtered.length === 0 ? (
        <div className="bg-surface-0 rounded-2xl border border-border-subtle text-center py-20">
          <div className="w-20 h-20 bg-primary/10 rounded-full flex items-center justify-center mx-auto mb-6">
            <CheckCircle2 size={36} className="text-primary" />
          </div>
          <h3 className="text-xl font-bold text-text-main">
            {searchQuery || filterStatus !== 'All' || filterPriority !== 'All'
              ? 'No tasks match your filters'
              : 'No tasks assigned to you'
            }
          </h3>
          <p className="text-text-muted mt-2 mb-8 max-w-sm mx-auto text-sm">
            {tasks.length === 0 ? 'When you are assigned to a task or create one for yourself, it will show up here.' : 'Try adjusting your filters.'}
          </p>
          {tasks.length === 0 && (
            <Button onClick={() => setIsCreatingTask(true)} leftIcon={<Plus size={16} />}>
              Create your first task
            </Button>
          )}
        </div>
      ) : (
        <div className="space-y-6">
          {Object.entries(grouped).map(([group, groupTasks]) => {
            const statusCfg = STATUS_CONFIG[group];
            const priorityCfg = PRIORITY_CONFIG[group];
            return (
              <div key={group}>
                {/* Group Header */}
                {groupBy !== 'none' && (
                  <div className="flex items-center gap-3 mb-3">
                    {statusCfg && <span className={`w-2.5 h-2.5 rounded-full ${statusCfg.dot}`} />}
                    <h3 className="text-sm font-black text-text-main uppercase tracking-wide">
                      {statusCfg?.label || priorityCfg?.label || group}
                    </h3>
                    <span className="text-xs font-bold px-2 py-0.5 rounded-full bg-surface-2 text-text-muted">
                      {groupTasks.length}
                    </span>
                    <div className="flex-1 h-px bg-border-subtle" />
                  </div>
                )}

                {/* Task rows */}
                <div className="bg-surface-0 rounded-2xl border border-border-subtle overflow-hidden shadow-sm">
                  {groupTasks.map((task, i) => {
                    const isDone = task.status === 'Done';
                    const overdue = isOverdue(task.dueDate) && !isDone;
                    return (
                      <div
                        key={task.id}
                        className={`group flex items-center gap-4 px-5 py-4 hover:bg-hover-bg transition-colors cursor-pointer ${
                          i !== 0 ? 'border-t border-border-subtle' : ''
                        } ${isDone ? 'opacity-60' : ''}`}
                        onClick={() => setSelectedTask(task)}
                      >
                        {/* Status toggle */}
                        <button
                          onClick={e => { e.stopPropagation(); toggleTaskStatus(task); }}
                          className="shrink-0 transition-transform active:scale-90 hover:scale-110"
                          title={`Click to cycle status (current: ${task.status})`}
                        >
                          {isDone ? (
                            <CheckCircle2 className="w-6 h-6 text-emerald-500" />
                          ) : task.status === 'InProgress' ? (
                            <Timer className="w-6 h-6 text-violet-500" />
                          ) : task.status === 'Blocked' ? (
                            <AlertCircle className="w-6 h-6 text-red-500" />
                          ) : (
                            <Circle className="w-6 h-6 text-text-subtle group-hover:text-primary transition-colors" />
                          )}
                        </button>

                        {/* Title + meta */}
                        <div className="flex-1 min-w-0">
                          <div className={`font-bold text-sm ${isDone ? 'line-through text-text-subtle' : 'text-text-main'}`}>
                            {task.title}
                          </div>
                          {task.description && (
                            <p className="text-xs text-text-muted mt-0.5 truncate max-w-xs">{task.description}</p>
                          )}
                        </div>

                        {/* Status Badge */}
                        <div className="shrink-0 hidden sm:block">
                          <StatusBadge status={task.status || 'Todo'} />
                        </div>

                        {/* Priority Badge */}
                        <div className="shrink-0 hidden md:block">
                          <PriorityBadge priority={task.priority} />
                        </div>

                        {/* Project link */}
                        <div className="shrink-0 hidden lg:block w-32">
                          {task.boardId ? (
                            <Link
                              to={`/boards/${task.boardId}`}
                              onClick={e => e.stopPropagation()}
                              className="flex items-center gap-1.5 text-xs font-bold text-primary hover:underline truncate"
                            >
                              <Layout size={11} />
                              <span className="truncate">{task.boardTitle || 'Board'}</span>
                            </Link>
                          ) : (
                            <span className="text-xs text-text-muted">Personal</span>
                          )}
                        </div>

                        {/* Due date */}
                        <div className="shrink-0 hidden md:block w-28">
                          {task.dueDate ? (
                            <div className={`flex items-center gap-1.5 text-xs font-medium ${overdue ? 'text-red-500' : 'text-text-muted'}`}>
                              <Clock size={12} />
                              {overdue && <span className="font-bold">!</span>}
                              {new Date(task.dueDate).toLocaleDateString('en-US', { month:'short', day:'numeric' })}
                            </div>
                          ) : (
                            <span className="text-xs text-text-subtle">No due date</span>
                          )}
                        </div>

                        {/* Actions */}
                        <div className="shrink-0 flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                          <button
                            onClick={e => { e.stopPropagation(); setSelectedTask(task); }}
                            className="p-1.5 rounded-lg text-text-muted hover:text-primary hover:bg-primary/10 transition-colors"
                            title="View task"
                          >
                            <ArrowUpRight size={15} />
                          </button>
                          <button
                            onClick={e => { e.stopPropagation(); setEditingTask(task); }}
                            className="p-1.5 rounded-lg text-text-muted hover:text-text-main hover:bg-hover-bg transition-colors"
                            title="Edit task"
                          >
                            <MoreHorizontal size={15} />
                          </button>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Modals */}
      <TaskModal
        isOpen={!!selectedTask}
        task={selectedTask}
        onClose={() => setSelectedTask(null)}
        onEdit={task => { setEditingTask(task); setSelectedTask(null); }}
        onDelete={handleDeleteTask}
      />
      <TaskFormModal
        isOpen={!!editingTask}
        onClose={() => setEditingTask(null)}
        onSubmit={handleEditTask}
        task={editingTask}
      />
      <TaskFormModal
        isOpen={isCreatingTask}
        onClose={() => setIsCreatingTask(false)}
        onSubmit={handleCreateTask}
      />
    </div>
  );
}
