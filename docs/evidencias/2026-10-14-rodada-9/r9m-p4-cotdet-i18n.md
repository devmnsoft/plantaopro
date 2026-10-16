# Rodada 9 — r9m-P4: i18n completude de `Cotacoes/Detalhes.cshtml` (fora do bloco ANVISA)

Data: 2026-10-15 · Base: `5f3fed8` (fechamento rodada 9) · Ambiente local/homologação · Sem push/merge/publish.

## 1. Objetivo e escopo

Fechar a dívida registrada em r9j §7: eliminar os literais estáticos pt-BR fora do bloco ANVISA em `Views/Administrativo360/Cotacoes/Detalhes.cshtml`, no mesmo padrão de RelFin/Produtos (100% dos literais estáticos): título/breadcrumb dinâmicos, header/botões/alerta de cancelamento, guia da tela (5 parágrafos), alerta de itens pendentes, cards de status/provedor/prazo, tabela de itens + modal de mapeamento, anexos, fila outbox (badges + próximas ações), estorno de transmissão e painel do assistente IA. O bloco ANVISA (r9e/r9j) e seu JS permanecem inalterados.

## 2. Mudanças (arquivos)

| Arquivo | Escopo | Resultado |
|---|---|---|
| `UiTexts.resx` + `.en/.es/.fr` | **103 chaves novas** `Adm360.CotDet.*` — guias com `<strong>` escapado, fragmentos com espaços preservados (`xml:space="preserve"`), nota do painel IA com `<a>` embutido (`@Html.Raw`) | contrato Adm360 **502 → 605 chaves × 4 idiomas** |
| `Views/Administrativo360/Cotacoes/Detalhes.cshtml` | 102 substituições exatas (contagem validada pelo gerador; strings dinâmicas compostas por fragmentos no Razor — ex.: `CanceladoEmPrefix` + data + `CanceladoEmSuffix`) | linhas residuais com acento = 7, todas comentários HTML/Razor não renderizados |
| `PlantaoPro.Tests/R7DF6Admin360I18nContratoTests.cs` | contagem 502→605 + chaves-amostra + âncoras canônicas pt-BR (mestre) e en/es/fr; **fact novo** `ViewsAdm360_R9mDetalhesCotacaoCompletudeSemLiterals` (chaves usadas/reutilizadas, estados de máquina/formatos preservados, 15 literais ausentes) | suíte efetiva 1397 → 1398 |

**Reuso de chaves existentes** (sem duplicar valor): `Shift.Back` (voltar ×2), `Clinical.ButtonCancel`, `Implantacao.CloseAria` (fechar ×3), `Login.HelpSummary` ("Como usar esta tela"), `Adm360.Cotacoes.ThResponseDeadline`, `Adm360.Cotacoes.StatusExpirada`.

**Convenções preservadas** (r9h/r9j): tokens universais `R$`, formatos `N2`/`N4`, `·`, `—`, `KB`, `SHA256:`, token de revisão `v@nº`, códigos de máquina inteiros (`NA_FILA`, `RELACIONADO`, `NAO_ATENDIDO`, `EXPORTADA_MANUALMENTE`, `CONFIGURACAO_PENDENTE`, …), marca ANVISA mantida nos 4 idiomas, sem BOM nos resx, valores vazios = 0.

## 3. Evidência

| Check | Resultado |
|---|---|
| Parse XML dos 4 resx + contagem `data name="Adm360.` | **605/605/605/605**; 0 `<value>` vazio |
| Paridade do conjunto de chaves entre culturas (R7E1) | PASS |
| Contratos i18n direcionados (R7DF6/R7E1/R7DF1/R7DF4) | **14/14 PASS** |
| Suíte completa (Release) | **1398/1398 PASS** |
| Scan de linhas com acento na view pós-substituição | 7 linhas — todas comentários `<!-- -->` / `@* *@` (não renderizados) |

Notas de processo: gerador com catálogo 103 chaves × 4 idiomas + substituições exact-count executou limpo após três correções — duas no próprio gerador (backslash final em string raw; apóstrofo francês dentro de string entre aspas simples) e uma na view L3 (chaves de interpolação `{}` do `$"…"` perdidas na transição f-string→concatenação do gerador; capturado pelo build Razor — CS1002 — antes de qualquer execução de teste).

## 4. Migrations

Nenhuma — somente view/recursos/teste.

## 5. Pendências remanescentes

- P1 (aprovação visual/screenshots) segue gateada pela visibilidade da janela desktop (r9l). A tela passa a renderizar via resx no lugar de literais: com a cultura de UI atual (pt-BR, canônica) o conteúdo visível é idêntico — sem mudança de comportamento nem de dados.
