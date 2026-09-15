import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { projectApi } from '@/api/projectApi';
import { useLanguage } from '@/context/LanguageContext';
import {
  ChevronLeft, Layout, Settings, Loader2, Users, Clock,
  Globe, Lock, User, UserPlus,
} from 'lucide-react';
import Button from '@/components/ui/Button';
import toast from 'react-hot-toast';
import { formatDistanceToNow } from 'date-fns';
import ProjectTasksBoard from './tabs/ProjectTasksBoard';
import ProjectSettingsTab from './tabs/ProjectSettingsTab';

export default function ProjectDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { t } = useLanguage();

  const [project, setProject] = useState(null);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState('tasks');

  useEffect(() => {
    fetchProject();
  }, [id]);

  const fetchProject = async () => {
    try {
      setLoading(true);
      const res = await projectApi.getProjectById(id);
      if (res.data?.data) {
        setProject(res.data.data);
      } else {
        toast.error('Project not found');
        navigate('/projects');
      }
    } catch (err) {
      console.error(err);
      toast.error('Failed to load project');
      navigate('/projects');
    } finally {
      setLoading(false);
    }
  };

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
            projectId={project.boardId || id}
            projectColor={project.color}
          />
        )}
        {/* PROJ-006: Members tab — only for Team projects */}
        {activeTab === 'members' && !isPersonal && (
          <div className="bg-surface-0 rounded-2xl border border-border-subtle p-8 text-center">
            <div className="w-16 h-16 bg-blue-500/10 rounded-2xl flex items-center justify-center mx-auto mb-4">
              <Users size={28} className="text-blue-500" />
            </div>
            <h3 className="text-lg font-bold text-text-main mb-2">Project Members</h3>
            <p className="text-text-muted text-sm mb-6">
              {project.memberCount || 0} member{project.memberCount !== 1 ? 's' : ''} in this project.
            </p>
            <Button variant="outline" leftIcon={<UserPlus size={15} />}>
              Invite Members
            </Button>
          </div>
        )}
        {activeTab === 'settings' && (
          <ProjectSettingsTab project={project} onUpdate={fetchProject} />
        )}
      </div>
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
