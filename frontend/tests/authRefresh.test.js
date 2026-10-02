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


test('bootstrap and interceptor share one refresh request', async () => {
  const { default: api, refreshSession } = await import('../src/api/axiosInstance.js?case=shared');
  let finish;
  let refreshes = 0;
  api.defaults.adapter = async config => {
    if (config.url === '/auth/refresh') {
      refreshes++;
      await new Promise(resolve => { finish = resolve; });
      return { data: { success: true, data: { token: 'synthetic-access', user: {} } }, status: 200, config };
    }
    if (!config._retry) throw new AxiosError('Unauthorized', 'ERR_BAD_REQUEST', config, null, { status: 401, config });
    return { data: {}, status: 200, config };
  };
  const bootstrap = refreshSession();
  const duplicateMount = refreshSession();
  assert.equal(bootstrap, duplicateMount);
  const request = api.get('/tasks');
  await new Promise(resolve => setTimeout(resolve, 10));
  finish();
  await Promise.all([bootstrap, duplicateMount, request]);
  assert.equal(refreshes, 1);
});

test('a losing refresh clears memory and does not retry rotation recursively', async () => {
  const { default: api, refreshSession, getAccessToken, setAccessToken } = await import('../src/api/axiosInstance.js?case=loser');
  setAccessToken('synthetic-old-access');
  let calls = 0;
  api.defaults.adapter = async config => {
    calls++;
    throw new AxiosError('Consumed', 'ERR_BAD_REQUEST', config, null, { status: 401, config });
  };
  await assert.rejects(refreshSession());
  assert.equal(getAccessToken(), null);
  assert.equal(calls, 1);
});

test('two tabs serialize cookie rotation through the shared Web Lock', async () => {
  const previousNavigator = Object.getOwnPropertyDescriptor(globalThis, 'navigator');
  let queue = Promise.resolve();
  const names = [];
  Object.defineProperty(globalThis, 'navigator', { configurable: true, value: { locks: {
    request(name, action) {
      names.push(name);
      const result = queue.then(action);
      queue = result.catch(() => {});
      return result;
    },
  } } });
  try {
    const a = await import('../src/api/axiosInstance.js?case=tab-a');
    const b = await import('../src/api/axiosInstance.js?case=tab-b');
    let cookieGeneration = 0;
    let active = 0;
    let maxActive = 0;
    const adapter = async config => {
      assert.equal(config.url, '/auth/refresh');
      const consumed = cookieGeneration;
      maxActive = Math.max(maxActive, ++active);
      await new Promise(resolve => setTimeout(resolve, 5));
      assert.equal(cookieGeneration, consumed);
      cookieGeneration++;
      active--;
      return { data: { success: true, data: { token: `synthetic-access-${cookieGeneration}` } }, status: 200, config };
    };
    a.default.defaults.adapter = adapter;
    b.default.defaults.adapter = adapter;
    await Promise.all([a.refreshSession(), b.refreshSession()]);
    assert.equal(cookieGeneration, 2);
    assert.equal(maxActive, 1);
    assert.deepEqual(names, ['taskhub-refresh', 'taskhub-refresh']);
    assert.equal(a.getAccessToken(), 'synthetic-access-1');
    assert.equal(b.getAccessToken(), 'synthetic-access-2');
  } finally {
    if (previousNavigator) Object.defineProperty(globalThis, 'navigator', previousNavigator);
    else delete globalThis.navigator;
  }
});
