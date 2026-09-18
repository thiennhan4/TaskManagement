import React from 'react';
import { Search, Bell, Menu, User, LogOut, Settings } from 'lucide-react';
import { useAuth } from '@/context/AuthContext';
import { useNavigate } from 'react-router-dom';
import Button from '@/components/ui/Button';
import LanguageSwitcher from '@/components/common/LanguageSwitcher';
import { useLanguage } from '@/context/LanguageContext';
import { useNotification } from '@/context/NotificationContext';
import { formatDistanceToNow } from 'date-fns';
import ThemeToggle from '@/components/theme/ThemeToggle';

const Topbar = ({ onMenuClick }) => {
  const { user, logout } = useAuth();
  const { t } = useLanguage();
  const { notifications, unreadCount, markAsRead, markAllAsRead } = useNotification();
  const [showNotifications, setShowNotifications] = React.useState(false);
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <header className="h-16 sticky top-0 bg-surface-0/80 backdrop-blur-md border-b border-border-subtle z-40 px-6 flex items-center justify-between transition-colors">
      <div className="flex items-center gap-4 flex-1">
        <button 
          onClick={onMenuClick}
          className="lg:hidden p-2 hover:bg-hover-bg rounded-lg transition-colors text-text-main"
        >
          <Menu size={20} />
        </button>
        
        <div className="relative max-w-md w-full hidden md:block">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-text-muted" size={18} />
          <input 
            type="text" 
            placeholder={t('nav.searchPlaceholder')}
            className="w-full pl-10 pr-4 py-2 bg-surface-1 border-none rounded-xl text-sm text-text-main placeholder:text-text-muted focus:ring-2 focus:ring-primary/20 transition-all outline-none"
          />
        </div>
      </div>

      <div className="flex items-center gap-3">
        <div className="relative">
          <Button 
            variant="ghost" 
            size="icon" 
            className="relative text-text-main"
            onClick={() => setShowNotifications(!showNotifications)}
          >
            <Bell size={20} />
            {unreadCount > 0 && (
              <span className="absolute top-2 right-2 w-2 h-2 bg-rose-500 rounded-full border-2 border-surface-0"></span>
            )}
          </Button>

          {showNotifications && (
            <>
              <div className="fixed inset-0 z-[45]" onClick={() => setShowNotifications(false)}></div>
              <div className="absolute right-0 mt-2 w-80 bg-surface-0 rounded-2xl shadow-premium border border-border-subtle z-50 overflow-hidden animate-in fade-in zoom-in duration-200">
                <div className="flex items-center justify-between p-4 border-b border-border-subtle bg-surface-1/50">
                  <h3 className="font-bold text-text-main">Notifications</h3>
                  {unreadCount > 0 && (
                    <button 
                      onClick={markAllAsRead}
                      className="text-xs font-bold text-primary hover:text-primary-hover transition-colors"
                    >
                      Mark all read
                    </button>
                  )}
                </div>
                <div className="max-h-80 overflow-y-auto">
                  {notifications.length === 0 ? (
                    <div className="p-8 text-center text-text-muted">
                      <Bell className="mx-auto mb-3 opacity-20" size={32} />
                      <p className="text-sm font-medium">No notifications yet</p>
                    </div>
                  ) : (
                    notifications.map(notif => (
                      <div 
                        key={notif.id} 
                        onClick={() => {
                          if (!notif.isRead) markAsRead(notif.id);
                          setShowNotifications(false);
                          if (notif.linkUrl) navigate(notif.linkUrl);
                        }}
                        className={`p-4 border-b border-border-subtle last:border-b-0 cursor-pointer hover:bg-hover-bg transition-colors flex gap-3 ${!notif.isRead ? 'bg-primary/5 dark:bg-primary/10' : ''}`}
                      >
                        <div className={`w-2 h-2 rounded-full mt-1.5 shrink-0 ${!notif.isRead ? 'bg-primary' : 'bg-transparent'}`} />
                        <div>
                          <p className={`text-sm ${!notif.isRead ? 'font-bold text-text-main' : 'font-medium text-text-muted'}`}>
                            {notif.title}
                          </p>
                          <p className="text-xs text-text-muted mt-0.5 line-clamp-2">{notif.message}</p>
                          <p className="text-[10px] text-text-subtle mt-2 font-bold uppercase tracking-wider">
                            {formatDistanceToNow(new Date(notif.createdAt), { addSuffix: true })}
                          </p>
                        </div>
                      </div>
                    ))
                  )}
                </div>
                <div className="p-3 border-t border-border-subtle bg-surface-1/50 text-center">
                  <button 
                    onClick={() => {
                      setShowNotifications(false);
                      navigate('/notifications');
                    }}
                    className="text-sm font-bold text-primary hover:text-primary-hover transition-colors"
                  >
                    View all notifications
                  </button>
                </div>
              </div>
            </>
          )}
        </div>
        <ThemeToggle />
        <LanguageSwitcher compact />
        
        <div className="h-8 w-[1px] bg-border-subtle mx-2"></div>

        <div className="flex items-center gap-3 group cursor-pointer relative">
          <div className="text-right hidden sm:block">
            <p className="text-sm font-bold text-text-main leading-none">{user?.fullName || t('nav.userName')}</p>
            <p className="text-xs text-text-muted mt-1 uppercase tracking-wider font-semibold">{user?.role || t('nav.member')}</p>
          </div>
          <div className="h-10 w-10 rounded-xl bg-primary/10 border border-primary/20 flex items-center justify-center text-primary font-bold overflow-hidden shrink-0">
            {user?.avatar ? (
              <img src={user.avatar} alt="Avatar" className="w-full h-full object-cover" />
            ) : (
              user?.fullName?.charAt(0) || 'U'
            )}
          </div>

          {/* Profile Dropdown */}
          <div className="absolute top-full right-0 mt-2 w-48 bg-surface-0 rounded-2xl shadow-premium border border-border-subtle opacity-0 translate-y-2 group-hover:opacity-100 group-hover:translate-y-0 transition-all pointer-events-none group-hover:pointer-events-auto p-2 overflow-hidden">
            <button className="w-full flex items-center gap-2 p-2 hover:bg-hover-bg rounded-xl text-sm font-medium text-text-main transition-colors text-left">
              <User size={16} /> {t('nav.profile')}
            </button>
            <button className="w-full flex items-center gap-2 p-2 hover:bg-hover-bg rounded-xl text-sm font-medium text-text-main transition-colors text-left">
              <Settings size={16} /> {t('nav.settings')}
            </button>
            <div className="h-[1px] bg-border-subtle my-2"></div>
            <button 
              onClick={handleLogout}
              className="w-full flex items-center gap-2 p-2 hover:bg-rose-50 dark:hover:bg-rose-500/10 text-rose-500 rounded-xl text-sm font-bold transition-colors text-left"
            >
              <LogOut size={16} /> {t('nav.logout')}
            </button>
          </div>
        </div>
      </div>
    </header>
  );
};

export default Topbar;
