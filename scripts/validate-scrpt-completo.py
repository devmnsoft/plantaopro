#!/usr/bin/env python3
from pathlib import Path
import json, re, sys
root=Path(__file__).resolve().parents[1]
sql=(root/'database/scrpt_completo.sql').read_text(encoding='utf-8')
errors=[]
for forbidden in ['\\i','DROP SCHEMA','DROP TABLE',' CASCADE','WHEN OTHERS THEN NULL']:
    if forbidden.lower() in sql.lower(): errors.append(f'Forbidden token: {forbidden}')
manifest=json.loads((root/'database/schema-manifest.json').read_text(encoding='utf-8'))
missing=[o['object'] for o in manifest['objects'] if f'CREATE TABLE IF NOT EXISTS plantaopro.{o["object"]}' not in sql]
if missing: errors.append('Missing manifest objects: '+', '.join(missing[:20]))
used=set()
for p in (root/'backend').rglob('*.cs'):
    used.update(re.findall(r'plantaopro\.([A-Za-z_][A-Za-z0-9_]*)', p.read_text(errors='ignore')))
missing_used=sorted(x for x in used if x not in {'com','local'} and f'plantaopro.{x}' not in sql)
if missing_used: errors.append('Missing application objects: '+', '.join(missing_used[:50]))
out={'valid':not errors,'errors':errors,'objects':len(manifest['objects'])}
(root/'artifacts').mkdir(exist_ok=True)
(root/'artifacts/database-validation.json').write_text(json.dumps(out,indent=2,ensure_ascii=False),encoding='utf-8')
(root/'artifacts/database-validation.md').write_text('# Validação do banco completo\n\n' + ('OK' if not errors else '\n'.join(errors)), encoding='utf-8')
if errors:
    print('\n'.join(errors)); sys.exit(1)
print('scrpt_completo.sql validado')
