# R7-D-F6 — Admin 360 (cotações + dashboard + resumo organizacional) em recursos localizados + convenção de TempData

Data: 2026-10-11 · Base: `3618afc` (R7-D-F5). Fatia 6 do corte R7-D (escopo em
`r7d0-varredura-design-textos-i18n.md`, seção 6, linha F6).

## Problema (achado R7-D0, Admin 360)

- Literais pt-BR hardcoded nas 3 telas-alvo:
  - `Views/Administrativo360/Cotacoes/Index.cshtml` — título/h2/subtítulo, botões
    (De/Para Geral, Configurar Contas), guia "Como Funciona" (4 passos), selects de
    provedor/status, cabeçalhos da tabela, badges de prazo/quarentena, estado vazio e
    link "Abrir Cotação";
  - `Views/Administrativo360/Gestao/Dashboard.cshtml` — título/h2 (com `&`), botões de
    exportação, rótulos de período, painel de capacidades contratadas (3 cards +
    badges habilitado/desabilitado + botões), 3 grupos de KPIs (~20 labels + units),
    links de ação e "Nenhum registro";
  - `Views/Administrativo360/Index.cshtml` — título/h2/subtítulo, 4 KPIs, 4 áreas de
    navegação e nota de histórico;
- **Convenção de TempData quebrada** nos 9 partials que escrevem mensagens de toast
  (`Administrativo360Controller.{ComprasRecebimentos, Financeiro, Vendas, Vales,
  Cadastros, Cirurgias, Organizacao, Orcamentos, EstoqueValorizacao}.cs`): 60 escritas
  literais diretas (`TempData["Success"/"Error"] = "…"`), 37 fallbacks
  (`resp.Error ?? "literal"`), 18 templates interpolados (`$"Evento de {tipo}…"`,
  ternários ativo/inativado, switch de status de colaborador) e 13 literais nos aliases
  `SuccessMessage`/`ErrorMessage` — o toast (`_ToastMessages.cshtml`) consome esses
  valores verbatim, ou seja, todo texto era pt-BR travado no controller.
- O base/Relatórios (`Administrativo360Controller.cs`, `.Relatorios*.cs`) não escreve
  mensagens de toast — fora do escopo (verificado na varredura).

## Solução

| Peça | O que mudou | Evidência |
|---|---|---|
| `UiTexts.resx` (+ `.en/.es/.fr`) | **+241 chaves** `Adm360.*` (114 de view + 127 de mensagem) em paridade total nos 4 arquivos (**851 chaves por arquivo**, 0 duplicatas, 0 missing/extra conferido por script de paridade) | paridade R7-E1 + contrato novo (fact 4) |
| 3 views-alvo | reescritas por recursos; **valores de máquina intactos** (marcas `OPMENEXO`/`INPART`, `IMPORTACAO_MANUAL`, códigos de status nos selects/switch, capacidades `PORTAIS_COTACAO`/`XML_RECEBIDOS`/`DASHBOARD_GERENCIAL`, rota `/api/administrativo360/gestao/exportar-cotacoes`, tag helpers `asp-action`) | contrato `R7DF6…ViewsAdm360…` (facts 2–3) |
| 9 partials do controller | todas as escritas de toast por `UiText.L("Adm360.…")`; interpolações viraram chaves condicionais; using de `PlantaoPro.Web.Localization` adicionado a cada parcial | contrato `R7DF6…ControllersAdm360…` (fact 1) |
| Testes `R7DF6Admin360I18nContratoTests.cs` (novo) | 4 fatos de contrato de fonte | números abaixo |

## Decisões registradas

1. **Estimativa × real**: a linha do escopo dizia "~30 chaves" (cobria só as views).
   Ao aplicar a convenção de TempData, o escopo real ficou em **241 chaves**
   (127 de mensagem distribuídas pelos 9 partials). Precedente direto: F3 (~25 → 165).
   A divergência é registrada aqui como desfecho da varredura fechada com o usuário.
2. **Reuso deliberado**: `Shift.ThActions` ("Ações", cabeçalho da tabela de cotações)
   e `Implantacao.CloseAria` ("Fechar", aria do fechamento dos alertas nas 3 views).
   **Sem** reuso de `Shift.ClearAll` ("Limpar tudo" ≠ "Limpar") nem de
   `Coverage.ApplyFilters` ("Aplicar filtros" ≠ "Filtrar") → chaves próprias
   `Adm360.ButtonFiltrar`/`Adm360.ButtonLimpar`.
