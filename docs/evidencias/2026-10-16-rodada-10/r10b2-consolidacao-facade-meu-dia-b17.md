# R10-B2 — Consolidação da facade Meu Dia sobre `IProductivityActionService` (B17)

- **Rodada:** 10 — fatia 2 de 4 (plano em `r10a0-auditoria-inicial-plano-sprint.md`)
- **Data:** 2026-10-16
- **Status:** IMPLEMENTADO + TESTADO (aceite por testes executados). HOMOLOGADO/LIBERADO seguem a janela viva.

## 1. O que a dívida dizia

`r8b17-meu-dia-facade-contrato.md` §5 item 1 registrou: "apagar `MeuDiaService`/`IMeuDiaService`/`MeuDiaRepository` e apontar `MeuDiaController` direto para `IProductivityActionService` (montando o mesmo `ProductivityQuery`) é de baixo risco estrutural; fazer junto com uma decisão sobre a janela 'hoje' (item 2)". Item 2 (mesmo doc): a janela "hoje" é `DateTimeOffset.UtcNow.AddDays(1)` — **fuso UTC, não o do usuário** — corrigir = mudança de comportamento/regra, fora de rito.

Validação R8-B17 (fonte canônica): o Meu Dia **não tinha fonte própria** — a `MeuDiaServices.cs` era delegação pura (`ListAsync`/`SummaryAsync`/`QuickActions`) + padrão de janela fixo ("hoje" UTC +1d, PageSize 25; agenda `Module:"CLINICO"`) + mapeamento de nomes PT-BR.

## 2. O que foi feito

| Arquivo | Mudança |
|---|---|
| `backend/PlantaoPro.Api/MeuDiaServices.cs` | **DELETADO** (IMeuDiaService/MeuDiaService/IMeuDiaRepository/MeuDiaRepository — 4 símbolos, 35 linhas) |
| `backend/PlantaoPro.Api/Program.cs` | Removidos os 2 `AddScoped` da fachada (L276–277); `IProductivityActionService` já registrado (L286) |
| `backend/PlantaoPro.Api/Controllers/MeuDiaController.cs` | Reescrito: depende **direto** de `IProductivityActionService`; o padrão de janela foi relocado para helpers privados `JanelaHoje()` / `JanelaAgendaClinica()` com comentário que documenta o caveat UTC e aponta para a dívida residual (r8b17 §5) |
| `backend/PlantaoPro.Tests/R8B17MeuDiaFacadeContratoTests.cs` | Contrato anti-drift **reescrito para a nova topologia** (4 fatos): (1) controller depende do canônico + fachada removida (arquivo inexistente + sem os símbolos em nenhum `.cs` do projeto Api); (2) controller só delega — sem SQL inline/conexão/repositório-alvo direto; (3) padrão de janela **mantra preservado** (`UtcNow.AddDays(1)` + `PageSize: 25` + `Module: "CLINICO"` + caveat UTC documentado); (4) Meu Dia e Central de Ações rosam no MESMO serviço canônico |

### Decisões registradas

1. **Janela UTC permanece** (caveat r8b17 §5 item 2 NÃO foi corrigido nesta fatia — seria mudança de comportamento; fica registrada no comentário do controller, no contrato `JanelaHoje_MantraUtcExplicitaEPreservada` e como dívida residual no relatório final da rodada).
2. **Comportamento HTTP idêntico por rota**: `GET /api/meu-dia` → `ListAsync(DueTo=UtcNow+1d, PageSize 25)`; `/pendencias` → `.Items` do mesmo query; `/agenda` → `.Items` de `ListAsync(Module="CLINICO", DueTo=UtcNow+1d)`; `/indicadores` → `SummaryAsync`; `/acoes-rapidas` → `QuickActions()`; `/alertas` → array vazio; `POST …/concluir|reabrir` → 410 (tudo inalterado).
3. **Regra de "deleção antecipada" respeitada**: R8-B17 só *validou* + contratou; a deleção efetiva acontece agora com evidência própria (esta fatia), como o próprio doc pedia.
4. **Web invariante**: o `MeuDiaController` Web chama a API via HTTP (rotas não mudaram); zero toque em `PlantaoPro.Web`.

## 3. Evidência executada

- Build sln Release: `BUILD_EXIT:0` (0 erros; avisos LF→CRLF habituais).
- Fatia: filtro `R8B17|MeuDiaWps2|R8B19` = **9/9 aprovados** (R8B17 reescrito ×4 + regressões Meu Dia/BI existentes do filtro).
- Suíte full Release: **`Total de testes: 1417`** (= 1416 pós-r10b1 + 1 fact novo do R8B17), **TEST_EXIT:0 — 1417/1417 limpo** (0 falhas; o `Falha ao registrar auditoria legada` no log é linha não-fatal recorrente em rodadas verdes).

## 4. Pendências residuais desta fatia

- Janela "hoje" em fuso do tenant/usuário (mudança de comportamento — decisão de produto, dívida registrada).
- Navegação webview (página Meu Dia renderizando via API) — se houver janela de homologação.
