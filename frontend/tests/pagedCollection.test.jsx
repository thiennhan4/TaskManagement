import { afterEach, expect, it, vi } from 'vitest';
import api from '@/api/axiosInstance';
import { getCollection } from '@/api/pagedCollection';
import { listApi } from '@/api/listApi';

vi.mock('@/api/axiosInstance', () => ({ default: { get: vi.fn() } }));
afterEach(() => vi.resetAllMocks());
const response = (items, totalPages = 1) => ({ data: { success: true, data: { items, totalPages } } });

it('preserves array consumers while following explicit page metadata', async () => {
  api.get.mockResolvedValueOnce(response([{ id: 'first' }], 2)).mockResolvedValueOnce(response([{ id: 'second' }], 2));
  const result = await getCollection('/v1/projects/project/members');
  expect(result.data.data.map(x => x.id)).toEqual(['first', 'second']);
  expect(api.get).toHaveBeenLastCalledWith('/v1/projects/project/members', { params: { page: 2, pageSize: 100 } });
});

it('propagates forbidden instead of returning an empty array', async () => {
  const denied = { response: { status: 403 } };
  api.get.mockRejectedValue(denied);
  await expect(getCollection('/v1/boards')).rejects.toBe(denied);
});

it('reads all nested task pages from the explicit column contract', async () => {
  api.get.mockResolvedValueOnce(response([{ id: 'list', tasks: { items: [{ id: 'one' }], totalPages: 2 } }]))
    .mockResolvedValueOnce(response([{ id: 'list', tasks: { items: [{ id: 'two' }], totalPages: 2 } }]));
  const result = await listApi.getListsByBoard('board');
  expect(result.data.data[0].tasks.map(x => x.id)).toEqual(['one', 'two']);
  expect(api.get).toHaveBeenLastCalledWith('/v1/boardlists/board/board', { params: { listId: 'list', taskPage: 2, taskPageSize: 100 } });
});
