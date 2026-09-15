import PropTypes from 'prop-types';
import { CalendarClock, Clock3 } from 'lucide-react';
import Badge from '@/components/ui/Badge';

export default function UpcomingTasks({ tasks, loading }) {
  return (
    <section className="rounded-2xl border border-border-subtle bg-surface-0 p-5 shadow-sm">
      <div className="mb-5 flex items-center justify-between">
        <div>
          <h2 className="text-xl font-black text-text-main">Upcoming</h2>
          <p className="text-sm font-medium text-text-muted">Deadlines in the next 7 days</p>
        </div>
        <CalendarClock size={20} className="text-primary" />
      </div>

      {loading ? (
        <div className="space-y-3">
          {[1, 2, 3].map((item) => <div key={item} className="h-20 rounded-xl bg-surface-2 animate-pulse" />)}
        </div>
      ) : tasks.length === 0 ? (
        <div className="rounded-2xl border border-dashed border-border-subtle bg-surface-1 p-8 text-center">
          <p className="font-bold text-text-main">No upcoming deadlines</p>
          <p className="mt-1 text-sm text-text-muted">Tasks with due dates will appear here.</p>
        </div>
      ) : (
        <div className="space-y-3">
          {tasks.slice(0, 5).map((task) => (
            <article key={task.id} className="border-l-4 border-primary bg-surface-1 px-4 py-3 rounded-r-xl">
              <div className="flex items-start justify-between gap-3">
                <h3 className="line-clamp-2 text-sm font-bold text-text-main">{task.title}</h3>
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
