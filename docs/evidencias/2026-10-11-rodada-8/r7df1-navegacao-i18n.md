# R7-D-F1 — Navegação global (sidebar + topbar) em recursos localizados

Data: 2026-10-11 · Base: `ff220cc` (R7-OB). Fatia 1 do corte R7-D (varredura design/textos/i18n;
escopo completo em `r7d0-varredura-design-textos-i18n.md`, seção 6).

## Problema (achado R7-D0, jornada 5)

Todo o texto de navegação global era pt-BR hardcoded: as 8 seções + ~30 itens do sidebar
(`_AppSidebar.cshtml:20-97`), topbar ("Abrir menu", "Você está em", "Ambiente operacional",
"Conexão/Protegida", "Buscar no PlantãoPro", "Ajuda contextual" em `_AppTopbar.cshtml`), e o
`session-health.js` que reescrevia "Protegida"/"Sem conexão" direto no JavaScript. Com o
R7-E1 já tendo entregue o mecanismo (`UiText.L` + `UiTexts.resx` ×4 + provider de cultura),
um usuário com idioma en/es/fr via perfil viajava a interface inteira em inglês **menos** o menu.

## Solução

| Peça | O que mudou | Evidência |
|---|---|---|
| `UiTexts.resx` (+ `.en/.es/.fr`) | **+63 chaves `Nav.*`** (59 de navegação + 4 de estado de conexão/subtítulo) em paridade total nos 4 arquivos; nomes das chaves padronizados p/ inglês (exceção herdada `Nav.Pendencias` etc. para espelhar os controllers) | teste de paridade R7-E1 + novo contrato (abaixo) |
| `_AppSidebar.cshtml` | todos os rótulos de seção/item/fallbacks/aria via `@UiText.L("Nav.…")`; **guardas de permissão e rotas intocadas**; termo de produto "Command Center" mantido (nome próprio) | contrato `Sidebar_UseRecursosENaoCarregaLiteraisPtBr` |
| `_AppTopbar.cshtml` | subtítulo default, aria do breadcrumb, contexto operacional, busca, ajuda e bloco de conexão via recursos | contrato `Topbar_UseRecursosENaoCarregaLiteraisPtBr` |
| `wwwroot/js/session-health.js` | sem pt-BR: passa a ler `data-online-text`/`data-offline-text`/`data-aria-online`/`data-aria-offline` aportados localizados pela view | contrato `SessionHealthJs_SemTextoPtBr_E_LiEstadosDosDataAttrsDaView` |
| Testes `R7DF1NavegacaoI18nContratoTests.cs` (novo) | 4 fatos de contrato de fonte: uso das chaves + ausência dos literais pt-BR nos dois partials, JS limpo, chaves presentes nos 4 resx | `[números da execução]` |

## Decisões registradas

1. **Escopo = rótulos de navegação apenas.** Não mexe em guarda de permissão, rota, claims,
   nem em contrato de API. Risco baixo porque a substituição é 1:1 do mesmo texto exibido.
2. **Estado de conexão via data-attrs**: o JS não tem acesso ao resx; a view aporta as
   4 variantes localizadas e o script alterna. Fallback do script preserva comportamento
   anterior se um data-attr faltar.
3. **"Command Center" e "Upgrade" continuam hardcoded** — nomes próprios/prática existente de
   todo o produto; não são rótulos de seção. Registrado como dívida da varredura (F2+).
4. `aria-label="PlantãoPro — início"` (logo) segue hardcoded — contém marca própria; follow-up.
5. Paridade dos 4 resx continua coberta pelo teste R7-E1 existente (igualdade de conjunto de
   chaves + valores não vazios); o novo contrato garante ainda que as chaves específicas de
   navegação existem nos 4 idiomas.

## Validação

- Build do projeto Web (compilação Razor dos partials): **0 erros** (`build14.log`,
  `BUILDWEB14_EXIT:0`); build do Tests: **0 erros**.
- Testes alvo (R7DF1 + R7E1 + V2150 + V2164): **56/56 aprovados em 9 s** (`build14.log`).
- Gate da rodada + suíte completa ×3: gate **52/52** · full1 **1257/1257** · full2
  **1257/1257** · full3 **1257/1257** — janela limpa, sem flake de limpeza neste ciclo
  (`suite-final2-summary.txt` de 10:01 UTC-3, `v2-full*.log`).

**Estado honesto**: IMPLEMENTADO → TESTADO. AINDA NÃO HOMOLOGADO em host vivo e não LIBERADO.
Commit local desta fatia; push diferido por decisão da rodada.
