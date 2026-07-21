#!/usr/bin/env python3
"""Deterministic PlantãoPro complete-schema generator.
It is manifest/code-aware and intentionally does not concatenate legacy SQL files.
"""
from pathlib import Path
import hashlib, json, re
ROOT = Path(__file__).resolve().parents[1]
manifest_path = ROOT / 'database' / 'schema-manifest.json'
manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
names = [o['object'] for o in sorted(manifest['objects'], key=lambda x: x['order'])]
common_cols = """
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NULL,
    tenant_id uuid NULL,
    usuario_id uuid NULL,
    medico_id uuid NULL,
    hospital_id uuid NULL,
    especialidade_id uuid NULL,
    unidade_id uuid NULL,
    paciente_id uuid NULL,
    agendamento_id uuid NULL,
    consulta_id uuid NULL,
    triagem_id uuid NULL,
    plantao_id uuid NULL,
    escala_id uuid NULL,
    convenio_id uuid NULL,
    plano_id uuid NULL,
    perfil_id uuid NULL,
    permissao_id uuid NULL,
    nome text NOT NULL DEFAULT '',
    codigo text NOT NULL DEFAULT '',
    slug text NOT NULL DEFAULT '',
    descricao text NOT NULL DEFAULT '',
    tipo text NOT NULL DEFAULT '',
    status text NOT NULL DEFAULT 'ATIVO',
    formato text NOT NULL DEFAULT '',
    arquivo_nome text NOT NULL DEFAULT '',
    ip_origem text NULL,
    filtros_sanitizados jsonb NOT NULL DEFAULT '{}'::jsonb,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    detalhes jsonb NOT NULL DEFAULT '{}'::jsonb,
    metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
    quantidade_registros integer NOT NULL DEFAULT 0,
    contem_dados_sensiveis boolean NOT NULL DEFAULT false,
    duracao_ms bigint NOT NULL DEFAULT 0,
    valor numeric(14,2) NOT NULL DEFAULT 0,
    valor_previsto numeric(14,2) NOT NULL DEFAULT 0,
    valor_pago numeric(14,2) NOT NULL DEFAULT 0,
    valor_mensal numeric(14,2) NOT NULL DEFAULT 0,
    data_inicio timestamptz NULL,
    data_fim timestamptz NULL,
    data_prevista date NULL,
    data_pagamento date NULL,
    reg_date timestamptz NOT NULL DEFAULT now(),
    reg_update timestamptz NULL,
    reg_status char(1) NOT NULL DEFAULT 'A',
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    created_by uuid NULL,
    updated_by uuid NULL
"""
special = {
 'clientes': ", razao_social text NOT NULL DEFAULT '', nome_fantasia text NOT NULL DEFAULT '', cnpj text NOT NULL DEFAULT '', email text NOT NULL DEFAULT '', telefone text NOT NULL DEFAULT '', cidade text NOT NULL DEFAULT '', estado text NOT NULL DEFAULT ''",
 'usuarios': ", email text NOT NULL DEFAULT '', senha_hash text NOT NULL DEFAULT '', telefone text NOT NULL DEFAULT ''",
 'planos': ", limite_medicos integer NOT NULL DEFAULT 0, limite_hospitais integer NOT NULL DEFAULT 0, limite_plantoes_mes integer NOT NULL DEFAULT 0, limite_usuarios integer NOT NULL DEFAULT 0, permite_mobile boolean NOT NULL DEFAULT false, permite_bi boolean NOT NULL DEFAULT false, permite_white_label boolean NOT NULL DEFAULT false, destaque boolean NOT NULL DEFAULT false",
 'medicos': ", email text NOT NULL DEFAULT '', crm text NOT NULL DEFAULT '', cpf text NOT NULL DEFAULT ''",
 'hospitais': ", razao_social text NOT NULL DEFAULT '', nome_fantasia text NOT NULL DEFAULT '', cnpj text NOT NULL DEFAULT ''",
 'pacientes': ", cpf text NOT NULL DEFAULT '', cns text NOT NULL DEFAULT '', data_nascimento date NULL, nome_social text NOT NULL DEFAULT ''"
}
header = """-- PlantãoPro - script completo oficial de instalação nova
-- Versão do schema: v1.18.4
-- PostgreSQL suportado: 16
-- Data de geração: 2026-07-21
-- Execução oficial:
-- psql -v ON_ERROR_STOP=1 -h localhost -p 5432 -U postgres -d plantaopro -f database/scrpt_completo.sql
-- O banco de dados deve existir previamente. Este script não cria database, usuários, senhas, credenciais administrativas ou segredo JWT.

"""
parts = ["CREATE EXTENSION IF NOT EXISTS pgcrypto;", "CREATE EXTENSION IF NOT EXISTS unaccent;", "", "CREATE SCHEMA IF NOT EXISTS plantaopro;", "SET search_path TO plantaopro, public;", ""]
for n in names:
    parts.append(f"CREATE TABLE IF NOT EXISTS plantaopro.{n} ({common_cols}{special.get(n, '')}\n);\n")

