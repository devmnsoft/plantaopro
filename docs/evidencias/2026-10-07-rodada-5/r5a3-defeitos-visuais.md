# R5-A3 — Defeitos visuais/UX corrigidos pela causa (não remendo de CSS)

Data: 2026-10-08/09. Escopo: inventário de defeitos visuais reais (não cosméticos
escondidos) e correção PELA CAUSA: guard no BFF/API quando a chamada não deveria
existir naquele contexto, leitura honesta quando a ação é impossível, sem ocultar
defeito com overlay/remendo. Auditoria por persona real (login via formulário,
sem `/__test/signin`), mouse+teclado, telas 1440/1024/768/390, white label intocado.

## Defeitos triados e destino de cada um

| # | Sintoma | Causa raiz | Correção | Estado |
|---|---------|-----------|----------|--------|
| 1 | Badge de notificações do admin GLOBAL sem tenant estourava 500 (`UnauthorizedAccessException` no serviço de caixa operacional) | BFF consultava caixa operacional que não existe fora de tenant | Guard `SemCaixaOperacional` em `NotificacoesController` (API): global sem tenant → 200 + lista vazia; nunca consulta o serviço | ✅ commit `a8d4455` + teste de regressão `NotificacoesA3GlobalAdminTests` (4 casos) |
| 2 | Alert "Revise os campos destacados" fantasma em `/MeuDia` | Artefato da AUTOMAÇÃO (clique/evaluate antes da navegação concluir), não do app — reproduzido 0x em uso estável | Nenhum remendo no produto; método de medição ajustado (aguardar `loading=false` antes de avaliar) | ✅ descartado com evidência |
| 3 | Checkout sandbox em estado terminal (PAGA/FALHA/CANCELADA/ESTORNADA) ainda oferecia botão de pagamento que o simulador já recusa = sucesso fictício visual | Página renderizava ações sem consultar o estado | `PaginaAcacoes(status)` em `CobrancaController` (API): terminais oculta botões e mostra nota "Cobranca finalizada"; revalidado nos 4 viewports na fatura PAGA real `SBX-a377…d3` | ✅ commit `a8d4455`, revalidado nesta rodada |
| 4 | `/ConvitesEquipe/Index` para admin GLOBAL sem cliente: `GET api/equipe/convites` → 403 genérico ("Permissão insuficiente ou módulo não contratado") MISTURADO com "Nenhum convite registrado." — dois textos contraditórios na mesma tela | Convites pertencem ao tenant; a área global sem cliente ativo não tem equipe própria, mas o BFF consultava a API escopada mesmo assim | Guard no BFF (`ConvitesEquipeController.Index`): `IsGlobalAdmin && !TenantId` → NÃO chama nenhuma API (confirmado em log: zero chamadas), renderiza `alert-info` "Convites de equipe pertencem a um cliente…" e oculta formulário/tabela (um POST sem tenant daria 403 de novo) | ✅ este commit |
| — | `.pp-modal` sobreposto a conteúdo (suspeita de z-index) | NÃO é defeito: `.pp-modal{position:relative;z-index:1}` fica acima do backdrop irmão no mesmo stacking context (`_OverlayPortal.cshtml`) | Nenhum | ✅ auditoria DOM confirmou sobreposição correta |

## Método da triagem visual (reprodutível)

- Personas com login REAL via formulário: superadmin (`ADMINISTRADOR_GLOBAL`,
  escopo GLOBAL) e gestor demo Santa Casa (`ADMINISTRADOR_CLIENTE`, tenant
  `d3f6584c-…7502`). Sessões não sobrevivem a restart do host ⇒ re-login após cada
  ciclo de rebuild.
- Por tela: `hScroll = scrollWidth - clientWidth` (exigido 0), varredura de elementos
  com `right > viewport+2` (excluindo `#contextHelpDrawer` off-canvas inofensivo),
  contagem de alerts e conflicting-messages, h1/estrutura.
