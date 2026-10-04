import { createContext, useContext, useEffect, useState } from 'react';
import { useAuth } from '@/context/authState';
import useNotificationStore from '@/stores/useNotificationStore';
import { startNotificationHub } from '@/realtime/notificationHub';

const NotificationContext = createContext(null);
export function NotificationProvider({ children }) {
  const { user } = useAuth();
  const [hub, setHub] = useState({ hubConnection: null, reconnectVersion: 0, hubError: null });
  useEffect(() => {
    if (!user?.id) return;
    let active = true;
    const store = useNotificationStore.getState();
    const guard = handler => (...args) => { if (active) handler(...args); };
    const stop = startNotificationHub({
      ReceiveNotification: guard(store.receive),
      NotificationRead: guard(store.applyRead),
      AllNotificationsRead: guard(store.applyReadAll),
    }, ({ connection, reconnected, error }) => {
      if (active) setHub(previous => ({ hubConnection: connection, hubError: error || null,
        reconnectVersion: previous.reconnectVersion + (reconnected ? 1 : 0) }));
    });
    store.fetchNotifications();
    return () => { active = false; stop(); };
  }, [user?.id]);
  return <NotificationContext.Provider value={hub}>{children}</NotificationContext.Provider>;
}
// eslint-disable-next-line react-refresh/only-export-components
export function useNotification() {
  const context = useContext(NotificationContext);
  if (!context) throw new Error('useNotification must be used within a NotificationProvider');
  return context;
}
