import fs from 'node:fs';
const KB=JSON.parse(fs.readFileSync(new URL('../knowledge.json',import.meta.url),'utf8'));
const API_GROQ='https://api.groq.com/openai/v1';
const API_OPENROUTER='https://openrouter.ai/api/v1';
const synonyms={
 '상승베기':['rising','up01','up02','up03','up04','up05','risingbossbehavior'],
 '낙하':['up01','landing','drop','track','lock'], '추적':['tracking','samplelanding','target','up01','up02'],
 '패링':['parry','parrystun','pendingparry','up04','gd02'], '파편':['fragment','fragmentdegrees','up04'],
 '방어':['guard','shield','gd01'], '검기':['sword','wave','sw01'], '필살기':['ultimate','up05','sw05'],
 '보스':['karmaenemy','boss'], '고유화':['personalization','confirmchoice'], '계승':['inheritance','buildcycle'],
 '선택':['tryselect','canselect','pursuitschedule','beginattack'], '코루틴':['coroutine','runpattern','execute'],
 '각도':['sin','cos','fragmentdegrees','aimfragment'], 'sin':['up04','aimfragment','fragmentdegrees'],
 'cos':['up04','aimfragment','fragmentdegrees'], 'clamp':['up01','clamplandingx'],
 '체력':['health','damage'], '공격':['attack','pattern'], '연속':['upbasic','basicstrike'],
 'melee':['upbasic','groundstrike'], 'combo':['upbasic','basicstrike'],
 'jump':['up01','up04'], 'lock':['up01','up02'], 'state':['karmaenemy','statemachine'],
 'frequency':['tryselect','pursuitschedule'], 'priority':['tryselect','pursuitschedule'],
 'fragment':['up04','aimfragment'], 'swept':['up05','motion']
};
function tokens(text){
  const enriched=String(text||'').replace(/([a-z])([A-Z])/g,'$1 $2').toLowerCase();
  const tokens=(enriched.match(/[a-z][a-z0-9_]{1,}|up0[1-5]|gd0[1-5]|sw0[1-5]|[가-힣]{2,}/g)||[]);
  const full=[...tokens];
  for(const tok of tokens) if(synonyms[tok]) full.push(...synonyms[tok]);
  return [...new Set(full)];
}
const docs=KB.documents.map(d=>({...d,search:(d.path+' '+d.text).toLowerCase(),terms:new Set(tokens(d.path+' '+d.text))}));
const slideSection=s=>s>=19&&s<=23?'rising':s>=24?'guard':s>=14?'sword':s>=10?'personalization':'boss';
function isQuestion(s){
  const v=String(s||'').trim();
  if(v.length<7)return false;
  return /[?？]|(왜|어떻게|무엇|뭐|어떤|무슨|설명|차이|이유|원리|어디|언제|몇|가능|맞나요|인가요|하나요|되나요|했나요|했습니까|습니까|까요|나요|죠|죠\?)|(why|how|what|which|where|when|explain|difference|can you|could you|does it|is it)/i.test(v);
}
function retrieve(query,{slide=19,section='rising',limit=10}={}){
 const t=tokens(query); const selected=section==='auto'?slideSection(+slide):section;
 const normalized=String(query).toLowerCase();
 const scores=docs.map(d=>{
   let score=0;
   for(const w of t){if(d.terms.has(w))score+=(/^(up|sw|gd)0[1-5]$|^upbasic$/.test(w)?7:2); if(d.path.toLowerCase().includes(w))score+=3;}
   if(d.section===selected)score+=4;
   if(d.kind==='later-diff')score+=Math.min(13,t.some(v=>d.search.includes(v))?13:0);
   if(d.kind==='submitted-slide' && d.slide===+slide)score+=18;
   if(d.kind==='submitted-slide' && Math.abs(d.slide-(+slide))<=1)score+=2;
   return {doc:d,score};
 }).filter(r=>r.score>5).sort((a,b)=>b.score-a.score);
 // Diversity: include authoritative code & slide; avoid entire result being different chunks of one file.
 const chosen=[];const perFile=new Map();
 for(const r of scores){
   const base=r.doc.path.split(':')[0].split(' [')[0];
   if((perFile.get(base)||0)>=2)continue;
   perFile.set(base,(perFile.get(base)||0)+1);
   chosen.push({...r.doc,score:r.score});
   if(chosen.length>=limit)break;
 }
 return chosen;
}
function checkPin(pin){const expected=process.env.APP_PIN;if(!expected)return true;return typeof pin==='string'&&pin===expected;}
function config(){return {groq:!!process.env.GROQ_API_KEY,openrouter:!!process.env.OPENROUTER_API_KEY,needsPin:!!process.env.APP_PIN,knowledge:KB.metadata,freeOnly:true};}
async function answer({question,slide=19,section='auto',language='en',provider='auto',history=[],pin}={}){
 if(!checkPin(pin))return {status:401,error:'Incorrect access PIN.'};
 question=String(question||'').trim().slice(0,1400);
 const context=Array.isArray(history)?history.slice(-3).map(x=>String(x).slice(0,250)).join(' | '):'';
 if(question.length<3)return {status:400,error:'Enter a question.'};
 slide=Math.min(28,Math.max(1,Number(slide)||19));
 section=['auto','boss','personalization','sword','rising','guard'].includes(section)?section:'auto';
 const selected=retrieve((context+' '+question).trim(),{slide,section});
 const evidence=selected.map(d=>({id:d.id,path:d.path,kind:d.kind,section:d.section,preview:d.text.slice(0,1100)}));
 if(!selected.length) return {status:200,mode:'unsupported',answer:'No relevant evidence found in the imported sources. I cannot answer reliably.',sources:[]};
 const groq=process.env.GROQ_API_KEY, openrouter=process.env.OPENROUTER_API_KEY;
 const use=provider==='groq'?'groq':provider==='openrouter'?'openrouter':groq?'groq':'openrouter';
 const routerModel=process.env.OPENROUTER_MODEL||'openrouter/free';
 if(use==='openrouter'&&routerModel!=='openrouter/free'&&!routerModel.endsWith(':free'))return {status:400,error:'Only free OpenRouter model IDs are allowed (openrouter/free or :free).'};
 if((use==='groq'&&!groq)||(use==='openrouter'&&!openrouter)){
   return {status:200,mode:'evidence-only',answer:'AI key not configured. These are matching source excerpts, NOT a generated answer.',sources:evidence,provider:'offline'};
 }
 const contexts=selected.map(d=>`[${d.id}] TYPE=${d.kind} PATH=${d.path}\n${d.text.slice(0,d.kind==='later-diff'?2200:1400)}`).join('\n\n');
 const instruction=`You are the evidence-grounded oral-exam assistant for K4RMA. The user asks a professor-level technical question in Korean or English. Return a directly speakable answer in ${language==='ko'?'KOREAN':'ENGLISH'}, with substantial implementation detail, NOT generic summaries. Target 3-5 sentences: (1) direct answer, (2) actual C# class/method/data flow, (3) why this design choice matters for behavior/correctness, (4) important guard/limitation when supported. If the professor asks why, explain rationale; if how, explain execution sequence; if compare, contrast both. Decode short Korean questions and follow-ups against previous-question context. Do not invent numeric parameters, APIs, tests or statements about actual Unity Play Mode. IMPORTANT priority: later-diff snippets are later changes relative to archived-source, and submitted-slide may be older; explicitly distinguish versions when conflicting. When evidence does not answer, admit uncertainty rather than filling gaps. Cite evidence IDs accurately; no citations outside the supplied excerpts. Return ONLY JSON fields: answer (string), evidence_ids (array of matching IDs), confidence (high|medium|low), caveat (string), explanation (string: optional one-sentence deeper technical point), question_understood (string: concise English paraphrase of the actual question). A claimed high confidence requires direct cited implementation evidence, not merely slides. Do not use high confidence if the selected snippets contradict each other or are only diff fragments.`;
 const payload={model:use==='groq'?(process.env.GROQ_CHAT_MODEL||'openai/gpt-oss-120b'):routerModel,temperature:0.1,max_completion_tokens:680,messages:[{role:'system',content:instruction},{role:'user',content:`Current slide: ${slide}, section: ${section}. Previous questions (context only): ${context || 'None'}\nQuestion: ${question}\n\nSOURCE EVIDENCE:\n${contexts}`}]};
 if(use==='groq'){payload.response_format={type:'json_object'};payload.reasoning_effort=process.env.GROQ_REASONING_EFFORT||'medium';}
 const url=use==='groq'?`${API_GROQ}/chat/completions`:`${API_OPENROUTER}/chat/completions`;
 const key=use==='groq'?groq:openrouter;
 let response;
 try{response=await fetch(url,{method:'POST',headers:{'Content-Type':'application/json','Authorization':'Bearer '+key,...(use==='openrouter'?{'HTTP-Referer':'https://github.com/azizbekdevuz/K4RMA','X-Title':'K4RMA Live Copilot'}:{})},body:JSON.stringify(payload),signal:AbortSignal.timeout(22000)});}
 catch(e){return {status:502,error:'Provider could not be reached: '+e.message,sources:evidence}};
 if(!response.ok){const err=(await response.text()).slice(0,300);return {status:502,error:`${use} API ${response.status}: ${err}`,sources:evidence}};
 const raw=await response.json();const text=raw.choices?.[0]?.message?.content||'';
 let parsed;try{parsed=JSON.parse(text.replace(/^```json\s*|\s*```$/g,''));}catch{parsed={answer:String(text).slice(0,1000),evidence_ids:[],confidence:'low',caveat:'Model returned unstructured text.'}};
 const ids=new Set(evidence.map(x=>x.id));const used=Array.isArray(parsed.evidence_ids)?parsed.evidence_ids.filter(x=>ids.has(x)):[];
 return {status:200,mode:'generated',answer:String(parsed.answer||'No answer returned.').slice(0,1200),confidence:used.length?['high','medium','low'].includes(parsed.confidence)?parsed.confidence:'medium':'low',explanation:String(parsed.explanation||'').slice(0,360),questionUnderstood:String(parsed.question_understood||'').slice(0,260),caveat:String(parsed.caveat||'').slice(0,400),sources:evidence.filter(e=>used.includes(e.id)),extraSources:evidence.filter(e=>!used.includes(e.id)).slice(0,2),provider:use,model:payload.model};
}
async function transcribe({audio,mimeType='audio/webm',pin}={}){
 if(!checkPin(pin))return {status:401,error:'Incorrect access PIN.'};
 if(!process.env.GROQ_API_KEY)return {status:503,error:'GROQ_API_KEY is missing; use manual question entry.'};
 if(!audio || typeof audio!=='string')return {status:400,error:'Missing audio.'};
 let bytes;try{bytes=Buffer.from(audio,'base64')}catch{return {status:400,error:'Invalid audio encoding.'}}
 if(bytes.length<500)return {status:400,error:'Recorded audio is too short.'};
 if(bytes.length>3_500_000)return {status:413,error:'Audio clip too large; use shorter clips.'};
 const allowed={'audio/webm':'webm','audio/ogg':'ogg','audio/mp4':'mp4','audio/wav':'wav','audio/mpeg':'mp3'};
 const type=Object.keys(allowed).find(k=>mimeType.startsWith(k))||'audio/webm';
 const form=new FormData();form.append('file',new Blob([bytes],{type}),`question.${allowed[type]}`);form.append('model',process.env.GROQ_TRANSCRIBE_MODEL||'whisper-large-v3-turbo');form.append('language','ko');form.append('prompt','K4RMA, Unity, C#, Mathf.Clamp, UP01, UP02, UP03, UP04, UP05, UPBasic, KarmaEnemy, KarmaGame, KarmaPatternCatalog, 상승베기, 검기, 방어, 코루틴, 패링, 고유화, 계승');form.append('response_format','json');form.append('temperature','0');
 let response;
 try{response=await fetch(`${API_GROQ}/audio/transcriptions`,{method:'POST',headers:{Authorization:'Bearer '+process.env.GROQ_API_KEY},body:form,signal:AbortSignal.timeout(25000)});}catch(e){return {status:502,error:'Speech API connection failed: '+e.message}}
 if(!response.ok)return {status:502,error:`Speech API ${response.status}: ${(await response.text()).slice(0,260)}`};
 const json=await response.json();const text=String(json.text||'').trim();
 return {status:200,text,isQuestion:isQuestion(text),language:'ko',source:'groq-whisper'};
}
export {answer,transcribe,config,isQuestion,retrieve,checkPin};
