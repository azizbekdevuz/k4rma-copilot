import {answer,transcribe,config} from './core.mjs';
export async function handler(type,input){
 if(type==='config')return {status:200,...config()};
 if(type==='answer')return await answer(input);
 if(type==='transcribe')return await transcribe(input);
 return {status:404,error:'Not found'};
}
