CREATE TABLE IF NOT EXISTS plantaopro.parametros_sistema (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NULL, codigo text NOT NULL, categoria text NOT NULL, nome text NOT NULL,
 descricao text NOT NULL DEFAULT '', tipo text NOT NULL, valor text NULL, valor_padrao text NULL, sensivel boolean NOT NULL DEFAULT false,
 editavel boolean NOT NULL DEFAULT true, status text NOT NULL DEFAULT 'ATIVO', reg_status char(1) NOT NULL DEFAULT 'A',
 reg_date timestamptz NOT NULL DEFAULT now(), reg_update timestamptz NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_parametros_sistema_global_codigo ON plantaopro.parametros_sistema(lower(codigo)) WHERE tenant_id IS NULL AND reg_status='A';
WITH catalog(codigo,categoria,nome,tipo,valor) AS (VALUES
 ('SISTEMA.LOCALE','SISTEMA','Localidade','TEXTO','pt-BR'),('SISTEMA.TIMEZONE','SISTEMA','Fuso horário','TEXTO','America/Belem'),
 ('SISTEMA.CURRENCY','SISTEMA','Moeda','TEXTO','BRL'),('SISTEMA.DATE_FORMAT','SISTEMA','Formato de data','TEXTO','dd/MM/yyyy'),('SISTEMA.TIME_FORMAT','SISTEMA','Formato de hora','TEXTO','HH:mm'),
 ('SEGURANCA.LOGIN_MAX_TENTATIVAS','SEGURANCA','Máximo de tentativas','INTEIRO','5'),('SEGURANCA.LOGIN_BLOQUEIO_MINUTOS','SEGURANCA','Bloqueio do login','INTEIRO','15'),
 ('SEGURANCA.SENHA_TAMANHO_MINIMO','SEGURANCA','Tamanho mínimo da senha','INTEIRO','12'),('SEGURANCA.SENHA_EXPIRACAO_DIAS','SEGURANCA','Expiração da senha','INTEIRO','90'),
 ('SEGURANCA.SESSAO_MINUTOS','SEGURANCA','Duração da sessão','INTEIRO','60'),('OPERACAO.PLANTAO_DURACAO_MAXIMA_HORAS','OPERACAO','Duração máxima','INTEIRO','168'),
 ('OPERACAO.CONFLITO_INTERVALO_MINUTOS','OPERACAO','Intervalo de conflito','INTEIRO','0'),('OPERACAO.CANCELAMENTO_ANTECEDENCIA_HORAS','OPERACAO','Antecedência de cancelamento','INTEIRO','24'),
 ('NOTIFICACOES.EMAIL_ATIVO','NOTIFICACOES','E-mail ativo','BOOLEANO','false'),('NOTIFICACOES.PUSH_ATIVO','NOTIFICACOES','Push ativo','BOOLEANO','false'),
 ('NOTIFICACOES.WHATSAPP_ATIVO','NOTIFICACOES','WhatsApp ativo','BOOLEANO','false'),('FINANCEIRO.MOEDA','FINANCEIRO','Moeda','TEXTO','BRL'),
 ('FINANCEIRO.CASAS_DECIMAIS','FINANCEIRO','Casas decimais','INTEIRO','2'),('ARQUIVOS.TAMANHO_MAXIMO_MB','ARQUIVOS','Tamanho máximo','INTEIRO','25'),
 ('LGPD.RETENCAO_LOGS_DIAS','LGPD','Retenção de logs','INTEIRO','365')
)
INSERT INTO plantaopro.parametros_sistema(id,codigo,categoria,nome,tipo,valor,valor_padrao)
SELECT md5('parameter:'||codigo)::uuid,codigo,categoria,nome,tipo,valor,valor FROM catalog ON CONFLICT DO NOTHING;
