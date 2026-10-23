import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'
import { mockApiPlugin } from './vite-mock-api.js'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    vueDevTools(),
    // Учебный mock API: /api/work-orders на dev-сервере (src/api/mockWorkOrders.js).
    mockApiPlugin(),
  ],
  // Порт клиента из схемы ЛР2 (docs/architecture.md)
  server: {
    port: 5173,
  },
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
})
