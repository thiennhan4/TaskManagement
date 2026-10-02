import Dropdown from '@/components/ui/Dropdown';
import { safeReturnLocation } from '@/utils/returnLocation';
import { useShallow } from 'zustand/react/shallow';
import React from 'react';
import { Search, Bell, Menu, User, LogOut, Settings } from 'lucide-react';
import { useAuth } from '@/context/authState';
import { useNavigate } from 'react-router-dom';
import Button from '@/components/ui/Button';
import LanguageSwitcher from '@/components/common/LanguageSwitcher';

import useNotificationStore from '@/stores/useNotificationStore';
import { formatDistanceToNow } from 'date-fns';
import ThemeToggle from '@/components/theme/ThemeToggle';

const Topbar = ({ onMenuClick }) => {
  const { user, logout } = useAuth();

  const { notifications, unreadCount, markAsRead, markAllAsRead } = useNotificationStore(useShallow(state => ({ notifications: state.notifications, unreadCount: state.unreadCount, markAsRead: state.markAsRead, markAllAsRead: state.markAllAsRead })));
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
            disabled aria-label="Global search unavailable" placeholder="Search coming later"
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
                          if (notif.linkUrl) navigate(safeReturnLocation(notif.linkUrl));
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

        <Dropdown label={<span className="flex items-center gap-2 text-text-main"><User size={20} />{user?.fullName || 'Profile'}</span>}>
          <button onClick={() => navigate('/profile')} className="w-full p-2 text-left rounded-xl hover:bg-hover-bg">Profile</button>
          <button onClick={() => navigate('/settings')} className="w-full p-2 text-left rounded-xl hover:bg-hover-bg">Settings</button>
          <button onClick={handleLogout} className="w-full p-2 text-left rounded-xl text-danger hover:bg-hover-bg">Logout</button>
        </Dropdown>
      </div>
    </header>
  );
};

export default Topbar;
