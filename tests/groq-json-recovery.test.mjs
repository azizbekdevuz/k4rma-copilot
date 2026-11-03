import test from 'node:test';import assert from 'node:assert/strict';
import {providerPayload,generate} from '../lib/providers.mjs';
test('Groq uses strict schema instead of fragile JSON object mode',()=>{
 const p=providerPayload('groq','openai/gpt-oss-120b','instructions','prompt');
 assert.equal(p.response_format.type,'json_schema');assert.equal(p.response_format.json_schema.strict,true);
 assert.equal(p.include_reasoning,false);assert.equal(p.reasoning_effort,'low');
 assert.deepEqual(p.response_format.json_schema.schema.required,['answer','explanation','question_understood','caveat','evidence_ids','confidence']);
});
test('retryable provider JSON error fails safely without unbounded retries',async()=>{
 const previous=process.env.GROQ_API_KEY;const pref=process.env.ANSWER_PROVIDER;
 try{process.env.GROQ_API_KEY='test-key';process.env.ANSWER_PROVIDER='groq';let calls=0;
 const fake=async()=>{calls++;return new Response(JSON.stringify({error:{code:'json_validate_failed'}}),{status:400})};
 await assert.rejects(generate('s','u',{provider:'groq',fetcher:fake,deadlineMs:1800}),/deadline/i);
 assert.equal(calls,0);
 }finally{if(previous===undefined)delete process.env.GROQ_API_KEY;else process.env.GROQ_API_KEY=previous;if(pref===undefined)delete process.env.ANSWER_PROVIDER;else process.env.ANSWER_PROVIDER=pref;}
});
test('provider answer and citations parse under strict contract',async()=>{
 const previous=process.env.GROQ_API_KEY;try{process.env.GROQ_API_KEY='test-key';let calls=0;
 const fake=async()=>{calls++;return new Response(JSON.stringify({choices:[{message:{content:JSON.stringify({answer:'A',explanation:'B',question_understood:'C',caveat:'',evidence_ids:['S22'],confidence:'medium'})}}]}),{status:200})};
 const result=await generate('s','u',{provider:'groq',fetcher:fake});assert.equal(result.answer,'A');assert.equal(calls,1);
 }finally{if(previous===undefined)delete process.env.GROQ_API_KEY;else process.env.GROQ_API_KEY=previous;}
});
