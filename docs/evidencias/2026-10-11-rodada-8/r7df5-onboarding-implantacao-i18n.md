# R7-D-F5 — Onboarding/Implantação em recursos localizados + acentuação corrigida

Data: 2026-10-11 · Base: `c07c9dc` (R7-D-F4). Fatia 5 do corte R7-D (escopo em
`r7d0-varredura-design-textos-i18n.md`, seção 6, linha F5).

## Problema (achado R7-D0, jornada de implantação)

- **Acentuação errada em strings do controller** (`OnboardingController.cs`):
  "as etapas refletem os **modulos** contratados" (:96) e "Etapa **concluida**." (:115) —
  visíveis como toast para todo perfil que opera a jornada;
- Literais pt-BR hardcoded nas 4 telas do módulo:
  - `Views/Onboarding/Index.cshtml` — hero, estados SemContextoCliente/SemOnboarding,
    resumo de progresso (aria do anel), legenda de status, linha do responsável,
    nota de dependência, critério, pills de status e botões Concluir/Pular/Restaurar;
  - `Views/Onboarding/NovoCliente.cshtml` — wizard MNSOFT completo: hero, stepper
    (5 passos), 3 seções com ~25 labels e helps, select de status inicial, rodapé e
    painel de resumo;
  - `Views/Onboarding/Sucesso.cshtml` — alerta de sucesso e 5 labels do resumo;
  - `Views/Implantacao/Index.cshtml` — hero, KPIs (4), filtro, estados (loading/empty),
    timeline/evidências e modal de validação;
- `wwwroot/js/implantacao.js` — 4 strings exibidas pelo JS ("Responsável: … Prazo: …",
  "Validar etapa", mensagem de falha de carga e de validação) hardcoded;
- `OnboardingController.cs` — 11 mensagens (tempdata/erro/info + nome do grupo
  fallback "Implantação geral").

## Solução

| Peça | O que mudou | Evidência |
|---|---|---|
| `UiTexts.resx` (+ `.en/.es/.fr`) | **+132 chaves** (`Onboarding.*` + `Implantacao.*`) em paridade total nos 4 arquivos (**610 chaves por arquivo**, 0 duplicatas, 0 missing/extra conferido por script de paridade) | paridade R7-E1 + contrato novo |
| `OnboardingController.cs` | 11 strings via `UiText.L("Onboarding.…")`; **acentos corrigidos nos valores pt-BR** (`módulos`, `concluída`); rotas/POSTs/helpers intactos | contrato `R7DF5…OnboardingController_…` |
| `Onboarding/Index.cshtml` | tudo via recursos; `ProgressAria`/`StepsAria` com `string.Format(InvariantCulture, …)`; **valores de máquina intactos** (`FINALIZADO`, `CONCLUIDO/PENDENTE/PULADA/BLOQUEADO`, comparação `Responsavel == "MNSOFT"`) | contrato `R7DF5…OnboardingViews…` |
| `Onboarding/NovoCliente.cshtml` | wizard completo via recursos; valores dos selects (`ATIVO`/`TESTE`) e passos do stepper (`data-step`, `aria-current`) íntegros | idem |
| `Onboarding/Sucesso.cshtml` | alerta + 5 labels + CTA via recursos | idem |
| `Implantacao/Index.cshtml` + `js/implantacao.js` | view via recursos + **5 data-attrs localizados** expostos ao JS (`data-responsavel-label`, `data-prazo-label`, `data-validar-etapa-label`, `data-load-error`, `data-validate-error`) — padrão F1 (session-health); JS consome os attrs com fallback pt-BR; rotas da API, ids e opções de status intactos | contrato `R7DF5…ImplantacaoCentral…` |
| Testes `R7DF5OnboardingImplantacaoI18nContratoTests.cs` (novo) | 4 fatos de contrato de fonte | `[números]` |

## Decisões registradas

