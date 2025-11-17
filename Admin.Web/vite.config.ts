import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'
import { fileURLToPath } from 'url'

const __filename = fileURLToPath(import.meta.url)
const __dirname = path.dirname(__filename)

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
      '@ckeditor5-local': path.resolve(__dirname, './CkEditor5/ckeditor5.js'),
      '@ckeditor5-static': path.resolve(__dirname, './CkEditor5'),
    },
  },
})
