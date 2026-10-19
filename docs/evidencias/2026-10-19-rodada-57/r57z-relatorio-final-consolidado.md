# R57 — Passada de tom · Relatório final consolidado

**Data:** 2026-10-19 · **Status: FECHADA e pushada** · **Prévia:** `r57a0-passada-de-tom-plano.md`

## Item da fila

Fila r53b1 §7, **item 3**: "Passada de tom: 31 exclamações em Views + 16 valores de toast resx
(seção 6)" — decisão de produto sobre o tom dos toasts de sucesso, pendente desde a auditoria
de tom da R53.

## Correção do inventário R53 (recon desta rodada)

A contagem "31 exclamações em Views" da R53 usava critério largo (todo `!` entre aspas,
incluindo código). Triage fina separou:

| Categoria | Onde | Decisão |
|---|---|---|
| Código Razor/JS (`@(!p.Ativo)`, `!==`, `!Model.X`) | Cadastros, Cirurgias, Orcamentos, Vales, Dashboard, Detalhes, Presenças, `_SuprimentosNav` etc. | preservar (não é copy) |
| CSS `!important` | MeusPagamentos e afins | preservar |
| Glifo decorativo `<span class="pp-alert__icon" aria-hidden="true">!</span>` ×3 | ResetPassword, Pacientes/Mesclar, Plantoes/_PlantaoForm | preservar (ícone, não frase) |
| **Tooltip** `title="Lote vencido!"` | Administrativo360/Estoque.cshtml L157 | **corrigir → sem pontuação final** |
| **Toasts resx** ×4/cultura ×4 culturas = 16 | Vale expédié/reconciliado, Orçamento aprovado, Vale valorizado | **corrigir → ponto** |
| **Mensagem de serviço** `"Cliente criado com sucesso!"` | PlantaoPro.Api/OnboardingService.cs L313 (toast do tenant) | **corrigir → ponto** |

Total real de copy tratada: **18 strings**.

## Decisão de produto — D-R57-1

**Tom declarativo.** Exclamação fim/meio-de-frase banida em texto visível ao usuário; ponto
em vez (tooltip: sem pontuação). Perguntas mantêm `?`. Alinha com precedência já existente
(`"Inventário aberto com sucesso."`, família `Adm360.Organizacao.*`). Exceções travadas no
contrato: glifo decorativo de alerta (`aria-hidden="true"`), CSS `!important`, operadores de
código (`!=`, `!==`, `!x`) e senhas de dados de teste (`B5senha123!`, `PlantaoPro.Admin@2026!Trocar`).

## Mudanças versionadas

1–4. **`UiTexts.resx` / `.en` / `.es` / `.fr`** — 4 toasts por cultura (pt "com sucesso! "→
   "com sucesso. " ×3 + "gerada!"; en "successfully! "→"successfully. " ×3 + "generated!";
   es "con éxito! "→"con éxito. " ×3 + "generada!"; fr "succès ! "→"succès. " ×3 +
   "générée !"). Chaves invariáveis (só o valor); contrato R7DF6 (653 chaves) intacto em
   estrutura.
5. **`Estoque.cshtml`** — `title="Lote vencido!"` → `title="Lote vencido"`.
6. **`OnboardingService.cs`** — `"Cliente criado com sucesso!"` → `"Cliente criado com sucesso."`.
7. **Novo** `R57TomExclamacaoContratoTests.cs` (3 fatos): nenhum `<value>` dos 4 locales com
   `!`; todo `!` em posição de copy de `.cshtml` coberto pelo glifo decorativo (≥3 glifos
   esperados); frase-a-frase das 2 ocorrências fora do resx (tooltip + onboarding).
8. **Ajuste R7DF6** (L313): literal travado `"Vale valorizado com sucesso e venda interna
   gerada!"` atualizado para a nova forma canônica com ponto — regressão **intencional** da
   decisão de tom (mesmo padrão do ajuste do guarda R12 na R56).

## Gates executados

| Gate | Resultado |
|---|---|
| Rebuild real `--no-incremental` (1ª) | exit 0 · 80 avisos · 0 erros |
| `r20b1_gate.ps1` (1ª) | **Now=160 Base=160 DIFF=NENHUMA · erros=0** |
| Dirigidos `R57TomExclamacaoContratoTests` | **3/3** (247 ms) |
| Suíte integral (1ª) | 1623/1624 — falhou 1: `R7DF6…RecursosContemAs653Chaves…` L313 (literal antigo travado) |
| Ajuste R7DF6 + rebuild real (2ª) | exit 0 · gate **Now=160 Base=160 DIFF=NENHUMA · erros=0** |
| Suíte integral (2ª) | **1624/1624** · exit 0 · 25 s |
| Smoke canônica R52 (contra binário final) | **`R52_E2E_OK FALHAS=0`** · MÉDIA_PADRAO=114ms (n=5) · log em `r57_smoke_e2e_log.txt` |
| Mini-e2e R57 `r57_tom_e2e.ps1` | **`R57_E2E_OK FALHAS=0`**: forma antiga ausente; tooltip novo **visível 100×** (lotes vencidos reais do volume demo renderizados); regra geral de tom OK em /Estoque e /Orcamentos (copy=0 além de glifos) · saída em `r57_tom_e2e_saida.txt` |
| Teardown | `PORTAS_LIVRES=True` (exit 1 dos shells background = processo morto pelo Stop-Process, padrão da casa) |

## Incidentes / notas

- Typos de digitação no novo arquivo de teste (`G lifo` com espaço) corrigidos antes do
  primeiro build — sem impacto nos gates.
- A única falha da suíte integral foi o canary R7DF6 travando o valor antigo — exatamente o
  canary que a mudança de tom precisava reescrever (mesma mecânica do ajuste R12 na R56).
- O e2e provou o tooltip novo **renderizado com dados reais** (100 ocorrências em Estoque);
  os toasts em si exigem interação (clique) e ficam travados pelo fato 1 do contrato
  (ausência de `!` em qualquer `<value>` dos 4 locales) + smoke de regressão.

## Suíte-base da casa

**1624** (era 1621 pós-R56; +3 do novo contrato R57).

## Próximo item da fila r53b1 §7

Item 4: **artigos de ajuda DIRETOR/RECEPCAO/PARCEIRO**. Depois: item 5 (acessibilidade
avançada).