1. **Reuso deliberado de chaves existentes**: `Onboarding.PageTitle` também é o
   eyebrow do wizard (mesma frase "Onboarding SaaS"); `Profile.Phone` (F2),
   `Clinical.LabelFullName` (F4) e `Shift.Back` (F3) para Telefone/Nome completo/Voltar;
   `Clinical.ButtonCancel` (F4) no modal da Central de Implantação;
   `Onboarding.ResponsiblePrefix` compartilhado entre o checklist do onboarding e o
   data-attr do JS (registro único da frase).
2. **Acentos corrigidos nos VALORES PT-BR dos recursos** (e não só no controller) —
   o contrato afirma a correção em ambos os pontos: literal antigo ausente no
   controller + valor com acento presente no resx pt-BR.
3. **`PlantãoPro v1.18.9` (kicker da Central) ficou hardcoded** — marca + versão são
   dados de build, não rótulo de interface (mesma regra F1 para nomes de marca).
4. **JS recebe textos por data-attrs localizados com fallback pt** (padrão F1): o
   arquivo `.js` não referencia os resx diretamente; a página injeta os valores já
   traduzidos. O template dinâmico `Responsável: X · Prazo: Y` virou
   `${responsavelLabel}: X · ${prazoLabel}: Y`.
5. **Valores de máquina intactos**: status `FINALIZADO/CONCLUIDO/PENDENTE/PULADA/BLOQUEADO`,
   opções `ATIVO/TESTE`, `PENDENTE/OK` do filtro, comparação `Responsavel == "MNSOFT"`,
   `data-step`/`aria-current` do stepper, rotas `/api/implantacao/*` e `data-*` de URL.
6. **Strings vindas da API** (`e.Titulo`, `e.Objetivo`, `e.Descricao`,
   `e.CriterioDescricao`, `e.Evidencia`, `Model.ProximaAcao`, nomes de módulo do
   checklist) seguem passando direto — conteúdo de dados do catálogo canônico
   (dívida do lado API, mesmo padrão das F2–F4).
7. **Quirk do Razor (achado empírico nesta fatia)**: a forma implícita
   `aria-label="@UiText.L("Onboarding.StepperAria")"` na linha do stepper
   (`<ol class="pp-stepper" …>`) gerava `RZ1027 "(" missing ")"` + `CS1525` no
   build do Web — embora a MESMA sintaxe compile em dezenas de outras views
   (mesma build) e a mesma linha com outra chave/tag/classe também compilar
   (bissecção: o trigger exige `<ol>` + classe `pp-stepper` + chave contendo
   "Stepper" + fechamento na linha seguinte — comportamento stateful do
   scanner de expressão implícita do Razor). Correção: expressão explícita
   `aria-label="@((string)UiText.L("Onboarding.StepperAria"))"` na linha do
   stepper (verificado em build limpo: 0 erros). Lição para as próximas
   varreduras: se o Razor apontar RZ1027 numa atribuição com `UiText.L`,
   embrulhar em `@( … )` antes de renomear chaves.

## Validação

- Paridade de recursos conferida por script antes do build: **610 chaves por arquivo**
  (`UiTexts.resx` + `.en/.es/.fr`), **0 duplicadas, 0 missing, 0 extra**
  (478 da base F1–F4 + 132 novas desta fatia).
- Build do Web com a correção do quirk Razor (expressão explícita no stepper): **0 erros**.
- Run direcionado (F1–F5 + i18n/contratos `R7E1`/`V2*`): **80/80 em ~5 s** — inclui os
  **4 fatos novos** de `R7DF5OnboardingImplantacaoI18nContratoTests`.
- Pipeline v2 (build limpo, `-m:1`, sem push): 
  - gate `phaseA` (contratos R7-E1/R7-A1/R7-OB + login): **52/52**;
  - full ×3: **1272 / 1272 / 1272**, todos verdes, **sem flake neste ciclo**
    (baseline da suíte sobe de 1268 para 1272 = +4 fatos do contrato desta fatia).

**Estado honesto**: IMPLEMENTADO → TESTADO. AINDA NÃO HOMOLOGADO em host vivo e não LIBERADO.
Commit local desta fatia; push diferido por decisão da rodada.
