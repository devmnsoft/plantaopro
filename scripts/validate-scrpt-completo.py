#!/usr/bin/env python3
import json,re,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
manifest=json.loads((ROOT/'database/install-manifest.json').read_text(encoding='utf-8'))
catalog=json.loads((ROOT/'database/object-catalog.json').read_text(encoding='utf-8'))
script=(ROOT/'database/scrpt_completo.sql').read_text(encoding='utf-8')
forbidden=re.findall(r'^\s*\\(?:if|else|endif|set|unset|echo|quit|connect|gexec|prompt|ir|i)\b',script,re.I|re.M)
if forbidden:
 print(json.dumps({'ok':False,'forbiddenPsqlMetacommands':forbidden},ensure_ascii=False)); sys.exit(1)
art=ROOT/'artifacts'; art.mkdir(exist_ok=True)
patterns={
 'table':r'CREATE\s+TABLE\s+IF\s+NOT\s+EXISTS\s+(?:plantaopro\.)?{name}\b',
 'view':r'CREATE\s+(?:OR\s+REPLACE\s+)?VIEW\s+(?:plantaopro\.)?{name}\b',
 'materialized view':r'CREATE\s+MATERIALIZED\s+VIEW\s+(?:IF\s+NOT\s+EXISTS\s+)?(?:plantaopro\.)?{name}\b',
 'sequence':r'CREATE\s+SEQUENCE\s+(?:IF\s+NOT\s+EXISTS\s+)?(?:plantaopro\.)?{name}\b',
 'function':r'CREATE\s+(?:OR\s+REPLACE\s+)?FUNCTION\s+(?:plantaopro\.)?{name}\b',
 'procedure':r'CREATE\s+(?:OR\s+REPLACE\s+)?PROCEDURE\s+(?:plantaopro\.)?{name}\b',
 'extension':r'CREATE\s+EXTENSION\s+IF\s+NOT\s+EXISTS\s+"?{name}"?\b'}
installed=[]; missing=[]; active=[]; legacy=[]
for o in catalog['objects']:
 if o.get('status')=='ACTIVE': active.append(o)
 if o.get('status')=='LEGACY_SUPPORTED': legacy.append(o)
 pat=patterns.get(o['type'],patterns['table']).format(name=re.escape(o['name']))
 (installed if re.search(pat,script,re.I) else missing).append(f"{o['schema']}.{o['name']}")
refs=sorted(set(re.findall(r'plantaopro\.([a-zA-Z_][\w]*)','\n'.join(p.read_text(errors='ignore') for p in (ROOT/'backend').rglob('*.cs'))))) if (ROOT/'backend').exists() else []
created_tables={f"plantaopro.{name}" for name in re.findall(r'CREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?(?:plantaopro\.)?([a-zA-Z_][\w]*)',script,re.I)}
installed_set=set(installed); active_set={f"{o['schema']}.{o['name']}" for o in active}
required_backend=sorted(({f"plantaopro.{name}" for name in refs} & active_set)-created_tables)
fk_targets={f"plantaopro.{name}" for name in re.findall(r'REFERENCES\s+(?:plantaopro\.)?([a-zA-Z_][\w]*)\s*\(',script,re.I)}
missing_fk_targets=sorted(fk_targets-created_tables)
required_manifest={o['name'] for s in manifest['sections'] for o in s.get('objects',[]) if o.get('required') and o.get('name','').startswith('plantaopro.') and o.get('type')=='table'}
missing_manifest=sorted(required_manifest-created_tables)
coverage=100.0 if not active else round(100*(len(active_set-set(missing))/len(active)),2)
all_missing=sorted(set(missing)|set(required_backend)|set(missing_fk_targets)|set(missing_manifest))
report={'ok':not all_missing,'coveragePercent':coverage,'referencedObjects':[f'plantaopro.{r}' for r in refs],'installedObjects':installed,'missingObjects':all_missing,'missingBackendRequired':required_backend,'missingForeignKeyTargets':missing_fk_targets,'missingManifestRequired':missing_manifest,'orphanObjects':sorted(installed_set-{f'plantaopro.{r}' for r in refs}),'legacyObjects':[f"{o['schema']}.{o['name']}" for o in legacy],'manifestSources':[o.get('source') for s in manifest['sections'] for o in s.get('objects',[]) if o.get('source')]}
(art/'database-object-coverage.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+"\n")
md=['# Cobertura de objetos do banco','',f"Cobertura: {coverage}%",f"OK: {report['ok']}",'','## Faltantes']+[f'- `{x}`' for x in missing or ['nenhum']]+['','## Instalados']+[f'- `{x}`' for x in installed]
(art/'database-object-coverage.md').write_text('\n'.join(md)+'\n')
print(json.dumps({'ok':report['ok'],'coveragePercent':coverage},ensure_ascii=False))
sys.exit(0 if report['ok'] else 1)
