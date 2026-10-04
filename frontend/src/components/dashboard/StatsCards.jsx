import PropTypes from 'prop-types';
import { LayoutDashboard, CheckCircle, Clock } from 'lucide-react';
import { useLanguage } from '@/context/LanguageContext';

export default function StatsCards({ stats }) {
  const { t } = useLanguage();
  const cards = [
    {
      title: t('dashboard.totalBoards'),
      value: stats?.totalBoards || 0,
      icon: LayoutDashboard,
      color: '#f7c948',
    },
    {
      title: t('dashboard.activeTasks'),
      value: stats?.activeTasks || 0,
      icon: Clock,
      color: '#4ecdc4',
    },
    {
      title: t('dashboard.completed'),
      value: stats?.completedTasks || 0,
      icon: CheckCircle,
      color: '#ff6b6b',
    },
  ];

  return (
    <div className="grid grid-cols-1 sm:grid-cols-3 gap-6 mb-10">
      {cards.map((card, idx) => {
        const Icon = card.icon;
        return (
          <div 
            key={idx}
            className="bg-surface-0 border-[3px] border-border-strong p-6 shadow-[4px_4px_0px_var(--color-border-strong)] flex items-center gap-4"
          >
            <div 
              className="w-12 h-12 border-[2px] border-border-strong flex items-center justify-center shrink-0"
              style={{ backgroundColor: card.color }}
            >
              <Icon className="text-text-main" size={24} strokeWidth={2.5} />
            </div>
            <div>
              <p className="text-text-muted font-bold uppercase tracking-widest text-[10px] mb-1">
                {card.title}
              </p>
              <h3 className="text-2xl font-black text-text-main leading-none">
                {card.value}
              </h3>
            </div>
          </div>
        );
      })}
    </div>
  );
}

StatsCards.propTypes = {
  stats: PropTypes.shape({
    totalBoards: PropTypes.number,
    activeTasks: PropTypes.number,
    completedTasks: PropTypes.number,
  }),
};
