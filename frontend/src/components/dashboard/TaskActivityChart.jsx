import PropTypes from 'prop-types';
import { BarChart3, Calendar } from 'lucide-react';

const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'];

export default function TaskActivityChart({ tasks, stats, loading }) {
  const counts = buildMonthlyCounts(tasks);
  const max = Math.max(1, ...counts);

  return (
    <section className="flex flex-col rounded-2xl border border-border-subtle bg-surface-0 shadow-sm xl:col-span-2 overflow-hidden">
      <div className="flex items-center justify-between border-b border-border-subtle p-6">
        <div className="flex items-center gap-3">
          <div className="grid h-10 w-10 place-items-center rounded-xl bg-primary/10 text-primary">
            <BarChart3 size={20} />
          </div>
          <div>
            <h2 className="text-lg font-bold text-text-main">Task Velocity</h2>
            <p className="text-xs font-semibold uppercase tracking-wider text-text-muted mt-0.5">
              Tasks created over time
            </p>
          </div>
        </div>
        <div className="hidden sm:flex items-center gap-2 rounded-lg border border-border-subtle bg-surface-1 px-3 py-1.5 text-xs font-bold text-text-muted">
          <Calendar size={14} />
          <span>Last 6 Months</span>
        </div>
      </div>

      <div className="flex-1 p-6 relative min-h-[280px]">
        {loading ? (
          <div className="absolute inset-6 rounded-xl bg-surface-2 animate-pulse" />
        ) : (
          <div className="relative h-full flex flex-col justify-end">
            {/* Background Grid Lines */}
            <div className="absolute inset-0 flex flex-col justify-between pointer-events-none">
              {[0, 1, 2, 3].map((i) => (
                <div key={i} className="flex items-center w-full">
                  <span className="w-8 text-[10px] font-bold text-text-subtle text-right pr-2">
                    {Math.round(max - (max / 3) * i)}
                  </span>
                  <div className="flex-1 border-t border-dashed border-border-subtle/50" />
                </div>
              ))}
            </div>

            {/* Chart Bars */}
            <div className="relative flex h-[200px] items-end justify-around pl-8">
              {counts.map((count, index) => {
                const heightPercent = Math.max(4, Math.round((count / max) * 100));
                return (
                  <div key={MONTHS[index]} className="group relative flex w-full flex-col items-center">
                    {/* Tooltip */}
                    <div className="absolute -top-12 opacity-0 transition-opacity group-hover:opacity-100 pointer-events-none z-10">
                      <div className="flex flex-col items-center rounded-lg border border-border-strong bg-surface-0 px-3 py-1.5 shadow-lg">
                        <span className="text-[10px] font-bold uppercase text-text-muted">{MONTHS[index]}</span>
                        <span className="text-sm font-black text-text-main leading-tight">{count}</span>
                      </div>
                      <div className="mx-auto h-2 w-2 rotate-45 border-b border-r border-border-strong bg-surface-0 -mt-1" />
                    </div>

                    {/* Bar */}
                    <div className="w-full max-w-[2.5rem] flex items-end justify-center h-[200px]">
                      <div
                        className="w-full rounded-t-lg bg-primary/80 transition-all duration-300 group-hover:bg-primary group-hover:shadow-[0_0_12px_var(--color-primary)]"
                        style={{ height: `${heightPercent}%` }}
                      />
                    </div>
                    {/* Axis Label */}
                    <span className="mt-3 text-xs font-bold text-text-muted">{MONTHS[index]}</span>
                  </div>
                );
              })}
            </div>
          </div>
        )}
      </div>
    </section>
  );
}

function buildMonthlyCounts(tasks = []) {
  const now = new Date();
  const year = now.getFullYear();
  const counts = Array(6).fill(0);

  tasks.forEach((task) => {
    const date = task.createdAt ? new Date(task.createdAt) : null;
    if (!date || Number.isNaN(date.getTime()) || date.getFullYear() !== year) return;
    const month = date.getMonth();
    if (month >= 0 && month < 6) counts[month] += 1;
  });

  return counts;
}

TaskActivityChart.propTypes = {
  tasks: PropTypes.array,
  stats: PropTypes.object,
  loading: PropTypes.bool,
};
