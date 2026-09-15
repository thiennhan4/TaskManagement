import PropTypes from 'prop-types';
import { UsersRound } from 'lucide-react';

export default function TeamWorkload({ tasks, loading }) {
  const workload = buildWorkload(tasks);
  const max = Math.max(1, ...workload.map((item) => item.count));

  return (
    <section className="rounded-2xl border border-border-subtle bg-surface-0 p-5 shadow-sm">
      <div className="mb-5 flex items-center justify-between">
        <div>
          <h2 className="text-xl font-black text-text-main">Team Workload</h2>
          <p className="text-sm font-medium text-text-muted">Assigned active tasks</p>
        </div>
        <UsersRound size={20} className="text-primary" />
      </div>

      {loading ? (
        <div className="space-y-4">
          {[1, 2, 3, 4].map((item) => <div key={item} className="h-10 rounded-xl bg-surface-2 animate-pulse" />)}
        </div>
      ) : workload.length === 0 ? (
        <div className="rounded-2xl border border-dashed border-border-subtle bg-surface-1 p-8 text-center">
          <p className="font-bold text-text-main">No workload data</p>
          <p className="mt-1 text-sm text-text-muted">Assigned tasks will power this view.</p>
        </div>
      ) : (
        <div className="space-y-4">
          {workload.slice(0, 5).map((member) => (
            <div key={member.name}>
              <div className="mb-2 flex items-center justify-between text-sm">
                <div className="flex min-w-0 items-center gap-2">
                  <span className="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-primary/10 text-xs font-black text-primary">
                    {member.name.charAt(0).toUpperCase()}
                  </span>
                  <span className="truncate font-bold text-text-main">{member.name}</span>
                </div>
                <span className="font-black text-text-muted">{member.count}</span>
              </div>
              <div className="h-2 overflow-hidden rounded-full bg-surface-2">
                <div className="h-full rounded-full bg-primary" style={{ width: `${Math.round((member.count / max) * 100)}%` }} />
              </div>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}

function buildWorkload(tasks = []) {
  const active = tasks.filter((task) => task.status !== 'Done');
  const grouped = active.reduce((acc, task) => {
    const name = task.assignedToName || task.ownerName || 'Unassigned';
    acc[name] = (acc[name] || 0) + 1;
    return acc;
  }, {});

  return Object.entries(grouped)
    .map(([name, count]) => ({ name, count }))
    .sort((a, b) => b.count - a.count);
}

TeamWorkload.propTypes = {
  tasks: PropTypes.array,
  loading: PropTypes.bool,
};
