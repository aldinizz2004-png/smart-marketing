/// <reference types="vitest/config" />
import { fileURLToPath, URL } from 'node:url'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  server: {
    port: 5173,
    strictPort: true,
    // Lets a phone open the dev server through a Cloudflare quick tunnel; set VITE_API_URL to empty to call the API through the proxy.
    allowedHosts: ['.trycloudflare.com'],
    proxy: {
      '/api': 'http://localhost:5233',
      '/health': 'http://localhost:5233',
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/test/setup.ts'],
    css: false,
    env: { VITE_API_URL: 'http://localhost:5233' },
  },
})
