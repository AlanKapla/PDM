import path from "node:path";
import { fileURLToPath } from "node:url";
import { defineConfig, loadEnv, type ProxyOptions } from "vite";
import react from "@vitejs/plugin-react";
import type { IncomingMessage, ServerResponse } from "node:http";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

/**
 * Entra: refresh_token z native password + nagłówek Origin → AADSTS9002326.
 * Proxy musi usunąć Origin/Referer przed forwardem do ciamlogin.com.
 */
function createNativeAuthProxy(
  tenantSubdomain: string,
  tenantName: string
): ProxyOptions {
  return {
    target: `https://${tenantSubdomain}.ciamlogin.com/${tenantName}.onmicrosoft.com`,
    changeOrigin: true,
    secure: true,
    rewrite: (requestPath: string) => requestPath.replace(/^\/native-auth/, ""),
    configure: (proxy) => {
      proxy.on(
        "proxyReq",
        (proxyReq: { removeHeader: (name: string) => void }) => {
          proxyReq.removeHeader("origin");
          proxyReq.removeHeader("Origin");
          proxyReq.removeHeader("referer");
          proxyReq.removeHeader("Referer");
        }
      );
      proxy.on(
        "error",
        (
          err: Error,
          _req: IncomingMessage,
          res: ServerResponse | undefined
        ) => {
          console.error("[vite native-auth proxy]", err.message);
          if (res && !res.headersSent) {
            res.writeHead(502, { "Content-Type": "application/json" });
            res.end(JSON.stringify({ error: "native_auth_proxy_error" }));
          }
        }
      );
    },
  };
}

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
        "/native-auth": createNativeAuthProxy(tenantSubdomain, tenantName),
      },
    },
    preview: {
      port: 4173,
      strictPort: false,
      proxy: {
        "/native-auth": createNativeAuthProxy(tenantSubdomain, tenantName),
      },
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
