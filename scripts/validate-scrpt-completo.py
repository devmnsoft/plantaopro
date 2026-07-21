#!/usr/bin/env python3
import json, os, subprocess, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
manifest=json.loads((ROOT/'database/schema-manifest.json').read_text(encoding='utf-8'))
required=[]
for s in manifest['sections']:
  for o in s.get('objects',[]):
    if o.get('required') and o.get('type') in ('table','view','function'):
      required.append(o['name'])
code_refs=sorted(set(__import__('re').findall(r'plantaopro\.([a-zA-Z_][\w]*)', '\n'.join(p.read_text(errors='ignore') for p in (ROOT/'backend').rglob('*.cs')))))
required += ['plantaopro.'+x for x in code_refs]
required=sorted(set(required))
art=ROOT/'artifacts'; art.mkdir(exist_ok=True)
def psql(sql):
  return subprocess.run(['psql','-v','ON_ERROR_STOP=1','-At','-c',sql],text=True,capture_output=True)
results=[]; ok=True
for obj in required:
  schema,name=obj.split('.',1)
  r=psql("select case when exists(select 1 from pg_class c join pg_namespace n on n.oid=c.relnamespace where n.nspname='%s' and c.relname='%s') then 'ok' else 'missing' end"%(schema,name))
  status='error' if r.returncode else r.stdout.strip()
  if status!='ok': ok=False
  results.append({'object':obj,'status':status,'stderr':r.stderr.strip()})
ext=[]
for e in ['pgcrypto','unaccent']:
  r=psql("select case when exists(select 1 from pg_extension where extname='%s') then 'ok' else 'missing' end"%e); ext.append({'extension':e,'status':r.stdout.strip() if r.returncode==0 else 'error'}); ok = ok and r.returncode==0 and r.stdout.strip()=='ok'
report={'ok':ok,'extensions':ext,'objects':results}
(art/'database-validation.json').write_text(json.dumps(report,indent=2,ensure_ascii=False),encoding='utf-8')
md=['# Validação database/scrpt_completo.sql','',f'OK: {ok}','', '## Extensões']+[f"- {x['extension']}: {x['status']}" for x in ext]+['','## Objetos']+[f"- {x['object']}: {x['status']}" for x in results]
(art/'database-validation.md').write_text('\n'.join(md)+'\n',encoding='utf-8')
print(json.dumps({'ok':ok,'objects':len(results)},ensure_ascii=False))
sys.exit(0 if ok else 1)
