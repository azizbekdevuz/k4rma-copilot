import http from 'node:http';
import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {handler} from './lib/http.mjs';
import {authorize} from './lib/security.mjs';
const root=path.dirname(fileURLToPath(import.meta.url));
const mime={'.html':'text/html;charset=utf-8','.js':'text/javascript;charset=utf-8','.css':'text/css;charset=utf-8','.json':'application/json;charset=utf-8','.svg':'image/svg+xml'};
const PORT=Number(process.env.PORT||3000);
const allowed=new Set(['/','/index.html','/app.js','/styles.css','/manifest.json','/icon.svg','/sw.js']);
function headers(res){res.setHeader('X-Content-Type-Options','nosniff');res.setHeader('Referrer-Policy','no-referrer');res.setHeader('X-Frame-Options','DENY');res.setHeader('Permissions-Policy','microphone=(self),camera=(),geolocation=()');res.setHeader('Content-Security-Policy',"default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self'; media-src 'self' blob:; object-src 'none'; base-uri 'none'; frame-ancestors 'none'");res.setHeader('Cache-Control','no-store');}
async function readBody(req){const parts=[];let length=0;for await(const part of req){length+=part.length;if(length>4_900_000){const e=new Error('Request body too large');e.status=413;throw e;}parts.push(part)}
 try{return JSON.parse(Buffer.concat(parts).toString('utf8'))}catch{const e=new Error('Invalid JSON request');e.status=400;throw e}}
function send(res,out){res.writeHead(out.status||200,{'Content-Type':'application/json;charset=utf-8'});res.end(JSON.stringify(out));}
const server=http.createServer(async(req,res)=>{
 headers(res);
 try{const url=new URL(req.url,'http://localhost');
   if(url.pathname.startsWith('/api/')){
     const route=url.pathname.slice(5);if(!['config','answer','transcribe'].includes(route))return send(res,{status:404,error:'Not found'});
     if(req.method!==(route==='config'?'GET':'POST'))return send(res,{status:405,error:'Method not allowed'});
     const body=route==='config'?{}:await readBody(req);
     const rejected=await authorize(req,route,body);if(rejected)return send(res,rejected);
     return send(res,await handler(route,body));
   }
   if(req.method!=='GET'&&! (req.method==='HEAD'))return send(res,{status:405,error:'Method not allowed'});
   const pathname=url.pathname==='/'?'/index.html':url.pathname;
   if(!allowed.has(pathname))return send(res,{status:404,error:'Not found'});
   const bytes=await fs.readFile(path.join(root,'public',pathname));
   res.writeHead(200,{'Content-Type':mime[path.extname(pathname)]||'application/octet-stream','Cache-Control':pathname==='/sw.js'?'no-cache':'public, max-age=300'});
   if(req.method==='HEAD')res.end();else res.end(bytes);
 }catch(e){const status=e.status||500;if(status===500)console.error('Server error:',e.message);send(res,{status,error:status===500?'Internal server error':e.message});}
});
server.listen(PORT,process.env.APP_PIN?'0.0.0.0':'127.0.0.1',()=>console.log(`K4RMA Signal listening on http://localhost:${PORT}`));
