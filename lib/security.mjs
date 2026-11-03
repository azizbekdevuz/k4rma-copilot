import crypto from 'node:crypto';
const windows=new Map();
function pinMatches(input){
 const expected=process.env.APP_PIN;
 if(!expected)return process.env.NODE_ENV==='production'?false:true;
 if(typeof input!=='string')return false;
 const a=crypto.createHash('sha256').update(input).digest();
 const b=crypto.createHash('sha256').update(expected).digest();
 return crypto.timingSafeEqual(a,b);
}
function securityConfig(){
 if(process.env.NODE_ENV==='production'&&(!process.env.APP_PIN||process.env.APP_PIN.length<8))return 'Production requires APP_PIN with at least eight characters.';
 if(process.env.VERCEL&&(!process.env.UPSTASH_REDIS_REST_URL||!process.env.UPSTASH_REDIS_REST_TOKEN))return 'Vercel production requires Upstash Redis REST credentials for distributed rate limits.';
 return null;
}
function localCount(key,max,period){const now=Date.now();if(windows.size>12000)for(const [k,v] of windows)if(v.until<now)windows.delete(k);
 const item=windows.get(key);const value=item&&item.until>now?item:{count:0,until:now+period};value.count++;windows.set(key,value);return value.count<=max;}
async function distributedCount(key,max,period){
 if(!process.env.UPSTASH_REDIS_REST_URL)return localCount(key,max,period);
 const bucket=Math.floor(Date.now()/period);const redisKey=`k4rma:${key}:${bucket}`;
 const url=process.env.UPSTASH_REDIS_REST_URL.replace(/\/$/,'')+'/pipeline';
 const r=await fetch(url,{method:'POST',headers:{'Authorization':`Bearer ${process.env.UPSTASH_REDIS_REST_TOKEN}`,'Content-Type':'application/json'},body:JSON.stringify([['INCR',redisKey],['EXPIRE',redisKey,Math.ceil(period/1000)+2]]),signal:AbortSignal.timeout(3000)});
 if(!r.ok)throw Error('Rate limit service unavailable');const result=await r.json();const count=Number(result?.[0]?.result);if(!Number.isFinite(count))throw Error('Rate limit service invalid response');return count<=max;
}
function requestIP(req){const src=process.env.VERCEL?req.headers['x-forwarded-for']?.split(',')[0]?.trim():req.socket?.remoteAddress||'local';return crypto.createHash('sha256').update(src||'unknown').digest('hex').slice(0,24);}
async function authorize(req,type,body){
 const problem=securityConfig();if(problem)return {status:503,error:problem};
 const ip=requestIP(req);const caps={answer:[12,60000],transcribe:[15,60000],config:[90,60000]};const [limit,period]=caps[type]||[20,60000];
 try{
   if(!await distributedCount(`req:${ip}:${type}`,limit,period))return {status:429,error:'Rate limit reached. Try again shortly.'};
   if(type!=='config'&&!pinMatches(body?.pin)){
     if(!await distributedCount(`auth:${ip}`,5,15*60*1000))return {status:429,error:'Too many invalid access attempts.'};
     return {status:401,error:'Invalid access PIN.'};
   }
 }catch{return {status:503,error:'Security rate limiter unavailable. Requests are blocked to protect your API quota.'}}
 return null;
}
export {pinMatches,securityConfig,authorize,localCount};
