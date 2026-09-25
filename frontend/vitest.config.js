import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  resolve: { alias: { '@': new URL('./src', import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, '$1') } },
  test: { environment: 'jsdom', include: ['tests/*.test.jsx'], clearMocks: true },
});
