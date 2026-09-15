import PropTypes from 'prop-types';
import { CheckCircle2, Clock3, Inbox, TimerReset } from 'lucide-react';
import DashboardStatCard from './DashboardStatCard';

export default function DashboardStats({ stats }) {
  const total = stats?.total ?? 0;
  const inProgress = stats?.inProgress ?? 0;
  const completed = stats?.done ?? 0;
  const overdue = stats?.overdue ?? 0;
  const totalBoards = stats?.totalBoards ?? 0;
  const completionRate = total > 0 ? Math.round((completed / total) * 100) : 0;

  const items = [
    {
      label: 'Total Tasks',
      value: total,
      trend: `${totalBoards} active ${totalBoards === 1 ? 'board' : 'boards'}`,
      trendType: 'neutral',
      tone: 'primary',
      featured: true,
      icon: <Inbox size={20} />,
    },
    {
      label: 'In Progress',
      value: inProgress,
      trend: `${stats?.todo ?? 0} to do`,
      trendType: inProgress > 0 ? 'positive' : 'neutral',
      tone: 'warning',
      icon: <TimerReset size={20} />,
    },
    {
      label: 'Completed',
      value: completed,
      trend: `${completionRate}% completion rate`,
      trendType: completionRate >= 50 ? 'positive' : 'neutral',
      tone: 'success',
      icon: <CheckCircle2 size={20} />,
    },
    {
      label: 'Overdue',
      value: overdue,
      trend: overdue > 0 ? 'Requires attention' : 'All tasks on schedule',
      trendType: overdue > 0 ? 'negative' : 'positive',
      tone: overdue > 0 ? 'danger' : 'neutral',
      icon: <Clock3 size={20} />,
    },
  ];

  return (
    <section className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
      {items.map((item) => (
        <DashboardStatCard key={item.label} {...item} />
      ))}
    </section>
  );
}

DashboardStats.propTypes = {
  stats: PropTypes.shape({
    total: PropTypes.number,
    todo: PropTypes.number,
    inProgress: PropTypes.number,
    done: PropTypes.number,
    overdue: PropTypes.number,
    totalBoards: PropTypes.number,
  }),
};
