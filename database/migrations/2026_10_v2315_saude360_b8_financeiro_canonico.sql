-- ============================================================================
-- v2315 | B8 Saúde 360 | Financeiro canônico: baixa, estorno e fechamento de caixa
-- ----------------------------------------------------------------------------
-- Decisão (R4-B8): para a jornada clínica (consulta -> conta a receber ->
-- recebimento -> caixa/lançamentos/fechamento -> estorno), clinica_* é a fonte
-- canônica de origem do financeiro. As camadas v115/v116 permanecem módulos de
-- consolidação com origem própria (faturamento em lote, relatórios, integração)
-- e NÃO recebem escritas cruzadas desta jornada. Não há nova tabela paralela:
-- esta migração apenas normaliza dados existentes e libera pagamentos parciais.
--
-- Alterações:
--   1) Normaliza contas geradas pela finalização de consulta (que gravava
--      somente valor_bruto/desconto/coparticipacao/valor_liquido): o par
--      operacional canônico passa a ser valor_total/valor_pendente.
--   2) Baixa parcial: remove a unicidade "um recebimento ativo por conta"
--      (ux_recebimento_conta_confirmado). A proteção contra pagamento em
--      excesso passa a ser transacional: lock da conta (FOR UPDATE) +
--      validação valor <= saldo pendente + predicado de valor esperado na
--      atualização da conta (conflicto 409 em corrida).
--   3) Índices de apoio à baixa/estorno/estornos/histórico/autorizações.
-- ============================================================================
set search_path to plantaopro, public;

update plantaopro.clinica_contas_receber
set    valor_total = coalesce(valor_liquido, 0)
where  reg_status = 'A'
  and  coalesce(valor_total, 0) = 0
  and  coalesce(valor_liquido, 0) > 0;

update plantaopro.clinica_contas_receber
set    valor_pendente = greatest(coalesce(valor_liquido, 0) - coalesce(valor_pago, 0), 0)
where  reg_status = 'A'
  and  coalesce(valor_pendente, 0) = 0
  and  coalesce(valor_liquido, 0) > coalesce(valor_pago, 0);

drop index if exists plantaopro.ux_recebimento_conta_confirmado;

-- Índices de apoio: nomes sem qualificação de schema (CREATE INDEX não admite;
-- o search_path acima já posiciona em plantaopro). Tabelas continuam qualificadas.
create index if not exists ix_clinica_recebimentos_cliente_conta_status
    on plantaopro.clinica_recebimentos (cliente_id, conta_receber_id, status);

create index if not exists ix_clinica_lancamentos_cliente_caixa
    on plantaopro.clinica_lancamentos (cliente_id, caixa_id);

create index if not exists ix_clinica_estornos_recebimento
    on plantaopro.clinica_estornos (recebimento_id);

create index if not exists ix_convenio_autorizacoes_cliente_consulta
    on plantaopro.convenio_autorizacoes (cliente_id, consulta_id);
