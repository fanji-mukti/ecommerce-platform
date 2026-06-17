/// <reference types="vitest" />
import { defineConfig } from 'vite';
import angular from '@analogjs/vite-plugin-angular';

export default defineConfig({
  plugins: [angular()],
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['@analogjs/vitest-angular/setup-zone'],
    include: ['src/**/*.spec.ts'],
    reporters: ['default'],
  },
  resolve: {
    mainFields: ['module'],
  },
});
