// Cache shell assets only. Never cache audio, model answers, or API traffic.
const CACHE='k4rma-shell-v1';const SHELL=['/','/app.js','/styles.css','/icon.svg','/manifest.json'];
self.addEventListener('install',e=>e.waitUntil(caches.open(CACHE).then(c=>c.addAll(SHELL)).then(()=>self.skipWaiting())));
self.addEventListener('activate',e=>e.waitUntil(self.clients.claim()));
self.addEventListener('fetch',e=>{const url=new URL(e.request.url);if(url.pathname.startsWith('/api/')||e.request.method!=='GET')return;e.respondWith(fetch(e.request).then(r=>{if(r.ok&&SHELL.includes(url.pathname)){const copy=r.clone();caches.open(CACHE).then(c=>c.put(e.request,copy));}return r}).catch(()=>caches.match(e.request)))});
