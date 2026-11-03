from pathlib import Path
from pptx import Presentation
import json, re, hashlib
ROOT=Path(__file__).parent
SRC=ROOT/'source_snapshot'
records=[]
def add(id,path,kind,text,priority=0,section='',slide=None):
    records.append({'id':id,'path':path,'kind':kind,'text':text,'priority':priority,'section':section,'slide':slide})
# Archive source, split by manageable overlapping code line spans
for p in sorted(SRC.rglob('*.cs')):
    rel=str(p.relative_to(SRC)).replace('\\','/')
    lines=p.read_text('utf-8-sig',errors='replace').splitlines()
    section='rising' if '/RisingSlash/' in rel else ('boss' if '/Enemies/' in rel or '/Core/' in rel else 'general')
    for i in range(0,len(lines),55):
        start=max(0,i-8); end=min(len(lines),i+65)
        text='\n'.join(f'{j+1:04d} {lines[j]}' for j in range(start,end))
        add('A-'+hashlib.sha1((rel+str(start)).encode()).hexdigest()[:10],rel+f':{start+1}-{end}','archived-source',text,0,section)
# Parse provided git diffs, only added or context lines from each diff file/hunk
for rev,fn in [('R1-R4','Pasted text(20261101-144528).txt'),('VFX-HARNESS','Pasted text(20261101-150643).txt')]:
    content=(ROOT.parent/fn).read_text('utf-8-sig',errors='replace').splitlines()
    current=None; chunk=[]; heading=''; idx=0
    def flush():
        global idx
        if current and chunk:
            section='rising' if ('RisingSlash' in current or 'PatternTest' in current) else 'boss'
            add('D-'+hashlib.sha1((rev+current+heading).encode()).hexdigest()[:10],current+' ['+rev+' supplied diff '+heading+']','later-diff', '\n'.join(chunk), 12,section)
    for line in content:
        if line.startswith('diff --git '):
            flush(); chunk=[];heading='';current=line.split(' b/',1)[-1]
        elif line.startswith('@@'):
            flush();chunk=[];heading=line.split('@@')[1].strip()
        elif line.startswith('+++') or line.startswith('---'):
            continue
        elif current and (line.startswith('+') or line.startswith(' ')):
            chunk.append(line[1:])
    flush()
# Submitted deck + note text, preserve slide context
presentation=Presentation(str(ROOT.parent/'10주차_발표자료_2조.pptx'))
for i,slide in enumerate(presentation.slides,1):
    body='\n'.join(shape.text for shape in slide.shapes if shape.has_text_frame and shape.text.strip())
    notes=''
    try: notes=slide.notes_slide.notes_text_frame.text
    except: pass
    section='boss' if i<=9 else 'personalization' if i<=13 else 'sword' if i<=18 else 'rising' if i<=23 else 'guard'
    add(f'S{i:02d}',f'Submitted presentation — slide {i}','submitted-slide',body+'\nPRESENTER NOTES:\n'+notes,4,section,i)
# An explicit notice in the index
metadata={'generated_from':'user-uploaded archived K4RMA feat/2d-prototype snapshot; user-supplied later diffs; Jeongmin submitted 28-slide PPTX','limitations':'Later diffs are indexed as supplied excerpts, not reconstructed full files or verified live Git HEAD. Later diff snippets override contradictory archived implementation when applicable. Unity Play Mode not verified.','sections':['boss','personalization','sword','rising','guard'],'slide_count':len(presentation.slides),'counts':{'archived_source':sum(x['kind']=='archived-source' for x in records),'later_diff':sum(x['kind']=='later-diff' for x in records),'slides':sum(x['kind']=='submitted-slide' for x in records)}}
(ROOT/'knowledge.json').write_text(json.dumps({'metadata':metadata,'documents':records},ensure_ascii=False,separators=(',',':')),encoding='utf-8')
print(metadata['counts'],'total records',len(records),'size', (ROOT/'knowledge.json').stat().st_size)
