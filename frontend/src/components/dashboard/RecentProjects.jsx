import PropTypes from 'prop-types';
import { FolderKanban } from 'lucide-react';
import Button from '@/components/ui/Button';
import Badge from '@/components/ui/Badge';

export default function RecentProjects({ projects, loading, onOpenProjects }) {
  return (
    <section className="rounded-3xl border border-border-subtle bg-bg-card p-5 shadow-premium">
      <div className="mb-5 flex items-center justify-between gap-3">
        <div>
          <h2 className="text-xl font-black text-text-main">Recent Projects</h2>
          <p className="text-sm font-medium text-text-muted">Active project spaces and progress signals</p>
        </div>
        <Button type="button" variant="ghost" size="sm" onClick={onOpenProjects}>See All</Button>
      </div>

      {loading ? (
        <div className="grid gap-4 md:grid-cols-2">
          {[1, 2].map((item) => <div key={item} className="h-44 animate-pulse rounded-3xl bg-surface-2" />)}
        </div>
      ) : projects.length === 0 ? (
        <div className="rounded-3xl border border-dashed border-border-subtle bg-surface-1 p-8 text-center">
          <FolderKanban className="mx-auto text-text-subtle" size={28} />
          <p className="mt-3 font-bold text-text-main">No projects yet</p>
          <p className="mt-1 text-sm text-text-muted">Create a project to track higher-level work.</p>
        </div>
      ) : (
        <div className="grid gap-4 md:grid-cols-2">
          {projects.slice(0, 2).map((project) => {
            const progress = clamp(project.progress ?? inferProjectProgress(project));
            return (
              <article key={project.id} className="rounded-3xl border border-border-subtle bg-surface-1 p-5">
                <div className="mb-5 flex items-start justify-between gap-3">
                  <div className="flex min-w-0 items-center gap-3">
                    <span
                      className="h-3 w-3 rounded-full"
                      style={{ backgroundColor: project.color || '#6366f1' }}
                    />
                    <div className="min-w-0">
                      <h3 className="truncate text-base font-black text-text-main">{project.name}</h3>
                      <p className="text-xs font-medium text-text-muted">{project.slug ? `/${project.slug}` : 'Project'}</p>
                    </div>
                  </div>
                  <Badge variant={project.status === 'Completed' ? 'success' : 'primary'}>{project.status || 'Active'}</Badge>
                </div>

                <div className="mb-3 flex items-center justify-between text-xs font-bold text-text-muted">
                  <span>Progress</span>
                  <span>{progress}%</span>
                </div>
                <div className="h-2 overflow-hidden rounded-full bg-surface-2">
                  <div className="h-full rounded-full bg-primary" style={{ width: `${progress}%` }} />
                </div>

                <div className="mt-5 flex items-center justify-between text-xs font-bold text-text-muted">
                  <span>{project.boardCount ?? 0} boards</span>
                  <span>{project.memberCount ?? 0} members</span>
                </div>
              </article>
            );
          })}
        </div>
      )}
    </section>
  );
}

function inferProjectProgress(project) {
  if (project.status === 'Completed') return 100;
  if (project.status === 'Planning') return 20;
  if (project.status === 'OnHold') return 45;
  return 60;
}

function clamp(value) {
  return Math.max(0, Math.min(100, Number(value) || 0));
}

RecentProjects.propTypes = {
  projects: PropTypes.array,
  loading: PropTypes.bool,
  onOpenProjects: PropTypes.func.isRequired,
};
