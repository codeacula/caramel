import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";

export default defineConfig({
  plugins: [vue()],
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      "/api": "http://localhost:5144",
      "/interactions": "http://localhost:5144",
      "/ws": {
        target: "http://localhost:5144",
        ws: true,
        changeOrigin: true,
      },
      "/auth": {
        target: "http://localhost:5146",
        changeOrigin: true,
      },
      "/chat": {
        target: "http://localhost:5146",
        changeOrigin: true,
      },
    },
  },
  build: {
    // Build straight into ASP.NET's static files for production
    outDir: "../Caramel.API/wwwroot",
    emptyOutDir: true,
  },
});
