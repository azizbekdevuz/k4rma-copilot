import http from 'node:http';
import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {handler} from './lib/http.mjs';
const DIR=path.dirname(fileURLToPath(import.meta.url));
const mime={'.html':'text/html;charset=utf-8','.js':'text/javascript;charset=utf-8','.css':'text/css;charset=utf-8','.json':'application/json;charset=utf-8','.svg':'image/svg+xml','.png':'image/png'};
const port=Number(process.env.PORT||3000);
const hits=new Map();
http.createServer(async(req,res)=>{
 try{
  const url=new URL(req.url,'http://localhost');
  res.setHeader('X-Content-Type-Options','nosniff');res.setHeader('Referrer-Policy','no-referrer');res.setHeader('Cache-Control','no-store');
  if(url.pathname.startsWith('/api/')){
   const name=url.pathname.slice(5);
   if(!['answer','transcribe','config'].includes(name))throw Object.assign(new Error('Missing API route'),{status:404});
   if(name!=='config'&&req.method!=='POST')throw Object.assign(new Error('Method not allowed'),{status:405});
   const ip=req.socket.remoteAddress||'unknown';const now=Date.now();const entry=hits.get(ip)||[];const recent=entry.filter(t=>now-t<60000);if(recent.length>30)throw Object.assign(new Error('Too many requests; retry shortly'),{status:429});recent.push(now);hits.set(ip,recent);
   let obj={};if(req.method==='POST'){let size=0;const chunks=[];for await(const c of req){size+=c.length;if(size>5_000_000)throw Object.assign(new Error('Request too large'),{status:413});chunks.push(c)}obj=JSON.parse(Buffer.concat(chunks).toString('utf8'))}
   const out=await handler(name,obj);res.writeHead(out.status||200,{'Content-Type':'application/json;charset=utf-8'});res.end(JSON.stringify(out));return;
  }
  const allowed=['/','/index.html','/app.js','/styles.css','/manifest.json','/icon.svg','/sw.js'];
  const p=url.pathname==='/'?'/index.html':url.pathname;
  if(!allowed.includes(p))throw Object.assign(new Error('Not found'),{status:404});
  const full=path.join(DIR,'public',p);
  const bytes=await fs.readFile(full);res.writeHead(200,{'Content-Type':mime[path.extname(full)]||'application/octet-stream','Cache-Control':p==='/sw.js'?'no-cache':'public,max-age=3600'});res.end(bytes);
 }catch(e){const status=e.status||500;res.writeHead(status,{'Content-Type':'application/json'});res.end(JSON.stringify({error:status===500?'Internal server error':e.message}));if(status===500)console.error(e)}
}).listen(port,()=>console.log(`K4RMA Live Copilot http://localhost:${port}`));
