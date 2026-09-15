import React, { createContext, useContext, useEffect, useState, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import { useAuth } from './AuthContext';
import { notificationApi } from '@/api/notificationApi';
import toast from 'react-hot-toast';

const NotificationContext = createContext(null);

export function NotificationProvider({ children }) {
  const { user, token } = useAuth();
  const [notifications, setNotifications] = useState([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [hubConnection, setHubConnection] = useState(null);

  // Fetch initial notifications
  const fetchNotifications = useCallback(async () => {
    if (!user) return;
    try {
      const response = await notificationApi.getMyNotifications();
      // Assuming response.data is an array or inside a standard wrapper
      const data = response.data?.data || response.data || [];
      setNotifications(data);
      setUnreadCount(data.filter(n => !n.isRead).length);
    } catch (error) {
      console.error('Failed to fetch notifications', error);
    }
  }, [user]);

  // Setup SignalR Connection
  useEffect(() => {
    if (user && token) {
      const connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/notification', {
          accessTokenFactory: () => token
        })
        .withAutomaticReconnect()
        .build();

      connection.start()
        .then(() => {
          console.log('Connected to Notification Hub!');
          setHubConnection(connection);
        })
        .catch(err => console.error('SignalR Connection Error: ', err));

      connection.on('ReceiveNotification', (notification) => {
        setNotifications(prev => [notification, ...prev]);
        setUnreadCount(prev => prev + 1);
        toast(notification.title, { icon: '🔔' });
      });

      return () => {
        connection.stop();
        setHubConnection(null);
      };
    }
  }, [user, token]);

  // Load notifications on mount
  useEffect(() => {
    fetchNotifications();
  }, [fetchNotifications]);

  const markAsRead = async (id) => {
    try {
      await notificationApi.markAsRead(id);
      setNotifications(prev => prev.map(n => n.id === id ? { ...n, isRead: true } : n));
      setUnreadCount(prev => Math.max(0, prev - 1));
    } catch (error) {
      console.error('Failed to mark as read', error);
    }
  };

  const markAllAsRead = async () => {
    try {
      await notificationApi.markAllAsRead();
      setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));
      setUnreadCount(0);
    } catch (error) {
      console.error('Failed to mark all as read', error);
    }
  };

  return (
    <NotificationContext.Provider value={{ notifications, unreadCount, markAsRead, markAllAsRead, hubConnection }}>
      {children}
    </NotificationContext.Provider>
  );
}

export function useNotification() {
  const context = useContext(NotificationContext);
  if (!context) {
    throw new Error('useNotification must be used within a NotificationProvider');
  }
  return context;
}
