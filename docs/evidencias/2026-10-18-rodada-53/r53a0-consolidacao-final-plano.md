# R53 · r53a0 — Consolidação final: documentação de design + relatório da diretriz

Data: 2026-10-18 · Sessão: `opencode` · Base: R52 (`07604ab`, suíte 1605, smoke e2e verde).

## Objetivo

Fechamento do roadmap de 30 sprints (R24→R53) do r24a1: **documentação de design consolidada** e
**relatório da diretriz com antes/depois por epic** — o artefato que registra, de uma vez, para onde
o produto foi em design, linguagem, acessibilidade e acesso durante a diretriz.

## Entregas (documento → arquivo)

1. **Documentação de design consolidada** — `docs/ux/design-system-consolidado.md` (canônico, vivo):
   cadeia CSS versionada e ordem canônica dos layouts; tokens e contraste medido; componentes
   (guia da tela, jornada guiada, page header, estados); acessibilidade mínima (R49) e pendências;
   régua de formulários (E5), listas nome×id (E6), estados (R44), navegação (R43/R46); linguagem
   simples, tom de voz e glossário canônico (E1/E8/R50); acesso e isolamento por perfil/tenant (E2);
   ajuda central por área (R51); linha de base de performance (R52); índice de contratos que travam
   cada regra.
2. **Relatório da diretriz — antes/depois por epic** — `docs/evidencias/2026-10-18-rodada-53/
   r53b1-diretriz-relatorio-antes-depois-por-epic.md`: para cada epic E0–E9 (com a ressalva honesta
   sobre E7), o estado antes (R24/R25), o que cada rodada entregou, os contratos permanentes que
   travam o resultado e as pendências remanescentes. Inclui a auditoria de tom executada nesta
   rodada (exclamações em copy) e o mapa das 4 diretrizes do usuário para epics/evidências.

## Método

- Fontes: relatórios finais versionados R24–R52 (`r{N}z-relatorio-final-consolidado.md`), inventário
  mestre R25 (`r25a0`), contratos de teste citados neles, estado atual dos layouts/CSS/views no repo
  (recon desta rodada), smoke e2e R52.
- Auditoria de tom nova (não existe contrato para ela ainda): contagem de `!` em strings literais de
  Views e em `<value>` dos 4 resx (script local `%TEMP%\opencode\r53_toneaudit*.ps1`); resultado entra
  no relatório como inventário de pendência, não como mudança.

## Gates

- Sem mudança de código-fonte nesta rodada (só docs): build/gate/suíte permanecem no estado verde da
  R52 (exit 0, gate 160/160, suíte 1605/1605).
- Byte-scan dos novos documentos (corrupção de canal `plantaopro`↔`plantaouro`) — `bad=0` obrigatório.
- Commits `docs (rodada 53 r53b1)` (design + relatório) e `docs (rodada 53 r53z)` (este plano +
  relatório final); push ao fim.
