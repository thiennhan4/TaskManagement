import { useState, useEffect, useRef } from 'react';
import { DragDropContext, Droppable, Draggable } from '@hello-pangea/dnd';
import { listApi } from '@/api/listApi';
import { taskApi } from '@/api/taskApi';
import TaskCard from '@/components/tasks/TaskCard';
import TaskModal from '@/components/tasks/TaskModal';
import TaskFormModal from '@/components/tasks/TaskFormModal';
import Button from '@/components/ui/Button';
import {
  Plus, Filter, Search, MoreHorizontal, Pencil, Trash2, X, Loader2, Columns3, Copy,
} from 'lucide-react';
import toast from 'react-hot-toast';
import { useBoardData } from '@/hooks/useBoardData';
import { upsertColumns, upsertTask, removeTask } from '@/utils/boardState';

const COLUMN_COLORS = [
  '#6366f1','#8b5cf6','#ec4899','#ef4444',
  '#f97316','#eab308','#22c55e','#06b6d4','#3b82f6','#64748b',
];

function ProjectTasksBoardContent({ projectId, requestedBoardId, canEditTasks: allowTaskEditing = true, canManageBoard: allowColumnEditing = true }) {

  const [selectedTask, setSelectedTask] = useState(null);
  const [editingTask, setEditingTask] = useState(null);
  const [showAddColumn, setShowAddColumn] = useState(false);
  const [colForm, setColForm] = useState({ name: '', color: '#6366f1', quantity: 1 });
  const [colLoading, setColLoading] = useState(false);
  const [editingCol, setEditingCol] = useState(null);
  const [editColForm, setEditColForm] = useState({ name: '', color: '' });
  const [editColLoading, setEditColLoading] = useState(false);
  const [deletingCol, setDeletingCol] = useState(null);
  const [deleteColLoading, setDeleteColLoading] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const { board, lists, setLists, columnPage, loading, loadingMore, loadError, partial, fetchBoardData, loadMore, capabilities } = useBoardData({ projectId, boardId: requestedBoardId, searchQuery });
  const canEditTasks = allowTaskEditing && capabilities.canCreateTasks === true;
  const canManageBoard = allowColumnEditing && capabilities.canManageColumns === true;

  const handleAddColumn = async (e) => {
    e.preventDefault();
    if (!colForm.name.trim()) { toast.error('Column name required'); return; }
    const qty = Math.max(1, Math.min(10, colForm.quantity || 1));
    setColLoading(true);
    try {
      const created = [];
      for (let i = 0; i < qty; i++) {
        const name = qty === 1 ? colForm.name.trim() : `${colForm.name.trim()} ${i + 1}`;
        const res = await listApi.createList({ name, boardId: board.id, position: lists.length + i, color: colForm.color });
        created.push({ ...res.data.data, tasks: [] });
      }
      setLists(prev => upsertColumns(prev, created));
      toast.success(qty > 1 ? `${qty} columns added` : 'Column added');
      setShowAddColumn(false);
      setColForm({ name: '', color: '#6366f1', quantity: 1 });
    } catch {
      toast.error('Failed to add column');
    } finally {
      setColLoading(false);
    }
  };

  const handleDuplicateColumn = async (list) => {
    try {
      const res = await listApi.createList({ name: `${list.name} (Copy)`, boardId: board.id, position: lists.findIndex(l => l.id === list.id) + 1, color: list.color || '#6366f1' });
      const newCol = { ...res.data.data, tasks: [] };
      setLists(prev => upsertColumns(prev, [newCol]));
      toast.success(`Duplicated "${list.name}"`);
    } catch { toast.error('Failed to duplicate'); }
  };

  const openEditCol = (list) => { setEditingCol(list); setEditColForm({ name: list.name, color: list.color || '#6366f1' }); };

  const handleEditColumn = async (e) => {
    e.preventDefault();
    if (!editColForm.name.trim()) return;
    setEditColLoading(true);
    try {
      await listApi.updateList(editingCol.id, { name: editColForm.name.trim(), position: editingCol.position, color: editColForm.color });
      setLists(prev => prev.map(l => l.id === editingCol.id ? { ...l, name: editColForm.name.trim(), color: editColForm.color } : l));
      toast.success('Column updated');
      setEditingCol(null);
    } catch { toast.error('Failed to update column'); }
    finally { setEditColLoading(false); }
  };

  const handleDeleteColumn = async () => {
    setDeleteColLoading(true);
    try {
      await listApi.deleteList(deletingCol.id);
      setLists(prev => prev.filter(l => l.id !== deletingCol.id));
      toast.success('Column deleted');
      setDeletingCol(null);
    } catch { toast.error('Failed to delete'); }
    finally { setDeleteColLoading(false); }
  };

  const handleCreateTask = async (listId, formData) => {
    try {
      const res = await taskApi.createTask(listId, formData);
      setLists(prev => upsertTask(prev, res.data.data));
      toast.success('Task created');
    } catch (err) {
      toast.error('Failed to create task');
      throw err;
    }
  };

  const handleEditTask = async (formData) => {
    if (!editingTask) return;
    await taskApi.updateTask(editingTask.id, formData);
    toast.success('Task updated');
    setEditingTask(null);
    fetchBoardData();
  };

  const handleDeleteTask = async (taskId) => {
    try {
      await taskApi.deleteTask(taskId);
      toast.success('Task deleted');
      setSelectedTask(null);
      setLists(prev => removeTask(prev, taskId));
    } catch { toast.error('Failed to delete task'); }
  };

  const handleToggleStatus = async (task) => {
    const newStatus = task.status === 'Done' ? 'Todo' : 'Done';
    try {
      await taskApi.changeTaskStatus(task.id, newStatus);
      setLists(prev => prev.map(l => ({ ...l, tasks: (l.tasks||[]).map(t => t.id === task.id ? { ...t, status: newStatus } : t) })));
    } catch { toast.error('Failed to update status'); }
  };

  const onDragEnd = async ({ destination, source, draggableId }) => {
    if (loading || !canEditTasks || partial || searchQuery) return;
    if (!destination) return;
    if (destination.droppableId === source.droppableId && destination.index === source.index) return;
    const srcIdx = lists.findIndex(l => String(l.id) === source.droppableId);
    const dstIdx = lists.findIndex(l => String(l.id) === destination.droppableId);
    if (srcIdx === -1 || dstIdx === -1) return;
    const newLists = lists.map(l => ({ ...l, tasks: [...(l.tasks||[])] }));
    const [movedTask] = newLists[srcIdx].tasks.splice(source.index, 1);
    newLists[dstIdx].tasks.splice(destination.index, 0, movedTask);
    setLists(newLists);
    try {
      await taskApi.moveTask(draggableId, { listId: destination.droppableId, position: destination.index, expectedUpdatedAt: movedTask.updatedAt ?? null });
      await fetchBoardData();
    } catch (error) {
      setLists(lists);
      toast.error(error.response?.data?.message || 'Failed to move task');
      await fetchBoardData();
    }
  };

  if (loading && !board) {
    return (
      <div className="flex items-center justify-center h-full">
        <Loader2 className="animate-spin text-primary" size={32} />
      </div>
    );
  }

  if (loadError) return <div role="alert" className="p-6 text-text-main"><p>{loadError}</p><Button onClick={fetchBoardData}>Retry</Button></div>;

  if (!board) {
    return (
      <div className="flex h-full items-center justify-center rounded-2xl border border-dashed border-border-subtle bg-surface-0 p-8 text-center">
        <div>
          <h3 className="text-base font-bold text-text-main">No board found</h3>
          <p className="mt-1 text-sm text-text-muted">This project does not have a task board yet.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="h-full flex flex-col">
      {/* Toolbar */}
      <div className="flex flex-wrap items-center justify-between gap-2 bg-surface-0 p-2 rounded-2xl border border-border-subtle shadow-sm mb-4 shrink-0">
        <div className="flex items-center gap-2">
          <Button variant="ghost" size="sm" leftIcon={<Filter size={14} />}>Filter</Button>
          <div className="h-4 w-px bg-border-subtle mx-1" />
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-text-muted" size={14} />
            <input
              type="text"
              placeholder="Search tasks..."
              value={searchQuery}
              onChange={e => setSearchQuery(e.target.value)}
              className="pl-9 pr-4 py-1.5 bg-transparent border-none text-sm focus:outline-none focus:ring-0 w-48 text-text-main placeholder:text-text-muted"
            />
          </div>
        </div>
        <div className="flex items-center gap-2">
          <span className="text-xs font-medium text-text-muted">
            {lists.reduce((a, l) => a + (l.tasks?.length||0), 0)} loaded tasks
          </span>
          {canManageBoard && <Button variant="primary" size="sm" leftIcon={<Columns3 size={16} />} onClick={() => setShowAddColumn(true)}>
            Add Column
          </Button>}
        </div>
      </div>

      {loading && <p role="status" className="text-sm text-text-muted">Loading tasks…</p>}
      {partial && <p role="status" className="mb-2 text-sm text-text-muted">Showing part of this board. Load remaining columns/cards to see all matches. Reordering is disabled until all cards are loaded.</p>}
      {columnPage?.page < columnPage?.totalPages && <Button disabled={loading || loadingMore} onClick={() => loadMore()}>Load more columns</Button>}
      {/* Kanban */}
      <div className="flex-1 overflow-x-auto pb-6">
        <DragDropContext onDragEnd={onDragEnd}>
          <div className="flex gap-5 h-full items-start" style={{ minWidth: 'max-content' }}>
            {lists.map(list => (
              <KanbanColumn
                key={list.id}
                list={list}
                tasks={list.tasks}
                onCreateTask={handleCreateTask}
                onTaskClick={task => setSelectedTask(task.id)}
                onToggleStatus={handleToggleStatus}
                onEditColumn={openEditCol}
                onDeleteColumn={setDeletingCol}
                onDuplicateColumn={handleDuplicateColumn}
                canEditTasks={canEditTasks}
                canManageBoard={canManageBoard}
                canDrag={canEditTasks && !loading && !partial && !searchQuery}
                onLoadMore={() => loadMore(list)}
              />
            ))}
            {canManageBoard && <button
              onClick={() => setShowAddColumn(true)}
              className="shrink-0 w-72 h-36 flex flex-col items-center justify-center gap-2 rounded-2xl border-2 border-dashed border-border-subtle text-text-subtle hover:border-primary hover:text-primary hover:bg-primary/5 transition-all duration-200"
            >
              <div className="w-10 h-10 rounded-xl border-2 border-current flex items-center justify-center">
                <Plus size={20} />
              </div>
              <span className="font-bold text-xs uppercase tracking-wider">Add Column</span>
            </button>}
          </div>
        </DragDropContext>
      </div>

      {/* Add Column Modal */}
      {showAddColumn && (
        <ColumnFormModal
          title="Add Column"
          form={colForm}
          setForm={setColForm}
          onSubmit={handleAddColumn}
          onClose={() => setShowAddColumn(false)}
          loading={colLoading}
          submitLabel="Add Column"
        />
      )}

      {/* Edit Column Modal */}
      {editingCol && (
        <ColumnFormModal
          title="Rename Column"
          form={editColForm}
          setForm={setEditColForm}
          onSubmit={handleEditColumn}
          onClose={() => setEditingCol(null)}
          loading={editColLoading}
          submitLabel="Save"
        />
      )}

      {/* Delete Column Confirm */}
      {deletingCol && (
        <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
          <div className="absolute inset-0 bg-slate-900/50 backdrop-blur-sm" onClick={() => !deleteColLoading && setDeletingCol(null)} />
          <div className="relative bg-surface-0 rounded-2xl shadow-2xl w-full max-w-md p-6">
            <div className="flex items-center gap-4 mb-4">
              <div className="w-12 h-12 rounded-xl bg-rose-500/10 flex items-center justify-center">
                <Trash2 size={22} className="text-rose-500" />
              </div>
              <div>
                <h3 className="text-lg font-bold text-text-main">Delete Column</h3>
                <p className="text-sm text-text-muted mt-0.5">This will delete {deletingCol.tasks?.length || 0} tasks too.</p>
              </div>
            </div>
            <p className="text-sm text-text-muted bg-surface-2 rounded-xl p-4 mb-6">
              Are you sure you want to delete <strong>"{deletingCol.name}"</strong>?
            </p>
            <div className="flex gap-3">
              <Button variant="ghost" className="flex-1" onClick={() => setDeletingCol(null)} disabled={deleteColLoading}>Cancel</Button>
              <button
                onClick={handleDeleteColumn}
                disabled={deleteColLoading}
                className="flex-1 flex items-center justify-center gap-2 px-4 py-2.5 bg-red-500 hover:bg-red-600 text-white font-bold rounded-xl transition-colors disabled:opacity-70"
              >
                {deleteColLoading ? <Loader2 size={16} className="animate-spin" /> : <Trash2 size={16} />}
                {deleteColLoading ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        </div>
      )}

      <TaskModal isOpen={!!selectedTask} taskId={selectedTask} readOnly={!canEditTasks} canEditTask={canEditTasks} canDeleteTask={canManageBoard} onClose={() => setSelectedTask(null)} onEdit={task => { setEditingTask(task); setSelectedTask(null); }} onDelete={handleDeleteTask} />
      <TaskFormModal isOpen={!!editingTask} onClose={() => setEditingTask(null)} onSubmit={handleEditTask} task={editingTask} />
    </div>
  );
}

function KanbanColumn({ list, tasks, onCreateTask, onTaskClick, onToggleStatus, onEditColumn, onDeleteColumn, onDuplicateColumn, canEditTasks, canManageBoard, canDrag, onLoadMore }) {
  const [showTaskForm, setShowTaskForm] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef(null);

  useEffect(() => {
    const handler = (e) => { if (menuRef.current && !menuRef.current.contains(e.target)) setMenuOpen(false); };
    if (menuOpen) document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [menuOpen]);

  const handleCreateTask = async (formData) => { await onCreateTask(list.id, formData); setShowTaskForm(false); };
  const accentColor = list.color || '#6366f1';

  return (
    <div className="shrink-0 w-72 flex flex-col" style={{ maxHeight: 'calc(100vh - 280px)' }}>
      <div className="flex items-center justify-between mb-3 px-1">
        <div className="flex items-center gap-2 min-w-0">
          <div className="w-2.5 h-2.5 rounded-full shrink-0" style={{ backgroundColor: accentColor }} />
          <h3 className="text-sm font-bold text-text-main uppercase tracking-wide truncate">{list.name}</h3>
          <span className="shrink-0 text-[10px] font-bold px-1.5 py-0.5 rounded-md text-white" style={{ backgroundColor: accentColor }}>
            {tasks?.length || 0}
          </span>
        </div>
        <div className="flex items-center gap-0.5 shrink-0">
          {canEditTasks && <button onClick={() => setShowTaskForm(true)} className="p-1.5 rounded-lg text-text-muted hover:text-text-main hover:bg-hover-bg transition-colors" title="Add task">
            <Plus size={15} />
          </button>}
          {canManageBoard && <div className="relative" ref={menuRef}>
            <button onClick={() => setMenuOpen(o => !o)} className="p-1.5 rounded-lg text-text-muted hover:text-text-main hover:bg-hover-bg transition-colors">
              <MoreHorizontal size={15} />
            </button>
            {menuOpen && (
              <div className="absolute right-0 top-full mt-1 w-48 bg-surface-0 border border-border-subtle rounded-xl shadow-lg z-30 overflow-hidden">
                <button onClick={() => { setMenuOpen(false); onEditColumn(list); }} className="w-full flex items-center gap-3 px-4 py-2.5 text-sm font-medium text-text-main hover:bg-hover-bg transition-colors">
                  <Pencil size={14} className="text-primary" /> Rename
                </button>
                <button onClick={() => { setMenuOpen(false); onDuplicateColumn(list); }} className="w-full flex items-center gap-3 px-4 py-2.5 text-sm font-medium text-text-main hover:bg-hover-bg transition-colors">
                  <Copy size={14} className="text-violet-500" /> Duplicate
                </button>
                <div className="h-px bg-border-subtle mx-2" />
                <button onClick={() => { setMenuOpen(false); onDeleteColumn(list); }} className="w-full flex items-center gap-3 px-4 py-2.5 text-sm font-medium text-rose-500 hover:bg-rose-50 dark:hover:bg-rose-950/30 transition-colors">
                  <Trash2 size={14} /> Delete
                </button>
              </div>
            )}
          </div>}
        </div>
      </div>
      <div className="h-0.5 rounded-full mb-3 mx-1" style={{ backgroundColor: accentColor }} />

      <Droppable droppableId={String(list.id)} isDropDisabled={!canDrag}>
        {(provided, snapshot) => (
          <div
            ref={provided.innerRef}
            {...provided.droppableProps}
            className={`flex-1 overflow-y-auto rounded-2xl p-2 transition-colors duration-150 ${snapshot.isDraggingOver ? 'bg-primary/10 ring-2 ring-primary/20' : 'bg-surface-2/50'}`}
            style={{ minHeight: '120px' }}
          >
            {tasks?.map((task, index) => (
              <TaskCard key={task.id} task={task} index={index} onClick={onTaskClick} onToggleStatus={canEditTasks ? onToggleStatus : undefined} readOnly={!canEditTasks} />
            ))}
            {provided.placeholder}
            {canEditTasks && <button
              onClick={() => setShowTaskForm(true)}
              className="w-full py-2.5 mt-1 border-2 border-dashed border-border-subtle rounded-xl text-text-subtle text-xs font-bold hover:border-primary hover:text-primary hover:bg-surface-0 transition-all flex items-center justify-center gap-1.5"
            >
              <Plus size={13} /> Add Task
            </button>}
          </div>
        )}
      </Droppable>
      {list.taskPage?.page < list.taskPage?.totalPages && <Button onClick={onLoadMore}>Load more cards</Button>}

      <TaskFormModal isOpen={showTaskForm} onClose={() => setShowTaskForm(false)} onSubmit={handleCreateTask} listId={list.id} />
    </div>
  );
}

function ColumnFormModal({ title, form, setForm, onSubmit, onClose, loading, submitLabel }) {
  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-slate-900/50 backdrop-blur-sm" onClick={() => !loading && onClose()} />
      <div className="relative bg-surface-0 rounded-2xl shadow-2xl w-full max-w-sm overflow-hidden">
        <div className="flex items-center justify-between px-6 py-4 border-b border-border-subtle">
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 rounded-lg flex items-center justify-center" style={{ backgroundColor: form.color + '20' }}>
              <Columns3 size={16} style={{ color: form.color }} />
            </div>
            <h2 className="text-base font-bold text-text-main">{title}</h2>
          </div>
          <button onClick={onClose} disabled={loading} className="p-1.5 rounded-xl text-text-subtle hover:text-text-main hover:bg-hover-bg transition-colors">
            <X size={16} />
          </button>
        </div>
        <form onSubmit={onSubmit} className="p-5 space-y-4">
          <div className="flex gap-3">
            <div className="flex-1">
              <label className="block text-xs font-bold text-text-muted uppercase tracking-wider mb-1.5">Column Name *</label>
              <input
                autoFocus
                type="text"
                placeholder="e.g., In Review, Todo..."
                maxLength={50}
                value={form.name}
                onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                className="w-full px-3 py-2.5 bg-surface-2 border border-border-subtle text-text-main placeholder:text-text-muted rounded-xl focus:ring-2 focus:border-primary outline-none transition-all text-sm"
              />
            </div>
            {'quantity' in form && (
              <div className="w-24 shrink-0">
                <label className="block text-xs font-bold text-text-muted uppercase tracking-wider mb-1.5">Qty</label>
                <div className="flex items-center border border-border-subtle rounded-xl overflow-hidden bg-surface-2">
                  <button type="button" onClick={() => setForm(f => ({ ...f, quantity: Math.max(1, (f.quantity||1)-1) }))} className="px-2.5 py-2.5 text-text-muted hover:text-text-main hover:bg-hover-bg transition-colors font-bold text-base leading-none">−</button>
                  <span className="flex-1 text-center text-sm font-bold text-text-main">{form.quantity||1}</span>
                  <button type="button" onClick={() => setForm(f => ({ ...f, quantity: Math.min(10,(f.quantity||1)+1) }))} className="px-2.5 py-2.5 text-text-muted hover:text-text-main hover:bg-hover-bg transition-colors font-bold text-base leading-none">+</button>
                </div>
              </div>
            )}
          </div>
          <div>
            <label className="block text-xs font-bold text-text-muted uppercase tracking-wider mb-2">Color</label>
            <div className="flex flex-wrap gap-2">
              {COLUMN_COLORS.map(color => (
                <button key={color} type="button" onClick={() => setForm(f => ({ ...f, color }))}
                  className="w-7 h-7 rounded-lg transition-transform hover:scale-110"
                  style={{ backgroundColor: color, boxShadow: form.color === color ? `0 0 0 2px white, 0 0 0 4px ${color}` : 'none', transform: form.color === color ? 'scale(1.15)' : 'scale(1)' }}
                />
              ))}
            </div>
          </div>
          <div className="flex gap-2 pt-1">
            <button type="button" onClick={onClose} disabled={loading} className="flex-1 px-4 py-2.5 border border-border-subtle text-text-muted font-bold rounded-xl hover:bg-hover-bg transition-colors text-sm">Cancel</button>
            <button
              type="submit" disabled={loading || !form.name.trim()}
              className="flex-[2] flex items-center justify-center gap-2 px-4 py-2.5 text-white font-bold rounded-xl transition-colors text-sm disabled:opacity-60"
              style={{ backgroundColor: form.color }}
            >
              {loading ? <Loader2 size={15} className="animate-spin" /> : <Plus size={15} />}
              {loading ? 'Saving...' : submitLabel}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default function ProjectTasksBoard(props) { return <ProjectTasksBoardContent key={props.projectId + ":" + (props.requestedBoardId || "")} {...props} />; }
