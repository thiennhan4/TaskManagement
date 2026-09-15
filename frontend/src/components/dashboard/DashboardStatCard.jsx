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
  featured = false,
}) {
  const displayLabel = label || title;

  const trendToneClass = {
    positive: 'text-success bg-success/10 border-success/20',
    negative: 'text-danger bg-danger/10 border-danger/20',
    neutral: 'text-text-subtle bg-surface-2 border-border-subtle',
  }[trendType] || 'text-text-subtle bg-surface-2 border-border-subtle';

  const TrendIcon = trendType === 'positive' ? TrendingUp : trendType === 'negative' ? TrendingDown : Minus;

  const displayTrend = trend || hint;

  return (
    <article className={`relative flex min-h-40 flex-col justify-between overflow-hidden rounded-3xl border p-5 shadow-premium transition-all duration-200 hover:-translate-y-0.5 hover:shadow-hover ${
      featured
        ? 'border-primary-dark/40 bg-gradient-to-br from-primary to-primary-dark text-text-inverse'
        : 'border-border-subtle bg-bg-card text-text-main'
    }`}>
      <div className="flex items-center justify-between gap-3">
        <p className={`truncate text-xs font-black uppercase ${featured ? 'text-text-inverse/70' : 'text-text-muted'}`}>{displayLabel}</p>
        <div className="grid h-11 w-11 shrink-0 place-items-center rounded-full bg-secondary text-white">
          {icon}
        </div>
      </div>

      <div className="mt-4 flex items-baseline justify-between gap-2">
        <strong className={`text-4xl font-black leading-none ${featured ? 'text-text-inverse' : 'text-text-main'}`}>
          {value}
        </strong>
      </div>

      {displayTrend && (
        <div className={`mt-4 flex items-center gap-1.5 border-t pt-3 text-xs font-medium ${
          featured ? 'border-text-inverse/15' : 'border-border-subtle/50'
        }`}>
          <span className={`inline-flex items-center gap-1 rounded-full border px-2.5 py-1 text-[11px] font-bold ${
            featured ? 'border-text-inverse/15 bg-text-inverse/10 text-text-inverse' : trendToneClass
          }`}>
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
  featured: PropTypes.bool,
};
