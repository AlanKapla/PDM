import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  resolve: {
    dedupe: ["react", "react-dom", "@emotion/react", "@emotion/styled", "@emotion/cache"],
  },
  server: {
    port: 5173,
    strictPort: false,
    open: true,
    hmr: {
      protocol: "ws",
      host: "localhost",
      port: 5173,
    },
  },
  preview: {
    port: 4173,
    strictPort: false,
  },
  test: {
    globals: true,
    environment: "jsdom",
    setupFiles: ["./src/test/setup.ts"],
    include: ["src/**/*.{test,spec,axe.test}.{ts,tsx}"],
    coverage: {
      provider: "v8",
      reporter: ["text", "html"],
    },
  },
});
