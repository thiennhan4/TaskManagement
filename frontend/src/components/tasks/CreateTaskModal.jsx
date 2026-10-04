import React, { useCallback, useState, useEffect, useRef } from 'react';
import { X, Calendar, Flag, User, Briefcase, Loader2 } from 'lucide-react';
import { useTasks } from '@/context/taskState';
import { taskApi } from '@/api/taskApi';
import { toast } from 'react-hot-toast';
import { boardApi } from '@/api/boardApi';
import { listApi } from '@/api/listApi';

export const CreateTaskModal = () => {
  const { showCreateModal, setShowCreateModal, refreshTasks } = useTasks();
  const [loading, setLoading] = useState(false);
  const [boards, setBoards] = useState([]);
  
  const [formData, setFormData] = useState({
    title: '',
    description: '',
    priority: 'Medium',
    status: 'Todo',
    dueDate: '',
    boardId: '',
    listId: '',
    assignedToId: ''
  });

  const [lists, setLists] = useState([]);
  const boardRequest = useRef(0);

  const fetchBoards = useCallback(() => {
    return boardApi.getBoards().then(response => {
      if (response.data.success) {
        setBoards(response.data.data);
      }
    }).catch(() => {
      console.error('Failed to fetch boards');
    });
  }, []);

  useEffect(() => {
    if (showCreateModal) {
      fetchBoards();
    }
    return () => { boardRequest.current++; };
  }, [showCreateModal, fetchBoards]);



  const handleBoardChange = async (boardId) => {
    const request = ++boardRequest.current;
    setFormData({ ...formData, boardId, listId: '' });
    setLists([]);
    try {
      const response = await listApi.getListChoices(boardId);
      if (request !== boardRequest.current) return;
      if (response.data.success) {
        setLists(response.data.data);
      }
    } catch {
      console.error('Failed to fetch lists');
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.title || !formData.listId) {
      toast.error('Title and Project List are required');
      return;
    }

    setLoading(true);
    try {
      const { data: result } = await taskApi.createTask(formData.listId, {
        title: formData.title,
        description: formData.description,
        priority: formData.priority,
        dueDate: formData.dueDate || null,
        assignedToId: formData.assignedToId || null
      });

      if (result.success) {
        toast.success('Task created successfully');
        setShowCreateModal(false);
        setFormData({
          title: '',
          description: '',
          priority: 'Medium',
          status: 'Todo',
          dueDate: '',
          boardId: '',
          listId: '',
          assignedToId: ''
        });
        refreshTasks();
      } else {
        toast.error(result.message || 'Failed to create task');
      }
    } catch (err) {
      toast.error(err.response?.data?.message || err.message || 'Failed to create task');
    } finally {
      setLoading(false);
    }
  };

  if (!showCreateModal) return null;

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center p-4 sm:p-6">
      <div className="absolute inset-0 bg-slate-900/40 backdrop-blur-sm transition-opacity" onClick={() => setShowCreateModal(false)} />
      
      <div className="relative bg-surface-0 rounded-2xl shadow-2xl w-full max-w-xl overflow-hidden animate-in fade-in zoom-in duration-200">
        <div className="flex items-center justify-between p-6 border-b border-border-subtle">
          <h2 className="text-xl font-bold text-text-main">Create New Task</h2>
          <button onClick={() => setShowCreateModal(false)} className="p-2 hover:bg-hover-bg rounded-xl transition-colors">
            <X className="w-5 h-5 text-text-muted" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-6 space-y-5">
          {/* Title */}
          <div>
            <label className="block text-sm font-bold text-text-main mb-1.5">Title <span className="text-red-500">*</span></label>
            <input
              autoFocus
              type="text"
              placeholder="e.g., Design login page"
              className="w-full px-4 py-2.5 bg-surface-1 border border-border-subtle text-text-main placeholder:text-text-subtle rounded-xl focus:ring-2 focus:ring-primary/30 focus:border-primary outline-none transition-all"
              value={formData.title}
              onChange={(e) => setFormData({ ...formData, title: e.target.value })}
              maxLength={200}
            />
            <div className="mt-1 flex justify-end">
              <span className="text-[10px] text-text-subtle font-medium">{formData.title.length}/200</span>
            </div>
          </div>

          {/* Description */}
          <div>
            <label className="block text-sm font-bold text-text-main mb-1.5">Description</label>
            <textarea
              placeholder="Add more details..."
              rows={3}
              className="w-full px-4 py-2.5 bg-surface-1 border border-border-subtle text-text-main placeholder:text-text-subtle rounded-xl focus:ring-2 focus:ring-primary/30 focus:border-primary outline-none transition-all resize-none"
              value={formData.description}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            {/* Priority */}
            <div>
              <label className="block text-sm font-bold text-text-main mb-1.5">Priority</label>
              <div className="relative">
                <Flag className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-text-subtle" />
                <select
                  className="w-full pl-10 pr-4 py-2.5 bg-surface-1 border border-border-subtle text-text-main rounded-xl focus:ring-2 focus:ring-primary/30 focus:border-primary outline-none transition-all appearance-none"
                  value={formData.priority}
                  onChange={(e) => setFormData({ ...formData, priority: e.target.value })}
                >
                  <option value="Low">Low</option>
                  <option value="Medium">Medium</option>
                  <option value="High">High</option>
                  <option value="Critical">Critical</option>
                </select>
              </div>
            </div>

            {/* Due Date */}
            <div>
              <label className="block text-sm font-bold text-text-main mb-1.5">Due Date</label>
              <div className="relative">
                <Calendar className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-text-subtle" />
                <input
                  type="date"
                  min={new Date().toISOString().split('T')[0]}
                  className="w-full pl-10 pr-4 py-2.5 bg-surface-1 border border-border-subtle text-text-main rounded-xl focus:ring-2 focus:ring-primary/30 focus:border-primary outline-none transition-all"
                  value={formData.dueDate}
                  onChange={(e) => setFormData({ ...formData, dueDate: e.target.value })}
                />
              </div>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            {/* Board Selection */}
            <div>
              <label className="block text-sm font-bold text-text-main mb-1.5">Project <span className="text-red-500">*</span></label>
              <div className="relative">
                <Briefcase className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-text-subtle" />
                <select
                  className="w-full pl-10 pr-4 py-2.5 bg-surface-1 border border-border-subtle text-text-main rounded-xl focus:ring-2 focus:ring-primary/30 focus:border-primary outline-none transition-all appearance-none"
                  value={formData.boardId}
                  onChange={(e) => handleBoardChange(e.target.value)}
                >
                  <option value="">Select Board</option>
                  {boards.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
                </select>
              </div>
            </div>

            {/* List Selection */}
            <div>
              <label className="block text-sm font-bold text-text-main mb-1.5">List <span className="text-red-500">*</span></label>
              <select
                disabled={!formData.boardId}
                className="w-full px-4 py-2.5 bg-surface-1 border border-border-subtle text-text-main rounded-xl focus:ring-2 focus:ring-primary/30 focus:border-primary outline-none transition-all appearance-none disabled:opacity-50"
                value={formData.listId}
                onChange={(e) => setFormData({ ...formData, listId: e.target.value })}
              >
                <option value="">Select List</option>
                {lists.map(l => <option key={l.id} value={l.id}>{l.name}</option>)}
              </select>
            </div>
          </div>

          <div className="flex gap-3 mt-8">
            <button
              type="button"
              onClick={() => setShowCreateModal(false)}
              className="flex-1 px-4 py-3 border border-border-subtle text-text-muted font-bold rounded-xl hover:bg-hover-bg transition-all"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={loading}
              className="flex-[2] px-4 py-3 bg-primary text-white font-bold rounded-xl hover:bg-primary-dark shadow-lg shadow-primary/20 transition-all disabled:opacity-70 flex items-center justify-center gap-2"
            >
              {loading ? (
                <>
                  <Loader2 className="w-5 h-5 animate-spin" />
                  <span>Creating...</span>
                </>
              ) : (
                <span>Create Task</span>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
