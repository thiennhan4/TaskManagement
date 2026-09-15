import PropTypes from 'prop-types';
import { BarChart3, Calendar } from 'lucide-react';

const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'];

export default function TaskActivityChart({ tasks, loading }) {
  const counts = buildMonthlyCounts(tasks);
  const max = Math.max(1, ...counts);

  return (
    <section className="flex min-h-[460px] flex-col overflow-hidden rounded-3xl border border-border-subtle bg-bg-card shadow-premium xl:col-span-2">
      <div className="flex items-center justify-between border-b border-border-subtle p-6">
        <div className="flex items-center gap-3">
          <div className="grid h-11 w-11 place-items-center rounded-full bg-secondary text-white">
            <BarChart3 size={20} />
          </div>
          <div>
            <h2 className="text-xl font-black text-text-main">Task Velocity</h2>
            <p className="mt-0.5 text-xs font-bold uppercase text-text-muted">
              Tasks created over time
            </p>
          </div>
        </div>
        <div className="hidden items-center gap-2 rounded-full border border-border-subtle bg-surface-1 px-3 py-1.5 text-xs font-bold text-text-muted sm:flex">
          <Calendar size={14} />
          <span>Last 6 Months</span>
        </div>
      </div>

      <div className="relative min-h-[320px] flex-1 p-6">
        {loading ? (
          <div className="absolute inset-6 animate-pulse rounded-3xl bg-surface-2" />
        ) : (
          <div className="relative flex h-full flex-col justify-end">
            {/* Background Grid Lines */}
            <div className="pointer-events-none absolute inset-0 flex flex-col justify-between">
              {[0, 1, 2, 3].map((i) => (
                <div key={i} className="flex w-full items-center">
                  <span className="w-8 text-[10px] font-bold text-text-subtle text-right pr-2">
                    {Math.round(max - (max / 3) * i)}
                  </span>
                  <div className="flex-1 border-t border-dashed border-border-subtle/50" />
                </div>
              ))}
            </div>

            {/* Chart Bars */}
            <div className="relative flex h-[240px] items-end justify-around pl-8">
              {counts.map((count, index) => {
                const heightPercent = Math.max(4, Math.round((count / max) * 100));
                return (
                  <div key={MONTHS[index]} className="group relative flex w-full flex-col items-center">
                    {/* Tooltip */}
                    <div className="pointer-events-none absolute -top-12 z-10 opacity-0 transition-opacity group-hover:opacity-100">
                      <div className="flex flex-col items-center rounded-2xl border border-border-strong bg-bg-card px-3 py-1.5 shadow-premium">
                        <span className="text-[10px] font-bold uppercase text-text-muted">{MONTHS[index]}</span>
                        <span className="text-sm font-black text-text-main leading-tight">{count}</span>
                      </div>
                      <div className="-mt-1 mx-auto h-2 w-2 rotate-45 border-b border-r border-border-strong bg-bg-card" />
                    </div>

                    {/* Bar */}
                    <div className="flex h-[240px] w-full max-w-[2.75rem] items-end justify-center">
                      <div
                        className="w-full rounded-t-2xl bg-gradient-to-t from-primary-dark to-primary transition-all duration-300 group-hover:shadow-hover"
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
  loading: PropTypes.bool,
};
