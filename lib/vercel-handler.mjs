import {authorize} from './security.mjs';
import {handler} from './http.mjs';
export async function runVercel(req,res,type){
 try{
   res.setHeader('Cache-Control','no-store');res.setHeader('X-Content-Type-Options','nosniff');res.setHeader('Referrer-Policy','no-referrer');
   if(req.method!==(type==='config'?'GET':'POST'))return res.status(405).json({error:'Method not allowed'});
   const body=type==='config'?{}:req.body;
   if(type!=='config'&&(!body||typeof body!=='object'||JSON.stringify(body).length>4_900_000))return res.status(413).json({error:'Invalid or oversized request'});
   const rejected=await authorize(req,type,body);if(rejected)return res.status(rejected.status).json({error:rejected.error});
   const output=await handler(type,body);return res.status(output.status||200).json(output);
 }catch(e){console.error('API error:',e.message);return res.status(500).json({error:'Internal server error'});}
}
