import fs from 'node:fs';
const KB=JSON.parse(fs.readFileSync(new URL('../knowledge.json',import.meta.url),'utf8'));
const aliases={
 '상승베기':['rising','up01','up02','up03','up04','up05'], '낙하':['up01','landing','drop'],
 '추적':['tracking','target','samplelanding'], '패링':['parry','parrystun','pendingparry'],
 '파편':['fragment','fragmentdegrees','up04'], '검기':['sword','wave'], '방어':['guard','shield'],
 '선택':['tryselect','canselect','beginattack'], '계승':['inheritance','buildcycle'],
 '고유화':['personalization','confirmchoice'], '코루틴':['coroutine','runpattern'],
 '각도':['sin','cos','aimfragment'], '연속':['upbasic','basicstrike'],
 '필살기':['ultimate','up05','sw05','gd05'], '충돌':['collision','contact'],
 '기절':['stun','stunned'], '체력':['health','damage'], '회피':['dodge','escape'],
 '공정':['warning','telegraph','recovery'], '왜':['why'], '어떻게':['how']
};
const stop=new Set(['the','and','for','with','from','this','that','what','does','your','about','have','will','when','how','why','code','game','player','attack','pattern','explain','사용','어떻게','이유','무엇','있나요','합니다','있습니다','위해','합니다']);
const sectionSlide={boss:[1,9],personalization:[10,13],sword:[14,18],rising:[19,23],guard:[24,28]};
function sectionForSlide(s){return Object.entries(sectionSlide).find(([,v])=>s>=v[0]&&s<=v[1])?.[0]||'boss'}
function termset(s){
 let raw=String(s||'').toLowerCase();const base=new Set(raw.match(/(?:upbasic|up0[1-5]|sw0[1-5]|gd0[1-5])|[a-z][a-z0-9_]{2,}|[가-힣]{2,}/g)||[]);
 for(const [k,values] of Object.entries(aliases))if(raw.includes(k))values.forEach(t=>base.add(t));
 for(const s of [...base])if(stop.has(s))base.delete(s);
 return base;
}
const indexed=KB.documents.map((d,i)=>{const p=String(d.path||'').toLowerCase();return {...d,_order:i,_path:p,_text:String(d.text||'').toLowerCase(),_terms:termset(d.path+' '+d.text)};});
function retrieve(query,{slide=19,section='auto',limit=5}={}){
 const s=Math.max(1,Math.min(28,Number(slide)||19));const topic=section==='auto'?sectionForSlide(s):section;
 const q=termset(query);if(q.size===0)return [];
 const exact=[...q].filter(x=>/^(?:upbasic|up0[1-5]|sw0[1-5]|gd0[1-5]|karma[a-z0-9_]+|rising[a-z0-9_]+)$/.test(x));
 const scored=[];
 for(const d of indexed){let matches=0,score=0,technical=0;
   for(const tok of q){if(tok==='why'||tok==='how')continue;
     const inPath=d._path.includes(tok),inText=d._text.includes(tok);
     if(!inPath&&!inText)continue;
     matches++; const special=exact.includes(tok);
     score+=inPath?(special?15:8):(special?9:2.2);
     if(special)technical++;
   }
   // Slide context breaks close ties, but never creates relevance on its own.
   if(matches===0)continue;
   if(d.kind==='later-diff'&&technical)score+=7;
   if(d.kind==='current-source')score+=8;
   if(d.kind==='archived-source' || d.kind==='source')score+=2;
   if(d.kind==='submitted-slide'&&d.slide===s)score+=Math.min(3,matches);
   if(d.section===topic)score+=Math.min(2,matches);
   if(matches===1&&technical===0)score*=0.55;
   if(score>=5)scored.push({d,score,matches,technical});
 }
 scored.sort((a,b)=>b.score-a.score||a.d._order-b.d._order);
 if(!scored.length)return [];
 // Require a genuine term match and disallow generic slide-context-only evidence.
 const chosen=[],counts=new Map();
 for(const item of scored){const d=item.d;const key=d.path.split(':')[0];if((counts.get(key)||0)>=2)continue;
   chosen.push({...d,score:Number(item.score.toFixed(1))});counts.set(key,(counts.get(key)||0)+1);
   if(chosen.length>=Math.max(1,Math.min(6,limit)))break;
 }
 return chosen;
}
function evidenceContext(items,maxChars=5700){let budget=Math.max(1000,maxChars);const pieces=[];
 for(const d of items){const header=`[${d.id}] ${d.kind} ${d.path}\n`;if(header.length+250>budget)break;
 const excerpt=d.text.slice(0,Math.min(1900,budget-header.length));pieces.push(header+excerpt);budget-=header.length+excerpt.length;
 }return pieces.join('\n\n');}
export {KB,termset,retrieve,evidenceContext,sectionForSlide};
