# R37 · r37a0 — E3 lote 3 (final): cadastros organizacionais + gestão → epic E3 completo

Data: 2026-10-18 · Sessão: `opencode` · Base: R36 (`94755e8`, suíte 1559).

## Escopo (plano herdado do r35a0)

As últimas 8 páginas do inventário sem bloco de instrução: cadastros organizacionais
(`Cadastros`, `Cargos`, `Colaboradores`, `Contratos`, `Departamentos`, `Index`) e gestão
(`Gestao\Dashboard`, `Gestao\Monitor`).

## Correções (r37b1)

**Guias "Guia da tela" (padrão da casa, 4 seções) em:** as 8 páginas acima. Redação fiel às regras
reais verificadas nas views/controllers: inativar departamento/cargo preserva histórico (nada apaga);
somente colaborador ativo pode receber contrato; encerrar contrato preserva registro para auditoria;
o Monitor só observa (ações ficam nas telas próprias); o Dashboard exporta CSV e filtra por período.

**Renome renderizado:** "Cadastros Mestre" → "Cadastros Organizacionais" (h1 + breadcrumb) —
linguagem simples + consistência com o `ViewData["Title"]` já existente na página. Verificado: nenhum
teste fixa o rótulo antigo (o único "Mestre" nos testes é nome de método do R8B15 sobre a permissão
"Mestre Operacional", outro conceito).

## Fato permanente

`R37InstrucoesPorPaginaLote3ContratoTests` (2 fatos): estrutura do guia nas 8 views (âncora + seções
1/2/4 invariantes) e ausência definitiva do título antigo na página de Cadastros.

## Fechamento do epic E3

Com R37 todas as **29 páginas Adm360** do inventário do r35a0 têm instrução de uso por página
(7 + 12 + 8 = 27 páginas com guia novo + as exclusões documentadas: 2 layouts de impressão e 1 parcial
reutilizável). O portal Saúde 360 já vinha coberto (blocos FormHelp da família R7); AdminSaas segue
fora de escopo (cockpit de operador MNSOFT, D-R24-3). **Epic E3 FECHADO.**

## Próximos epics da fila

R38–R39 E2 (acesso isolado por CPF/CNPJ+email/senha; tenant visível só para super-admin — AdminSaas é
já a zona super-admin, decision D-R24-3; revisão final do vocabulário "tenant" no E2) e R40+ E9
(evolução SaaS + consolidação dos flakes de carga num snapshot único).
