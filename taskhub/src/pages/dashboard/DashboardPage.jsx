import { useCallback, useState, useEffect, useRef } from 'react';
import { useAuth } from '@/context/AuthContext';
import { useNavigate } from 'react-router-dom';
import { boardApi } from '@/api/boardApi';
import { dashboardApi } from '@/api/dashboardApi';
import { Plus, Layout, CheckCircle2, Clock, MoreVertical, Pencil, Trash2, X, Loader2, Users } from 'lucide-react';
import Button from '@/components/ui/Button';
import Card from '@/components/ui/Card';
import Badge from '@/components/ui/Badge';
import { useLanguage } from '@/context/LanguageContext';
import toast from 'react-hot-toast';

// ── Color Palette ──
const BOARD_COLORS = [
  '#6366f1', '#8b5cf6', '#ec4899', '#ef4444',
  '#f97316', '#eab308', '#22c55e', '#06b6d4',
  '#3b82f6', '#14b8a6', '#84cc16', '#f43f5e',
];

export default function DashboardPage() {
  const { user } = useAuth();
  const { t } = useLanguage();
  const navigate = useNavigate();

  const [boards, setBoards] = useState([]);
  const [stats, setStats] = useState({ total: 0, todo: 0, inProgress: 0, done: 0, overdue: 0, totalBoards: 0, teamMembers: 0 });
  const [loading, setLoading] = useState(true);

  // Edit modal state
  const [editingBoard, setEditingBoard] = useState(null);   // board object being edited
  const [editForm, setEditForm] = useState({ name: '', color: '' });
  const [editLoading, setEditLoading] = useState(false);

  // Delete confirm state
  const [deletingBoard, setDeletingBoard] = useState(null);
  const [deleteLoading, setDeleteLoading] = useState(false);

  // Create board modal state
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [createForm, setCreateForm] = useState({ name: '', color: '#6366f1' });
  const [createLoading, setCreateLoading] = useState(false);

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      const [boardsRes, statsRes] = await Promise.all([
        boardApi.getBoards(),
        dashboardApi.getStats(),
      ]);
      setBoards(boardsRes.data.data || []);
      setStats(statsRes.data.data || {});
    } catch (err) {
      console.error('Failed to fetch dashboard data', err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchData(); }, [fetchData]);

  // ── Create Board ──
  const handleCreateBoard = async (e) => {
    e.preventDefault();
    if (!createForm.name.trim()) { toast.error(t('dashboard.boardNameRequired')); return; }
    setCreateLoading(true);
    try {
      const res = await boardApi.createBoard({ name: createForm.name.trim(), color: createForm.color });
      setBoards(prev => [res.data.data, ...prev]);
      setStats(prev => ({ ...prev, totalBoards: (prev.totalBoards || 0) + 1 }));
      toast.success(t('dashboard.boardCreated'));
      setShowCreateModal(false);
      setCreateForm({ name: '', color: '#6366f1' });
    } catch (err) {
      toast.error(err?.response?.data?.message || 'Failed to create board');
    } finally {
      setCreateLoading(false);
    }
  };

  // ── Update Board ──
  const openEditModal = (e, board) => {
    e.stopPropagation();
    setEditingBoard(board);
    setEditForm({ name: board.name, color: board.color || '#6366f1' });
  };

  const handleUpdateBoard = async (e) => {
    e.preventDefault();
    if (!editForm.name.trim()) { toast.error(t('dashboard.boardNameRequired')); return; }
    setEditLoading(true);
    try {
      await boardApi.updateBoard(editingBoard.id, { name: editForm.name.trim(), color: editForm.color });
      setBoards(prev => prev.map(b =>
        b.id === editingBoard.id ? { ...b, name: editForm.name.trim(), color: editForm.color } : b
      ));
      toast.success(t('dashboard.boardUpdated'));
      setEditingBoard(null);
    } catch (err) {
      toast.error(err?.response?.data?.message || 'Failed to update board');
    } finally {
      setEditLoading(false);
    }
  };

  // ── Delete Board ──
  const openDeleteConfirm = (e, board) => {
    e.stopPropagation();
    setDeletingBoard(board);
  };

  const handleDeleteBoard = async () => {
    setDeleteLoading(true);
    try {
      await boardApi.deleteBoard(deletingBoard.id);
      setBoards(prev => prev.filter(b => b.id !== deletingBoard.id));
      setStats(prev => ({ ...prev, totalBoards: Math.max(0, (prev.totalBoards || 1) - 1) }));
      toast.success(t('dashboard.boardDeleted'));
      setDeletingBoard(null);
    } catch (err) {
      toast.error(err?.response?.data?.message || 'Failed to delete board');
    } finally {
      setDeleteLoading(false);
    }
  };

  return (
    <div className="space-y-8">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-black text-text-main tracking-tight">
            {t('dashboard.welcome', { name: user?.fullName?.split(' ')[0] || t('nav.userName') })}
          </h1>
          <p className="text-text-muted mt-1 font-medium">{t('dashboard.subtitle')}</p>
        </div>
        <Button leftIcon={<Plus size={18} />} onClick={() => setShowCreateModal(true)} className="md:w-auto w-full">
          {t('dashboard.createBoard')}
        </Button>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
        <StatCard icon={<Layout className="text-primary" />} label={t('dashboard.totalBoards')} value={stats.totalBoards ?? 0} trend={t('dashboard.trendBoards')} />
        <StatCard icon={<Clock className="text-secondary" />} label={t('dashboard.activeTasks')} value={(stats.todo ?? 0) + (stats.inProgress ?? 0)} trend={t('dashboard.trendTasks')} />
        <StatCard icon={<CheckCircle2 className="text-emerald-500" />} label={t('dashboard.completed')} value={stats.done ?? 0} trend={t('dashboard.trendCompleted')} />
        <StatCard icon={<Users className="text-amber-500" />} label={t('dashboard.teamMembers')} value={stats.teamMembers ?? 0} trend={t('dashboard.trendMembers')} />
      </div>

      {/* Recent Boards */}
      <div>
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-xl font-bold text-text-main">{t('dashboard.recentBoards')}</h2>
          <Button variant="ghost" size="sm">{t('dashboard.viewAll')}</Button>
        </div>

        {loading ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {[1, 2, 3].map(i => <div key={i} className="h-48 rounded-2xl bg-slate-100 animate-pulse" />)}
          </div>
        ) : boards.length === 0 ? (
          <Card className="text-center py-16">
            <div className="w-16 h-16 bg-slate-50 rounded-full flex items-center justify-center mx-auto mb-4">
              <Layout size={32} className="text-slate-300" />
            </div>
            <h3 className="text-lg font-bold text-text-main">{t('dashboard.noBoards')}</h3>
            <p className="text-text-muted mb-6">{t('dashboard.noBoardsDesc')}</p>
            <Button variant="outline" onClick={() => setShowCreateModal(true)}>{t('common.getStarted')}</Button>
          </Card>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {boards.map(board => (
              <BoardCard
                key={board.id}
                board={board}
                onClick={() => navigate(`/boards/${board.id}`)}
                onEdit={openEditModal}
                onDelete={openDeleteConfirm}
              />
            ))}
          </div>
        )}
      </div>

      {/* ── Create Board Modal ── */}
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

      {/* ── Edit Board Modal ── */}
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

      {/* ── Delete Confirm Dialog ── */}
      {deletingBoard && (
        <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
          <div className="absolute inset-0 bg-slate-900/50 backdrop-blur-sm" onClick={() => !deleteLoading && setDeletingBoard(null)} />
          <div className="relative bg-white dark:bg-slate-800 rounded-2xl shadow-2xl w-full max-w-md p-6 animate-in fade-in zoom-in duration-200">
            <div className="flex items-center gap-4 mb-4">
              <div className="w-12 h-12 rounded-xl bg-red-100 flex items-center justify-center shrink-0">
                <Trash2 size={22} className="text-red-500" />
              </div>
              <div>
                <h3 className="text-lg font-bold text-text-main">{t('dashboard.deleteBoard')}</h3>
                <p className="text-sm text-text-muted mt-0.5">{t('dashboard.deleteWarning')}</p>
              </div>
            </div>
            <p className="text-sm text-text-muted bg-slate-50 dark:bg-slate-700/50 rounded-xl p-4 mb-6">
              {t('dashboard.deleteConfirm', { name: deletingBoard.name })}
            </p>
            <div className="flex gap-3">
              <Button variant="ghost" className="flex-1" onClick={() => setDeletingBoard(null)} disabled={deleteLoading}>
                {t('common.cancel')}
              </Button>
              <button
                onClick={handleDeleteBoard}
                disabled={deleteLoading}
                className="flex-1 flex items-center justify-center gap-2 px-4 py-2.5 bg-red-500 hover:bg-red-600 text-white font-bold rounded-xl transition-colors disabled:opacity-70"
              >
                {deleteLoading ? <Loader2 size={16} className="animate-spin" /> : <Trash2 size={16} />}
                {deleteLoading ? (t('common.delete') + '...') : t('dashboard.deleteBoard')}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// ── Board Card with dropdown menu ──
function BoardCard({ board, onClick, onEdit, onDelete }) {
  const { t } = useLanguage();
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef(null);

  // Close when clicking outside
  useEffect(() => {
    const handler = (e) => {
      if (menuRef.current && !menuRef.current.contains(e.target)) setMenuOpen(false);
    };
    if (menuOpen) document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [menuOpen]);

  return (
    <div
      className="group relative bg-white dark:bg-slate-800 rounded-2xl border border-border-subtle shadow-sm hover:shadow-md hover:border-primary/40 transition-all duration-200 cursor-pointer p-6"
      onClick={onClick}
    >
      {/* Header row */}
      <div className="flex justify-between items-start mb-4">
        <div
          className="w-12 h-12 rounded-xl flex items-center justify-center text-white shadow-sm"
          style={{ backgroundColor: board.color || '#6366f1' }}
        >
          <Layout size={22} />
        </div>

        <div className="flex items-center gap-2">
          <Badge variant="primary">Active</Badge>

          {/* 3-dot menu */}
          <div className="relative" ref={menuRef}>
            <button
              id={`board-menu-${board.id}`}
              onClick={(e) => { e.stopPropagation(); setMenuOpen(o => !o); }}
              className="p-1.5 rounded-lg text-slate-400 hover:text-slate-700 hover:bg-slate-100 transition-colors opacity-0 group-hover:opacity-100"
            >
              <MoreVertical size={16} />
            </button>

            {menuOpen && (
              <div className="absolute right-0 top-full mt-1 w-44 bg-white dark:bg-slate-800 border border-border-subtle rounded-xl shadow-lg z-20 overflow-hidden">
                <button
                  onClick={(e) => { setMenuOpen(false); onEdit(e, board); }}
                  className="w-full flex items-center gap-3 px-4 py-2.5 text-sm font-medium text-text-main hover:bg-slate-50 dark:hover:bg-slate-700/50 transition-colors"
                >
                  <Pencil size={14} className="text-primary" />
                  {t('dashboard.editBoard')}
                </button>
                <div className="h-px bg-border-subtle mx-2" />
                <button
                  onClick={(e) => { setMenuOpen(false); onDelete(e, board); }}
                  className="w-full flex items-center gap-3 px-4 py-2.5 text-sm font-medium text-red-500 hover:bg-red-50 dark:hover:bg-red-500/10 transition-colors"
                >
                  <Trash2 size={14} />
                  {t('dashboard.deleteBoard')}
                </button>
              </div>
            )}
          </div>
        </div>
      </div>

      <h3 className="text-lg font-bold text-text-main mb-1 group-hover:text-primary transition-colors truncate">
        {board.name}
      </h3>
      <p className="text-xs text-text-muted font-medium mb-5">
        {board.updatedAt
          ? t('dashboard.updatedAt', { date: new Date(board.updatedAt).toLocaleDateString() })
          : t('dashboard.createdAt', { date: new Date(board.createdAt).toLocaleDateString() })}
      </p>

      {/* Color accent bar at bottom */}
      <div
        className="absolute bottom-0 left-0 right-0 h-1 rounded-b-2xl opacity-60 group-hover:opacity-100 transition-opacity"
        style={{ backgroundColor: board.color || '#6366f1' }}
      />
    </div>
  );
}

// ── Reusable Board Form Modal (Create & Edit) ──
function BoardFormModal({ title, form, setForm, onSubmit, onClose, loading, submitLabel }) {
  const { t } = useLanguage();
  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-slate-900/50 backdrop-blur-sm" onClick={() => !loading && onClose()} />
      <div className="relative bg-white dark:bg-slate-800 rounded-2xl shadow-2xl w-full max-w-md overflow-hidden animate-in fade-in zoom-in duration-200">

        {/* Modal Header */}
        <div className="flex items-center justify-between px-6 py-5 border-b border-border-subtle">
          <h2 className="text-xl font-bold text-text-main">{title}</h2>
          <button
            onClick={onClose}
            disabled={loading}
            className="p-2 rounded-xl text-slate-400 hover:text-slate-700 hover:bg-slate-100 transition-colors"
          >
            <X size={18} />
          </button>
        </div>

        {/* Form */}
        <form onSubmit={onSubmit} className="p-6 space-y-5">
          {/* Name */}
          <div>
            <label className="block text-sm font-bold text-text-main mb-1.5">
              Board Name <span className="text-red-500">*</span>
            </label>
            <input
              autoFocus
              type="text"
              placeholder="e.g., Marketing Q3"
              maxLength={100}
              value={form.name}
              onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
              className="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-700 border border-border-subtle rounded-xl focus:ring-2 focus:ring-primary/30 focus:border-primary outline-none transition-all text-sm text-text-main"
            />
          </div>

          {/* Color */}
          <div>
            <label className="block text-sm font-bold text-text-main mb-2">Board Color</label>
            <div className="flex flex-wrap gap-2">
              {BOARD_COLORS.map(color => (
                <button
                  key={color}
                  type="button"
                  onClick={() => setForm(f => ({ ...f, color }))}
                  className="w-8 h-8 rounded-lg transition-transform hover:scale-110 focus:outline-none"
                  style={{ backgroundColor: color, boxShadow: form.color === color ? `0 0 0 3px white, 0 0 0 5px ${color}` : 'none' }}
                />
              ))}
            </div>
            {/* Preview */}
            <div
              className="mt-3 h-2 rounded-full transition-colors"
              style={{ backgroundColor: form.color }}
            />
          </div>

          {/* Actions */}
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

function StatCard({ icon, label, value, trend }) {
  return (
    <Card className="flex flex-col" padding="p-5">
      <div className="flex items-center justify-between mb-4">
        <div className="p-2.5 bg-slate-50 dark:bg-slate-800 rounded-xl">{icon}</div>
        <Badge variant="neutral" className="text-[10px]">{trend}</Badge>
      </div>
      <p className="text-sm font-bold text-text-muted uppercase tracking-wider">{label}</p>
      <h3 className="text-3xl font-black text-text-main mt-1">{value}</h3>
    </Card>
  );
}
