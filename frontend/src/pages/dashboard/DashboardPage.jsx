import { useCallback, useEffect, useState } from 'react';
import { useAuth } from '@/context/AuthContext';
import { useNavigate } from 'react-router-dom';
import { boardApi } from '@/api/boardApi';
import { dashboardApi } from '@/api/dashboardApi';
import { projectApi } from '@/api/projectApi';
import { taskApi } from '@/api/taskApi';
import { Plus, Trash2, X, Loader2 } from 'lucide-react';
import Button from '@/components/ui/Button';
import DashboardHeader from '@/components/dashboard/DashboardHeader';
import DashboardStats from '@/components/dashboard/DashboardStats';
import TaskActivityChart from '@/components/dashboard/TaskActivityChart';
import RecentActivity from '@/components/dashboard/RecentActivity';
import UpcomingTasks from '@/components/dashboard/UpcomingTasks';
import RecentProjects from '@/components/dashboard/RecentProjects';
import TeamWorkload from '@/components/dashboard/TeamWorkload';
import RecentBoards from '@/components/dashboard/RecentBoards';
import { useLanguage } from '@/context/LanguageContext';
import toast from 'react-hot-toast';

const BOARD_COLORS = [
  '#6366f1', '#8b5cf6', '#ec4899', '#ef4444',
  '#f97316', '#eab308', '#22c55e', '#06b6d4',
  '#3b82f6', '#14b8a6', '#84cc16', '#f43f5e',
];

const EMPTY_STATS = {
  total: 0,
  todo: 0,
  inProgress: 0,
  done: 0,
  overdue: 0,
  totalBoards: 0,
  teamMembers: 0,
};

