import PropTypes from 'prop-types';
import { Bell } from 'lucide-react';
import { useLanguage } from '@/context/LanguageContext';
import { useNotification } from '@/context/NotificationContext';

export default function DashboardHeader({ user }) {
  const { t } = useLanguage();
  const { unreadCount } = useNotification();

  const getGreeting = () => {
    const hour = new Date().getHours();
    if (hour < 12) return 'Good Morning';
    if (hour < 18) return 'Good Afternoon';
    return 'Good Evening';
  };

  const displayName = user?.fullName || user?.email || t('nav.userName') || 'User';
  const avatarInitial = displayName.charAt(0).toUpperCase();

  return (
    <header className="flex flex-col gap-5 rounded-3xl border border-border-subtle bg-bg-card px-5 py-5 shadow-premium sm:flex-row sm:items-center sm:justify-between sm:px-7">
      <div className="min-w-0">
        <p className="text-sm font-bold uppercase text-text-muted">
          Dashboard
        </p>
        <h1 className="mt-1 text-2xl font-black text-text-main sm:text-3xl">
          {getGreeting()}, {displayName}
        </h1>
        <p className="mt-2 max-w-2xl text-sm font-medium text-text-muted">
          {t('dashboard.subtitle') || 'Here is an overview of your active projects and tasks.'}
        </p>
      </div>

      <div className="flex shrink-0 items-center gap-3">
        <button
          type="button"
          className="relative grid h-12 w-12 place-items-center rounded-full border border-border-subtle bg-surface-1 text-text-main transition-colors hover:bg-hover-bg"
          aria-label="Notifications"
        >
          <Bell size={20} />
          {unreadCount > 0 && (
            <span className="absolute right-3 top-3 h-2.5 w-2.5 rounded-full bg-danger ring-2 ring-bg-card" />
          )}
        </button>
        <div className="flex items-center gap-3 rounded-full border border-border-subtle bg-surface-1 py-1.5 pl-2 pr-4">
          <div className="grid h-10 w-10 place-items-center overflow-hidden rounded-full bg-secondary text-sm font-black text-white">
            {user?.avatar ? (
              <img src={user.avatar} alt={displayName} className="h-full w-full object-cover" />
            ) : (
              avatarInitial
            )}
          </div>
          <div className="hidden min-w-0 sm:block">
            <p className="max-w-36 truncate text-sm font-black text-text-main">{displayName}</p>
            <p className="text-xs font-semibold text-text-muted">{user?.role || t('nav.member')}</p>
          </div>
        </div>
      </div>
    </header>
  );
}

DashboardHeader.propTypes = {
  user: PropTypes.shape({
    avatar: PropTypes.string,
    email: PropTypes.string,
    fullName: PropTypes.string,
    role: PropTypes.string,
  }),
};
