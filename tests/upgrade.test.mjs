import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import {answer, config, retrieve} from '../lib/core.mjs';
const ui=fs.readFileSync(new URL('../public/app.js',import.meta.url),'utf8');
const page=fs.readFileSync(new URL('../public/index.html',import.meta.url),'utf8');
test('interface has distinct presenting and Q&A modes',()=>{
 assert.match(page,/id="modeTeam"/);assert.match(page,/id="modeQA"/);
 assert.match(ui,/receivedMode!=='qa'/);
});
test('all named UI translation keys exist in English and Korean',()=>{
 const en=ui.match(/const EN=\{([\s\S]*?)\};\nconst KO=/)[1];
 const ko=ui.match(/const KO=\{([\s\S]*?)\};\nconst topics=/)[1];
 const keys=[...en.matchAll(/(?:^|,)\s*([a-zA-Z][\w]*)\s*:/g)].map(m=>m[1]);
 for(const key of keys)assert.match(ko,new RegExp('(?:^|,)\\s*'+key+'\\s*:'),'missing translation '+key);
 assert.ok(keys.length>75);
});
test('source answer refuses to invent content when no key configured',async()=>{
 const r=await answer({question:'How does UP01 lock the landing point?',slide:19});
 assert.equal(r.mode,'evidence-only');assert.ok(r.sources.length);
});
test('free tier safeguards reject accidentally configured paid OpenRouter model',async()=>{
 let old=process.env.OPENROUTER_MODEL,oldG=process.env.GROQ_API_KEY,oldO=process.env.OPENROUTER_API_KEY;
 try{process.env.OPENROUTER_MODEL='some/paid-model';process.env.OPENROUTER_API_KEY='test';delete process.env.GROQ_API_KEY;
 const r=await answer({question:'Why is UP04 using cosine?',provider:'openrouter',slide:22});
 assert.equal(r.status,400);assert.match(r.error,/free/);
 }finally{if(old===undefined)delete process.env.OPENROUTER_MODEL;else process.env.OPENROUTER_MODEL=old;if(oldG===undefined)delete process.env.GROQ_API_KEY;else process.env.GROQ_API_KEY=oldG;if(oldO===undefined)delete process.env.OPENROUTER_API_KEY;else process.env.OPENROUTER_API_KEY=oldO;}
});
test('new source retrieval preserves later diff relevance',()=>{
 const items=retrieve('UPBasic combo three strikes BasicStrike', {slide:19,section:'rising'});
 assert.ok(items.some(x=>x.kind==='later-diff'));
});
