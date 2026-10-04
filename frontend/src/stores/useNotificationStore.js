import { create } from 'zustand';
import { notificationApi } from '@/api/notificationApi';

// Retain one server page; never shift server offsets with lifetime event appends.
export const NOTIFICATION_PAGE_SIZE = 20;
let epoch = 0;
let pageVersion = 0;
let pageRevision = 0;
let countVersion = 0;
let selectedPage = 1;
let pageRequest;
let countRequest;
const mutations = new Map();
const initial = { notifications: [], unreadCount: 0, page: null, isLoading: false, error: null };
const message = error => error.response?.data?.message || 'Could not update notifications';
const useNotificationStore = create((set, get) => ({
  ...initial,
  reset: () => {
    epoch++; pageVersion++; countVersion++;
    selectedPage = 1;
    pageRequest = null; countRequest = null; mutations.clear();
    set(initial);
  },
  fetchUnreadCount: () => {
    if (countRequest) return countRequest;
    const generation = epoch;
    const version = countVersion;
    const pending = notificationApi.getUnreadCount().then(response => {
      if (generation !== epoch) return;
      if (version !== countVersion) {
        countRequest = null;
        return get().fetchUnreadCount();
      }
      set({ unreadCount: response.data.data });
    }).catch(error => { if (generation === epoch) set({ error: message(error) }); })
      .finally(() => { if (countRequest === pending) countRequest = null; });
    countRequest = pending;
    return pending;
  },
  fetchNotifications: (page = selectedPage) => {
    // Realtime refresh must preserve navigation even before that page resolves.
    selectedPage = page;
    if (pageRequest?.page === page) return pageRequest.promise;
    const generation = epoch;
    const version = ++pageVersion;
    const revision = pageRevision;
    set({ isLoading: true, error: null });
    const pending = notificationApi.getMyNotifications(page, NOTIFICATION_PAGE_SIZE).then(response => {
      if (generation !== epoch || version !== pageVersion) return;
      if (revision !== pageRevision) {
        pageRequest = null;
        return get().fetchNotifications(page);
      }
      const { items, ...metadata } = response.data.data;
      if (metadata.page > Math.max(1, metadata.totalPages)) {
        pageRequest = null;
        return get().fetchNotifications(Math.max(1, metadata.totalPages));
      }
      set({ notifications: [...new Map(items.map(item => [item.id, item])).values()].slice(0, NOTIFICATION_PAGE_SIZE), page: metadata });
    }).catch(error => { if (generation === epoch && version === pageVersion) set({ error: message(error) }); })
      .finally(() => {
        if (generation === epoch && version === pageVersion) set({ isLoading: false });
        if (pageRequest?.promise === pending) pageRequest = null;
      });
    pageRequest = { page, promise: pending };
    get().fetchUnreadCount();
    return pending;
  },
  applyRead: (id) => {
    set(state => ({ notifications: state.notifications.map(item => item.id === id && !item.isRead ? { ...item, isRead: true } : item) }));
    pageVersion++; pageRequest = null; countVersion++;
    get().fetchUnreadCount();
    return get().fetchNotifications();
  },
  applyReadAll: () => {
    set(state => ({ notifications: state.notifications.map(item => item.isRead ? item : { ...item, isRead: true }) }));
    pageVersion++; pageRequest = null; countVersion++;
    get().fetchUnreadCount();
    return get().fetchNotifications();
  },
  receive: (notification) => {
    pageRevision++;
    set(state => ({ notifications: state.notifications.map(item => item.id === notification.id ? { ...item, ...notification, isRead: item.isRead || notification.isRead } : item) }));
    countVersion++;
    get().fetchUnreadCount();
    return get().fetchNotifications();
  },
  markAsRead: (id) => {
    if (get().notifications.find(item => item.id === id)?.isRead) return Promise.resolve();
    return mutate('read:' + id, () => notificationApi.markAsRead(id), () => get().applyRead(id), set);
  },
  markAllAsRead: () => mutate('all', notificationApi.markAllAsRead, () => get().applyReadAll(), set),
}));

function mutate(key, request, apply, set) {
  if (mutations.has(key)) return mutations.get(key);
  const generation = epoch;
  const pending = request().then(() => { if (generation === epoch) return apply(); })
    .catch(error => { if (generation === epoch) set({ error: message(error) }); })
    .finally(() => { if (mutations.get(key) === pending) mutations.delete(key); });
  mutations.set(key, pending);
  return pending;
}
export default useNotificationStore;
