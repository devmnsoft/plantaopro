-- R7-OB: jornada de onboarding — etapa "Definir o idioma da interface" (opcional, núcleo).
-- R7-E1 (v2344) criou a preferência persistida (usuarios.idioma_preferido) e o padrão de
-- organização (tenants.metadata->>'idiomaPadrao'). Esta migração expõe a configuração na
-- jornada por contrato com critério DERIVADO DE DADOS (mesma filosofia C7: a verdade segue
-- os dados, não o botão): concluída quando o padrão de idioma da organização está registrado
-- OU ao menos um usuário ativo do tenant tem preferência de idioma salva.
-- Opcional (obrigatorio=false): se nada for definido, pt-BR continua sendo o padrão silencioso;
-- a etapa pode ser pulada com justificativa persistida sem travar o onboarding.
-- Rerun idempotente: upsert pelo codigo único + MaterializarAsync só cria para tenants novos;
-- tenants existentes recebem a etapa via POST /api/onboarding/sincronizar (ou iniciar).

insert into plantaopro.onboarding_etapas_catalogo
(codigo,titulo,objetivo,descricao,responsavel,pre_requisito_codigo,modulo_codigo,criterio_codigo,link_acao,ordem,obrigatorio)
values
('ONB_PREFERENCIAS','Definir o idioma da interface','Garantir que a operação escolha conscientemente o idioma da interface (padrão da organização ou preferência individual).','Etapa opcional do núcleo: pode ser pulada com justificativa sem travar o onboarding. O critério deriva dos dados persistidos: padrão de idioma da organização registrado OU ao menos um usuário ativo com preferência de idioma salva (coluna v2344).','CLIENTE',null,null,'IDIOMA_PREFERENCIA_DEFINIDA','/Usuario/Index',50,false)
on conflict (codigo) do update set
    titulo = excluded.titulo,
    objetivo = excluded.objetivo,
    descricao = excluded.descricao,
    responsavel = excluded.responsavel,
    pre_requisito_codigo = excluded.pre_requisito_codigo,
    modulo_codigo = excluded.modulo_codigo,
    criterio_codigo = excluded.criterio_codigo,
    link_acao = excluded.link_acao,
    ordem = excluded.ordem,
    obrigatorio = excluded.obrigatorio,
    reg_update = now();