3. **Aliases de toast preservados**: as ações financeiras/comerciais continuam escrevendo
   em `TempData[ok ? "SuccessMessage" : "ErrorMessage"]` — a unificação das quatro chaves
   do `_ToastMessages` (Success/Error/SuccessMessage/ErrorMessage) segue como backlog
   pós-MVP documentado no próprio partial do toast. Nesta fatia apenas o literal do ramo
   de sucesso foi keyizado; o ramo de erro continua sendo `resp.Error` direto.
4. **`resp.Error` dinâmico permanece pass-through** (dívida documentada, padrão F2–F5):
   o texto de erro vindo da API não é rótulo de interface estavel — localizar no lado API
   quando o contrato de erro for consolidado.
5. **Mensagens interpoladas → chaves condicionais** (o `UiText.L` não tem overload com
   parâmetros, confirmado na fonte):
   - Vales: tuple switch `"CONSUMO"/"RETORNO"/"PERDA"` → `(okKey, fallbackKey)` por
     evento; o valor de máquina `tipo` permanece só na rota;
   - Cadastros: ternário `ativo ? OkPartnerActivated : OkPartnerDeactivated` (idem
     Produto);
   - Organizacao: argumentos de sucesso da `TransicaoAsync` (5 transições de
     departamento/cargo/contrato) + switch de status de colaborador
     (`ATIVO/AFASTADO/DESLIGADO`).
6. **Escritas dinâmicas legítimas mantidas**: `TempData["Error"] = erro;` / `= null;`
   (valores calculados em cima, majoritariamente `resp.Error`) — não são literais e
   continuam corretos.
7. **Setas `&rarr;` ficaram no markup** (fora do texto das chaves) — segurança de
   entidade XML nos resx.
8. **Quirk Razor RZ1027**: não acionou em nenhuma view desta fatia (a mesma
   `aria-label="@UiText.L(…)"` implícita compilou sem embrulhar em `@((string)…)`).

## Dívidas registradas (fora do escopo desta fatia)

- `Views/Shared/_SuprimentosNav.cshtml` — ~45 literais compartilhados por ~60 views de
  módulo; candidata a fatia própria.
- Literais de formulário em `ComFormError(...)` (ex.: `Organizacao.cs:237–241`).
- Mensagens de exceção de domínio (ex.: `ArgumentException("Tipo de evento inválido.")`
  em Vales) — não chegam ao toast.
- Demais views sob `Views/Administrativo360/` (80+ arquivos: departamentos, contratos,
  vale detalhes, inventários etc.) — próximos cortes R7-D se a varredura seguir.

## Validação

- Paridade de recursos conferida por script antes do build: **851 chaves por arquivo**
  (`UiTexts.resx` + `.en/.es/.fr`), **0 duplicadas, 0 missing, 0 extra**
  (610 da base F1–F5 + 241 novas desta fatia).
- Builds limpos (`-m:1 -nodeReuse:false`, taskkill + build-server shutdown): Web e
  Tests com **0 erros** (Razor das 3 views incluído no build do Web).
- Run direcionado (F1–F6 + i18n/contratos `R7E1`/`V2*`): **84/84 em ~4 s** — inclui os
  **4 fatos novos** de `R7DF6Admin360I18nContratoTests`.
- Pipeline v2:
  - gate `phaseA` (contratos R7-E1/R7-A1/R7-OB + login): **52/52**;
  - full ×3: **1276 / 1275+1 flake / 1276**. A falha do full2 foi
    `Npgsql 40P01 (impasse)` em `SaasGovernancaB6Rodada4Tests.PurgarAsync()`/`Dispose()`
    — categoria ① do catálogo de flakes; **provada transiente** por rerun isolado da
    classe: **8/8 aprovados** em ~1 s.
  - baseline da suíte sobe de 1272 para 1276 (+4 fatos do contrato desta fatia).

**Estado honesto**: IMPLEMENTADO → TESTADO. AINDA NÃO HOMOLOGADO em host vivo e não LIBERADO
(homologação ao vivo segue dependente do roteiro `r6a1b` §7 + upgrade v2339→v2345 do banco
principal + deploy IIS).
Commit local desta fatia; push diferido por decisão da rodada (regra do prompt §master:
nenhum push/merge como consequência automática desta tarefa).
