import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

const defaultDevServerPort: number = 5173

export default defineConfig(({ mode }) => {
  // Load every variable from .env files (empty prefix = not only VITE_*); real environment variables win.
  // These values are only used by Vite itself and are never exposed to browser code.
  const environmentValues: Record<string, string> = loadEnv(mode, process.cwd(), '')
  const devServerPort: number = Number(environmentValues.DEV_SERVER_PORT ?? defaultDevServerPort)

  return {
    plugins: [react(), tailwindcss()],
    // If you change the port, also add the new origin to the API's Cors:AllowedOrigins.
    server: { port: devServerPort, strictPort: true },
  }
})
