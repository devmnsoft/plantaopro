#!/usr/bin/env python3
import json, re, sys
from pathlib import Path
if len(sys.argv) != 3:
    raise SystemExit('usage: catalog-schema.py <schema.sql> <catalog.json>')
src, dst = map(Path, sys.argv[1:])
text = src.read_text(encoding='utf-8')
items = []
patterns = {
    'table': r'^CREATE TABLE ([^\s(]+)',
    'index': r'^CREATE (?:UNIQUE )?INDEX ([^\s]+)',
    'constraint': r'^ALTER TABLE ONLY ([^\s]+)\s+ADD CONSTRAINT ([^\s]+)',
    'view': r'^CREATE VIEW ([^\s]+)',
    'function': r'^CREATE FUNCTION ([^\s(]+)',
    'trigger': r'^CREATE TRIGGER ([^\s]+)',
    'sequence': r'^CREATE SEQUENCE ([^\s]+)',
    'extension': r'^CREATE EXTENSION .*? ([^\s;]+)'
}
for line in text.splitlines():
    line = line.strip()
    for kind, pattern in patterns.items():
        m = re.search(pattern, line, re.I)
        if m:
            items.append({'type': kind, 'name': ' '.join(m.groups())})
            break
catalog = {'source': str(src), 'objects': sorted(items, key=lambda x:(x['type'], x['name']))}
dst.write_text(json.dumps(catalog, ensure_ascii=False, indent=2)+"\n", encoding='utf-8')