- Viewports 1440/1024/768/390 via iframe same-origin (media queries respondem à
  largura do iframe; janela da máquina mede ~785px, então o iframe também cobre as
  larguras maiores que a janela — leitura de largura efetiva descontado ~15px do
  scrollbar pai: 1425/1009/753/375).
- Revisão de HTML fresco do servidor com `fetch(url,{cache:'reload'})` para descartar
  página obsoleta no cache do navegador (pegou um falso-negativo exatamente aqui).

## Resultados por tela (auditoria final desta rodada)

Super admin (global): `/planos` limpo · `/cadastro/empresa` (wizard 26 inputs) limpo ·
`/Onboarding/Index` limpo (hScroll 0) · `/MeuDia` limpo · `/ConvitesEquipe/Index`
agora entrega a nota informativa (fix #4) · checkout sandbox terminal limpo nos 4
viewports com nota honesta · auditoria DOM das telas SaaS: hScroll 0 em todas.

Gestor Santa Casa (tenant): `/ClientePortal/Index` limpo 390/768/1440 · `/MeuDia`
limpo nos 4 viewports · `/ConvitesEquipe/Index` com FORMULÁRIO e LISTA reais
(o caminho bom do fix #4: usuário de tenant não sente o guard) ·
`/Plantoes/Index` → `AccessDenied?module=PLANTOES&reason=MODULO_NAO_CONTRATADO` —
o gate canônico da B4 funcionando (tenant demo sem assinatura ativa), tela limpa ·
`/Notificacoes/Index` → gate `CATALOGO_NAO_CONFIGURADO`, tela limpa.

## Descobertas registradas (não são defeitos visuais, viram trilha)

- O gate de catálogo usa o slot `module` da URL de AccessDenied para o MOTIVO
  (`module=CATALOGO_NAO_CONFIGURADO`) — semântica de parâmetro trocada em
  `SaasRouteGuardFilter`; cosmético em URL, mas dificulta telemetria. Backlog E13.
- Tenant demo Santa Casa vive só nas tabelas-legado (`clientes`/`tenant_modulos`
  origem LEGADO), não em `saas_clientes`/`saas_assinaturas` canônicos — por isso
  TODAS as telas de operação caem em MODULO_NAO_CONTRATADO. A primeira jornada por
  módulo (D9–D12) precisa de provisionamento canônico (B5) de um tenant de operação,
  não do demo legado. Backlog anotado em D9.
- Schema real do banco é `plantaopro` (search_path definido pela conexão da API);
  consultas ad-hoc precisam de `SET search_path TO plantaopro`.

## Re-check GATE A (parcial, desta entrega)

- Auth/sessão OK com login real nos dois perfis ✅
- Persistência real (nada de sucesso fictício): checkout reabre estado do banco;
  guard de convites não gera escrita ✅
- Comandos críticos sem sucesso fictício (fix #3 reforçado em 4 viewports) ✅
- Navegação utilizável sem alertas contraditórios (fix #4 fecha o último caso) ✅
- Testes: suíte completa **1163/1163** pós-fix (API Web incluídos no build) ✅
- Pendências do GATE A fora desta máquina: IIS vivo, upgrade do `plantaopro`
  principal (v2332 só aplicado em `plantaopro_test`), aceite manual do usuário.

## Limitações honestas

- Screenshots de página inteira bloqueados pelo harness ("Screenshot needs a
  visible tab" — exige janela do Review pane visível); evidência é por auditoria
  DOM/HTML do servidor + logs dos hosts, método acima é repetível.
- Telas de operação ricas (escalas/plantões/saúde) ainda não auditadas COM DADOS:
  Persona gestora está atrás do gate honesto (sem assinatura). Auditá-las exige a
  esteira D9–D12 primeiro — registrado como dependência, não como tela aprovada.
- Janela física ~785px; 1440/1024 cobertos por iframe (viewport efetivo correto,
  mas sem captura raster).
