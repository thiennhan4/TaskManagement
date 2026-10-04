import axiosInstance, { getSessionVersion } from '@/api/axiosInstance';

// Dashboard remounts/invalidation can request the same resource concurrently.
// Only pending work is shared, keyed by session and complete query; no cache.
const pending = new Map();
function get(url, config) {
  const key = JSON.stringify([getSessionVersion(), url, config.params]);
  if (!pending.has(key)) {
    const request = axiosInstance.get(url, config).finally(() => pending.delete(key));
    pending.set(key, request);
  }
  return pending.get(key);
}

export const dashboardApi = {
  getStats: (scope = 'Personal', teamId = null) =>
    get('/dashboard/stats', { params: { scope, ...(teamId ? { teamId } : {}) } }),
  getVelocity: (timeframe = 'SixMonths', scope = 'Personal', teamId = null) =>
    get('/dashboard/velocity', { params: { timeframe, scope, ...(teamId ? { teamId } : {}) } }),
  getActivity: (scope = 'Personal', teamId = null, page = 1, pageSize = 10) =>
    get('/dashboard/activity', { params: { scope, ...(teamId ? { teamId } : {}), page, pageSize } }),
  getProjectActivity: (projectId, page = 1, pageSize = 5) =>
    get(`/v1/projects/${projectId}/activity`, { params: { page, pageSize } }),
  getUpcomingTasks: (scope = 'Personal', teamId = null, page = 1, pageSize = 5) =>
    get('/dashboard/upcoming', { params: { scope, ...(teamId ? { teamId } : {}), page, pageSize } }),
};
