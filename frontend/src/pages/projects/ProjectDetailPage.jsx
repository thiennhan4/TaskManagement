import React, { useCallback, useState, useEffect, useRef } from 'react';
import { useParams, useNavigate, useSearchParams } from 'react-router-dom';
import { projectApi } from '@/api/projectApi';
import { dashboardApi } from '@/api/dashboardApi';
import { projectMemberApi } from '@/api/projectMemberApi';
import teamApi from '@/api/teamApi';
import InviteMemberModal from '@/components/projects/InviteMemberModal';
import { useAuth } from '@/context/authState';
import {
  ChevronLeft, Layout, Settings, Loader2, Users, Clock, Activity,
  Globe, Lock, User, UserPlus,
} from 'lucide-react';
import Button from '@/components/ui/Button';
import PageControls from '@/components/ui/PageControls';
import toast from 'react-hot-toast';
import { formatDistanceToNow } from 'date-fns';
import ProjectTasksBoard from './tabs/ProjectTasksBoard';
import ProjectSettingsTab from './tabs/ProjectSettingsTab';
import RecentActivity from '@/components/dashboard/RecentActivity';

export default function ProjectDetailPage() {
  const { id } = useParams();
  return <ProjectDetailContent key={id} />;
}

function ProjectDetailContent() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const requestedBoardId = searchParams.get('boardId');
  const { user } = useAuth();

  const [project, setProject] = useState(null);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState('tasks');
  const [members, setMembers] = useState([]);
  const [memberPage, setMemberPage] = useState(1);
  const [memberPageInfo, setMemberPageInfo] = useState(null);
  const [memberError, setMemberError] = useState('');
  const [teamRole, setTeamRole] = useState(null);
  const [showInvite, setShowInvite] = useState(false);
  const [activity, setActivity] = useState([]);
  const [activityPage, setActivityPage] = useState(1);
  const [activityTotalPages, setActivityTotalPages] = useState(0);
  const [activityLoading, setActivityLoading] = useState(false);
  const [activityError, setActivityError] = useState('');
  const requestVersion = useRef(0);

  const fetchProject = useCallback(async () => {
    const version = ++requestVersion.current;
    try {
      setLoading(true);
      const res = await projectApi.getProjectById(id);
      if (version !== requestVersion.current) return;
      if (res.data?.data) {
        setProject(res.data.data);
        if (res.data.data.projectType === 'Team') {
          const [membersResult, teamsResult] = await Promise.allSettled([
            projectMemberApi.getPage(id, memberPage), teamApi.getTeam(res.data.data.workspaceId),
          ]);
          if (version !== requestVersion.current) return;
          setMembers(membersResult.status === 'fulfilled' ? membersResult.value.data.data.items : []);
          setMemberPageInfo(membersResult.status === 'fulfilled' ? membersResult.value.data.data : null);
          setMemberError(membersResult.status === 'rejected' ? 'Project members could not be loaded.' : '');
          setTeamRole(teamsResult.status === 'fulfilled' ? teamsResult.value.data.data.currentUserRole : null);
        } else {
          setMembers([]);
          setTeamRole(null);
        }
      } else {
        toast.error('Project not found');
        navigate('/projects');
      }
    } catch (err) {
      if (version !== requestVersion.current) return;
      console.error(err);
      toast.error('Failed to load project');
      navigate('/projects');
    } finally {
      if (version === requestVersion.current) setLoading(false);
    }
  }, [id, navigate, memberPage]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      fetchProject();
    }, 0);

    return () => { window.clearTimeout(timeoutId); requestVersion.current++; };
  }, [fetchProject]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => { setActivityPage(1); }, 0);
    return () => window.clearTimeout(timeoutId);
  }, [id]);

  useEffect(() => {
    if (activeTab !== 'activity' || !project) return;
    let active = true;
    const timeoutId = window.setTimeout(async () => {
      setActivityLoading(true);
      setActivity([]);
      setActivityError('');
      try {
        const response = await dashboardApi.getProjectActivity(id, activityPage, 5);
        if (active) {
          setActivity(response.data.data?.items || []);
          setActivityTotalPages(response.data.data?.totalPages || 0);
        }
      } catch {
        if (active) setActivityError('Project activity could not be loaded.');
      } finally {
        if (active) setActivityLoading(false);
      }
    }, 0);
    return () => { active = false; window.clearTimeout(timeoutId); };
  }, [activeTab, id, project, activityPage]);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-full min-h-[400px]">
        <div className="flex flex-col items-center gap-4">
          <div className="relative">
            <div className="w-16 h-16 rounded-2xl bg-primary/10 flex items-center justify-center">
              <Loader2 className="animate-spin text-primary" size={28} />
            </div>
            <div className="absolute -inset-1 rounded-2xl border-2 border-primary/20 animate-pulse" />
          </div>
          <p className="text-sm text-text-muted font-medium">Loading project...</p>
        </div>
      </div>
    );
  }

  if (!project) return null;

  // PROJ-006: Personal projects hide Members/Invite tabs
  // API returns projectType as string (e.g. "Personal" or "Team")
  const isPersonal = project.projectType === 'Personal' || project.type === 'Personal';
  const memberRole = project.currentUserRole;
  const canManage = isPersonal || project.ownerId === user?.id ||
    memberRole === 'Owner' || memberRole === 'Admin' || teamRole === 'Owner' || teamRole === 'Manager';
  const readOnly = !isPersonal && memberRole === 'Guest';

  const statusColor = {
    Active: 'bg-emerald-500',
    Planning: 'bg-blue-500',
    Completed: 'bg-violet-500',
    OnHold: 'bg-amber-500',
  }[project.status] || 'bg-slate-400';

  return (
    <div className="h-full flex flex-col">
      {/* Header */}
      <div className="shrink-0 mb-1">
        {/* Breadcrumb */}
        <div className="flex items-center gap-2 text-xs text-text-muted mb-4">
          <button
            onClick={() => navigate('/projects')}
            className="flex items-center gap-1 hover:text-primary transition-colors font-medium"
          >
            <ChevronLeft size={14} />
            Projects
          </button>
          <span>/</span>
          <span className="text-text-main font-semibold truncate">{project.name}</span>
        </div>

        {/* Project Header Card */}
        <div
          className="relative rounded-2xl border border-border-subtle p-5 mb-4 overflow-hidden"
          style={{ borderLeftColor: project.color || '#6366f1', borderLeftWidth: 4 }}
        >
          {/* Background glow */}
          <div
            className="absolute inset-0 opacity-5 pointer-events-none"
            style={{ background: `radial-gradient(ellipse at top left, ${project.color || '#6366f1'}, transparent 60%)` }}
          />

          <div className="relative flex flex-col md:flex-row md:items-center justify-between gap-4">
            <div className="flex items-center gap-4">
              <div
                className="w-14 h-14 rounded-2xl flex items-center justify-center text-2xl shrink-0 border"
                style={{ background: `${project.color || '#6366f1'}15`, borderColor: `${project.color || '#6366f1'}30` }}
              >
                {project.emoji || '📁'}
              </div>
              <div>
                <div className="flex items-center gap-2.5 flex-wrap">
                  <h1 className="text-2xl font-black text-text-main tracking-tight">{project.name}</h1>
                  <span className={`flex items-center gap-1 text-[10px] font-black px-2.5 py-1 rounded-full text-white uppercase tracking-wider ${statusColor}`}>
                    <span className="w-1.5 h-1.5 rounded-full bg-white/60 inline-block" />
                    {project.status}
                  </span>
                  {/* PROJ-006: Project type badge */}
                  <span className={`flex items-center gap-1 text-[10px] font-black px-2.5 py-1 rounded-full uppercase tracking-wider ${
                    isPersonal
                      ? 'bg-violet-100 text-violet-700 dark:bg-violet-900/30 dark:text-violet-300'
                      : 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300'
                  }`}>
                    {isPersonal ? <User size={10} /> : <Users size={10} />}
                    {isPersonal ? 'Personal' : 'Team'}
                  </span>
                </div>
                <p className="text-sm text-text-muted mt-1 max-w-xl line-clamp-1">
                  {project.description || 'No description provided.'}
                </p>
                <div className="flex items-center gap-4 mt-2">
                  {!isPersonal && (
                    <div className="flex items-center gap-1.5 text-xs text-text-muted">
                      <Users size={12} />
                      <span>{project.memberCount || 0} members</span>
                    </div>
                  )}
                  <div className="flex items-center gap-1.5 text-xs text-text-muted">
                    <Clock size={12} />
                    <span>Created {formatDistanceToNow(new Date(project.createdAt), { addSuffix: true })}</span>
                  </div>
                  <div className="flex items-center gap-1.5 text-xs text-text-muted">
                    {project.visibility === 'Public' ? <Globe size={12} /> : <Lock size={12} />}
                    <span>{project.visibility || 'Private'}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Tab Navigation */}
        <div className="flex items-center gap-1 border-b border-border-subtle overflow-x-auto no-scrollbar">
          <TabButton
            active={activeTab === 'tasks'}
            onClick={() => setActiveTab('tasks')}
            icon={<Layout size={15} />}
            label="Tasks Board"
            count={project.boardCount}
          />
          {/* PROJ-006: Only show Members tab for Team projects */}
          {!isPersonal && (
            <TabButton
              active={activeTab === 'members'}
              onClick={() => setActiveTab('members')}
              icon={<Users size={15} />}
              label="Members"
              count={project.memberCount}
            />
          )}
          <TabButton
            active={activeTab === 'activity'}
            onClick={() => setActiveTab('activity')}
            icon={<Activity size={15} />}
            label="Activity"
          />
          <TabButton
            active={activeTab === 'settings'}
            onClick={() => setActiveTab('settings')}
            icon={<Settings size={15} />}
            label="Settings"
          />
        </div>
      </div>

      {/* Tab Content */}
      <div className="flex-1 overflow-hidden pt-4">
        {activeTab === 'tasks' && (
          <ProjectTasksBoard
            projectId={id}
            requestedBoardId={requestedBoardId}
            canEditTasks={!readOnly}
            canManageBoard={canManage}
          />
        )}
        {/* PROJ-006: Members tab — only for Team projects */}
        {activeTab === 'members' && !isPersonal && (
          <div className="bg-surface-0 rounded-2xl border border-border-subtle p-6">
            <div className="mb-5 flex items-center justify-between gap-3">
              <h3 className="text-lg font-bold text-text-main">Project Members</h3>
              {canManage && <Button variant="outline" leftIcon={<UserPlus size={15} />} onClick={() => setShowInvite(true)}>Invite Members</Button>}
            </div>
            <PageControls page={memberPageInfo} loading={loading} onPage={setMemberPage} />
            {memberError ? <p role="alert">{memberError}</p> : members.length === 0 ? <p className="text-sm text-text-muted">No project members yet.</p> : (
              <ul className="space-y-2">
                {members.map((member) => (
                  <li key={member.id} className="flex items-center justify-between rounded-xl border border-border-subtle bg-surface-1 px-4 py-3">
                    <span className="min-w-0 truncate text-sm font-semibold text-text-main">{member.fullName || member.email}</span>
                    <span className="text-xs font-semibold text-text-muted">{member.role}</span>
                  </li>
                ))}
              </ul>
            )}
          </div>
        )}
        {activeTab === 'activity' && (
          <div className="space-y-3">
            {activityError && <p role="alert" className="rounded-xl border border-danger/30 bg-danger/10 p-4 text-sm text-text-main">{activityError}</p>}
            <RecentActivity activities={activity} loading={activityLoading} />
            {activityTotalPages > 1 && (
              <div className="flex items-center justify-end gap-3 text-sm text-text-muted">
                <Button variant="outline" size="sm" disabled={activityPage <= 1 || activityLoading} onClick={() => setActivityPage((page) => page - 1)}>Previous</Button>
                <span>Page {activityPage} of {activityTotalPages}</span>
                <Button variant="outline" size="sm" disabled={activityPage >= activityTotalPages || activityLoading} onClick={() => setActivityPage((page) => page + 1)}>Next</Button>
              </div>
            )}
          </div>
        )}
        {activeTab === 'settings' && canManage && (
          <ProjectSettingsTab project={project} onUpdate={fetchProject} />
        )}
        {activeTab === 'settings' && !canManage && <p className="p-6 text-sm text-text-muted">You can view this project but cannot manage its settings.</p>}
      </div>
      {!isPersonal && <InviteMemberModal isOpen={showInvite} onClose={() => { setShowInvite(false); fetchProject(); }} projectId={id} projectName={project.name} />}
    </div>
  );
}

function TabButton({ active, onClick, icon, label, count }) {
  return (
    <button
      onClick={onClick}
      className={`flex items-center gap-2 pb-3 px-3 border-b-2 font-bold text-sm transition-all whitespace-nowrap ${
        active
          ? 'border-primary text-primary'
          : 'border-transparent text-text-muted hover:text-text-main hover:border-border-strong'
      }`}
    >
      {icon}
      {label}
      {count > 0 && (
        <span className={`text-[10px] font-black px-1.5 py-0.5 rounded-full ${
          active ? 'bg-primary/10 text-primary' : 'bg-surface-2 text-text-subtle'
        }`}>
          {count}
        </span>
      )}
    </button>
  );
}
