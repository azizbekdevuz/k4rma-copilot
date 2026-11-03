/** Merge partially overlapping speech-recognition windows, without repeating shared words. */
export function stitchTranscript(previous, current){
 const a=String(previous||'').trim(),b=String(current||'').trim();
 if(!a)return b;if(!b)return a;
 if(a.endsWith(b))return a;
 if(b.includes(a)&&a.length>=8)return b;
 const max=Math.min(a.length,b.length,180);
 for(let n=max;n>=8;n--){if(a.slice(-n).toLowerCase()===b.slice(0,n).toLowerCase())return a+b.slice(n);}
 // Whisper may remove punctuation/spacing at the boundary; keep both when uncertain.
 return a+' '+b;
}
export function normalizedQuestion(text){return String(text||'').toLowerCase().replace(/[\s?？.!。,:;，]/g,'').slice(-160)}
