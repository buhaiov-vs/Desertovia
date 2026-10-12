import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// End-to-end tests run their own backend next to the development one.
const backend = process.env.DESERTOD_BACKEND ?? 'http://localhost:5080'

// changeOrigin stays false so the backend sees the Vite host; OAuth redirect URLs and cookies then use port 5190.
const proxied = { target: backend, changeOrigin: false }

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5190,
    strictPort: true,
    proxy: {
      '/api': proxied,
      '/auth': proxied,
      '/signin-twitch': proxied,
      '/signin-google': proxied,
      '/hubs': { ...proxied, ws: true },
    },
  },
  build: {
    outDir: '../src/Desertod.Api/wwwroot',
    emptyOutDir: true,
  },
})
