# R57 — Passada de tom (fila r53b1 §7 · item 3)

**Data:** 2026-10-19 · **Prévia desta rodada.** Gates executados no relatório final.

## Escopo (origem)

Fila de pendências consolidadas da R53 (`r53b1-diretriz-relatorio-antes-depois-por-epic.md` §7,
item 3): "Passada de tom: 31 exclamações em Views + 16 valores de toast resx (seção 6)".
A auditoria de tom da R53 (§6) inventariou exclamações em copy e delegou a **decisão de
produto sobre o tom dos toasts de sucesso**.

## Reconhecimento (correção do inventário R53)

A contagem "31 exclamações em Views" da R53 usou um critério largo (todo `!` dentro de trechos
entre aspas). Triage fina desta rodada separou **copy visível** de código:

| Categoria | Exemplo | Decisão |
|---|---|---|
| Código Razor/JS | `value="@(!p.Ativo)"`, `!==`, `!Model.X` | não alterar (não é copy) |
| CSS | `display:none!important` | não alterar |
| Ícone decorativo | `<span class="pp-alert__icon" aria-hidden="true">!</span>` ×3 (ResetPassword, Pacientes/Mesclar, Plantoes/_PlantaoForm) | preservar (glifo, não frase) |
| **Copy visível real** | tooltip `title="Lote vencido!"` (Estoque.cshtml L157) | **corrigir** |
| Toasts resx | 4 valores por cultura × 4 culturas = 16 | **corrigir** |
| Mensagem de serviço | `"Cliente criado com sucesso!"` (OnboardingService.cs L313 → toast do tenant) | **corrigir** |

Total real de copy tratada: **18 strings** (16 resx + 1 tooltip + 1 mensagem API).

## Decisão de produto — D-R57-1

**Tom declarativo.** Exclamação fim/meio-de-frase é banida em texto visível ao usuário;
substituição por ponto (tooltip: sem pontuação final). Alinha com precedência já presente na
base (ex.: `"Inventário aberto com sucesso."`, `Adm360.Organizacao.*`: `"Departamento
inativado. Cargos e colaboradores..."`). Perguntas mantêm `?`.

**Exceções travadas como permitidas** (contrato explicita cada uma):
1. Glifo decorativo de alerta `>!</span>` com `aria-hidden="true"` (ícone, não frase).
2. CSS `!important`.
3. Operador lógico/igualdade de código (`!=`, `!==`, `!x`).
4. Senhas de dados de teste (`B5senha123!`, `PlantaoPro.Admin@2026!Trocar`) — dado, não copy.

## Mudanças

1. **`UiTexts.resx` (pt-BR)** — 4 toasts: `com sucesso! ` → `com sucesso. ` (×3),
   `gerada!` → `gerada.` (1).
2. **`UiTexts.en.resx`** — 4 toasts: `successfully! ` → `successfully. ` (×3), `generated!` → `generated.`.
3. **`UiTexts.es.resx`** — 4 toasts: `con éxito! ` → `con éxito. ` (×3), `generada!` → `generada.`.
4. **`UiTexts.fr.resx`** — 4 toasts: `succès ! ` → `succès. ` (×3), `générée !` → `générée.`.
5. **`Administrativo360/Estoque.cshtml`** — `title="Lote vencido!"` → `title="Lote vencido"`.
6. **`PlantaoPro.Api/OnboardingService.cs`** — `"Cliente criado com sucesso!"` → `"Cliente criado com sucesso."`.

Chaves resx invariáveis (só o valor muda) — contrato R7DF6 (653 chaves) intocado. Nomes de
código, rotas, ícones `bi-exclamation-*` invariáveis.

## Contrato novo

`R57TomExclamacaoContratoTests` — 3 fatos:
1. `Resx_NenhumValorVisivelContemExclamacao` — nenhum `<value>` dos 4 locales contém `!`.
2. `Views_SemExclamacaoEmTextoVisivelForaDoGlifoDeAlerta` — em todo `.cshtml` de Views, a
   contagem de `!` em posição de copy (`![\s"'<,)]`) é coberta integralmente pelos glifos
   decorativos `>!</span>` (≥3 esperados); qualquer excesso falha o fato com a lista de arquivos.
3. `MensagensCorrigidas_NaoRegressamAExclamacao` — trava frase a frase as 2 ocorrências fora
   do resx (tooltip do Estoque sem `!` e mensagem do onboarding com ponto).

Suíte esperada: 1621 → **1624**.

## Gates (executados no relatório final)

1. Rebuild real `dotnet build backend\PlantaoPro.sln -c Release --no-incremental` → log
   `%TEMP%\opencode\r20b1_build.log` → `r20b1_gate.ps1` (espera `Now=160 Base=160 DIFF=NENHUMA`).
2. Contratos dirigidos R57 (3/3).
3. Suíte integral (~1624).
4. Smoke canônica R52 (`R52_E2E_OK FALHAS=0`) contra o binário final.
5. Docscan dos docs desta rodada.
