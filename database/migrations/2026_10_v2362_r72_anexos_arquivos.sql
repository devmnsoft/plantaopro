-- PlantaoPro v2.36.2 (R72): anexo de arquivos binarios (PDF/Word/imagem) por entidade.
-- Pedido do usuario (rodada 72): antes desta rodada o unico upload do sistema era a Central
-- XML (arquivos .xml apenas). Funcionarios (colaboradores), empresas (parceiros) e notas
-- fiscais (pre-emissao) nao tinham como receber documentos em PDF/Word/imagem.
-- Convencao da casa: conteudo em base64 em coluna text (mesmo padrao de
-- v115_recurso_documentos), auditavel (created_by/created_at) e com exclusao macia
-- (reg_status 'I'). Idempotente: create ... if not exists + comentario estavel.

create table if not exists plantaopro.anexos_arquivos (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid not null,
    categoria varchar(30) not null check (categoria in ('FUNCIONARIO', 'EMPRESA', 'NOTA_FISCAL', 'OUTRO')),
    entidade_id uuid null,
    nome_original text not null,
    extensao varchar(10) not null default '',
    tipo_mime varchar(120) not null default '',
    conteudo text not null default '',
    sha256 char(64) not null default '',
    tamanho int not null default 0,
    observacao text null,
    created_at timestamptz not null default now(),
    created_by uuid null,
    reg_status char(1) not null default 'A'
);

create index if not exists ix_anexos_arquivos_tenant_alvo
    on plantaopro.anexos_arquivos (tenant_id, categoria, entidade_id) where reg_status = 'A';

comment on table plantaopro.anexos_arquivos is
    'Anexos binarios base64 de funcionarios/empresas/notas fiscais (R72 - pedido do usuario).';
comment on column plantaopro.anexos_arquivos.conteudo is
    'Conteudo do arquivo em base64 (convencao da casa, ver v115_recurso_documentos).';
