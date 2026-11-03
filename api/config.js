import {handler} from '../lib/http.mjs';
export default async function(req,res){const output=await handler('config',{});res.status(200).json(output)}
