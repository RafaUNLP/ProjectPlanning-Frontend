import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";

export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: {
      "/bonita": {
        target: "http://localhost:49828", 
        changeOrigin: true,
        secure: false,
      },
    },
  },
});
