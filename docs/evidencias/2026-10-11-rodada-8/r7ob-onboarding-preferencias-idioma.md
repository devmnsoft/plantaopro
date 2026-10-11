# R7-OB — Passo "Definir o idioma da interface" na jornada de onboarding (v2345)

Data: 2026-10-11 · Base: `HEAD = cedd209` (R8-A1; `734a3e8` = auditoria R8-A0). Banco de prova:
`plantaopro_test` (PostgreSQL 18 local, serviço Windows); migração **v2345** aplicada via
`PlantaoPro.Tools.Database upgrade` (coluna/etapa verificadas pelos testes).

## Problema (achado R8-A0)

A jornada de onboarding por contrato (`OnboardingJornadaService`, catálogo
`onboarding_etapas_catalogo`) tinha 12 etapas e **nenhuma** para idioma — a linha da matriz
R8-A0 marcava "passo 6 Preferências (idioma) pendente → agora habilitado por R7-E1", mas o
habilitamento era só no dado (`usuarios.idioma_preferido`, v2344): a jornada não expunha nem
derivava nada disso. A fila da rodada 7 previa este corte como próximo pela ordem de
dependências (R7-E1 → R7-OB → R7-D).

## Solução (escopo mínimo e honesto)

| Peça | O que mudou | Evidência |
|---|---|---|
| Migração `2026_10_v2345_r7ob_onboarding_preferencias_idioma.sql` | Nova linha de catálogo `ONB_PREFERENCIAS` (upsert idempotente, mesmo formato do v2333): núcleo (`modulo_codigo` nulo), `ordem=50` (após identidade visual, antes dos módulos), **opcional**, responsável CLIENTE, critério `IDIOMA_PREFERENCIA_DEFINIDA`, link `/Usuario/Index` (onde o R7-E1 colocou o seletor de idioma) | manifest `migration-manifest.json` (checksum SHA-256 verificado pelo próprio tool: `15d6bdb1…`); aplica limpo em `plantaopro_test` |
| `OnboardingJornadaService.cs` | Case `IDIOMA_PREFERENCIA_DEFINIDA` no avaliador + entrada em `CriterioDescricoes`. Predicado **idêntico ao do login (R7-E1)**: padrão da organização (`tenants.metadata->>'idiomaPadrao'`, chave `id`, `reg_status='A'`) OU ao menos um usuário ativo (`reg_status='A'`, escopo `tenant_id`/`cliente_id`) com `idioma_preferido` não nulo. Evidências legíveis nomeiam qual fonte concluiu | testes O2/O3 abaixo |
| Web | **Zero mudança** — a tela de Implantação renderiza o checklist genérico vindo de `api/onboarding/checklist`; a etapa nova aparece automaticamente após `sincronizar`/`iniciar` | comportamento coberto pelos testes do service (mesmo contrato das C7) |
| Testes `R7OBOnboardingPreferenciasIdiomaTests.cs` (novo) | 5 fatos, molde C7 (GUIDs fixos `7a0b…`, semente idempotente por teste, coleção serial): **O1** materialização fiel ao catálogo + idempotência do iniciar; **O2** conclusão pela preferência do usuário e **reversão** quando ela some; **O3** conclusão pelo padrão da organização nomeando o idioma na evidência + reversão ao limpar; **O4** opcional pulável com justificativa persistida e restaurar sem duplicar; **O5** clique sem dado não conclui (409, sem sucesso fictício) | **5/5 aprovados em 2 s** contra `plantaopro_test` real (`build13.log`, `run-r7ob.log`) |

## Decisões de semântica (registradas para não virar surpresa)

1. **Opcional** (`obrigatorio=false`): nada definido = pt-BR por omissão é estado válido do
   produto; a etapa existe para o tenant escolher conscientemente (organizações não
   lusófonas não podem passar despercebidas), mas não trava o final do onboarding. Pulável
   com justificativa persistida, como o white label (C5).
2. **Conclusão derivada de dados** (filosofia C7): o clique sozinho não conclui (409); a
   verdade segue os dados — apagar a preferência ou o padrão reverte a etapa.
3. **Duas fontes aceitas**: preferência individual (UI já existe, R7-E1) OU padrão da
   organização (aí ainda **sem UI administrativa** — dívida registrada no doc do R7-E1 §6;
   o critério aceita a fonte mesmo assim para não punir quem configurou direto no dado).
4. Wizard MNSOFT `NovoCliente` (stepper 5 passos, único POST) **não foi alterado** — outra
   oportunidade da matriz (§12.6), fora deste corte.

## Validação

- Migração v2345 aplicada em `plantaopro_test` via `PlantaoPro.Tools.Database upgrade`
  (checksum conferido pelo tool).
- Lição de execução: o `NpgsqlConnectionStringBuilder` do tool **não aceita formato URL**
  (`postgresql://usuario:senha@host:db`, o que o psql usa) e falha com *"Format of the
  initialization string does not conform to specification starting at index 0"*. A variável
  `PLANTAOPRO_CONNECTION_STRING` precisa do formato chaves:
  `Host=127.0.0.1;Port=5432;Database=plantaopro_test;Username=postgres;Password=***`
- Testes R7-OB: **5/5** isolados (2 s); duas iterações de teste capturaram fatos reais do
  schema (ambos bugs do teste, não do produto): o literal jsonb exige chaves `{...}`, e
  `tenants.metadata` é **NOT NULL** com conteúdo mínimo `{}` — o que explica por que o R7-E1
  lê o padrão com `coalesce(metadata->>'idiomaPadrao','')` (chave ausente, nunca coluna nula).
- Gate da rodada: contratos + R7E1 + R7A1 + R7OB = **52/52 aprovados** (`v2-phaseA-contracts-r7e1-r7a1-r7ob.log`).
- Suíte completa ×3 (total agora 1253 por execução): **1252/1253** (full1) ·
  **1253/1253** (full2) · **1253/1253** (full3). Falha única do full1 = mesma assinatura de
  flake de limpeza já catalogada na R8-A0: `Npgsql 40P01` em `PurgarAsync` dentro de
  `Dispose` (`SaasComercialB4MatrizTests.cs:417`), assertions do teste passaram; classe
  reexecutada isolada = **12/12 em 1 s** (`rerun-b4.log`). Máquina calma nesta janela
  (suíte completa ~20-35 s por execução).

**Estado honesto**: IMPLEMENTADO → TESTADO (suíte + banco real). AINDA NÃO HOMOLOGADO em
host vivo e não LIBERADO. Commit local desta fatia (`git log --oneline -3`); push diferido
por decisão da rodada (regra da master: não publicar como consequência automática).
