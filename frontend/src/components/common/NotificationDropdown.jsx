import { useShallow } from 'zustand/react/shallow';
import React, { useState, useRef, useEffect } from 'react';
import { Bell, Check, CheckCheck } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import useNotificationStore from '../../stores/useNotificationStore';
import { formatDistanceToNow } from 'date-fns';

const NotificationDropdown = () => {
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef(null);
  const navigate = useNavigate();
  
  const { 
    notifications, 
    unreadCount, 
    markAsRead, 
    markAllAsRead 
  } = useNotificationStore(useShallow(state => ({ notifications: state.notifications, unreadCount: state.unreadCount, markAsRead: state.markAsRead, markAllAsRead: state.markAllAsRead })));

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleNotificationClick = (notification) => {
    if (!notification.isRead) {
      markAsRead(notification.id);
    }
    setIsOpen(false);
    if (notification.linkUrl) {
      navigate(notification.linkUrl);
    }
  };

  return (
    <div className="relative" ref={dropdownRef}>
      <button 
        onClick={() => setIsOpen(!isOpen)}
        className="relative p-2 text-text-muted hover:bg-hover-bg rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-primary/30"
      >
        <Bell size={20} />
        {unreadCount > 0 && (
          <span className="absolute top-1 right-1 flex h-4 w-4 items-center justify-center rounded-full bg-red-500 text-[10px] font-bold text-white">
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </button>

      {isOpen && (
        <div className="absolute right-0 mt-2 w-80 sm:w-96 bg-surface-0 rounded-lg shadow-xl border border-border-subtle z-50 overflow-hidden flex flex-col max-h-[80vh]">
          <div className="flex items-center justify-between p-4 border-b border-border-subtle bg-surface-1/50">
            <h3 className="font-semibold text-text-main">Notifications</h3>
            {unreadCount > 0 && (
              <button 
                onClick={markAllAsRead}
                className="text-xs flex items-center text-primary-600 hover:text-primary-700 font-medium transition-colors"
              >
                <CheckCheck size={14} className="mr-1" />
                Mark all read
              </button>
            )}
          </div>
          
          <div className="overflow-y-auto flex-1">
            {notifications.length === 0 ? (
              <div className="p-8 text-center text-text-muted">
                <Bell size={32} className="mx-auto mb-3 text-text-subtle opacity-50" />
                <p className="text-sm">You're all caught up!</p>
              </div>
            ) : (
              <ul className="divide-y divide-border-subtle">
                {notifications.map((notification) => (
                  <li 
                    key={notification.id} 
                    onClick={() => handleNotificationClick(notification)}
                    className={`p-4 hover:bg-hover-bg cursor-pointer transition-colors flex gap-3 ${!notification.isRead ? 'bg-primary/5' : ''}`}
                  >
                    <div className="flex-shrink-0 mt-1">
                      <div className={`w-2 h-2 rounded-full ${!notification.isRead ? 'bg-primary-500' : 'bg-transparent'}`}></div>
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className={`text-sm ${!notification.isRead ? 'font-semibold text-text-main' : 'text-text-muted'}`}>
                        {notification.title}
                      </p>
                      <p className="text-sm text-text-muted truncate mt-0.5">
                        {notification.message}
                      </p>
                      <p className="text-xs text-text-subtle mt-1.5">
                        {formatDistanceToNow(new Date(notification.createdAt), { addSuffix: true })}
                      </p>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </div>
          
          <div className="p-2 border-t border-border-subtle bg-surface-1 text-center">
            <button className="text-sm text-primary-600 hover:text-primary-700 font-medium w-full py-1.5 transition-colors">
              View All Notifications
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default NotificationDropdown;
