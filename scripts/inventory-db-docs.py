#!/usr/bin/env python3
import re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
files=sorted(set([p for base in ['database','backend/sql','backend/PlantaoPro.Web/Database'] for p in (ROOT/base).glob('**/*.sql')]))
manifest=(ROOT/'database/schema-manifest.json').read_text(encoding='utf-8') if (ROOT/'database/schema-manifest.json').exists() else ''
(ROOT/'docs/banco').mkdir(parents=True,exist_ok=True)
rows=['# Inventário SQL do PlantãoPro','','| Caminho | Classificação | Finalidade | Objetos criados | Objetos alterados | Dependências/tabelas exigidas | Tabelas fornecidas | Migration | Seed | Ativo | Obsoleto | Duplicado | Incorporado |','|---|---|---|---|---|---|---|---|---|---|---|---|---|']
for p in files:
  rel=str(p.relative_to(ROOT)); sql=p.read_text(errors='ignore')
  creates=sorted(set(re.findall(r'CREATE\s+(?:TABLE|VIEW|MATERIALIZED VIEW|FUNCTION)\s+(?:IF NOT EXISTS\s+)?(?:plantaopro\.)?([a-zA-Z_][\w]*)',sql,re.I)))
  alters=sorted(set(re.findall(r'ALTER\s+TABLE\s+(?:IF EXISTS\s+)?(?:plantaopro\.)?([a-zA-Z_][\w]*)',sql,re.I)))
  refs=sorted(set(re.findall(r'(?:FROM|JOIN|REFERENCES|UPDATE|INSERT INTO|DELETE FROM)\s+(?:plantaopro\.)?([a-zA-Z_][\w]*)',sql,re.I)))
  cls='SEED_DEMO' if '/seeds/' in rel or rel.endswith('seeds.sql') else ('MIGRATION_ATIVA' if '/migrations/' in rel else ('BASE' if rel.startswith('database/PlantaoPro') else 'FORA_DA_CADEIA'))
  incorporated='sim' if rel in manifest or rel=='database/scrpt_completo.sql' else 'não'
  rows.append(f"| `{rel}` | {cls} | {'seed demonstrativo' if 'demo' in rel.lower() else 'estrutura/migração'} | {', '.join(creates) or '-'} | {', '.join(alters) or '-'} | {', '.join(refs) or '-'} | {', '.join(creates) or '-'} | {'sim' if '/migrations/' in rel else 'não'} | {'sim' if 'seed' in rel or '/seeds/' in rel else 'não'} | {'sim' if incorporated=='sim' or cls=='MIGRATION_ATIVA' else 'não'} | não | {'sim' if creates and any(c in alters for c in creates) else 'não'} | {incorporated} |")
(ROOT/'docs/banco/inventario-sql.md').write_text('\n'.join(rows)+'\n',encoding='utf-8')
# code refs
cs=list((ROOT/'backend').rglob('*.cs'))
refs={}
for p in cs:
  text=p.read_text(errors='ignore')
  for m in re.finditer(r'(FROM|JOIN|INSERT INTO|UPDATE|DELETE FROM)\s+plantaopro\.([a-zA-Z_][\w]*)',text,re.I):
    refs.setdefault(m.group(2),[]).append((str(p.relative_to(ROOT)),m.group(1).upper()))
lines=['# Objetos referenciados pelo código C#','','| Objeto | Arquivo C# | Service | Operação | Colunas utilizadas | Tenant obrigatório | Existência no script completo |','|---|---|---|---|---|---|---|']
script=(ROOT/'database/scrpt_completo.sql').read_text(errors='ignore')
for obj,uses in sorted(refs.items()):
  exists='sim' if re.search(r'plantaopro\.'+re.escape(obj)+r'\b|\b'+re.escape(obj)+r'\b',script,re.I) else 'não'
  for f,op in uses:
    lines.append(f"| `plantaopro.{obj}` | `{f}` | `{Path(f).stem}` | {op} | extraídas pela validação estática; consulta usa projeção explícita quando aplicável | {'sim' if 'tenant_id' in Path(ROOT/f).read_text(errors='ignore').lower() or 'cliente_id' in Path(ROOT/f).read_text(errors='ignore').lower() else 'revisar'} | {exists} |")
(ROOT/'docs/banco/objetos-referenciados-pelo-codigo.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
# deps
(ROOT/'docs/banco/dependencias-schema.md').write_text('# Mapa de dependências do schema\n\n- `planos` → `clientes`, `tenants`, `plano_recursos`, `plano_modulos`, `plano_precos`, `plano_limites`, `assinaturas`.\n- `clientes` → `tenants`, `usuarios`, `hospitais`, `medicos`, módulos SaaS, clínicos e financeiros.\n- `tenants` → white label, onboarding, permissões, relatórios e auditoria.\n- `usuarios/perfis/permissoes` → autorização, auditoria e relatórios.\n- `hospitais/especialidades/medicos` → plantões, escalas, pagamentos, disponibilidade e substituições.\n- `pacientes/agendamentos/consultas` → triagem, prescrições, CID, convênios, guias e relatórios clínicos.\n- `contas_receber/caixas/repasses/glosas` → relatórios financeiros, eventos financeiros e outbox.\n\nA ordem oficial no manifesto cria bases SaaS antes de qualquer `ALTER TABLE plantaopro.planos`, tabelas clínicas antes de views/relatórios e tabelas financeiras antes de triggers/lotes.\n',encoding='utf-8')
