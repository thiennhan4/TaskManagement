import PropTypes from 'prop-types';
import { CalendarClock, Clock3 } from 'lucide-react';
import Badge from '@/components/ui/Badge';
import { Link } from 'react-router-dom';

export default function UpcomingTasks({ tasks, loading }) {
  return (
    <section className="rounded-3xl border border-border-subtle bg-bg-card p-5 shadow-premium">
      <div className="mb-5 flex items-center justify-between gap-3">
        <div>
          <h2 className="text-lg font-black text-text-main">Upcoming</h2>
          <p className="text-sm font-medium text-text-muted">Deadlines in the next 7 days</p>
        </div>
        <div className="grid h-11 w-11 shrink-0 place-items-center rounded-full bg-secondary text-white">
          <CalendarClock size={20} />
        </div>
      </div>

      {loading ? (
        <div className="space-y-3">
          {[1, 2, 3].map((item) => <div key={item} className="h-20 animate-pulse rounded-2xl bg-surface-2" />)}
        </div>
      ) : tasks.length === 0 ? (
        <div className="rounded-3xl border border-dashed border-border-subtle bg-surface-1 p-8 text-center">
          <p className="font-bold text-text-main">No upcoming deadlines</p>
          <p className="mt-1 text-sm text-text-muted">Tasks with due dates will appear here.</p>
        </div>
      ) : (
        <div className="space-y-3">
          {tasks.slice(0, 5).map((task) => (
            <article key={task.id} className="rounded-2xl border border-border-subtle bg-surface-1 px-4 py-3">
              <div className="flex items-start justify-between gap-3">
                <h3 className="min-w-0 text-sm font-bold text-text-main"><Link className="inline-block min-h-10 break-words hover:underline" to={`/tasks/${task.id}`}>{task.title}</Link></h3>
                <Badge variant={task.priority === 'High' || task.priority === 'Critical' ? 'danger' : 'neutral'}>
                  {task.priority || 'Normal'}
                </Badge>
              </div>
              <div className="mt-2 flex items-center gap-2 text-xs font-medium text-text-muted">
                <Clock3 size={13} />
                <span>{formatDate(task.dueDate)}</span>
                {task.listName && <span className="truncate">- {task.listName}</span>}
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}

function formatDate(date) {
  if (!date) return 'No due date';
  return new Date(date).toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
}

UpcomingTasks.propTypes = {
  tasks: PropTypes.array,
  loading: PropTypes.bool,
};
