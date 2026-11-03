import test from 'node:test';import assert from 'node:assert/strict';import {retrieve,isQuestion,answer,config} from '../lib/core.mjs';
test('knowledge contains all 28 submitted slides',()=>assert.equal(config().knowledge.slide_count,28));
test('recognizes Korean question',()=>assert.equal(isQuestion('UP04에서 왜 Sin과 Cos를 사용하나요?'),true));
test('ignores ordinary short narration',()=>assert.equal(isQuestion('공격을 실행합니다.'),false));
test('retrieves later diff for latest UP01 tracking',()=>assert.ok(retrieve('UP01 tracking SampleLanding LockLanding',{slide:19,section:'rising'}).some(r=>r.kind==='later-diff')));
test('retrieves presentation for slide',()=>assert.ok(retrieve('UP04 파편 패링',{slide:22}).some(r=>r.kind==='submitted-slide')));
test('does not fabricate AI response without key',async()=>{const a=await answer({question:'How does UP04 aim 14 fragments?',slide:22});assert.ok(['evidence-only','generated'].includes(a.mode));assert.ok(Array.isArray(a.sources));});
test('rejects trivial question',async()=>{const a=await answer({question:'x'});assert.equal(a.status,400)});
