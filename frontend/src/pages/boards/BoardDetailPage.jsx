import Modal from '@/components/ui/Modal';
import MoveTaskModal from '@/components/boards/MoveTaskModal';
import SharedColumnFormModal from '@/components/boards/ColumnFormModal';
import { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { DragDropContext, Droppable } from '@hello-pangea/dnd';
import { listApi } from '@/api/listApi';
import { taskApi } from '@/api/taskApi';
import TaskCard from '@/components/tasks/TaskCard';
import TaskModal from '@/components/tasks/TaskModal';
import TaskFormModal from '@/components/tasks/TaskFormModal';
import Button from '@/components/ui/Button';
import {
  Plus, ChevronLeft, Filter, Search,
  MoreHorizontal, Pencil, Trash2, X, Loader2,
  Columns3, Copy,
} from 'lucide-react';
import toast from 'react-hot-toast';
import { useLanguage } from '@/context/LanguageContext';
import { useBoardData } from '@/hooks/useBoardData';
import { upsertColumns, upsertTask, removeTask } from '@/utils/boardState';

// ── Column color palette ──
const COLUMN_COLORS = [
  '#6366f1', '#8b5cf6', '#ec4899', '#ef4444',
  '#f97316', '#eab308', '#22c55e', '#06b6d4',
  '#3b82f6', '#64748b',
];

function BoardDetailContent() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { t } = useLanguage();

  // Task modals
  const [selectedTask, setSelectedTask] = useState(null);
  const [movingTask, setMovingTask] = useState(null);
  const [moveFeedback, setMoveFeedback] = useState('');
  const boardSurface = useRef(null);
  const [editingTask, setEditingTask] = useState(null);

  // Add column modal
  const [showAddColumn, setShowAddColumn] = useState(false);
  const [colForm, setColForm] = useState({ name: '', color: '#6366f1', quantity: 1 });
  const [colLoading, setColLoading] = useState(false);

  // Edit column modal
  const [editingCol, setEditingCol] = useState(null);
  const [editColForm, setEditColForm] = useState({ name: '', color: '' });
  const [editColLoading, setEditColLoading] = useState(false);

  // Delete column confirm
  const [deletingCol, setDeletingCol] = useState(null);
  const [deleteColLoading, setDeleteColLoading] = useState(false);

  // Search
  const [searchQuery, setSearchQuery] = useState('');
  const { board, lists, setLists, columnPage, loading, loadingMore, loadError, partial, fetchBoardData, loadMore, boardPresence, capabilities } = useBoardData({ boardId: id, searchQuery });
  useEffect(() => { if (board?.projectId) navigate("/projects/" + board.projectId + "?boardId=" + board.id, { replace: true }); }, [board, navigate]);

  // ── Add column(s) ──
  const handleAddColumn = async (e) => {
    e.preventDefault();
    if (!colForm.name.trim()) { toast.error(t('board.columnNameRequired')); return; }
    const qty = Math.max(1, Math.min(10, colForm.quantity || 1));
    setColLoading(true);
    try {
      const created = [];
      for (let i = 0; i < qty; i++) {
        const name = qty === 1 ? colForm.name.trim() : `${colForm.name.trim()} ${i + 1}`;
        const res = await listApi.createList({
          name,
          boardId: id,
          position: lists.length + i,
          color: colForm.color,
        });
        created.push({ ...res.data.data, tasks: [] });
      }
      setLists(prev => upsertColumns(prev, created));
      toast.success(qty > 1 ? t('board.columnsAdded', { count: qty }) : t('board.columnAdded'));
      setShowAddColumn(false);
      setColForm({ name: '', color: '#6366f1', quantity: 1 });
    } catch (err) {
      toast.error(err?.response?.data?.message || 'Failed to add column');
    } finally {
      setColLoading(false);
    }
  };

  // ── Duplicate column ──
  const handleDuplicateColumn = async (list) => {
    try {
      const res = await listApi.createList({
        name: `${list.name} (Copy)`,
        boardId: id,
        position: lists.findIndex(l => l.id === list.id) + 1,
        color: list.color || '#6366f1',
      });
      const newCol = { ...res.data.data, tasks: [] };
      setLists(prev => upsertColumns(prev, [newCol]));
      toast.success(t('board.columnDuplicated', { name: list.name }));
    } catch (err) {
      toast.error(err?.response?.data?.message || t('board.duplicateError') || 'Failed to duplicate column');
    }
  };

  // ── Rename column ──
  const openEditCol = (list) => {
    setEditingCol(list);
    setEditColForm({ name: list.name, color: list.color || '#6366f1' });
  };

  const handleEditColumn = async (e) => {
    e.preventDefault();
    if (!editColForm.name.trim()) { toast.error(t('board.columnNameRequired')); return; }
    setEditColLoading(true);
    try {
      await listApi.updateList(editingCol.id, {
        name: editColForm.name.trim(),
        position: editingCol.position,
        color: editColForm.color,
      });
      setLists(prev => prev.map(l =>
        l.id === editingCol.id ? { ...l, name: editColForm.name.trim(), color: editColForm.color } : l
      ));
      toast.success(t('board.columnUpdated'));
      setEditingCol(null);
    } catch (err) {
      toast.error(err?.response?.data?.message || 'Failed to update column');
    } finally {
      setEditColLoading(false);
    }
  };

  // ── Delete column ──
  const handleDeleteColumn = async () => {
    if (deleteColLoading) return;
    setDeleteColLoading(true);
    try {
      await listApi.deleteList(deletingCol.id);
      setLists(prev => prev.filter(l => l.id !== deletingCol.id));
      toast.success(t('board.columnDeleted'));
      setDeletingCol(null);
    } catch (err) {
      toast.error(err?.response?.data?.message || 'Failed to delete column');
    } finally {
      setDeleteColLoading(false);
    }
  };

  // ── Task handlers ──
  const handleCreateTask = async (listId, formData) => {
    try {
      const res = await taskApi.createTask(listId, formData);
      const newTask = res.data.data;
      toast.success(t('board.taskCreated'));
      setLists(prev => upsertTask(prev, newTask));
    } catch (err) {
      toast.error(err?.response?.data?.message || 'Failed to create task');
      throw err;
    }
  };

  const handleEditTask = async (formData) => {
    if (!editingTask) return;
    try {
      await taskApi.updateTask(editingTask.id, formData);
      toast.success(t('board.taskUpdated'));
      setEditingTask(null);
      fetchBoardData();
    } catch {
      toast.error('Failed to update task');
    }
  };

  const handleDeleteTask = async (taskId) => {
    try {
      await taskApi.deleteTask(taskId);
      toast.success(t('board.taskDeleted'));
      setSelectedTask(null);
      setLists(prev => removeTask(prev, taskId));
    } catch (err) {
      toast.error(err?.response?.data?.message || 'Failed to delete task');
      throw err;
    }
  };

  const handleToggleStatus = async (task) => {
    const newStatus = task.status === 'Done' ? 'Todo' : 'Done';
    try {
      await taskApi.changeTaskStatus(task.id, newStatus);
      setLists(prev => prev.map(l => ({
        ...l,
        tasks: (l.tasks || []).map(t => t.id === task.id ? { ...t, status: newStatus } : t)
      })));
    } catch {
      toast.error(t('board.statusUpdateError'));
    }
  };

  // ── Drag & Drop ──
  const onDragEnd = async (result) => {
    if (loading || partial || searchQuery || !capabilities.canCreateTasks) return;
    const { destination, source, draggableId } = result;
    if (!destination) return;
    if (destination.droppableId === source.droppableId && destination.index === source.index) return;

    const srcIdx = lists.findIndex(l => String(l.id) === source.droppableId);
    const dstIdx = lists.findIndex(l => String(l.id) === destination.droppableId);
    if (srcIdx === -1 || dstIdx === -1) return;

    const newLists = lists.map(l => ({ ...l, tasks: [...(l.tasks || [])] }));
    const [movedTask] = newLists[srcIdx].tasks.splice(source.index, 1);
    newLists[dstIdx].tasks.splice(destination.index, 0, movedTask);
    setLists(newLists);

    try {
      await taskApi.moveTask(draggableId, {
        listId: destination.droppableId,
        position: destination.index,
        expectedUpdatedAt: movedTask.updatedAt ?? null,
      });
      setMoveFeedback(`${movedTask.title} moved to ${lists[dstIdx].name}.`);
      await fetchBoardData();
    } catch {
      toast.error(t('board.moveTaskError'));
      fetchBoardData();
    }
  };

  if (loading && !board) {
    return (
      <div className="flex items-center justify-center h-full">
        <div className="flex flex-col items-center gap-3">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary" />
          <p className="text-sm text-text-muted font-medium">{t('board.loadingBoard')}</p>
        </div>
      </div>
    );
  }

  if (loadError) return <p role="alert">{loadError}</p>;
  if (!board) return <div className="text-center py-20 text-text-muted">{t('board.boardNotFound')}</div>;

  return (
    <div className="h-full min-w-0 flex flex-col">
      {/* Board Header */}
      <div className="mb-6 shrink-0">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-4">
          <div className="flex items-center gap-3">
            <Button variant="ghost" size="icon" aria-label="Back to dashboard" onClick={() => navigate('/dashboard')}>
              <ChevronLeft size={20} />
            </Button>
            <div>
              <div className="flex items-center gap-2.5">
                <div className="w-3.5 h-3.5 rounded-full" style={{ backgroundColor: board.color || '#6366f1' }} />
                <h1 className="text-2xl font-black text-text-main tracking-tight">{board.name}</h1>
                
                {/* Active users presence avatar pile */}
                {boardPresence.length > 0 && (
                  <div className="flex items-center -space-x-1.5 ml-4">
                    {boardPresence.map((member) => (
                      <div
                        key={member.userId}
                        title={member.fullName}
                        className="w-7 h-7 rounded-full border-2 border-surface-0 bg-primary/10 flex items-center justify-center text-[10px] font-extrabold text-primary overflow-hidden shadow-sm"
                      >
                        {member.avatarUrl ? (
                          <img src={member.avatarUrl} alt={member.fullName} className="w-full h-full object-cover" />
                        ) : (
                          member.fullName?.charAt(0).toUpperCase() || 'U'
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </div>
              <p className="text-xs text-text-muted font-semibold uppercase tracking-widest mt-0.5">
                {lists.length} of {columnPage?.totalItems ?? lists.length} columns loaded · {lists.reduce((acc, l) => acc + (l.tasks?.length || 0), 0)} loaded tasks
              </p>
            </div>
          </div>
        </div>

        {/* Toolbar */}
        <div className="flex flex-wrap gap-2 items-center justify-between bg-surface-0 p-2 rounded-2xl border border-border-subtle shadow-sm">
          <div className="flex items-center gap-2">
            <Button variant="ghost" size="sm" leftIcon={<Filter size={14} />}>{t('board.filter')}</Button>
            <div className="h-4 w-px bg-border-subtle mx-1" />
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-text-muted" size={14} />
              <input
                type="text"
                placeholder={t('board.searchPlaceholder')}
                value={searchQuery}
                onChange={e => setSearchQuery(e.target.value)}
                className="pl-9 pr-4 py-1.5 bg-transparent border-none text-sm focus:outline-none focus:ring-0 w-48 text-text-main placeholder:text-text-muted"
              />
            </div>
          </div>
          <Button
            variant="primary"
            size="sm"
            leftIcon={<Columns3 size={16} />}
            onClick={() => setShowAddColumn(true)}
          >
            {t('board.addColumn')}
          </Button>
        </div>
      </div>

      {loading && <p role="status" className="text-text-muted">Loading tasks…</p>}
      {partial && <div role="status" className="p-3 text-text-muted">Showing part of this board. Load remaining columns/cards to see all tasks. Reordering is disabled until all cards are loaded.</div>}
      <div className="flex flex-wrap gap-2">
        {columnPage?.page < columnPage?.totalPages && <Button disabled={loadingMore} onClick={() => loadMore()}>Load more columns</Button>}
        {lists.filter(list => list.taskPage?.page < list.taskPage?.totalPages).map(list => <Button key={list.id} disabled={loadingMore} onClick={() => loadMore(list)}>Load more cards: {list.name}</Button>)}
      </div>
      {/* Kanban Board */}
      <p role={moveFeedback ? 'status' : undefined} aria-live="polite" className="text-sm text-text-muted mb-2">{moveFeedback || 'Scroll across columns. Move actions are available when the full board is loaded.'}</p>
      <div ref={boardSurface} tabIndex={-1} aria-label="Task board" className="min-w-0 max-w-full flex-1 overflow-x-auto overscroll-x-contain snap-x snap-proximity pb-6">
        <DragDropContext onDragEnd={loading || partial || searchQuery ? () => {} : onDragEnd}>
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
                readOnly={!capabilities.canCreateTasks}
                dragDisabled={loading || partial || Boolean(searchQuery)}
                onMove={!loading && !partial && !searchQuery && capabilities.canCreateTasks ? setMovingTask : undefined}
              />
            ))}

            {/* Add Column Card */}
            <button
              onClick={() => setShowAddColumn(true)}
              className="shrink-0 w-72 h-36 flex flex-col items-center justify-center gap-2 rounded-2xl border-2 border-dashed border-border-subtle text-text-subtle hover:border-primary hover:text-primary hover:bg-primary/5 transition-all duration-200"
            >
              <div className="w-10 h-10 rounded-xl border-2 border-current flex items-center justify-center">
                <Plus size={20} />
              </div>
              <span className="font-bold text-xs uppercase tracking-wider">{t('board.addColumn')}</span>
            </button>
          </div>
        </DragDropContext>
      </div>
      {movingTask && <MoveTaskModal key={movingTask} taskId={movingTask} lists={lists} onClose={() => setMovingTask(null)} onMoved={(task, name) => {
        setLists(previous => upsertTask(previous, task)); setMoveFeedback(`${task.title} moved to ${name}.`); setMovingTask(null);
        requestAnimationFrame(() => boardSurface.current?.focus()); fetchBoardData();
      }} />}

      {/* ── Add Column Modal ── */}
      {showAddColumn && (
        <ColumnFormModal
          title={t('board.addColumn')}
          form={colForm}
          setForm={setColForm}
          onSubmit={handleAddColumn}
          onClose={() => setShowAddColumn(false)}
          loading={colLoading}
          submitLabel={t('board.addColumn')}
        />
      )}

      {/* ── Edit Column Modal ── */}
      {editingCol && (
        <ColumnFormModal
          title={t('board.renameColumn')}
          form={editColForm}
          setForm={setEditColForm}
          onSubmit={handleEditColumn}
          onClose={() => setEditingCol(null)}
          loading={editColLoading}
          submitLabel={t('common.save')}
        />
      )}

      {/* ── Delete Column Confirm ── */}
      {deletingCol && (
        <Modal isOpen title="Delete column" closeDisabled={deleteColLoading} onClose={() => setDeletingCol(null)} maxWidth="max-w-md">
          <div className="p-6">
            <div className="flex items-center gap-4 mb-4">
              <div className="w-12 h-12 rounded-xl bg-red-100 flex items-center justify-center">
                <Trash2 size={22} className="text-red-500" />
              </div>
              <div>
                <h3 className="text-lg font-bold text-text-main">{t('board.deleteColumn')}</h3>
                <p className="text-sm text-text-muted mt-0.5">{t('board.deleteColumnWarning', { count: deletingCol.tasks?.length || 0 })}</p>
              </div>
            </div>
            <p className="text-sm text-text-muted bg-surface-2 rounded-xl p-4 mb-6">
              {t('board.deleteColumnConfirm', { name: deletingCol.name })}
            </p>
            <div className="flex gap-3">
              <Button variant="ghost" className="flex-1" onClick={() => setDeletingCol(null)} disabled={deleteColLoading}>
                {t('common.cancel')}
              </Button>
              <Button variant="danger" onClick={handleDeleteColumn}
                disabled={deleteColLoading}
                className="flex-1 flex items-center justify-center gap-2 px-4 py-2.5 bg-red-500 hover:bg-red-600 text-white font-bold rounded-xl transition-colors disabled:opacity-70"
              >
                {deleteColLoading ? <Loader2 size={16} className="animate-spin" /> : <Trash2 size={16} />}
                {deleteColLoading ? (t('board.saving')) : t('common.delete')}
              </Button>
            </div>
          </div>
        </Modal>
      )}

      {/* ── Task Detail Modal ── */}
      <TaskModal
        isOpen={!!selectedTask}
        taskId={selectedTask}
        onClose={() => setSelectedTask(null)}
        onEdit={task => { setEditingTask(task); setSelectedTask(null); }}
        onDelete={handleDeleteTask} onChanged={fetchBoardData}
      />

      {/* ── Edit Task Modal ── */}
      <TaskFormModal
        isOpen={!!editingTask}
        onClose={() => setEditingTask(null)}
        onSubmit={handleEditTask}
        task={editingTask}
      />
    </div>
  );
}

// ══════════════════════════════════════════════════════
// KanbanColumn — Inline, self-contained column component
// ══════════════════════════════════════════════════════
function KanbanColumn({ list, tasks, onCreateTask, onTaskClick, onToggleStatus, onEditColumn, onDeleteColumn, onDuplicateColumn, onMove, readOnly, dragDisabled }) {
  const { t } = useLanguage();
  const [showTaskForm, setShowTaskForm] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef(null);

  useEffect(() => {
    const handler = (e) => { if (menuRef.current && !menuRef.current.contains(e.target)) setMenuOpen(false); };
    if (menuOpen) document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [menuOpen]);

  const handleCreateTask = async (formData) => {
    await onCreateTask(list.id, formData);
    setShowTaskForm(false);
  };

  const accentColor = list.color || '#6366f1';

  return (
    <div className="snap-start shrink-0 w-[min(18rem,calc(100vw-3rem))] sm:w-72 flex flex-col" style={{ maxHeight: 'calc(100dvh - 200px)' }}>
      {/* Column Header */}
      <div className="flex items-center justify-between mb-3 px-1">
        <div className="flex items-center gap-2 min-w-0">
          <div className="w-2.5 h-2.5 rounded-full shrink-0" style={{ backgroundColor: accentColor }} />
          <h3 className="text-sm font-bold text-text-main uppercase tracking-wide truncate">
            {list.name}
          </h3>
          <span
            className="shrink-0 text-xs font-bold px-1.5 py-0.5 rounded-md bg-surface-3 text-text-main"
          >
            {tasks?.length || 0}
          </span>
        </div>

        <div className="flex items-center gap-0.5 shrink-0">
          <button
            onClick={() => setShowTaskForm(true)}
            className="p-1.5 rounded-lg text-text-muted hover:text-text-main hover:bg-hover-bg transition-colors"
            title={t('board.addTask')}
          >
            <Plus size={15} />
          </button>

          {/* Column menu */}
          <div className="relative" ref={menuRef}>
            <button
              aria-label={`Column actions for ${list.name}`} aria-expanded={menuOpen}
              onClick={() => setMenuOpen(o => !o)}
              className="min-h-10 min-w-10 p-1.5 rounded-lg text-text-muted hover:text-text-main hover:bg-hover-bg transition-colors"
            >
              <MoreHorizontal size={15} />
            </button>
            {menuOpen && (
              <div className="absolute right-0 top-full mt-1 w-48 bg-surface-0 border border-border-subtle rounded-xl shadow-lg z-30 overflow-hidden">
                <button
                  onClick={() => { setMenuOpen(false); onEditColumn(list); }}
                  className="w-full flex items-center gap-3 px-4 py-2.5 text-sm font-medium text-text-main hover:bg-hover-bg transition-colors"
                >
                  <Pencil size={14} className="text-primary" /> {t('board.renameColumn')}
                </button>
                <button
                  onClick={() => { setMenuOpen(false); onDuplicateColumn(list); }}
                  className="w-full flex items-center gap-3 px-4 py-2.5 text-sm font-medium text-text-main hover:bg-hover-bg transition-colors"
                >
                  <Copy size={14} className="text-violet-500" /> {t('board.duplicateColumn')}
                </button>
                <div className="h-px bg-border-subtle mx-2" />
                <button
                  onClick={() => { setMenuOpen(false); onDeleteColumn(list); }}
                  className="w-full flex items-center gap-3 px-4 py-2.5 text-sm font-medium text-red-500 hover:bg-red-50 dark:hover:bg-red-500/10 transition-colors"
                >
                  <Trash2 size={14} /> {t('board.deleteColumn')}
                </button>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Color bar */}
      <div className="h-0.5 rounded-full mb-3 mx-1" style={{ backgroundColor: accentColor }} />

      {/* Droppable area */}
      <Droppable droppableId={String(list.id)}>
        {(provided, snapshot) => (
          <div
            ref={provided.innerRef}
            {...provided.droppableProps}
            className={`flex-1 overflow-y-auto rounded-2xl p-2 transition-colors duration-150 ${
              snapshot.isDraggingOver ? 'bg-primary/5 ring-2 ring-primary/20' : 'bg-surface-2/50'
            }`}
            style={{ minHeight: '120px' }}
          >
            {tasks?.map((task, index) => (
              <TaskCard
                key={task.id}
                task={task}
                index={index}
                onClick={onTaskClick}
                onToggleStatus={onToggleStatus}
                onMove={onMove} readOnly={readOnly} dragDisabled={dragDisabled}
              />
            ))}
            {provided.placeholder}

            {/* Inline add task button */}
            <button
              onClick={() => setShowTaskForm(true)}
              className="w-full py-2.5 mt-1 border-2 border-dashed border-border-subtle rounded-xl text-text-subtle text-xs font-bold hover:border-primary hover:text-primary hover:bg-hover-bg transition-all flex items-center justify-center gap-1.5"
            >
              <Plus size={13} /> {t('board.addTask')}
            </button>
          </div>
        )}
      </Droppable>

      {/* Task Form Modal */}
      <TaskFormModal
        isOpen={showTaskForm}
        onClose={() => setShowTaskForm(false)}
        onSubmit={handleCreateTask}
        listId={list.id}
      />
    </div>
  );
}

// ════════════════════════════════
// Reusable Column Form Modal
// ════════════════════════════════
function ColumnFormModal(props) { return <SharedColumnFormModal {...props} colors={COLUMN_COLORS} />; }

export default function BoardDetailPage() { const { id } = useParams(); return <BoardDetailContent key={id} />; }
