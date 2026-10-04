import api from '@/api/axiosInstance';

// Compatibility for existing array consumers. Each request is bounded; metadata,
// rather than an empty-page guess, determines whether another request is needed.
export async function getCollection(url, params = {}) {
  const first = await api.get(url, { params: { ...params, page: 1, pageSize: 100 } });
  const items = [...first.data.data.items];
  for (let page = 2; page <= first.data.data.totalPages; page++) {
    const next = await api.get(url, { params: { ...params, page, pageSize: 100 } });
    items.push(...next.data.data.items);
  }
  return { ...first, data: { ...first.data, data: items } };
}
