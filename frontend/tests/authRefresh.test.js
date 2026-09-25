import test from 'node:test';
import assert from 'node:assert/strict';
import { AxiosError } from 'axios';

for (const status of [403, 429]) {
  test(`${status} preserves the server error without refreshing or redirecting`, async () => {
    const { default: api } = await import(`../src/api/axiosInstance.js?status=${status}`);
    let calls = 0;
    api.defaults.adapter = async config => {
      calls++;
      throw new AxiosError('Denied', 'ERR_BAD_REQUEST', config, null, { status, config, data: { message: 'Try later' } });
    };
    await assert.rejects(api.get('/tasks'), error => error.response.status === status && error.response.data.message === 'Try later');
    assert.equal(calls, 1);
  });
}

for (const url of ['/auth/refresh', '/auth/google']) {
  test(`${url} rejects 401 without recursively refreshing`, async () => {
    const { default: api } = await import(`../src/api/axiosInstance.js?case=${encodeURIComponent(url)}`);
    let requests = 0;
    api.defaults.adapter = async (config) => {
      requests += 1;
      throw new AxiosError('Unauthorized', 'ERR_BAD_REQUEST', config, null, { status: 401, config });
    };
    const result = await Promise.race([
      api.post(url).then(() => 'resolved', () => 'rejected'),
      new Promise((resolve) => { const timer = setTimeout(() => resolve('timeout'), 500); timer.unref(); }),
    ]);
    assert.equal(result, 'rejected');
    assert.equal(requests, 1);
  });
}

test('expired session rejects and redirects after one failed refresh', async () => {
  globalThis.window = { location: { href: '' } };
  const { default: api, getAccessToken } = await import('../src/api/axiosInstance.js?case=expired');
  const requests = [];
  api.defaults.adapter = async (config) => {
    requests.push(config.url);
    throw new AxiosError('Unauthorized', 'ERR_BAD_REQUEST', config, null, { status: 401, config });
  };
  await assert.rejects(api.get('/auth/me'));
  assert.deepEqual(requests, ['/auth/me', '/auth/refresh']);
  assert.equal(getAccessToken(), null);
  assert.equal(window.location.href, '/login');
  delete globalThis.window;
});
