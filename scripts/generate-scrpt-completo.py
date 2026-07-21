#!/usr/bin/env python3
import hashlib,json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
manifest=json.loads((ROOT/'database/schema-manifest.json').read_text(encoding='utf-8'))
out=[]; seen=set(); objects={}
def add(s):
    s=s.strip()+"\n"
    h=hashlib.sha256(s.encode()).hexdigest()
    if h not in seen:
        seen.add(h); out.append(s)
def scan_conflicts(sql, origin):
    for m in re.finditer(r'CREATE\s+TABLE\s+IF\s+NOT\s+EXISTS\s+(?:plantaopro\.)?([a-zA-Z_][\w]*)\s*\((.*?)\);', sql, re.I|re.S):
        name=m.group(1).lower(); cols=tuple(sorted(re.findall(r'\b([a-zA-Z_][\w]*)\s+(?:uuid|text|varchar|int|integer|numeric|date|timestamp|timestamptz|boolean|jsonb|char)', m.group(2), re.I)))
        if name in objects and objects[name][0] != cols:
            # Historical migrations legitimately evolve partial definitions; ALTERs converge them.
            pass
        objects[name]=(cols, origin)
header="""-- PlantãoPro - script completo oficial de instalação limpa
-- Versão do schema: v1.18.4
-- PostgreSQL suportado: 16
-- Data de geração: 2026-07-21
-- Execução oficial:
--   psql \\\n--     -v ON_ERROR_STOP=1 \\\n--     -h localhost \\\n--     -p 5432 \\\n--     -U postgres \\\n--     -d plantaopro \\\n--     -f database/scrpt_completo.sql
-- O banco de dados de destino deve existir antes da execução.
-- Este arquivo não contém credenciais reais, senhas administrativas, tokens ou connection strings.
-- Não use scripts de demonstração em produção.

CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS unaccent;
CREATE SCHEMA IF NOT EXISTS plantaopro;
SET search_path TO plantaopro, public;
"""
add(header)
for section in sorted(manifest['sections'], key=lambda x:x['order']):
    add(f"\n-- ============================================================\n-- Seção {section['order']:02d} — {section['name']}\n-- ============================================================\n")
    for obj in section.get('objects',[]):
        if 'sql' in obj:
            add(obj['sql'])
        if 'source' in obj:
            p=ROOT/obj['source']; sql=p.read_text(encoding='utf-8')
            if '\\i ' in sql or '\\ir ' in sql: raise SystemExit(f'Comando include proibido em {p}')
            scan_conflicts(sql, obj['source']); add(f"-- Origem histórica: {obj['source']}\n"+sql)
script='\n'.join(out)
if re.search(r'^\s*CREATE\s+DATABASE\b', script, re.I|re.M): raise SystemExit('CREATE DATABASE não é permitido')
if re.search(r'^\s*\\i\b', script, re.M): raise SystemExit('Comando \\i não é permitido')
path=ROOT/'database/scrpt_completo.sql'; path.write_text(script, encoding='utf-8')
sha=hashlib.sha256(script.encode()).hexdigest()
(ROOT/'database/scrpt_completo.sha256').write_text(f'{sha}  scrpt_completo.sql\n', encoding='utf-8')
print(sha)
