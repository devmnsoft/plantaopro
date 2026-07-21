#!/usr/bin/env python3
import hashlib,json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
manifest=json.loads((ROOT/'database/schema-manifest.json').read_text(encoding='utf-8'))
out=[]; seen=set(); objects={}; conflicts=[]; canonical_sources=set(); canonical_tables=set(manifest.get('canonicalTables', []))
for section in manifest['sections']:
    for obj in section.get('objects',[]):
        if obj.get('name'):
            canonical_tables.add(obj['name'].split('.')[-1].lower())
        if obj.get('category','').startswith('CANONICAL') or obj.get('type')=='canonical':
            if obj.get('source'): canonical_sources.add(obj['source'])
def add(s):
    s=s.strip()+"\n"
    h=hashlib.sha256(s.encode()).hexdigest()
    if h not in seen:
        seen.add(h); out.append(s)
def cols_from(body):
    return tuple(sorted(c.lower() for c in re.findall(r'(?:^|,)\s*([a-zA-Z_][\w]*)\s+(?:uuid|text|varchar|int|integer|numeric|date|timestamp|timestamptz|boolean|jsonb|char)', body, re.I|re.S)))
def has_compat(sql, name):
    return re.search(r'ALTER\s+TABLE\s+(?:IF\s+EXISTS\s+)?(?:plantaopro\.)?'+re.escape(name)+r'\s+ADD\s+COLUMN\s+IF\s+NOT\s+EXISTS', sql, re.I) is not None
def scan_conflicts(sql, origin):
    for m in re.finditer(r'CREATE\s+TABLE\s+IF\s+NOT\s+EXISTS\s+(?:plantaopro\.)?([a-zA-Z_][\w]*)\s*\((.*?)\);', sql, re.I|re.S):
        name=m.group(1).lower(); cols=cols_from(m.group(2))
        if name in objects and objects[name]['cols'] != cols:
            conflict={'table':f'plantaopro.{name}','first_origin':objects[name]['origin'],'second_origin':origin,'first_columns':list(objects[name]['cols']),'second_columns':list(cols),'canonical':name in canonical_tables,'compatibility_alter':has_compat(sql,name) or objects[name].get('compat',False) or origin.startswith('plantaopro.') or objects[name]['origin'].startswith('plantaopro.')}
            conflicts.append(conflict)
            if not conflict['canonical'] and not (origin.startswith('database/migrations/') and objects[name]['origin'].startswith('database/migrations/')):
                write_reports(); raise SystemExit(f"Conflito de schema sem modelo canônico/ALTER compatível: plantaopro.{name} ({conflict['first_origin']} x {origin})")
        objects[name]={'cols':cols,'origin':origin,'compat':has_compat(sql,name) or origin in canonical_sources}
def write_reports():
    art=ROOT/'artifacts'; art.mkdir(exist_ok=True)
    (art/'schema-conflicts.json').write_text(json.dumps(conflicts,ensure_ascii=False,indent=2)+"\n",encoding='utf-8')
    lines=['# Relatório de conflitos de schema','']
    for c in conflicts:
        lines += [f"## {c['table']}",f"- Primeira origem: `{c['first_origin']}`",f"- Segunda origem: `{c['second_origin']}`",f"- Canônico no manifesto: `{c['canonical']}`",f"- ALTER compatibilidade: `{c['compatibility_alter']}`",f"- Colunas primeira: {', '.join(c['first_columns'])}",f"- Colunas segunda: {', '.join(c['second_columns'])}",'']
    if not conflicts: lines.append('Nenhum conflito detectado.')
    (art/'schema-conflicts.md').write_text('\n'.join(lines)+"\n",encoding='utf-8')
header=f"""-- PlantãoPro - script completo oficial de instalação limpa
-- Versão do schema: {manifest.get('schemaVersion','v1.18.6')}
-- PostgreSQL suportado: 16
-- Data de geração: {manifest.get('generatedAt','2026-07-21')}
-- Execução oficial:
--   psql \\\n--     -v ON_ERROR_STOP=1 \\\n--     -h localhost \\\n--     -p 5432 \\\n--     -U postgres \\\n--     -d plantaopro \\\n--     -f database/scrpt_completo.sql
-- O banco de dados de destino deve existir antes da execução.
-- Este arquivo não contém credenciais reais, senhas administrativas, tokens ou connection strings.
-- Não use scripts de demonstração em produção.

CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS unaccent;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE SCHEMA IF NOT EXISTS plantaopro;
SET search_path TO plantaopro, public;
"""
add(header)
for section in sorted(manifest['sections'], key=lambda x:x['order']):
    add(f"\n-- ============================================================\n-- Seção {section['order']:02d} — {section['name']}\n-- ============================================================\n")
    for obj in section.get('objects',[]):
        if 'sql' in obj:
            scan_conflicts(obj['sql'], obj.get('name','manifest-inline')); add(obj['sql'])
        if 'source' in obj:
            p=ROOT/obj['source']; sql=p.read_text(encoding='utf-8')
            if '\\i ' in sql or '\\ir ' in sql: raise SystemExit(f'Comando include proibido em {p}')
            scan_conflicts(sql, obj['source']); add(f"-- Origem histórica: {obj['source']}\n"+sql)
write_reports()
script='\n'.join(out)
if re.search(r'^\s*CREATE\s+DATABASE\b', script, re.I|re.M): raise SystemExit('CREATE DATABASE não é permitido')
if re.search(r'^\s*\\i\b', script, re.M): raise SystemExit('Comando \\i não é permitido')
path=ROOT/'database/scrpt_completo.sql'; path.write_text(script, encoding='utf-8')
sha=hashlib.sha256(script.encode()).hexdigest()
(ROOT/'database/scrpt_completo.sha256').write_text(f'{sha}  scrpt_completo.sql\n', encoding='utf-8')
print(sha)
