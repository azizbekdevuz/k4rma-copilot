import test from 'node:test';import assert from 'node:assert/strict';
import {pinMatches,securityConfig,localCount} from '../lib/security.mjs';
import {stitchTranscript,normalizedQuestion} from '../public/audio-utils.js';
import {retrieve} from '../lib/retrieval.mjs';
test('PIN uses secure comparison and rejects bad credentials',()=>{const p=process.env.APP_PIN;try{process.env.APP_PIN='secret-long-value';assert.equal(pinMatches('secret-long-value'),true);assert.equal(pinMatches('bad'),false);}finally{if(p===undefined)delete process.env.APP_PIN;else process.env.APP_PIN=p;}});
test('production deployment rejects weak PIN and Vercel without a shared limiter',()=>{const env={...process.env};try{process.env.NODE_ENV='production';process.env.APP_PIN='1234';assert.match(securityConfig(),/eight/);process.env.APP_PIN='correct-horse-battery';process.env.VERCEL='1';delete process.env.UPSTASH_REDIS_REST_URL;assert.match(securityConfig(),/Upstash/);}finally{for(const k of ['NODE_ENV','APP_PIN','VERCEL','UPSTASH_REDIS_REST_URL']){if(env[k]===undefined)delete process.env[k];else process.env[k]=env[k];}}});
test('IP rate limit enforces threshold',()=>{const key='test-'+Date.now();assert.equal(localCount(key,2,60000),true);assert.equal(localCount(key,2,60000),true);assert.equal(localCount(key,2,60000),false)});
test('unrelated questions are not forced into current slide evidence',()=>assert.equal(retrieve('왜 잘못하면 F 입니까?',{slide:22}).length,0));
test('overlapping Korean transcripts are stitched rather than duplicated',()=>{const a='UP04에서 왜 Sin과 Cos를',b='왜 Sin과 Cos를 사용하나요?';const merged=stitchTranscript(a,b);assert.equal(merged,'UP04에서 왜 Sin과 Cos를 사용하나요?');assert.equal(normalizedQuestion('UP04?'),'up04');});
