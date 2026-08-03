/**
 * CORS proxy for Entra External ID Native Auth (dev only).
 *
 * Usage: npm run dev:cors
 */
import http from "node:http";
import https from "node:https";
import { loadEnv } from "vite";

const env = loadEnv("development", process.cwd(), "");
const tenantSubdomain = env.VITE_AZURE_B2C_DOMAIN || "pdmapp";
const tenantName = env.VITE_AZURE_B2C_TENANT_NAME || "pdmapp";
const tenantId =
  env.VITE_AZURE_B2C_TENANT_ID || "77b1686a-7dc5-4d4d-976c-2c78a8f040d2";

const localApiPath = "/api";
const port = 3001;
// Align with msal/customAuth authority (onmicrosoft.com path)
const proxyTarget = `https://${tenantSubdomain}.ciamlogin.com/${tenantName}.onmicrosoft.com`;
const proxyHostname = `${tenantSubdomain}.ciamlogin.com`;
void tenantId;

const ALLOWED_REQUEST_HEADERS = new Set([
  "content-type",
  "accept",
  "client-request-id",
  "x-client-sku",
  "x-client-ver",
  "x-client-os",
  "x-client-cpu",
  "x-client-current-telemetry",
  "x-client-last-telemetry",
]);

function collectBody(req) {
  return new Promise((resolve, reject) => {
    const chunks = [];
    req.on("data", (chunk) => {
      chunks.push(chunk);
    });
    req.on("end", () => {
      resolve(Buffer.concat(chunks));
    });
    req.on("error", reject);
  });
}

function buildForwardHeaders(req, bodyLength) {
  const headers = {
    host: proxyHostname,
    "content-length": String(bodyLength),
  };

  for (const [name, value] of Object.entries(req.headers)) {
    if (value === undefined) {
      continue;
    }
    const lower = name.toLowerCase();
    if (!ALLOWED_REQUEST_HEADERS.has(lower)) {
      continue;
    }
    headers[lower] = Array.isArray(value) ? value.join(",") : value;
  }

  if (!headers["content-type"]) {
    headers["content-type"] = "application/x-www-form-urlencoded";
  }

  return headers;
}

http
  .createServer((req, res) => {
    const requestUrl = new URL(req.url ?? "/", `http://localhost:${port}`);

    if (req.method === "OPTIONS") {
      res.writeHead(204, {
        "Access-Control-Allow-Origin": "*",
        "Access-Control-Allow-Methods": "GET, POST, PUT, DELETE, OPTIONS",
        "Access-Control-Allow-Headers":
          "Content-Type, Authorization, client-request-id, x-client-sku, x-client-ver, x-client-os, x-client-cpu, x-client-current-telemetry, x-client-last-telemetry",
      });
      res.end();
      return;
    }

    if (!requestUrl.pathname.startsWith(localApiPath)) {
      res.writeHead(404, { "Content-Type": "text/plain" });
      res.end("Not Found");
      return;
    }

    const targetPath =
      requestUrl.pathname.replace(localApiPath, "") + requestUrl.search;
    const targetUrl = `${proxyTarget}${targetPath}`;

    void (async () => {
      try {
        const body = await collectBody(req);
        console.log(
          `${req.method} ${req.url} → ${targetUrl} (${body.length} bytes)`
        );

        if (targetPath.includes("/token") && body.length > 0) {
          const params = new URLSearchParams(body.toString("utf8"));
          console.log(
            `  grant_type=${params.get("grant_type")} passwordLength=${params.get("password")?.length ?? 0} client_id=${params.get("client_id")} scopes=${params.get("scope")}`
          );
        }

        if (targetPath.includes("/challenge") && body.length > 0) {
          const params = new URLSearchParams(body.toString("utf8"));
          console.log(
            `  challenge request challenge_type=${params.get("challenge_type")}`
          );
        }

        const proxyReq = https.request(
          targetUrl,
          {
            method: req.method,
            headers: buildForwardHeaders(req, body.length),
          },
          (proxyRes) => {
            const chunks = [];
            proxyRes.on("data", (c) => chunks.push(c));
            proxyRes.on("end", () => {
              const responseBody = Buffer.concat(chunks);
              const text = responseBody.toString("utf8");
              if (targetPath.includes("/challenge")) {
                try {
                  const json = JSON.parse(text);
                  console.log(
                    `  ← challenge_type=${json.challenge_type ?? "(none)"} status=${proxyRes.statusCode}`
                  );
                } catch {
                  console.log(`  ← challenge raw ${proxyRes.statusCode} ${text.slice(0, 200)}`);
                }
              } else if (proxyRes.statusCode && proxyRes.statusCode >= 400) {
                console.log(`  ← ${proxyRes.statusCode} ${text.slice(0, 500)}`);
              }
              res.writeHead(proxyRes.statusCode ?? 500, {
                "Access-Control-Allow-Origin": "*",
                "Access-Control-Allow-Methods": "GET, POST, PUT, DELETE, OPTIONS",
                "Access-Control-Allow-Headers":
                  "Content-Type, Authorization, client-request-id, x-client-sku, x-client-ver, x-client-os, x-client-cpu, x-client-current-telemetry, x-client-last-telemetry",
                "Content-Type":
                  proxyRes.headers["content-type"] ?? "application/json",
                "Content-Length": String(responseBody.length),
              });
              res.end(responseBody);
            });
          }
        );

        proxyReq.on("error", (err) => {
          console.error("Proxy error:", err);
          res.writeHead(500, { "Content-Type": "text/plain" });
          res.end("Proxy error.");
        });

        proxyReq.write(body);
        proxyReq.end();
      } catch (err) {
        console.error("Proxy handler error:", err);
        res.writeHead(500, { "Content-Type": "text/plain" });
        res.end("Proxy error.");
      }
    })();
  })
  .listen(port, () => {
    console.log(`Native Auth CORS proxy: http://localhost:${port}${localApiPath}`);
    console.log(`Forwarding to ${proxyTarget}`);
    console.log(
      "Note: f8cdef31-... in Portal logs = Microsoft Graph tenant (often User.Read), NOT your CIAM tenant."
    );
  });
