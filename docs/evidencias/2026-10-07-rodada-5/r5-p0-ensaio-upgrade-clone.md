# R5-P0 — Ensaio de upgrade do banco principal em clone (ferramenta oficial)

Data: 2026-10-09. Escopo: provar, sem tocar no banco `plantaopro` (apenas leitura),
que `dotnet run --project backend\PlantaoPro.Tools.Database -- upgrade` roda verde de
ponta a ponta sobre um clone fiel do banco principal, deixando-o no shape canonico
da rodada 5. Nenhum dado do banco principal foi alterado pelo ensaio.

## Metodo

1. `create database plantaopro_p0ensaio template plantaopro` com zero conexoes ativas
   no principal (verificado via `pg_stat_activity` antes do clone).
2. Upgrade pela ferramenta oficial com `PLANTAOPRO_CONNECTION_STRING` apontando para
   o clone; comando identico ao que sera usado no P0 formal.
3. Verificacoes estruturais + `status` oficial + repeticao do upgrade (idempotencia).
4. Prova final de fechamento: drop do clone, novo clone fresco e upgrade completo em
   comando unico.

## Achados do ensaio (todos corrigidos no repositorio, nao na mao)

1. **Divergencia de checksum aplicado** em `2026_fix_clinica_financeiro_minimo`: o
   banco principal registrou o arquivo na revisao do commit `3e12b8e1`
   (`407017a9…`); o arquivo recebeu edicao aditiva comprovada (+11 linhas de
   `add column if not exists` em `clinica_caixa`, convergencia de shape, commits
   `git log`/diff) e o manifesto foi atualizado para `cd6316fa…` sem atualizar o
   registro no banco. Escaneamento completo manifesto-vs-banco achou exatamente
   ESTA divergencia (ok=4 registros validados, 1 divergente, 0 arquivos ausentes).
   Solucao: **recuperacao excepcional controlada na ferramenta** (mesmo padrao do
   precedente v2197): para a versao e o checksum antigo exatos, a linha antiga e
   removida e o conteudo atual e reaplicado e re-registrado. Reaplicacao segura
   porque todo o arquivo e idempotente (DDL `if not exists`; os 3 inserts demo tem
   guarda `where not exists`).
2. **Shape legado de `plantaopro.tenant_modulos`**: linhagens pre-catalogo SaaS tem a
   tabela com colunas antigas (`codigo/dados/criado_em`) sem as colunas canonicas; os
   `create table if not exists` da sequencia canonica nao convergem shape e o indice
   unico parcial de `2026_plantao_pro_self_service_white_label`
   (`tenant_id, lower(codigo_modulo) WHERE reg_status='A'`) falhava com 42703.
   Solucao: migration **v2337** replicando exatamente o bloco de colunas da
   reconciliacao canonica v2197 (mesmas colunas/nulabilidades; tabela com 0 linhas no
   principal; v2197 segue sendo a reconciliacao de dados/constraints ao chegar sua vez).
   Inserida no manifesto imediatamente antes de self_service (ordem do array e a ordem
   de execucao da ferramenta; nenhum teste ancora a posicao, apenas a presenca).
3. **Códigos ADM360 fiscais criados apenas pelo runtime da API**: `ADM360:CRIAR,
   EDITAR, CONFIGURAR, REABRIR, CONFIRMAR, CANCELAR, CONFERIR` so existiam no dicionario
   de permissoes do `Program.cs` da API; em bancos onde a API nova ainda nao subiu, a
   sanidade da v2322 (R5-A2) quebrava. Solucao: migration **v2338** cadastrando os 7
   codigos faltantes com o MESMO padrao da v2198 (acao EDITAR, modulo ADM360 quando
   existir), idempotente, posicionada antes da v2322 no manifesto.
   Obs.: `ADM360:CONFERIR` nao consta no catalogo do runtime; ficou coberto pela v2338
   (o evaluator D11 dual-scope usa os codigos por acao) — candidado a decisao futura de
   catalogo junto com a granularidade SAUDE360.

## Resultado da prova de fechamento (clone fresco)

- Clone novo do principal → upgrade oficial em comando unico: **104 aplicacoes em
  ordem canonica**, incluindo a recuperacao de drift (`recuperacao controlada de
  checksum … reaplicando conteudo atual`), v2337, v2338 e toda a cadeia R5
  (v2322–v2336). Sem erros.
- `status` oficial pos-upgrade: `aplicadas=115 esperadas=111 falhas=0 pendentes=0`.
- Re-tick do upgrade no clone ja atualizado: **0 aplicacoes** (idempotencia).
- Verificacoes estruturais no clone:
  - `upgrade_solicitacoes`, `cobranca_providers/cobrancas/webhook_eventos`,
    `onboarding_etapas_catalogo` (12 etapas) e `tenant_onboarding_checklist`: OK;
  - `token_hash` presente em `cadastro_cliente_convites` e `usuario_convites` (2/2);
  - 13 codigos ADM360 fiscais presentes (faltando 0);
  - grants finos ADMINISTRADOR_CLIENTE: PACIENTES=31, CONSULTAS=31 (no-op natural nos
    modulos sem catalogo, conforme contrato documentado da v2335);
  - `tenant_modulos` com as 6 colunas canonicas verificadas (codigo_modulo,
    modulo_id, habilitado, origem, reg_status, reg_date);
  - registro do drift agora com checksum `cd6316fa…` (conteudo executado = arquivo);
  - `clinica_caixa` com as 8 colunas da edicao posterior (convergidas pela reaplicacao);
  - zero linhas com `success=false` no registro.
- Suite completa `dotnet test` (hosts derrubados): **1183 aprovados, 0 falhas**.

## O que isso garante para o P0 formal

No banco principal real resta executar o MESMO comando unico do ensaio; a recuperacao
de checksum e as guardas de shape/codigos estao no repositorio (ferramenta + manifesto
+ v2337/v2338), sem procedimento manual pendente nem remendo fora das migrations.
Decisoes do usuario continuam pendentes: push do `main`, execucao do upgrade no banco
principal + deploy IIS, e granularidade SAUDE360 (que tambem destrava unificar o FK de
`agendamentos.unidade_id`).
