const CACHE_NAME = "pdm-static-v2";
const STATIC_ASSETS = [
  "/",
  "/index.html",
];

// Install SW
self.addEventListener("install", (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME).then((cache) => cache.addAll(STATIC_ASSETS))
  );
  self.skipWaiting();
});

// Intercept fetch
self.addEventListener("fetch", (event) => {
  const req = event.request;
  const url = new URL(req.url);

  // 1) IGNORE chrome-extension:// REQUESTS
  if (url.protocol === "chrome-extension:") {
    return; // do not intercept
  }

  // 2) IGNORE navigation + API requests + auth redirects + favicons
  if (
    req.mode === "navigate" ||      // index.html routing
    url.pathname.startsWith("/api/") ||
    url.pathname.includes("User/") ||
    url.pathname.startsWith("/favicon") ||
    url.pathname === "/logo.png" ||
    url.pathname === "/logo.svg"
  ) {
    return; // let the browser handle it normally
  }

  // 3) Cache only static assets (GET only)
  if (req.method !== "GET") {
    return;
  }

  event.respondWith(
    caches.match(req).then((cached) => {
      // Found in cache → return cached version
      if (cached) {
        return cached;
      }

      // Not in cache → fetch and optionally cache
      return fetch(req)
        .then((res) => {
          if (res && res.status === 200) {
            const clone = res.clone();
            caches.open(CACHE_NAME).then((cache) => {
              cache.put(req, clone);
            });
          }
          return res;
        })
        .catch(() => {
          // Offline fallback (optional)
          return caches.match("/index.html");
        });
    })
  );
});

// Clean old caches
self.addEventListener("activate", (event) => {
  event.waitUntil(
    caches.keys().then((keys) =>
      Promise.all(
        keys
          .filter((key) => key !== CACHE_NAME)
          .map((key) => caches.delete(key))
      )
    )
  );
  self.clients.claim();
});