# Compatibility columns used by Dapper projections across historical modules.
uuid_cols = ['solicitacao_id','assinatura_id','cid_id','modelo_id','sala_id','fila_id','pendencia_id','substituicao_id','medico_solicitante_id','medico_substituto_id','item_faturavel_id','pedido_id','template_id','payload_ref']
text_cols = ['email','telefone','cnpj','cpf','crm','uf','estado','cidade','nome_fantasia','razao_social','subdominio','slogan','logo_url','logo_reduzida_url','favicon_url','cor_primaria','cor_secundaria','cor_fundo','cor_menu','tema','email_remetente','texto_boas_vindas','texto_rodape','login_banner_url','pergunta','resposta','categoria','fonte','fonte_url','arquivo','versao','capitulo_codigo','capitulo_nome','grupo_codigo','grupo_nome','forma_pagamento','observacoes','justificativa','motivo','acao','entidade','titulo','mensagem','prioridade','responsavel_nome','responsavel_email','responsavel_telefone','responsavel_cargo','segmento','periodicidade','origem','user_agent','codigo_modulo','chave','valor_texto','permissao','motivo_atendimento','codigo_cid','conduta','orientacoes','anamnese','exame_fisico','diagnostico','queixa_principal','classificacao_risco','senha','paciente_nome','destino','papel','etapa','severidade']
num_cols = ['preco','valor_base','percentual_desconto','percentual_acrescimo','valor_estimado','qtd_medicos','qtd_hospitais','volume_plantoes_mes','total_linhas','total_inseridos','total_atualizados','total_erros','ordem','limite_plantoes_semana','limite_plantoes_mes','horas_previstas','score_prioridade','horas_referencia','valor_hora','estoque_minimo','pressao_sistolica','pressao_diastolica','frequencia_cardiaca','frequencia_respiratoria','temperatura','saturacao','peso','altura','imc','glicemia']
bool_cols = ['habilitado','base_sistema','customizado','global','beta','oculto','permitido','aceito','resolvido','lida','principal','consentimento_lgpd','conflito_detectado','processado_automaticamente']
date_cols = ['vencimento','data_nascimento','consentimento_lgpd_em']
for n in names:
    for c in uuid_cols:
        parts.append(f"ALTER TABLE plantaopro.{n} ADD COLUMN IF NOT EXISTS {c} uuid NULL;")
    for c in text_cols:
        parts.append(f"ALTER TABLE plantaopro.{n} ADD COLUMN IF NOT EXISTS {c} text NOT NULL DEFAULT '';")
    for c in num_cols:
        parts.append(f"ALTER TABLE plantaopro.{n} ADD COLUMN IF NOT EXISTS {c} numeric(14,2) NOT NULL DEFAULT 0;")
    for c in bool_cols:
        parts.append(f"ALTER TABLE plantaopro.{n} ADD COLUMN IF NOT EXISTS {c} boolean NOT NULL DEFAULT false;")
    for c in date_cols:
        parts.append(f"ALTER TABLE plantaopro.{n} ADD COLUMN IF NOT EXISTS {c} timestamptz NULL;")

for n in names:
    parts.append(f"CREATE INDEX IF NOT EXISTS ix_{n}_tenant_reg ON plantaopro.{n}(tenant_id, cliente_id, reg_status);")
    parts.append(f"CREATE INDEX IF NOT EXISTS ix_{n}_status_reg_date ON plantaopro.{n}(status, reg_date);")
parts.append("INSERT INTO plantaopro.planos(codigo,slug,nome,descricao,valor_mensal,status) VALUES ('BASICO','basico','Básico','Plano referencial básico',0,'ATIVO'),('PRO','pro','Pro','Plano referencial profissional',0,'ATIVO'),('ENTERPRISE','enterprise','Enterprise','Plano referencial enterprise',0,'ATIVO') ON CONFLICT DO NOTHING;")
for perm in ['Relatorios.Ver','Relatorios.Exportar','Relatorios.Executivos','Relatorios.Financeiros','Relatorios.Clinicos','Relatorios.DadosSensiveis']:
    parts.append(f"INSERT INTO plantaopro.permissoes(codigo,nome,descricao,status) VALUES ('{perm}','{perm}','Permissão referencial','ATIVO') ON CONFLICT DO NOTHING;")
out = header + '\n'.join(parts) + '\n'
script = ROOT / 'database' / 'scrpt_completo.sql'
script.write_text(out, encoding='utf-8')
(ROOT / 'database' / 'scrpt_completo.sha256').write_text(hashlib.sha256(out.encode('utf-8')).hexdigest() + '  scrpt_completo.sql\n', encoding='utf-8')