export default function DashboardPage() {
  const { user } = useAuth();
  const { t } = useLanguage();
  const navigate = useNavigate();

  const [boards, setBoards] = useState([]);
  const [projects, setProjects] = useState([]);
  const [recentTasks, setRecentTasks] = useState([]);
  const [upcomingTasks, setUpcomingTasks] = useState([]);
  const [myTasks, setMyTasks] = useState([]);
  const [stats, setStats] = useState(EMPTY_STATS);
  const [loading, setLoading] = useState(true);

  const [editingBoard, setEditingBoard] = useState(null);
  const [editForm, setEditForm] = useState({ name: '', color: '' });
  const [editLoading, setEditLoading] = useState(false);

  const [deletingBoard, setDeletingBoard] = useState(null);
  const [deleteLoading, setDeleteLoading] = useState(false);

  const [showCreateModal, setShowCreateModal] = useState(false);
  const [createForm, setCreateForm] = useState({ name: '', color: '#6366f1' });
  const [createLoading, setCreateLoading] = useState(false);

  const fetchData = useCallback(async () => {
    setLoading(true);

    const [boardsRes, statsRes, recentTasksRes, upcomingRes, projectsRes, myTasksRes] = await Promise.allSettled([
      boardApi.getBoards(),
      dashboardApi.getStats(),
      dashboardApi.getRecentTasks('all', 24),
      dashboardApi.getUpcomingTasks(7),
      projectApi.getProjects(),
      taskApi.getMyTasks(),
    ]);

    if (boardsRes.status === 'fulfilled') {
      setBoards(boardsRes.value.data.data || []);
    }

    if (statsRes.status === 'fulfilled') {
      setStats({ ...EMPTY_STATS, ...(statsRes.value.data.data || {}) });
    }

    if (recentTasksRes.status === 'fulfilled') {
      setRecentTasks(recentTasksRes.value.data.data || []);
    }

    if (upcomingRes.status === 'fulfilled') {
      setUpcomingTasks(upcomingRes.value.data.data || []);
    }

    if (projectsRes.status === 'fulfilled') {
      setProjects(projectsRes.value.data.data || []);
    }

    if (myTasksRes.status === 'fulfilled') {
      setMyTasks(myTasksRes.value.data.data || []);
    }

    const hasError = [boardsRes, statsRes, recentTasksRes, upcomingRes, projectsRes, myTasksRes]
      .some((result) => result.status === 'rejected');
    if (hasError) {
      toast.error('Some dashboard data could not be loaded');
    }

    setLoading(false);
  }, []);

  useEffect(() => { fetchData(); }, [fetchData]);

  const handleCreateBoard = async (event) => {
    event.preventDefault();
    if (!createForm.name.trim()) {
      toast.error(t('dashboard.boardNameRequired'));
      return;
    }

    setCreateLoading(true);
    try {
      const res = await boardApi.createBoard({ name: createForm.name.trim(), color: createForm.color });
      setBoards((prev) => [res.data.data, ...prev]);
      setStats((prev) => ({ ...prev, totalBoards: (prev.totalBoards || 0) + 1 }));
      toast.success(t('dashboard.boardCreated'));
      setShowCreateModal(false);
      setCreateForm({ name: '', color: '#6366f1' });
    } catch (err) {
      toast.error(err?.response?.data?.message || 'Failed to create board');
    } finally {
      setCreateLoading(false);
    }
  };

  const openEditModal = (event, board) => {
    event.stopPropagation();
    setEditingBoard(board);
    setEditForm({ name: board.name, color: board.color || '#6366f1' });
  };

  const handleUpdateBoard = async (event) => {
    event.preventDefault();
    if (!editForm.name.trim()) {
      toast.error(t('dashboard.boardNameRequired'));
      return;
    }

    setEditLoading(true);
    try {
      await boardApi.updateBoard(editingBoard.id, { name: editForm.name.trim(), color: editForm.color });
      setBoards((prev) => prev.map((board) =>
        board.id === editingBoard.id ? { ...board, name: editForm.name.trim(), color: editForm.color } : board
      ));
      toast.success(t('dashboard.boardUpdated'));
      setEditingBoard(null);
    } catch (err) {
      toast.error(err?.response?.data?.message || 'Failed to update board');
    } finally {
      setEditLoading(false);
    }
  };

  const openDeleteConfirm = (event, board) => {
    event.stopPropagation();
    setDeletingBoard(board);
  };

  const handleDeleteBoard = async () => {
    setDeleteLoading(true);
    try {
      await boardApi.deleteBoard(deletingBoard.id);
      setBoards((prev) => prev.filter((board) => board.id !== deletingBoard.id));
      setStats((prev) => ({ ...prev, totalBoards: Math.max(0, (prev.totalBoards || 1) - 1) }));
      toast.success(t('dashboard.boardDeleted'));
      setDeletingBoard(null);
    } catch (err) {
      toast.error(err?.response?.data?.message || 'Failed to delete board');
    } finally {
      setDeleteLoading(false);
    }
  };

  return (
    <div className="space-y-6 pb-8">
      <DashboardHeader
        userFullName={user?.fullName}
        onCreateBoard={() => setShowCreateModal(true)}
      />

      <DashboardStats stats={stats} />

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-3">
        <TaskActivityChart tasks={recentTasks} stats={stats} loading={loading} />
        <RecentActivity activities={recentTasks} loading={loading} />
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-3">
        <UpcomingTasks tasks={upcomingTasks} loading={loading} />
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-3">
        <RecentProjects
          projects={projects}
          loading={loading}
          onOpenProjects={() => navigate('/projects')}
        />
        <TeamWorkload tasks={myTasks} loading={loading} />
      </div>

      <RecentBoards
        boards={boards}
        loading={loading}
        onBoardClick={(boardId) => navigate(`/boards/${boardId}`)}
        onCreateBoard={() => setShowCreateModal(true)}
        onEdit={openEditModal}
        onDelete={openDeleteConfirm}
      />

      {showCreateModal && (
        <BoardFormModal
          title={t('dashboard.createBoard')}
          form={createForm}
          setForm={setCreateForm}
          onSubmit={handleCreateBoard}
          onClose={() => setShowCreateModal(false)}
          loading={createLoading}
          submitLabel={t('dashboard.createBoard')}
        />
      )}

      {editingBoard && (
        <BoardFormModal
          title={t('dashboard.editBoard')}
          form={editForm}
          setForm={setEditForm}
          onSubmit={handleUpdateBoard}
          onClose={() => setEditingBoard(null)}
          loading={editLoading}
          submitLabel={t('common.save')}
        />
      )}

      {deletingBoard && (
        <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
          <div className="absolute inset-0 bg-slate-900/50 backdrop-blur-sm" onClick={() => !deleteLoading && setDeletingBoard(null)} />
          <div className="relative w-full max-w-md rounded-2xl border border-border-subtle bg-surface-0 p-6 shadow-2xl animate-in fade-in zoom-in duration-200">
            <div className="mb-4 flex items-center gap-4">
              <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-rose-500/10">
                <Trash2 size={22} className="text-rose-500" />
              </div>
              <div>
                <h3 className="text-lg font-bold text-text-main">{t('dashboard.deleteBoard')}</h3>
                <p className="mt-0.5 text-sm text-text-muted">{t('dashboard.deleteWarning')}</p>
              </div>
            </div>
            <p className="mb-6 rounded-xl bg-surface-2 p-4 text-sm text-text-muted">
              {t('dashboard.deleteConfirm', { name: deletingBoard.name })}
            </p>
            <div className="flex gap-3">
              <Button variant="ghost" className="flex-1" onClick={() => setDeletingBoard(null)} disabled={deleteLoading}>
                {t('common.cancel')}
              </Button>
              <button
                onClick={handleDeleteBoard}
                disabled={deleteLoading}
                className="flex flex-1 items-center justify-center gap-2 rounded-xl bg-rose-500 px-4 py-2.5 font-bold text-white transition-colors hover:bg-rose-600 disabled:opacity-70"
              >
                {deleteLoading ? <Loader2 size={16} className="animate-spin" /> : <Trash2 size={16} />}
                {deleteLoading ? `${t('common.delete')}...` : t('dashboard.deleteBoard')}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function BoardFormModal({ title, form, setForm, onSubmit, onClose, loading, submitLabel }) {
  const { t } = useLanguage();

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-slate-900/50 backdrop-blur-sm" onClick={() => !loading && onClose()} />
      <div className="relative w-full max-w-md overflow-hidden rounded-2xl border border-border-subtle bg-surface-0 shadow-2xl animate-in fade-in zoom-in duration-200">
        <div className="flex items-center justify-between border-b border-border-subtle px-6 py-5">
          <h2 className="text-xl font-bold text-text-main">{title}</h2>
          <button
            type="button"
            onClick={onClose}
            disabled={loading}
            className="rounded-xl p-2 text-text-subtle transition-colors hover:bg-hover-bg hover:text-text-main"
          >
            <X size={18} />
          </button>
        </div>

        <form onSubmit={onSubmit} className="space-y-5 p-6">
          <div>
            <label className="mb-1.5 block text-sm font-bold text-text-main">
              Board Name <span className="text-rose-500">*</span>
            </label>
            <input
              autoFocus
              type="text"
              placeholder="e.g., Marketing Q3"
              maxLength={100}
              value={form.name}
              onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
              className="w-full rounded-xl border border-border-subtle bg-surface-2 px-4 py-2.5 text-sm text-text-main outline-none transition-all focus:border-primary focus:ring-2 focus:ring-primary/30"
            />
          </div>

          <div>
            <label className="mb-2 block text-sm font-bold text-text-main">Board Color</label>
            <div className="flex flex-wrap gap-2">
              {BOARD_COLORS.map((color) => (
                <button
                  key={color}
                  type="button"
                  onClick={() => setForm((current) => ({ ...current, color }))}
                  className="h-8 w-8 rounded-lg transition-transform hover:scale-110 focus:outline-none focus:ring-2 focus:ring-primary/30"
                  style={{ backgroundColor: color, boxShadow: form.color === color ? `0 0 0 3px var(--color-surface-0), 0 0 0 5px ${color}` : 'none' }}
                  aria-label={`Select color ${color}`}
                />
              ))}
            </div>
            <div className="mt-3 h-2 rounded-full transition-colors" style={{ backgroundColor: form.color }} />
          </div>

          <div className="flex gap-3 pt-2">
            <Button type="button" variant="ghost" className="flex-1" onClick={onClose} disabled={loading}>
              {t('common.cancel')}
            </Button>
            <Button type="submit" className="flex-1" isLoading={loading} disabled={loading || !form.name.trim()}>
              {submitLabel}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
