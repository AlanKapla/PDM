const CACHE_NAME = "pdm-static-v3";
const STATIC_ASSETS = ["/", "/index.html"];

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

  // 2) IGNORE navigation + API + auth + favicons
  //    + JS/CSS/assets — NIGDY cache-first (iOS PWA trzymało stare bundlе auth
  //    po hard kill / deploy i spinner kręcił się na wiecznie-starym kodzie).
  if (
    req.mode === "navigate" ||
    url.pathname.startsWith("/api/") ||
    url.pathname.startsWith("/native-auth/") ||
    url.pathname.includes("User/") ||
    url.pathname.startsWith("/favicon") ||
    url.pathname === "/logo.png" ||
    url.pathname === "/logo.svg" ||
    url.pathname.startsWith("/assets/") ||
    url.pathname.endsWith(".js") ||
    url.pathname.endsWith(".css") ||
    url.pathname.endsWith(".map")
  ) {
    return; // let the browser handle it normally
  }

  // 3) Cache only static assets (GET only) — np. ikony, fonty
  if (req.method !== "GET") {
    return;
  }

  event.respondWith(
    caches.match(req).then((cached) => {
      if (cached) {
        return cached;
      }

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
          return caches.match("/index.html");
        });
    })
  );
});

// Clean old caches + przejmij klientów od razu (iOS PWA po hard kill)
self.addEventListener("activate", (event) => {
  event.waitUntil(
    caches
      .keys()
      .then((keys) =>
        Promise.all(
          keys
            .filter((key) => key !== CACHE_NAME)
            .map((key) => caches.delete(key))
        )
      )
      .then(() => self.clients.claim())
  );
});
