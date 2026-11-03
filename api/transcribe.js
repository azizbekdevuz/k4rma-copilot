import {handler} from '../lib/http.mjs';
export default async function(req,res){if(req.method!=='POST')return res.status(405).json({error:'Method not allowed'});const output=await handler('transcribe',req.body||{});res.status(output.status||200).json(output)}
