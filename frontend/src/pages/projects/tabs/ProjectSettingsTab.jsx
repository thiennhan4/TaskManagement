import React, { useEffect, useState } from 'react';
import { projectApi } from '@/api/projectApi';
import teamApi from '@/api/teamApi';
import { useAuth } from '@/context/AuthContext';
import { Save, Loader2, Globe, Lock, Trash2, Archive } from 'lucide-react';
import Button from '@/components/ui/Button';
import toast from 'react-hot-toast';
import { useNavigate } from 'react-router-dom';

export default function ProjectSettingsTab({ project, onUpdate }) {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [teams, setTeams] = useState([]);
  const [targetTeamId, setTargetTeamId] = useState('');
  const [converting, setConverting] = useState(false);
  const [form, setForm] = useState({
    name: project?.name || '',
    description: project?.description || '',
    status: project?.status || 'Active',
    visibility: project?.visibility || 'Private',
    color: project?.color || '#6366f1',
    emoji: project?.emoji || '📁',
  });
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (project.projectType !== 'Personal' || project.ownerId !== user?.id) return;
    let active = true;
    teamApi.getTeams().then((response) => {
      if (active) setTeams(response.data.data || []);
    }).catch(() => {
      if (active) setTeams([]);
    });
    return () => { active = false; };
  }, [project.projectType, project.ownerId, user?.id]);

  const handleConvert = async () => {
    if (!targetTeamId || !window.confirm('Convert this Personal project to a Team project? Team members may gain access to its boards, tasks, comments, and history. This cannot be undone.')) return;
    setConverting(true);
    try {
      await projectApi.convertToTeam(project.id, targetTeamId);
      toast.success('Project converted to a Team project');
      onUpdate?.();
    } catch (error) {
      toast.error(error?.response?.data?.message || 'Project conversion failed');
    } finally {
      setConverting(false);
    }
  };

  const COLORS = ['#6366f1','#8b5cf6','#ec4899','#ef4444','#f97316','#eab308','#22c55e','#06b6d4','#3b82f6'];
  const EMOJIS = ['📁','🚀','🎯','⚡','🔥','💡','🛠️','📊','🎨','🌟'];
  const STATUSES = ['Active', 'Planning', 'Completed', 'OnHold'];

  const handleSave = async (e) => {
    e.preventDefault();
    if (!form.name.trim()) { toast.error('Project name required'); return; }
    setSaving(true);
    try {
      await projectApi.updateProject(project.id, form);
      toast.success('Project settings saved!');
      onUpdate?.();
    } catch (err) {
      toast.error(err?.response?.data?.message || 'Failed to save');
    } finally {
      setSaving(false);
    }
  };

  const handleArchive = async () => {
    try {
      await projectApi.archiveProject(project.id);
      toast.success('Project archived');
      navigate('/projects');
    } catch { toast.error('Failed to archive'); }
  };

  const handleDelete = async () => {
    if (!confirm(`Are you sure you want to delete "${project.name}"? This cannot be undone.`)) return;
    try {
      await projectApi.deleteProject(project.id);
      toast.success('Project deleted');
      navigate('/projects');
    } catch { toast.error('Failed to delete'); }
  };

  return (
    <div className="max-w-2xl space-y-6 pb-10">
      <form onSubmit={handleSave} className="bg-surface-0 border border-border-subtle rounded-2xl p-6 space-y-5">
        <h2 className="text-base font-black text-text-main">General Settings</h2>

        {/* Emoji + Name row */}
        <div className="flex gap-3">
          <div>
            <label className="block text-xs font-bold text-text-muted uppercase tracking-wider mb-2">Icon</label>
            <div className="flex flex-wrap gap-1.5 w-40">
              {EMOJIS.map(emoji => (
                <button key={emoji} type="button"
                  onClick={() => setForm(f => ({ ...f, emoji }))}
                  className={`w-9 h-9 text-lg rounded-xl transition-all hover:scale-110 ${form.emoji === emoji ? 'ring-2 ring-primary bg-primary/10' : 'bg-surface-2 hover:bg-hover-bg'}`}
                >
                  {emoji}
                </button>
              ))}
            </div>
          </div>
          <div className="flex-1">
            <label className="block text-xs font-bold text-text-muted uppercase tracking-wider mb-2">Project Name *</label>
            <input
              type="text"
              value={form.name}
              onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
              className="w-full px-4 py-2.5 bg-surface-2 border border-border-subtle rounded-xl focus:ring-2 focus:ring-primary/30 focus:border-primary outline-none text-sm text-text-main"
            />
          </div>
        </div>

        {/* Description */}
        <div>
          <label className="block text-xs font-bold text-text-muted uppercase tracking-wider mb-2">Description</label>
          <textarea
            rows={3}
            value={form.description}
            onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
            placeholder="Describe the project..."
            className="w-full px-4 py-2.5 bg-surface-2 border border-border-subtle rounded-xl focus:ring-2 focus:ring-primary/30 focus:border-primary outline-none text-sm resize-none text-text-main"
          />
        </div>

        {/* Status + Visibility */}
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-xs font-bold text-text-muted uppercase tracking-wider mb-2">Status</label>
            <select
              value={form.status}
              onChange={e => setForm(f => ({ ...f, status: e.target.value }))}
              className="w-full px-4 py-2.5 bg-surface-2 border border-border-subtle rounded-xl focus:ring-2 focus:ring-primary/30 focus:border-primary outline-none text-sm text-text-main"
            >
              {STATUSES.map(s => <option key={s} value={s}>{s}</option>)}
            </select>
          </div>
          <div>
            <label className="block text-xs font-bold text-text-muted uppercase tracking-wider mb-2">Visibility</label>
            <div className="flex gap-2">
              {['Public','Private'].map(v => (
                <button key={v} type="button"
                  onClick={() => setForm(f => ({ ...f, visibility: v }))}
                  className={`flex-1 flex items-center justify-center gap-2 py-2.5 rounded-xl border text-sm font-bold transition-all ${
                    form.visibility === v
                      ? 'border-primary bg-primary/10 text-primary'
                      : 'border-border-subtle text-text-muted hover:bg-hover-bg'
                  }`}
                >
                  {v === 'Public' ? <Globe size={14} /> : <Lock size={14} />}
                  {v}
                </button>
              ))}
            </div>
          </div>
        </div>

        {/* Color */}
        <div>
          <label className="block text-xs font-bold text-text-muted uppercase tracking-wider mb-2">Project Color</label>
          <div className="flex gap-2 flex-wrap">
            {COLORS.map(color => (
              <button key={color} type="button"
                onClick={() => setForm(f => ({ ...f, color }))}
                className="w-8 h-8 rounded-xl transition-transform hover:scale-110"
                style={{ backgroundColor: color, boxShadow: form.color === color ? `0 0 0 2px white, 0 0 0 4px ${color}` : 'none', transform: form.color === color ? 'scale(1.15)' : 'scale(1)' }}
              />
            ))}
          </div>
        </div>

        <div className="flex justify-end pt-2">
          <button type="submit" disabled={saving}
            className="flex items-center gap-2 px-6 py-2.5 bg-primary hover:bg-primary/90 text-white font-bold rounded-xl transition-all disabled:opacity-60 text-sm shadow-lg shadow-primary/20"
          >
            {saving ? <Loader2 size={15} className="animate-spin" /> : <Save size={15} />}
            {saving ? 'Saving...' : 'Save Changes'}
          </button>
        </div>
      </form>

      {project.projectType === 'Personal' && project.ownerId === user?.id && (
        <section className="rounded-2xl border border-border-subtle bg-surface-0 p-6 space-y-3">
          <h2 className="text-base font-black text-text-main">Convert to Team</h2>
          <p className="text-sm text-text-muted">Move this project into a Team. Existing boards, tasks, comments, and history stay with the project.</p>
          <label htmlFor="conversion-team" className="block text-sm font-bold text-text-main">Target Team</label>
          <select id="conversion-team" value={targetTeamId} onChange={(event) => setTargetTeamId(event.target.value)} className="w-full rounded-xl border border-border-subtle bg-surface-0 px-4 py-2.5 text-sm text-text-main">
            <option value="">Select a Team</option>
            {teams.map((team) => <option key={team.id} value={team.id}>{team.name}</option>)}
          </select>
          <Button type="button" onClick={handleConvert} disabled={!targetTeamId || converting}>{converting ? 'Converting...' : 'Convert to Team'}</Button>
        </section>
      )}

      {/* Danger Zone */}
      <div className="border border-red-200 dark:border-red-500/20 rounded-2xl p-6 space-y-4 bg-red-50/30 dark:bg-red-500/5">
        <h2 className="text-base font-black text-red-600 dark:text-red-400">Danger Zone</h2>
        <div className="flex items-center justify-between py-3 border-b border-red-200/50 dark:border-red-500/20">
          <div>
            <p className="text-sm font-bold text-text-main">Archive Project</p>
            <p className="text-xs text-text-muted mt-0.5">Hide this project without deleting data.</p>
          </div>
          <button onClick={handleArchive}
            className="flex items-center gap-2 px-4 py-2 border border-amber-400 text-amber-600 font-bold text-sm rounded-xl hover:bg-amber-50 dark:hover:bg-amber-500/10 transition-colors"
          >
            <Archive size={15} /> Archive
          </button>
        </div>
        <div className="flex items-center justify-between py-3">
          <div>
            <p className="text-sm font-bold text-text-main">Delete Project</p>
            <p className="text-xs text-text-muted mt-0.5">Permanently delete this project and all its data.</p>
          </div>
          <button onClick={handleDelete}
            className="flex items-center gap-2 px-4 py-2 border border-red-400 text-red-600 font-bold text-sm rounded-xl hover:bg-red-50 dark:hover:bg-red-500/10 transition-colors"
          >
            <Trash2 size={15} /> Delete
          </button>
        </div>
      </div>
    </div>
  );
}
