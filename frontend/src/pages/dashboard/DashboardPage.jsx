import Modal from '@/components/ui/Modal';
import Input from '@/components/ui/Input';
import { Link } from 'react-router-dom';
import { useCallback, useEffect, useRef, useState } from 'react';
import { useAuth } from '@/context/authState';
import { useNavigate } from 'react-router-dom';
import { boardApi } from '@/api/boardApi';
import { dashboardApi } from '@/api/dashboardApi';
import { projectApi } from '@/api/projectApi';
import teamApi from '@/api/teamApi';
import Select from '@/components/ui/Select';
import { useNotification } from '@/context/NotificationContext';
import { Trash2, X, Loader2 } from 'lucide-react';
import Button from '@/components/ui/Button';
import DashboardHeader from '@/components/dashboard/DashboardHeader';
import DashboardStats from '@/components/dashboard/DashboardStats';
import TaskActivityChart from '@/components/dashboard/TaskActivityChart';
import RecentActivity from '@/components/dashboard/RecentActivity';
import UpcomingTasks from '@/components/dashboard/UpcomingTasks';
import RecentProjects from '@/components/dashboard/RecentProjects';
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
  const { hubConnection, reconnectVersion } = useNotification();
  const { t } = useLanguage();
  const navigate = useNavigate();

  const [boards, setBoards] = useState([]);
  const [projects, setProjects] = useState([]);
  const [activity, setActivity] = useState([]);
  const [velocity, setVelocity] = useState([]);
  const [velocityLoading, setVelocityLoading] = useState(true);
  const [velocityError, setVelocityError] = useState('');
  const velocityVersion = useRef(0);
  const [velocityTimeframe, setVelocityTimeframe] = useState('SixMonths');
  const [scope, setScope] = useState('Personal');
  const [teams, setTeams] = useState([]);
  const [teamLoadError, setTeamLoadError] = useState(false);
  const [teamId, setTeamId] = useState('');
  const [dashboardError, setDashboardError] = useState('');
  const [upcomingTasks, setUpcomingTasks] = useState([]);
  const [stats, setStats] = useState(EMPTY_STATS);
  const [loading, setLoading] = useState(true);
  const requestVersion = useRef(0);

  const [editingBoard, setEditingBoard] = useState(null);
  const [editForm, setEditForm] = useState({ name: '', color: '' });
  const [editLoading, setEditLoading] = useState(false);

  const [deletingBoard, setDeletingBoard] = useState(null);
  const [deleteLoading, setDeleteLoading] = useState(false);

  const clearScopedData = () => {
    requestVersion.current += 1;
    velocityVersion.current += 1;
    setStats(EMPTY_STATS);
    setVelocity([]);
    setActivity([]);
    setUpcomingTasks([]);
    setProjects([]);
    setBoards([]);
    setLoading(true);
    setDashboardError('');
  };

  useEffect(() => {
    let active = true;
    teamApi.getTeams().then((response) => {
      if (active) {
        setTeams(response.data.data || []);
        setTeamLoadError(false);
      }
    }).catch(() => {
      if (active) {
        setTeams([]);
        setTeamLoadError(true);
      }
    });
    return () => { active = false; };
  }, []);

  const fetchData = useCallback(async (isCurrent = () => true) => {
    const currentRequest = ++requestVersion.current;
    setLoading(true);
    setStats(EMPTY_STATS);
    setActivity([]);
    setUpcomingTasks([]);
    setDashboardError('');

    if (scope === 'Team' && !teamId) {
      setLoading(false);
      return;
    }

    const [boardsRes, statsRes, activityRes, upcomingRes, projectsRes] = await Promise.allSettled([
      scope === 'Personal' ? boardApi.getBoards() : Promise.resolve(null),
      dashboardApi.getStats(scope, teamId || null),
      dashboardApi.getActivity(scope, teamId || null),
      dashboardApi.getUpcomingTasks(scope, teamId || null),
      scope === 'Personal' ? projectApi.getProjects() : Promise.resolve(null),
    ]);

    if (!isCurrent() || currentRequest !== requestVersion.current) return;

    const personalProjects = (projectsRes.status === 'fulfilled' ? projectsRes.value?.data?.data || [] : [])
      .filter((project) => project.projectType === 'Personal' && project.ownerId === user?.id);
    const personalProjectIds = new Set(personalProjects.map((project) => project.id));
    if (boardsRes.status === 'fulfilled') {
      setBoards((boardsRes.value?.data?.data || []).filter((board) => personalProjectIds.has(board.projectId)));
    }

    if (statsRes.status === 'fulfilled') {
      setStats({ ...EMPTY_STATS, ...(statsRes.value.data.data || {}) });
    }

    if (activityRes.status === 'fulfilled') {
      setActivity(activityRes.value.data.data?.items || []);
    }

    if (upcomingRes.status === 'fulfilled') {
      setUpcomingTasks(upcomingRes.value.data.data?.items || []);
    }

    if (projectsRes.status === 'fulfilled') setProjects(personalProjects);

    const hasError = [boardsRes, statsRes, activityRes, upcomingRes, projectsRes]
      .some((result) => result.status === 'rejected');
    if (hasError) {
      setDashboardError('Some dashboard data could not be loaded. Please try again.');
    }

    setLoading(false);
  }, [scope, teamId, user?.id]);

  useEffect(() => {
    let active = true;
    const timeoutId = window.setTimeout(() => {
      fetchData(() => active);
    }, 0);

    return () => { active = false; requestVersion.current++; window.clearTimeout(timeoutId); };
  }, [fetchData]);


  const fetchVelocity = useCallback(async () => {
    const request = ++velocityVersion.current;
    setVelocityLoading(true); setVelocityError('');
    if (scope === 'Team' && !teamId) { setVelocityLoading(false); return; }
    try {
      const response = await dashboardApi.getVelocity(velocityTimeframe, scope, teamId || null);
      if (request === velocityVersion.current) setVelocity(response.data.data || []);
    } catch {
      if (request === velocityVersion.current) setVelocityError('Velocity could not be loaded.');
    } finally { if (request === velocityVersion.current) setVelocityLoading(false); }
  }, [velocityTimeframe, scope, teamId]);
  useEffect(() => {
    const timer = window.setTimeout(fetchVelocity, 0);
    return () => { velocityVersion.current++; window.clearTimeout(timer); };
  }, [fetchVelocity]);

  const personalProjectIds = projects
    .filter((project) => project.projectType === 'Personal' && project.ownerId === user?.id)
    .map((project) => project.id).sort().join('|');


  useEffect(() => {
    if (!hubConnection) return;
    let active = true;
    let timer;
    let eventVersion = 0;
    const affected = new Set();
    const flush = async () => {
      timer = null;
      const kinds = new Set(affected); affected.clear();
      const version = ++eventVersion;
      const scopeVersion = requestVersion.current;
      if (kinds.has('project')) {
        fetchData(); fetchVelocity();
        return;
      }
      const current = () => active && version === eventVersion && scopeVersion === requestVersion.current;
      const work = [dashboardApi.getActivity(scope, teamId || null).then(response => {
        if (current()) setActivity(response.data.data?.items || []);
      })];
      if (kinds.has('task')) {
        work.push(dashboardApi.getStats(scope, teamId || null).then(response => { if (current()) setStats({ ...EMPTY_STATS, ...response.data.data }); }));
        work.push(dashboardApi.getUpcomingTasks(scope, teamId || null).then(response => { if (current()) setUpcomingTasks(response.data.data?.items || []); }));
        fetchVelocity();
      }
      const results = await Promise.allSettled(work);
      if (current() && results.some(result => result.status === 'rejected')) setDashboardError('Some dashboard data could not be loaded. Please try again.');
    };
    const handleActivity = event => {
      if (!event?.projectId || (scope === 'Team' && !teamId)) return;
      if (scope === 'Personal' && !personalProjectIds.split('|').includes(event.projectId)) return;
      const type = event.eventType || '';
      affected.add(type.startsWith('Comment') ? 'activity' : type.startsWith('Task') ? 'task' : 'project');
      // A burst shares one bounded window, with no indefinite debounce starvation.
      if (!timer) timer = window.setTimeout(flush, 100);
    };
    hubConnection.on('ProjectActivity', handleActivity);
    return () => { active = false; window.clearTimeout(timer); hubConnection.off('ProjectActivity', handleActivity); };
  }, [hubConnection, scope, teamId, personalProjectIds, fetchData, fetchVelocity]);


  useEffect(() => {
    if (!hubConnection) return;
    const projectIds = scope === 'Personal' ? personalProjectIds.split('|').filter(Boolean) : [];
    if (scope === 'Team' && teamId) hubConnection.invoke('JoinTeam', teamId).catch(() => {});
    projectIds.forEach((projectId) => hubConnection.invoke('JoinProject', projectId).catch(() => {}));
    return () => {
      if (scope === 'Team' && teamId) hubConnection.invoke('LeaveTeam', teamId).catch(() => {});
      projectIds.forEach((projectId) => hubConnection.invoke('LeaveProject', projectId).catch(() => {}));
    };
  }, [hubConnection, scope, teamId, personalProjectIds]);

  useEffect(() => {
    if (!hubConnection || reconnectVersion === 0) return;
    if (scope === 'Team' && teamId) hubConnection.invoke('JoinTeam', teamId).catch(() => {});
    if (scope === 'Personal') {
      personalProjectIds.split('|').filter(Boolean)
        .forEach((projectId) => hubConnection.invoke('JoinProject', projectId).catch(() => {}));
    }
  }, [hubConnection, reconnectVersion, scope, teamId, personalProjectIds]);

  const openEditModal = (event, board) => {
    event.stopPropagation();
    setEditingBoard(board);
    setEditForm({ name: board.name, color: board.color || '#6366f1' });
  };

  const handleUpdateBoard = async (event) => {
    event.preventDefault();
    if (editLoading) return;
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
    if (deleteLoading) return;
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
      <p className="text-sm text-text-muted">Dashboard metrics cover project tasks in the selected scope. <Link to="/my-tasks" className="underline text-text-main">My Tasks</Link> includes standalone and personal tasks.</p>
      <DashboardHeader
        user={user}
      />

      <div className="flex flex-col gap-3 rounded-3xl border border-border-subtle bg-bg-card p-4 sm:flex-row sm:items-end">
        <Select label="Dashboard scope" value={scope} onChange={(event) => { clearScopedData(); setScope(event.target.value); setTeamId(''); }} aria-label="Dashboard scope">
          <option value="Personal">Personal</option>
          <option value="Team">Team</option>
        </Select>
        {scope === 'Team' && (
          <Select label="Team" value={teamId} onChange={(event) => { clearScopedData(); setTeamId(event.target.value); }} aria-label="Team">
            <option value="">Select a team</option>
            {teams.map((team) => <option key={team.id} value={team.id}>{team.name}</option>)}
          </Select>
        )}
      </div>
      {scope === 'Team' && teamLoadError && (
        <div role="alert" className="rounded-2xl border border-danger/30 bg-danger/10 p-4 text-sm text-text-main">
          Teams could not be loaded. Reload the page to try again.
        </div>
      )}
      {velocityError && <div role="alert">{velocityError} <Button onClick={fetchVelocity}>Retry velocity</Button></div>}
      {dashboardError && (
        <div role="alert" className="rounded-2xl border border-danger/30 bg-danger/10 p-4 text-sm text-text-main">
          {dashboardError} <Button type="button" onClick={() => fetchData()}>Retry</Button>
        </div>
      )}
      {scope === 'Team' && !teamId ? (
        <div className="rounded-3xl border border-border-subtle bg-bg-card p-8 text-center text-text-muted">Select a team to view its dashboard.</div>
      ) : (
        <>

      <DashboardStats stats={stats} loading={loading} />

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-3">
        <TaskActivityChart points={velocity} timeframe={velocityTimeframe} onTimeframeChange={(value) => { velocityVersion.current++; setVelocity([]); setVelocityLoading(true); setVelocityTimeframe(value); }} loading={velocityLoading} />
        <div className="grid gap-6">
          <RecentActivity activities={activity} loading={loading} />
          <UpcomingTasks tasks={upcomingTasks} loading={loading} />
        </div>
      </div>

        </>
      )}

      {scope === 'Personal' && (
      <>
      <div className="grid grid-cols-1 gap-6">
        <RecentProjects
          projects={projects}
          loading={loading}
          onOpenProjects={() => navigate('/projects')}
        />
      </div>

      <RecentBoards
        boards={boards}
        loading={loading}
        onBoardClick={(boardId) => navigate(`/boards/${boardId}`)}
        onCreateBoard={() => navigate('/projects')}
        createLabel="Open Projects"
        onEdit={openEditModal}
        onDelete={openDeleteConfirm}
      />
      </>
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
        <Modal isOpen title={t('dashboard.deleteBoard')} closeDisabled={deleteLoading} onClose={() => setDeletingBoard(null)} maxWidth="max-w-md">
          <div className="p-6">
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
              <Button variant="danger" onClick={handleDeleteBoard}
                disabled={deleteLoading}
                className="flex flex-1 items-center justify-center gap-2 rounded-xl bg-rose-500 px-4 py-2.5 font-bold text-white transition-colors hover:bg-rose-600 disabled:opacity-70"
              >
                {deleteLoading ? <Loader2 size={16} className="animate-spin" /> : <Trash2 size={16} />}
                {deleteLoading ? `${t('common.delete')}...` : t('dashboard.deleteBoard')}
              </Button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  );
}

function BoardFormModal({ title, form, setForm, onSubmit, onClose, loading, submitLabel }) {
  const { t } = useLanguage();

  return (
    <Modal isOpen title={title} onClose={onClose} closeDisabled={loading} maxWidth="max-w-md">
        <form onSubmit={onSubmit} className="space-y-5 p-6">
          <div>
            <Input label="Board name" required disabled={loading}
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
    </Modal>
  );
}
