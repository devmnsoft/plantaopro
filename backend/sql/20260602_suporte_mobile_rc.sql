-- PlantaoPro Release Candidate - suporte mobile e histórico de chamados
-- Seguro para execução incremental em PostgreSQL.

CREATE SCHEMA IF NOT EXISTS plantaopro;

CREATE TABLE IF NOT EXISTS plantaopro.chamados_suporte (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id uuid NULL,
    usuario_id uuid NULL,
    protocolo varchar(32) NOT NULL,
    titulo varchar(180) NOT NULL,
    descricao text NOT NULL,
    categoria varchar(60) NOT NULL DEFAULT 'GERAL',
    prioridade varchar(30) NOT NULL DEFAULT 'NORMAL',
    status varchar(30) NOT NULL DEFAULT 'ABERTO',
    origem varchar(30) NOT NULL DEFAULT 'WEB',
    resolucao text NULL,
    justificativa_cancelamento text NULL,
    reg_status char(1) NOT NULL DEFAULT 'A',
    criado_em timestamp without time zone NOT NULL DEFAULT now(),
    atualizado_em timestamp without time zone NOT NULL DEFAULT now(),
    resolvido_em timestamp without time zone NULL,
    cancelado_em timestamp without time zone NULL
);

CREATE TABLE IF NOT EXISTS plantaopro.chamado_mensagens (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    chamado_id uuid NOT NULL,
    usuario_id uuid NULL,
    tipo_autor varchar(30) NOT NULL DEFAULT 'USUARIO',
    mensagem text NOT NULL,
    visibilidade varchar(30) NOT NULL DEFAULT 'CLIENTE',
    reg_status char(1) NOT NULL DEFAULT 'A',
    criado_em timestamp without time zone NOT NULL DEFAULT now()
);

ALTER TABLE plantaopro.chamados_suporte ADD COLUMN IF NOT EXISTS cliente_id uuid NULL;
ALTER TABLE plantaopro.chamados_suporte ADD COLUMN IF NOT EXISTS usuario_id uuid NULL;
ALTER TABLE plantaopro.chamados_suporte ADD COLUMN IF NOT EXISTS protocolo varchar(32) NOT NULL DEFAULT ('SUP-' || replace(gen_random_uuid()::text, '-', ''));
ALTER TABLE plantaopro.chamados_suporte ADD COLUMN IF NOT EXISTS origem varchar(30) NOT NULL DEFAULT 'WEB';
ALTER TABLE plantaopro.chamados_suporte ADD COLUMN IF NOT EXISTS reg_status char(1) NOT NULL DEFAULT 'A';
ALTER TABLE plantaopro.chamados_suporte ADD COLUMN IF NOT EXISTS criado_em timestamp without time zone NOT NULL DEFAULT now();
ALTER TABLE plantaopro.chamados_suporte ADD COLUMN IF NOT EXISTS atualizado_em timestamp without time zone NOT NULL DEFAULT now();
ALTER TABLE plantaopro.chamado_mensagens ADD COLUMN IF NOT EXISTS tipo_autor varchar(30) NOT NULL DEFAULT 'USUARIO';

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'uq_chamados_suporte_protocolo'
          AND conrelid = 'plantaopro.chamados_suporte'::regclass
    ) THEN
        ALTER TABLE plantaopro.chamados_suporte
        ADD CONSTRAINT uq_chamados_suporte_protocolo UNIQUE (protocolo);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_chamado_mensagens_chamado'
          AND conrelid = 'plantaopro.chamado_mensagens'::regclass
    ) THEN
        ALTER TABLE plantaopro.chamado_mensagens
        ADD CONSTRAINT fk_chamado_mensagens_chamado
        FOREIGN KEY (chamado_id) REFERENCES plantaopro.chamados_suporte(id);
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_chamados_suporte_cliente_status ON plantaopro.chamados_suporte (cliente_id, status, reg_status);
CREATE INDEX IF NOT EXISTS ix_chamados_suporte_usuario_criado ON plantaopro.chamados_suporte (usuario_id, criado_em DESC);
CREATE INDEX IF NOT EXISTS ix_chamado_mensagens_chamado_criado ON plantaopro.chamado_mensagens (chamado_id, criado_em);
