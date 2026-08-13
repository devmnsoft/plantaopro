# v1.65 — diagnóstico dos prints reais

## Admin SaaS
- **Problema observado:** topo e KPIs cortados, conteúdo estreito e checklist comprimido.
- **Causa provável:** regras concorrentes do shell e ausência de uma composição principal/lateral estável na rota.
- **Arquivos afetados:** `_Layout.cshtml`, `v161-medical-experience.css` e `AdminSaas/Index.cshtml`.
- **Correção aplicada:** shell em grid com coluna de conteúdo flexível; conteúdo com largura interna controlada; `pp-admin-layout` com lateral de 320–420 px, queda para uma coluna abaixo de 1200 px e cards laterais com largura integral.
- **Como validar:** abrir `/AdminSaas/Index` em 1366×768 e 1024×768; confirmar que topbar, hero e KPIs não se interceptam e que não existe rolagem horizontal.

## Landing pública
- **Problema observado:** hero dominante, cards altos e ritmo vertical excessivo.
- **Causa provável:** escala tipográfica e alturas mínimas maiores que o conteúdo real.
- **Arquivos afetados:** `CommercialDemoWeb/Landing.cshtml` e `v161-medical-experience.css`.
- **Correção aplicada:** hero responsivo limitado, cards identificados por `pp-public-card` e altura mínima reduzida para 180 px, preservando ações alinhadas ao rodapé.
- **Como validar:** abrir `/` nos viewports 390×844, 1366×768 e 1920×1080.

## Cadastro self-service
- **Problema observado:** campos desalinhados, labels coladas, divisórias brutas e formulário concentrado à esquerda.
- **Causa provável:** view baseada diretamente no grid/utilitários genéricos, sem a composição do design system.
- **Arquivos afetados:** `Cadastro/Cadastro.cshtml` e `v161-medical-experience.css`.
- **Correção aplicada:** onboarding em cinco etapas, resumo lateral, cards semânticos, grid de duas colunas, campos de largura integral, ajuda/erro associados, consentimentos e ações responsivas.
- **Como validar:** abrir `/cadastro/empresa`, redimensionar de 1440 px até 360 px e provocar validação vazia.

## Modal de confirmação
- **Problema observado:** confirmação renderizada como card no fim do fluxo.
- **Causa provável:** dependência de classes Bootstrap para posicionar/ocultar o modal, com partial fora do portal.
- **Arquivos afetados:** `_ConfirmModal.cshtml`, `_OverlayPortal.cshtml`, `overlays.css`, `_Layout.cshtml` e `plantaopro-ui.js`.
- **Correção aplicada:** modal nasce com `hidden` dentro de um portal fixo z-index 9999; abertura/fechamento independem do Bootstrap, incluem backdrop, Escape, bloqueio de scroll e retorno de foco.
- **Como validar:** acionar qualquer elemento `data-confirm`, fechar com Escape e verificar que o foco retorna ao acionador.
