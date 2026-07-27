import path from "node:path";
import { fileURLToPath } from "node:url";
import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const tenantSubdomain: string = env.VITE_AZURE_B2C_DOMAIN || "pdmapp";
  const tenantName: string = env.VITE_AZURE_B2C_TENANT_NAME || "pdmapp";

  return {
    plugins: [react()],
    resolve: {
      alias: {
        "@pdm-shared": path.resolve(__dirname, "../shared"),
        react: path.resolve(__dirname, "node_modules/react"),
        "react-dom": path.resolve(__dirname, "node_modules/react-dom"),
      },
      dedupe: ["react", "react-dom", "@emotion/react", "@emotion/styled", "@emotion/cache"],
    },
    optimizeDeps: {
      include: ["react", "react-dom", "@emotion/react", "@emotion/styled"],
    },
    server: {
      port: 5173,
      strictPort: true,
      open: true,
      proxy: {
        "/native-auth": {
          target: `https://${tenantSubdomain}.ciamlogin.com/${tenantName}.onmicrosoft.com`,
          changeOrigin: true,
          secure: true,
          rewrite: (requestPath: string) =>
            requestPath.replace(/^\/native-auth/, ""),
        },
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
  };
});
