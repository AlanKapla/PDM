const CACHE_NAME = "pdm-static-v1";
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

// SW must NOT touch navigation, API or dynamic requests
self.addEventListener("fetch", (event) => {
  const req = event.request;
  const url = new URL(req.url);

  // Do NOT handle:
  // - navigation requests
  // - API
  // - auth redirects
  if (
    req.mode === "navigate" ||
    url.pathname.startsWith("/api/") ||
    url.pathname.includes("User/")
  ) {
    return; // do nothing, let browser handle it
  }

  // Cache only static assets
  event.respondWith(
    caches.match(req).then((cached) => {
      return (
        cached ||
        fetch(req).then((res) => {
          if (res.status === 200) {
            const clone = res.clone();
            caches.open(CACHE_NAME).then((cache) => cache.put(req, clone));
          }
          return res;
        })
      );
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
