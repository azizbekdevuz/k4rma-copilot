/** Replace archived source + partial patches with a verified local Git checkout.
 * Usage: node tools/sync-source.mjs "D:\\path\\K4RMA" */
import fs from 'node:fs';import path from 'node:path';import crypto from 'node:crypto';import {execFileSync} from 'node:child_process';
const repo=process.argv[2];if(!repo){console.error('Usage: node tools/sync-source.mjs <local-K4RMA-repo-path>');process.exit(2)}
const root=path.resolve(repo),base=path.join(root,'Assets','K4RMA');if(!fs.existsSync(base)){console.error('No Assets/K4RMA in selected repository');process.exit(2)}
let commit='unknown',branch='unknown',dirty='unknown';
try{commit=execFileSync('git',['-C',root,'rev-parse','HEAD'],{encoding:'utf8'}).trim();branch=execFileSync('git',['-C',root,'branch','--show-current'],{encoding:'utf8'}).trim();dirty=execFileSync('git',['-C',root,'status','--porcelain'],{encoding:'utf8'}).trim()?'dirty':'clean';}catch{}
if(dirty==='dirty')console.warn('WARNING: Git workspace is dirty; code index may differ from commit '+commit);
const target=new URL('../knowledge.json',import.meta.url);const kb=JSON.parse(fs.readFileSync(target,'utf8'));
const prior=kb.documents.filter(d=>d.kind==='submitted-slide');const imported=[];
function walk(folder){for(const entry of fs.readdirSync(folder,{withFileTypes:true})){const p=path.join(folder,entry.name);if(entry.isDirectory())walk(p);else if(entry.isFile()&&p.endsWith('.cs')){
const rel=path.relative(root,p).replaceAll('\\','/');const lines=fs.readFileSync(p,'utf8').split(/\r?\n/);const section=rel.includes('/RisingSlash/')?'rising':rel.includes('/SwordWave/')?'sword':rel.includes('/Guard/')?'guard':rel.includes('/Enemies/')||rel.includes('/Core/')?'boss':'general';
for(let i=0;i<lines.length;i+=55){const start=Math.max(0,i-6),end=Math.min(lines.length,i+60);const text=lines.slice(start,end).map((line,j)=>`${String(start+j+1).padStart(4,'0')} ${line}`).join('\n');const id='C-'+crypto.createHash('sha256').update(rel+start+commit).digest('hex').slice(0,12);imported.push({id,path:`${rel}:${start+1}-${end}`,kind:'current-source',text,section,priority:30,slide:null});}
}}}
walk(base);
kb.documents=[...imported,...prior];kb.metadata={...kb.metadata,generated_from:`Local Git branch ${branch} at ${commit} (${dirty}) plus submitted deck`,limitations:dirty==='dirty'?'Source checkout contains uncommitted changes; commit ID alone does not fully identify contents. Unity runtime not verified.':'Code indexed from Git checkout; Unity runtime not verified.',branch,commit,workspace:dirty,counts:{current_source:imported.length,slides:prior.length},slide_count:prior.length};
fs.writeFileSync(target,JSON.stringify(kb),'utf8');
console.log(`Indexed ${imported.length} code excerpts and ${prior.length} slides from ${branch} @ ${commit} (${dirty}).`);
