import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { defineConfig } from 'vitest/config';
import vue from '@vitejs/plugin-vue';

function localApiToken(): string | undefined {
  const localAppData = process.env.LOCALAPPDATA;
  if (!localAppData) return undefined;
  try {
    const config = JSON.parse(readFileSync(join(localAppData, 'B2BStore', 'settings.local.json'), 'utf8')) as {
      B2BStore?: { AdminToken?: unknown };
    };
    return typeof config.B2BStore?.AdminToken === 'string' && config.B2BStore.AdminToken.length > 0
      ? config.B2BStore.AdminToken
      : undefined;
  } catch {
    return undefined;
  }
}

export default defineConfig(({ command }) => {
  const token = command === 'serve' ? localApiToken() : undefined;
  // Server-side only; this value is never embedded in the browser bundle.
  const apiTarget = process.env.B2BSTORE_API_URL || 'http://127.0.0.1:5080';
  const protectedProxy = {
    target: apiTarget,
    configure(proxy: {
      on(event: 'proxyReq', listener: (proxyRequest: { setHeader(name: string, value: string): void }) => void): void;
    }): void {
      if (!token) return;
      proxy.on('proxyReq', (proxyRequest) => {
        proxyRequest.setHeader('Authorization', `Bearer ${token}`);
      });
    },
  };

  return {
  plugins: [vue()],
  test: {
    environment: 'jsdom',
    include: ['src/**/*.test.ts'],
  },
  server: {
    host: '127.0.0.1',
    proxy: {
      '/api': protectedProxy,
      '/health': { target: apiTarget },
    },
  },
  };
});
