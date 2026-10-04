import { HubConnectionBuilder } from '@microsoft/signalr';
import { getAccessToken } from '@/api/axiosInstance';

// Serialize teardown/startup, including StrictMode's immediate cleanup/setup.
let lifecycle = Promise.resolve();
export function startNotificationHub(handlers, publish) {
  let disposed = false;
  let connection;
  lifecycle = lifecycle.then(async () => {
    if (disposed) return;
    connection = new HubConnectionBuilder()
      .withUrl('/hubs/notification', { accessTokenFactory: () => getAccessToken() || '' })
      .withAutomaticReconnect().build();
    for (const [event, handler] of Object.entries(handlers)) connection.on(event, handler);
    connection.onreconnected(() => { if (!disposed) publish({ connection, reconnected: true }); });
    connection.onclose(() => { if (!disposed) publish({ connection: null, error: 'Live updates disconnected' }); });
    try {
      await connection.start();
      if (!disposed) publish({ connection });
    } catch {
      if (!disposed) publish({ connection: null, error: 'Live updates unavailable' });
      for (const [event, handler] of Object.entries(handlers)) connection.off(event, handler);
      await connection.stop().catch(() => {});
    }
  });
  return () => {
    disposed = true;
    const stopping = connection?.stop().catch(() => {});
    if (connection) for (const [event, handler] of Object.entries(handlers)) connection.off(event, handler);
    lifecycle = lifecycle.then(() => stopping).catch(() => {});
  };
}
