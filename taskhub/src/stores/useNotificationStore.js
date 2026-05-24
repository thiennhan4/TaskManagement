import { create } from 'zustand';
import * as signalR from '@microsoft/signalr';
import { notificationApi } from '../api/notificationApi';

const useNotificationStore = create((set, get) => ({
  notifications: [],
  unreadCount: 0,
  connection: null,
  isInitialized: false,

  initialize: async () => {
    if (get().isInitialized) return;
    
    try {
      // 1. Fetch initial notifications
      const response = await notificationApi.getMyNotifications();
      const initialNotifications = response.data.data || [];
      const unreadCount = initialNotifications.filter(n => !n.isRead).length;

      set({ 
        notifications: initialNotifications,
        unreadCount,
        isInitialized: true
      });

      // 2. Setup SignalR Connection
      const token = localStorage.getItem('token');
      if (!token) return;

      const connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/notification', {
          accessTokenFactory: () => token
        })
        .withAutomaticReconnect()
        .build();

      connection.on('ReceiveNotification', (notification) => {
        set((state) => {
          const newNotifications = [notification, ...state.notifications];
          return {
            notifications: newNotifications,
            unreadCount: state.unreadCount + 1
          };
        });
        
        // Optional: show a toast here if desired, or dispatch custom event
      });

      await connection.start();
      set({ connection });
    } catch (error) {
      console.error('Failed to initialize notifications:', error);
    }
  },

  disconnect: () => {
    const { connection } = get();
    if (connection) {
      connection.stop();
      set({ connection: null, isInitialized: false, notifications: [], unreadCount: 0 });
    }
  },

  markAsRead: async (id) => {
    try {
      await notificationApi.markAsRead(id);
      set((state) => ({
        notifications: state.notifications.map(n => 
          n.id === id ? { ...n, isRead: true } : n
        ),
        unreadCount: Math.max(0, state.unreadCount - 1)
      }));
    } catch (error) {
      console.error('Failed to mark notification as read:', error);
    }
  },

  markAllAsRead: async () => {
    try {
      await notificationApi.markAllAsRead();
      set((state) => ({
        notifications: state.notifications.map(n => ({ ...n, isRead: true })),
        unreadCount: 0
      }));
    } catch (error) {
      console.error('Failed to mark all as read:', error);
    }
  }
}));

export default useNotificationStore;
