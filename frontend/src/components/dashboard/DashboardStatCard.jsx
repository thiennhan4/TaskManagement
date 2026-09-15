import PropTypes from 'prop-types';
import { TrendingUp, TrendingDown, Minus } from 'lucide-react';

export default function DashboardStatCard({
  icon,
  label,
  title,
  value,
  hint,
  trend,
  trendType = 'neutral',
  tone = 'primary',
}) {
  const displayLabel = label || title;

  const toneClass = {
    primary: 'bg-primary/10 text-primary',
    success: 'bg-emerald-500/10 text-emerald-500',
    warning: 'bg-amber-500/10 text-amber-500',
    danger: 'bg-rose-500/10 text-rose-500',
    neutral: 'bg-surface-2 text-text-muted',
  }[tone] || 'bg-primary/10 text-primary';

  const trendToneClass = {
    positive: 'text-emerald-500 bg-emerald-500/10 border-emerald-500/20',
    negative: 'text-rose-500 bg-rose-500/10 border-rose-500/20',
    neutral: 'text-text-subtle bg-surface-2 border-border-subtle',
  }[trendType] || 'text-text-subtle bg-surface-2 border-border-subtle';

  const TrendIcon = trendType === 'positive' ? TrendingUp : trendType === 'negative' ? TrendingDown : Minus;

  const displayTrend = trend || hint;

  return (
    <article className="flex flex-col justify-between rounded-2xl border border-border-subtle bg-surface-0 p-5 shadow-sm transition-all duration-200 hover:-translate-y-0.5 hover:border-primary/30 hover:shadow-md">
      <div className="flex items-center justify-between gap-3">
        <p className="text-xs font-bold uppercase tracking-wider text-text-muted truncate">{displayLabel}</p>
        <div className={`grid h-10 w-10 shrink-0 place-items-center rounded-xl ${toneClass}`}>
          {icon}
        </div>
      </div>

      <div className="mt-4 flex items-baseline justify-between gap-2">
        <strong className="text-3xl sm:text-4xl font-extrabold leading-none tracking-tight text-text-main">
          {value}
        </strong>
      </div>

      {displayTrend && (
        <div className="mt-4 flex items-center gap-1.5 pt-3 border-t border-border-subtle/50 text-xs font-medium">
          <span className={`inline-flex items-center gap-1 rounded-md border px-2 py-0.5 text-[11px] font-semibold ${trendToneClass}`}>
            <TrendIcon size={12} />
            {displayTrend}
          </span>
        </div>
      )}
    </article>
  );
}

DashboardStatCard.propTypes = {
  icon: PropTypes.node,
  label: PropTypes.string,
  title: PropTypes.string,
  value: PropTypes.oneOfType([PropTypes.string, PropTypes.number]).isRequired,
  hint: PropTypes.string,
  trend: PropTypes.string,
  trendType: PropTypes.oneOf(['positive', 'negative', 'neutral']),
  tone: PropTypes.oneOf(['primary', 'success', 'warning', 'danger', 'neutral']),
};

