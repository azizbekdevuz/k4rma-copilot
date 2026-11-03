const SCHEMA={type:'object',additionalProperties:false,properties:{answer:{type:'string'},explanation:{type:'string'},question_understood:{type:'string'},caveat:{type:'string'},evidence_ids:{type:'array',items:{type:'string'}},confidence:{type:'string',enum:['high','medium','low']}},required:['answer','explanation','question_understood','caveat','evidence_ids','confidence']};
const endpoint={groq:'https://api.groq.com/openai/v1/chat/completions',openrouter:'https://openrouter.ai/api/v1/chat/completions',openai:'https://api.openai.com/v1/chat/completions',anthropic:'https://api.anthropic.com/v1/messages'};
const providerKeys={groq:'GROQ_API_KEY',openrouter:'OPENROUTER_API_KEY',openai:'OPENAI_API_KEY',anthropic:'ANTHROPIC_API_KEY'};
const providerModels={groq:['GROQ_CHAT_MODEL','openai/gpt-oss-120b'],openrouter:['OPENROUTER_MODEL','openrouter/free'],openai:['OPENAI_MODEL','gpt-4.1-mini'],anthropic:['ANTHROPIC_MODEL','claude-haiku-4-5-20251001']};
function configured(name){return Boolean(process.env[providerKeys[name]])}
function candidates(requested='auto'){
 const all=['groq','openai','anthropic','openrouter'];
 if(requested!=='auto'&&!all.includes(requested))return [];
 const preference=(process.env.ANSWER_PROVIDER||'auto').toLowerCase();
 const primary=requested==='auto'?preference:requested;
 const ranked=primary!=='auto'&&all.includes(primary)?[primary,...all.filter(x=>x!==primary)]:all;
 return [...new Set(ranked)].filter(name=>configured(name)&&(name==='groq'||name==='openrouter'||process.env.ALLOW_PAID_FALLBACK==='true'||name===primary));
}
function allowedModel(provider,model){
 if(provider==='openrouter'&&process.env.ALLOW_PAID_OPENROUTER!=='true')return model==='openrouter/free'||model.endsWith(':free');
 return true;
}
function providerPayload(provider,model,system,user){
 if(provider==='anthropic')return {model,max_tokens:950,temperature:0,system,messages:[{role:'user',content:user}],output_config:{format:{type:'json_schema',schema:SCHEMA}}};
 const req={model,messages:[{role:'system',content:system},{role:'user',content:user}],temperature:0,max_completion_tokens:provider==='groq'?1600:1000};
 if(provider==='groq'){
   req.reasoning_effort='low';req.include_reasoning=false;
   if(/^openai\/gpt-oss-(20|120)b$/.test(model))req.response_format={type:'json_schema',json_schema:{name:'k4rma_answer',strict:true,schema:SCHEMA}};
   else req.response_format={type:'json_object'};
 }else if(provider==='openai')req.response_format={type:'json_schema',json_schema:{name:'k4rma_answer',strict:true,schema:SCHEMA}};
 else req.response_format={type:'json_object'};
 return req;
}
class ProviderError extends Error{constructor(message,status=502,retryable=false){super(message);this.status=status;this.retryable=retryable}}
async function postModel(provider,system,user,remainingMs,fetcher=fetch){
 const key=process.env[providerKeys[provider]];const [env,fallback]=providerModels[provider];const model=process.env[env]||fallback;
 if(!allowedModel(provider,model))throw new ProviderError('OpenRouter model is not a free variant; change OPENROUTER_MODEL or explicitly opt into paid routing.',400,false);
 const headers={'Content-Type':'application/json'};
 if(provider==='anthropic'){headers['x-api-key']=key;headers['anthropic-version']='2023-06-01'}else headers.Authorization=`Bearer ${key}`;
 if(provider==='openrouter'){headers['HTTP-Referer']='https://github.com/azizbekdevuz/K4RMA';headers['X-Title']='K4RMA Signal'}
 let response;const budget=Math.min(remainingMs,provider==='groq'?11000:13500);
 if(budget<2500)throw new ProviderError('Answer deadline exceeded',504,false);
 try{response=await fetcher(endpoint[provider],{method:'POST',headers,body:JSON.stringify(providerPayload(provider,model,system,user)),signal:AbortSignal.timeout(budget)});}
 catch(e){throw new ProviderError(`Provider network or timeout failure (${provider})`,502,true)}
 if(!response.ok){let issue;try{issue=await response.json()}catch{issue={}};
   const code=issue?.error?.code;const retriable=[408,429,500,502,503,504].includes(response.status)||code==='json_validate_failed';
   throw new ProviderError(`${provider} returned ${response.status}${code?' ('+code+')':''}`,response.status===429?429:502,retriable);
 }
 let json;try{json=await response.json()}catch{throw new ProviderError('Provider returned malformed transport JSON',502,true)}
 const content=provider==='anthropic'?json.content?.filter(c=>c.type==='text').map(c=>c.text).join(''):json.choices?.[0]?.message?.content;
 if(!content||typeof content!=='string')throw new ProviderError('Provider returned no text answer',502,true);
 let parsed;try{parsed=JSON.parse(content.replace(/^```json\s*|\s*```$/g,''))}catch{throw new ProviderError('Provider returned invalid answer JSON',502,true)}
 if(!parsed||typeof parsed.answer!=='string'||!Array.isArray(parsed.evidence_ids)||!['high','medium','low'].includes(parsed.confidence))throw new ProviderError('Provider answer failed schema validation',502,true);
 return {...parsed,provider,model,usage:json.usage||null};
}
async function generate(system,user,{provider='auto',deadlineMs=23000,fetcher=fetch}={}){
 const order=candidates(provider);if(!order.length)throw new ProviderError('No answer API configured',503,false);
 const started=Date.now();let last=null;
 for(const name of order){
   const remaining=deadlineMs-(Date.now()-started);
   if(remaining<2500)break;
   try{return await postModel(name,system,user,remaining,fetcher)}
   catch(e){last=e;if(!e.retryable)break;}
 }
 throw last||new ProviderError('All providers exceeded the answer deadline',504,true);
}
export {SCHEMA,configured,candidates,allowedModel,generate,ProviderError,providerPayload};
