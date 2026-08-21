import { defineConfig, loadEnv } from 'vite';
import vue from '@vitejs/plugin-vue';
import basicSsl from '@vitejs/plugin-basic-ssl';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');

  return {
    base: env.VITE_BASE_PATH || '/leadcaptura/',
    plugins: [vue(), mode === 'development' ? basicSsl() : null],
    server: {
      port: 5173,
      proxy: {
        '/api': {
          target: 'http://localhost:5018',
          changeOrigin: true
        }
      }
    }
  };
});
