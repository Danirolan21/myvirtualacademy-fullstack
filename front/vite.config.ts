import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { resolve } from 'path'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: { '@': resolve(__dirname, 'src') }
  },
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5100',
        changeOrigin: true,
        secure: false
      },
      '/assets': {
        target: 'http://localhost:5100',
        changeOrigin: true,
        secure: false
      },
      '/uploads': {
        target: 'http://localhost:5100',
        changeOrigin: true,
        secure: false
      }
    }
  }
})
