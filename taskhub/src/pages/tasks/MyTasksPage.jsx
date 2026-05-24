import { useState, useEffect } from 'react';
import { useAuth } from '@/context/AuthContext';
import { taskApi } from '@/api/taskApi';
import { CheckCircle2, Circle, Clock, Layout, Search, Filter, MoreVertical, ExternalLink, Plus } from 'lucide-react';
import { Link } from 'react-router-dom';
import toast from 'react-hot-toast';
import TaskModal from '@/components/tasks/TaskModal';
import TaskFormModal from '@/components/tasks/TaskFormModal';
import Button from '@/components/ui/Button';
import Card from '@/components/ui/Card';
import Badge from '@/components/ui/Badge';

export default function MyTasks() {
  const { user } = useAuth();
  const [tasks, setTasks] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selectedTask, setSelectedTask] = useState(null);
  const [editingTask, setEditingTask] = useState(null);
  const [isCreatingTask, setIsCreatingTask] = useState(false);

  useEffect(() => {
    fetchMyTasks();
  }, []);

  const fetchMyTasks = async () => {
    try {
      setLoading(true);
      const res = await taskApi.getMyTasks();
      setTasks(res.data.data);
    } catch (err) {
      console.error('Failed to fetch tasks', err);
    } finally {
      setLoading(false);
    }
  };

  const toggleTaskStatus = async (task) => {
    const newStatus = task.status === 'Done' ? 'Todo' : 'Done';
    try {
      await taskApi.updateTask(task.id, { ...task, status: newStatus });
      setTasks(tasks.map(t => t.id === task.id ? { ...t, status: newStatus } : t));
      toast.success(`Task marked as ${newStatus}!`);
    } catch (err) {
      toast.error(err?.response?.data?.message || 'Failed to update task');
    }
  };

  const handleEditTask = async (formData) => {
    if (!editingTask) return;
    try {
      await taskApi.updateTask(editingTask.id, formData);
      toast.success('Task updated!');
      setEditingTask(null);
      fetchMyTasks();
    } catch (err) {
      toast.error('Failed to update task');
    }
  };

  const handleCreateTask = async (formData) => {
    try {
      await taskApi.createPersonalTask(formData);
      toast.success('Task created!');
      setIsCreatingTask(false);
      fetchMyTasks();
    } catch (err) {
      toast.error('Failed to create task');
    }
  };

  const handleDeleteTask = async (taskId) => {
    try {
      await taskApi.deleteTask(taskId);
      toast.success('Task deleted!');
      setTasks(t => t.filter(task => task.id !== taskId));
    } catch (err) {
      toast.error(err?.response?.data?.message || 'Failed to delete task');
    }
  };

  const PRIORITY_VARIANTS = {
    High: 'danger',
    Medium: 'warning',
    Low: 'success',
  };

  return (
    <div className="space-y-8">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-black text-text-main tracking-tight">
            My Tasks
          </h1>
          <p className="text-text-muted mt-1 font-medium">
            Keep track of everything you're working on across all projects.
          </p>
        </div>
        <Button 
          variant="primary" 
          leftIcon={<Plus size={16} />}
          onClick={() => setIsCreatingTask(true)}
        >
          Create Task
        </Button>
      </div>

      {/* Toolbar */}
      <div className="flex flex-col md:flex-row gap-4 items-center justify-between">
        <div className="flex items-center gap-2 w-full md:w-auto">
          <div className="relative flex-1 md:w-64">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-text-muted" size={16} />
            <input 
              type="text" 
              placeholder="Search tasks..." 
              className="w-full pl-10 pr-4 py-2 bg-white border border-border-subtle rounded-xl text-sm focus:ring-2 focus:ring-primary/20 transition-all"
            />
          </div>
          <Button variant="outline" size="md" leftIcon={<Filter size={16} />}>Filter</Button>
        </div>
        <div className="flex items-center gap-2 w-full md:w-auto">
          <Badge variant="neutral" className="px-3 py-1">Total: {tasks.length}</Badge>
          <Badge variant="success" className="px-3 py-1">Done: {tasks.filter(t => t.status === 'Done').length}</Badge>
        </div>
      </div>

      {loading ? (
        <div className="space-y-4">
          {[1, 2, 3, 4].map(i => (
            <div key={i} className="h-20 rounded-2xl bg-slate-100 animate-pulse"></div>
          ))}
        </div>
      ) : tasks.length === 0 ? (
        <Card className="text-center py-20">
          <div className="w-20 h-20 bg-primary/10 rounded-full flex items-center justify-center mx-auto mb-6">
            <CheckCircle2 size={40} className="text-primary" />
          </div>
          <h3 className="text-xl font-bold text-text-main">No tasks assigned to you</h3>
          <p className="text-text-muted mb-8 max-w-md mx-auto">
            When you're assigned to a task or create one for yourself, it will show up here.
          </p>
          <Button onClick={() => setIsCreatingTask(true)}>Create Task</Button>
        </Card>
      ) : (
        <div className="bg-white rounded-2xl border border-border-subtle shadow-premium overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left">
              <thead>
                <tr className="bg-slate-50 border-b border-border-subtle">
                  <th className="px-6 py-4 text-xs font-bold text-text-muted uppercase tracking-wider w-12"></th>
                  <th className="px-6 py-4 text-xs font-bold text-text-muted uppercase tracking-wider">Task Details</th>
                  <th className="px-6 py-4 text-xs font-bold text-text-muted uppercase tracking-wider">Project</th>
                  <th className="px-6 py-4 text-xs font-bold text-text-muted uppercase tracking-wider">Priority</th>
                  <th className="px-6 py-4 text-xs font-bold text-text-muted uppercase tracking-wider">Due Date</th>
                  <th className="px-6 py-4 text-xs font-bold text-text-muted uppercase tracking-wider w-12"></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border-subtle">
                {tasks.map((task) => (
                  <tr key={task.id} className="group hover:bg-slate-50/50 transition-colors">
                    <td className="px-6 py-4">
                      <button
                        onClick={() => toggleTaskStatus(task)}
                        className="transition-transform active:scale-90"
                      >
                        {task.status === 'Done' ? (
                          <CheckCircle2 className="w-6 h-6 text-emerald-500" />
                        ) : (
                          <Circle className="w-6 h-6 text-slate-300 group-hover:text-primary transition-colors" />
                        )}
                      </button>
                    </td>
                    <td className="px-6 py-4">
                      <div 
                        className={`font-bold text-sm cursor-pointer hover:text-primary transition-colors ${task.status === 'Done' ? 'text-slate-400 line-through' : 'text-text-main'}`}
                        onClick={() => setSelectedTask(task)}
                      >
                        {task.title}
                      </div>
                      {task.label && (
                        <span className="inline-block mt-1 text-[10px] font-bold text-slate-400 uppercase tracking-widest">
                          {task.label}
                        </span>
                      )}
                    </td>
                    <td className="px-6 py-4">
                      {task.boardId ? (
                        <Link 
                          to={`/boards/${task.boardId}`} 
                          className="flex items-center gap-2 text-xs font-bold text-primary hover:underline"
                        >
                          <Layout size={12} />
                          {task.boardTitle}
                        </Link>
                      ) : (
                        <span className="text-xs font-medium text-slate-400">Personal</span>
                      )}
                    </td>
                    <td className="px-6 py-4">
                      <Badge variant={PRIORITY_VARIANTS[task.priority] || 'neutral'}>
                        {task.priority || 'Normal'}
                      </Badge>
                    </td>
                    <td className="px-6 py-4">
                      {task.dueDate ? (
                        <div className="flex items-center gap-2 text-xs font-medium text-text-muted">
                          <Clock size={14} />
                          {new Date(task.dueDate).toLocaleDateString()}
                        </div>
                      ) : (
                        <span className="text-xs text-slate-300">-</span>
                      )}
                    </td>
                    <td className="px-6 py-4 text-right">
                      <div className="flex items-center justify-end gap-2 opacity-0 group-hover:opacity-100 transition-opacity">
                        <Button variant="ghost" size="icon" onClick={() => setSelectedTask(task)}>
                          <ExternalLink size={16} />
                        </Button>
                        <Button variant="ghost" size="icon">
                          <MoreVertical size={16} />
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Task Detail Modal */}
      <TaskModal
        isOpen={!!selectedTask}
        task={selectedTask}
        onClose={() => setSelectedTask(null)}
        onEdit={(task) => { setEditingTask(task); setSelectedTask(null); }}
        onDelete={handleDeleteTask}
      />

      {/* Edit Task Modal */}
      <TaskFormModal
        isOpen={!!editingTask}
        onClose={() => setEditingTask(null)}
        onSubmit={handleEditTask}
        task={editingTask}
      />

      {/* Create Task Modal */}
      <TaskFormModal
        isOpen={isCreatingTask}
        onClose={() => setIsCreatingTask(false)}
        onSubmit={handleCreateTask}
      />
    </div>
  );
}
