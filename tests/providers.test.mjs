import test from 'node:test';import assert from 'node:assert/strict';
import {providerPayload,allowedModel,generate,candidates} from '../lib/providers.mjs';
test('OpenAI request uses strict structured output and configured model',()=>{
 const req=providerPayload('openai','gpt-4.1-mini','system','question');
 assert.equal(req.model,'gpt-4.1-mini');assert.equal(req.response_format.type,'json_schema');assert.equal(req.response_format.json_schema.strict,true);
});
test('Anthropic request follows Messages API schema contract',()=>{
 const req=providerPayload('anthropic','claude-haiku-4-5-20251001','system','question');
 assert.equal(req.system,'system');assert.equal(req.output_config.format.type,'json_schema');assert.equal(req.messages[0].role,'user');
});
test('OpenRouter paid model is blocked without explicit opt-in',()=>{
 const p=process.env.ALLOW_PAID_OPENROUTER;try{delete process.env.ALLOW_PAID_OPENROUTER;assert.equal(allowedModel('openrouter','vendor/model'),false);assert.equal(allowedModel('openrouter','vendor/model:free'),true)}finally{if(p===undefined)delete process.env.ALLOW_PAID_OPENROUTER;else process.env.ALLOW_PAID_OPENROUTER=p}
});
test('Paid fallback is opt-in if Groq fails',()=>{
 const prev={GROQ_API_KEY:process.env.GROQ_API_KEY,OPENAI_API_KEY:process.env.OPENAI_API_KEY,ANSWER_PROVIDER:process.env.ANSWER_PROVIDER,ALLOW_PAID_FALLBACK:process.env.ALLOW_PAID_FALLBACK};
 try{process.env.GROQ_API_KEY='mock';process.env.OPENAI_API_KEY='mock';process.env.ANSWER_PROVIDER='groq';delete process.env.ALLOW_PAID_FALLBACK;assert.ok(!candidates('auto').includes('openai'));process.env.ALLOW_PAID_FALLBACK='true';assert.ok(candidates('auto').includes('openai'))}
 finally{for(const [k,v]of Object.entries(prev)){if(v===undefined)delete process.env[k];else process.env[k]=v}}
});
test('OpenAI provider parses direct JSON response without leaking key',async()=>{
 const old=process.env.OPENAI_API_KEY;
 try{process.env.OPENAI_API_KEY='private-key';let endpoint='',auth='';const fake=async(u,options)=>{endpoint=u;auth=options.headers.Authorization;return new Response(JSON.stringify({choices:[{message:{content:JSON.stringify({answer:'X',explanation:'',question_understood:'Y',caveat:'',evidence_ids:['S22'],confidence:'high'})}}]}),{status:200})};const got=await generate('s','q',{provider:'openai',fetcher:fake});assert.equal(got.answer,'X');assert.match(endpoint,/api\.openai\.com/);assert.equal(auth,'Bearer private-key')}
 finally{if(old===undefined)delete process.env.OPENAI_API_KEY;else process.env.OPENAI_API_KEY=old}
});
