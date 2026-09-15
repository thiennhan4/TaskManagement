import PropTypes from 'prop-types';
import { Plus, Sparkles } from 'lucide-react';
import Button from '@/components/ui/Button';
import { useLanguage } from '@/context/LanguageContext';

export default function DashboardHeader({ userFullName, onCreateBoard }) {
  const { t } = useLanguage();

  const getGreeting = () => {
    const hour = new Date().getHours();
    if (hour < 12) return 'Good morning';
    if (hour < 18) return 'Good afternoon';
    return 'Good evening';
  };

  const firstName = userFullName ? userFullName.split(' ')[0] : null;

  return (
    <div className="rounded-2xl border border-border-subtle bg-surface-0 p-6 sm:p-7 shadow-sm transition-all">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-6">
        <div className="space-y-1.5">
          <div className="flex items-center gap-2">
            <Sparkles size={14} className="text-primary animate-pulse" />
            <span className="text-xs font-bold uppercase tracking-wider text-primary">
              {getGreeting()}
            </span>
          </div>
          <h1 className="text-2xl sm:text-3xl font-extrabold tracking-tight text-text-main">
            {firstName 
              ? t('dashboard.welcome', { name: firstName })
              : (t('dashboard.welcome', { name: 'User' }) || 'Welcome back!')}
          </h1>
          <p className="text-sm font-medium text-text-muted max-w-xl">
            {t('dashboard.subtitle') || 'Here is an overview of your active projects and tasks.'}
          </p>
        </div>

        {onCreateBoard && (
          <div className="shrink-0 w-full sm:w-auto">
            <Button
              leftIcon={<Plus size={18} />}
              onClick={onCreateBoard}
              className="w-full sm:w-auto shadow-sm"
            >
              {t('dashboard.createBoard') || 'Create Board'}
            </Button>
          </div>
        )}
      </div>
    </div>
  );
}

DashboardHeader.propTypes = {
  userFullName: PropTypes.string,
  onCreateBoard: PropTypes.func,
};

