# Checklist mobile — v1.77.0

Viewports contratados: `360x800`, `390x844`, `430x932`, `768x1024`, `1024x768`, `1366x768`, `1440x900` e `1920x1080`.

| Critério | Validação estática | Runtime |
|---|---|---|
| Sem overflow horizontal crítico | Smoke verifica `noHorizontalOverflow`/`noClippedCards` | Bloqueado |
| Cards e foco do perfil | Grid usa `minmax(0, 1fr)` e bloco empilha abaixo de 768px | Bloqueado |
| Botões tocáveis | CTA do foco tem mínimo de 44px no mobile | Bloqueado |
| Tabelas | Gate exige wrapper ou alternativa mobile | Bloqueado |
| Modais/drawers | Smoke verifica início oculto, ARIA, abertura e fechamento | Bloqueado |
| Topbar/sidebar/toasts | Contratos automatizados permanecem ativos | Bloqueado |

Rotas P0/P1 devem ser fotografadas pelo runner v177 antes da homologação; nenhum screenshot foi fabricado neste ambiente.
