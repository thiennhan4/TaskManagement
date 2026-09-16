import PropTypes from 'prop-types';
import { BarChart3, Calendar } from 'lucide-react';

const TIMEFRAMES = [
  { value: 'Week', label: 'Week' },
  { value: 'Month', label: 'Month' },
  { value: 'SixMonths', label: 'Six Months' },
  { value: 'Year', label: 'Year' },
];

const formatBucket = (value, timeframe) => new Intl.DateTimeFormat(undefined, {
  ...(timeframe === 'Week' || timeframe === 'Month'
    ? { day: 'numeric', month: 'short' }
    : { month: 'short', year: '2-digit' }),
  timeZone: 'UTC',
}).format(new Date(value));

export default function TaskActivityChart({ points = [], timeframe = 'SixMonths', onTimeframeChange, loading }) {
  const max = Math.max(1, ...points.flatMap((point) => [point.created, point.completed]));

  return (
    <section className="flex min-h-[460px] flex-col overflow-hidden rounded-3xl border border-border-subtle bg-bg-card shadow-premium xl:col-span-2">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border-subtle p-6">
        <div className="flex items-center gap-3">
          <div className="grid h-11 w-11 place-items-center rounded-full bg-secondary text-white">
            <BarChart3 size={20} />
          </div>
          <div>
            <h2 className="text-xl font-black text-text-main">Task Velocity</h2>
            <p className="mt-0.5 text-xs font-bold uppercase text-text-muted">Tasks created and completed</p>
          </div>
        </div>
        <div className="flex items-center gap-2 rounded-full border border-border-subtle bg-surface-1 px-3 py-1.5 text-xs font-bold text-text-muted">
          <Calendar size={14} />
          <label className="sr-only" htmlFor="dashboard-velocity-timeframe">Velocity timeframe</label>
          <select
            id="dashboard-velocity-timeframe"
            value={timeframe}
            onChange={(event) => onTimeframeChange?.(event.target.value)}
            className="bg-transparent text-text-main outline-none"
          >
            {TIMEFRAMES.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
          </select>
        </div>
      </div>

      <div className="flex items-center justify-end gap-4 px-6 pt-4 text-xs font-bold text-text-muted">
        <span className="flex items-center gap-2"><span className="h-2.5 w-2.5 rounded-sm bg-primary" />Created</span>
        <span className="flex items-center gap-2"><span className="h-2.5 w-2.5 rounded-sm bg-secondary" />Completed</span>
      </div>

      <div className="relative min-h-[320px] flex-1 overflow-x-auto p-6">
        {loading ? (
          <div className="absolute inset-6 animate-pulse rounded-3xl bg-surface-2" />
        ) : points.length === 0 ? (
          <div className="flex h-full items-center justify-center text-sm text-text-muted">No velocity data available.</div>
        ) : (
          <div className="relative flex h-full flex-col justify-end" style={{ minWidth: `${points.length * 42}px` }}>
            <div className="pointer-events-none absolute inset-0 flex flex-col justify-between">
              {[0, 1, 2, 3].map((index) => (
                <div key={index} className="flex w-full items-center">
                  <span className="w-8 pr-2 text-right text-[10px] font-bold text-text-subtle">
                    {Math.round(max - (max / 3) * index)}
                  </span>
                  <div className="flex-1 border-t border-dashed border-border-subtle/50" />
                </div>
              ))}
            </div>
            <div className="relative flex h-[240px] items-end justify-around pl-8">
              {points.map((point) => (
                <div key={point.start} className="group relative flex min-w-0 flex-1 flex-col items-center">
                  <div className="pointer-events-none absolute -top-12 z-10 rounded-xl border border-border-strong bg-bg-card px-3 py-1.5 text-xs text-text-main opacity-0 shadow-premium transition-opacity group-hover:opacity-100">
                    {formatBucket(point.start, timeframe)}: {point.created} created, {point.completed} completed
                  </div>
                  <div className="flex h-[240px] w-full max-w-[3rem] items-end justify-center gap-1" aria-label={`${formatBucket(point.start, timeframe)}: ${point.created} created, ${point.completed} completed`}>
                    <div className="w-1/2 rounded-t-lg bg-primary" style={{ height: `${(point.created / max) * 100}%` }} />
                    <div className="w-1/2 rounded-t-lg bg-secondary" style={{ height: `${(point.completed / max) * 100}%` }} />
                  </div>
                  <span className="mt-3 text-[10px] font-bold text-text-muted sm:text-xs">{formatBucket(point.start, timeframe)}</span>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </section>
  );
}

TaskActivityChart.propTypes = {
  points: PropTypes.arrayOf(PropTypes.shape({
    start: PropTypes.string.isRequired,
    end: PropTypes.string.isRequired,
    created: PropTypes.number.isRequired,
    completed: PropTypes.number.isRequired,
  })),
  timeframe: PropTypes.oneOf(TIMEFRAMES.map((option) => option.value)),
  onTimeframeChange: PropTypes.func,
  loading: PropTypes.bool,
};
