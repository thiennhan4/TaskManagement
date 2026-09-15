import PropTypes from 'prop-types';
import { Activity, Clock, CheckCircle2, FileText, ArrowUpRight } from 'lucide-react';
import { formatDistanceToNow } from 'date-fns';

export default function RecentActivity({ activities = [], loading }) {
  return (
    <section className="flex flex-col rounded-2xl border border-border-subtle bg-surface-0 shadow-sm xl:col-span-1 h-[375px]">
      <div className="flex items-center justify-between border-b border-border-subtle p-6">
        <div className="flex items-center gap-3">
          <div className="grid h-10 w-10 place-items-center rounded-xl bg-primary/10 text-primary">
            <Activity size={20} />
          </div>
          <div>
            <h2 className="text-lg font-bold text-text-main">Recent Activity</h2>
            <p className="text-xs font-semibold uppercase tracking-wider text-text-muted mt-0.5">
              Your latest updates
            </p>
          </div>
        </div>
      </div>

      <div className="flex-1 overflow-y-auto p-6 scrollbar-hide">
        {loading ? (
          <div className="flex h-full flex-col gap-6">
            {[1, 2, 3, 4].map((i) => (
              <div key={i} className="flex gap-4">
                <div className="h-10 w-10 shrink-0 rounded-full bg-surface-2 animate-pulse" />
                <div className="flex-1 space-y-2 py-1">
                  <div className="h-4 w-3/4 rounded bg-surface-2 animate-pulse" />
                  <div className="h-3 w-1/2 rounded bg-surface-2 animate-pulse" />
                </div>
              </div>
            ))}
          </div>
        ) : activities?.length > 0 ? (
          <div className="relative border-l border-border-subtle/50 ml-5 space-y-8 pb-4">
            {activities.slice(0, 5).map((activity) => (
              <ActivityItem key={activity.id || Math.random()} activity={activity} />
            ))}
          </div>
        ) : (
          <div className="flex h-full flex-col items-center justify-center text-center">
            <div className="mb-3 grid h-12 w-12 place-items-center rounded-full bg-surface-1 text-text-subtle">
              <Clock size={24} />
            </div>
            <h3 className="text-sm font-bold text-text-main">No recent activity</h3>
            <p className="mt-1 max-w-[200px] text-xs font-medium text-text-muted">
              Start working on tasks to see updates here.
            </p>
          </div>
        )}
      </div>
    </section>
  );
}

function ActivityItem({ activity }) {
  const isDone = activity.status === 'Done' || activity.status === 'Completed';
  const Icon = isDone ? CheckCircle2 : FileText;
  const toneClass = isDone ? 'bg-emerald-500/10 text-emerald-500' : 'bg-primary/10 text-primary';
  const label = isDone ? 'Completed task' : 'Created task';
  
  let timeAgo = 'Just now';
  if (activity.updatedAt || activity.createdAt) {
    try {
      timeAgo = formatDistanceToNow(new Date(activity.updatedAt || activity.createdAt), { addSuffix: true });
    } catch {
      // fallback
    }
  }

  return (
    <div className="relative pl-6">
      <span className={`absolute -left-[18px] top-1 flex h-9 w-9 items-center justify-center rounded-full border-[3px] border-surface-0 ${toneClass}`}>
        <Icon size={14} />
      </span>
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-sm font-medium text-text-main leading-snug">
            <span className="font-semibold text-text-muted">{label}</span>{' '}
            <span className="font-bold">{activity.title || 'Untitled task'}</span>
          </p>
          <div className="mt-1 flex items-center gap-2">
            <span className="text-[11px] font-semibold text-text-subtle">{timeAgo}</span>
            {activity.projectName && (
              <>
                <span className="text-[10px] text-text-subtle">•</span>
                <span className="text-[11px] font-semibold text-text-muted">{activity.projectName}</span>
              </>
            )}
          </div>
        </div>
        <button type="button" className="shrink-0 text-text-subtle transition-colors hover:text-primary">
          <ArrowUpRight size={16} />
        </button>
      </div>
    </div>
  );
}

ActivityItem.propTypes = {
  activity: PropTypes.object.isRequired,
};

RecentActivity.propTypes = {
  activities: PropTypes.array,
  loading: PropTypes.bool,
};
