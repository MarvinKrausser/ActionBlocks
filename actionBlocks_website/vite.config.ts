import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Dev server forwards /hub to a locally running game server (dotnet run).
const devServer = process.env.DEV_SERVER_URL ?? 'http://localhost:5080';

export default defineConfig({
  plugins: [react()],
  build: {
    // Hashed bundles go to /static; /assets holds the exchangeable graphics from public/.
    assetsDir: 'static',
  },
  css: {
    modules: {
      localsConvention: 'camelCaseOnly',
    },
  },
  server: {
    proxy: {
      '/hub': { target: devServer, ws: true, changeOrigin: true },
    },
  },
});
