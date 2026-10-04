import { useShallow } from 'zustand/react/shallow';
import React, { useEffect } from 'react';
import { useLanguage } from '@/context/LanguageContext';
import useNotificationStore from "@/stores/useNotificationStore";
import { Bell, Check, Trash2, MailOpen, Loader2 } from 'lucide-react';
import Card from '@/components/ui/Card';
import Button from '@/components/ui/Button';
import PageControls from '@/components/ui/PageControls';

export default function NotificationsPage() {
  const { t } = useLanguage();
  const { 
    notifications, 
    unreadCount,
    page, error,
    isLoading, 
    fetchNotifications, 
    markAsRead, 
    markAllAsRead 
  } = useNotificationStore(useShallow(state => ({ notifications: state.notifications, unreadCount: state.unreadCount, page: state.page, error: state.error, isLoading: state.isLoading, fetchNotifications: state.fetchNotifications, markAsRead: state.markAsRead, markAllAsRead: state.markAllAsRead })));

  useEffect(() => {
    fetchNotifications();
  }, [fetchNotifications]);

  const handleMarkAsRead = (e, id) => {
    e.stopPropagation();
    markAsRead(id);
  };

  const handleActionClick = (url) => {
    if (url) {
      window.location.assign(url); // In a real app, use React Router's navigate
    }
  };

  if (isLoading && notifications.length === 0) {
    return (
      <div className="flex h-full items-center justify-center">
        <Loader2 className="animate-spin text-primary" size={40} />
      </div>
    );
  }

  return (
    <div className="space-y-6 animate-in fade-in duration-500 pb-8 max-w-4xl mx-auto">
      {/* Header */}
      {error && <div role="alert">{error} <Button onClick={() => fetchNotifications()}>Retry</Button></div>}
      <PageControls page={page} loading={isLoading} onPage={fetchNotifications} />
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 sticky top-0 bg-surface-1 pt-6 pb-4 z-10">
        <div className="flex items-center gap-4">
          <div className="w-12 h-12 bg-rose-500/10 rounded-2xl flex items-center justify-center text-rose-500 shadow-inner">
            <Bell size={24} />
          </div>
          <div>
            <h1 className="text-3xl font-black text-text-main tracking-tight flex items-center gap-3">
              {t('nav.notifications') || 'Notifications'}
              {unreadCount > 0 && (
                <span className="bg-rose-500 text-white text-sm px-2.5 py-0.5 rounded-full font-bold">
                  {unreadCount} new
                </span>
              )}
            </h1>
            <p className="text-sm font-bold text-text-subtle uppercase tracking-widest">
              Stay Updated
            </p>
          </div>
        </div>
        
        {unreadCount > 0 && (
          <Button variant="outline" onClick={markAllAsRead} className="gap-2">
            <Check size={18} />
            Mark all as read
          </Button>
        )}
      </div>

      {error ? null : notifications.length === 0 ? (
        <Card padding="p-12" className="flex flex-col items-center justify-center text-center border-dashed border-2">
          <div className="w-20 h-20 bg-surface-1 rounded-full flex items-center justify-center text-text-subtle mb-4">
            <Bell size={40} />
          </div>
          <h3 className="text-xl font-bold mb-2 text-text-main">All Caught Up!</h3>
          <p className="text-text-muted max-w-sm">
            You don't have any notifications right now. Check back later for updates on your tasks and projects.
          </p>
        </Card>
      ) : (
        <div className="space-y-3">
          {notifications.map((notification) => (
            <Card 
              key={notification.id} 
              className={`p-4 transition-all hover:shadow-md cursor-pointer ${
                !notification.isRead 
                  ? 'bg-surface-0 border-l-4 border-l-primary' 
                  : 'bg-surface-2/50 opacity-75'
              }`}
              onClick={() => handleActionClick(notification.actionUrl)}
            >
              <div className="flex gap-4">
                <div className={`mt-1 flex-shrink-0 w-10 h-10 rounded-full flex items-center justify-center ${
                  !notification.isRead ? 'bg-primary/10 text-primary' : 'bg-surface-2 text-text-subtle'
                }`}>
                  {!notification.isRead ? <Bell size={20} /> : <MailOpen size={20} />}
                </div>
                
                <div className="flex-1 min-w-0">
                  <div className="flex items-start justify-between gap-4">
                    <div>
                      <h4 className={`text-base mb-1 ${!notification.isRead ? 'font-bold text-text-main' : 'font-semibold text-text-muted'}`}>
                        {notification.title}
                      </h4>
                      <p className="text-text-muted text-sm leading-relaxed mb-2">
                        {notification.message}
                      </p>
                    </div>
                    
                    {!notification.isRead && (
                      <button 
                        onClick={(e) => handleMarkAsRead(e, notification.id)}
                        className="p-2 text-text-subtle hover:text-primary transition-colors flex-shrink-0 bg-surface-2 hover:bg-primary/10 rounded-lg group"
                        title="Mark as read"
                      >
                        <Check size={18} className="group-hover:scale-110 transition-transform" />
                      </button>
                    )}
                  </div>
                  
                  <div className="flex items-center justify-between mt-2">
                    <span className="text-xs font-semibold text-text-subtle uppercase tracking-wider">
                      {new Date(notification.createdAt).toLocaleString()}
                    </span>
                    
                    {notification.actionLabel && notification.actionUrl && (
                      <span className="text-sm font-bold text-primary">
                        {notification.actionLabel} &rarr;
                      </span>
                    )}
                  </div>
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
