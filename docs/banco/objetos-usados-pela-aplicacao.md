# Objetos PostgreSQL usados pela aplicação

A varredura considerou ocorrências C# de `FROM plantaopro.`, `JOIN plantaopro.`, `INSERT INTO plantaopro.`, `UPDATE plantaopro.`, `DELETE FROM plantaopro.` e chamadas Dapper (`QueryAsync`, `QueryFirst`, `QuerySingle`, `ExecuteAsync`, `ExecuteScalar`).

## Controle automatizado

`scripts/validate-scrpt-completo.py` extrai os objetos `plantaopro.<nome>` do código C# e compara contra `database/scrpt_completo.sql`. O CI falha se um objeto utilizado pela aplicação não estiver presente no script completo.

## Objetos críticos e índices

- SaaS/identidade: `planos`, `clientes`, `tenants`, `usuarios`, `perfis`, `permissoes`, `perfil_permissoes`, `tenant_modulos`, `assinaturas`; filtro por `tenant_id`/`cliente_id`; índice `ix_<tabela>_tenant_reg`.
- Operação: `hospitais`, `unidades`, `especialidades`, `medicos`, `plantoes`, `escalas`, `convites`, `pagamentos`; filtro por tenant/cliente e período; índices `ix_<tabela>_tenant_reg` e `ix_<tabela>_status_reg_date`.
- Saúde 360: `pacientes`, `agendamentos`, `painel_chamada_fila`, `triagens`, `consultas`, `cid_tabela`, `prescricoes`, `convenios`, `planos_saude`; filtro por tenant/cliente; sem conteúdo clínico em logs.
- Financeiro: `clinica_contas_receber`, `clinica_recebimentos`, `clinica_caixa`, `caixa_movimentos`, `glosas`, `repasses_medicos`; filtro por tenant/cliente e período.
- Auditoria/observabilidade: `auditoria`, `auditoria_lgpd_eventos`, `eventos_sistema`, `api_request_logs`, `api_error_logs`, `login_tentativas`, `relatorio_exportacoes`.
