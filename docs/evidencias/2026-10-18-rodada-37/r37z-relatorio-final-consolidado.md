# R37 · r37z — Relatório final consolidado

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R36 (`94755e8`, suíte 1559).

## Escopo da rodada

Epic **E3** da diretriz (r24a1) — **lote 3 de 3 (FINAL)**: instruções de uso por página nas últimas 8
páginas do inventário (cadastros organizacionais + gestão). **Fecha o epic E3.**

## Commits

| Commit | Conteúdo |
|---|---|
| `befb5b7` | r37b1: 8 guias "Guia da tela" (`Cadastros`, `Cargos`, `Colaboradores`, `Contratos`, `Departamentos`, `Index`, `Gestao\Dashboard`, `Gestao\Monitor`) + renome renderizado "Cadastros Mestre" → "Cadastros Organizacionais" (h1 + breadcrumb, consistente com o ViewData Title já existente) + fato `R37InstrucoesPorPaginaLote3ContratoTests` (estrutura ×8 + anti-título-antigo) + r37a0 |

## Fidedignidade dos guias

Redigidos contra as regras reais das telas: inativar departamento/cargo **preserva** histórico (nada apaga);
somente colaborador **ativo** pode receber contrato; encerrar contrato preserva registro para auditoria;
o Monitor de Integrações **só observa** (ações ficam nas telas próprias); o Dashboard de gestão filtra por
período e exporta CSV.

## Gates e suíte

- Rebuild real → multiset **160/160 idêntico ao pristino**, 0 erros.
- Classes dependentes: **15/15 verde** (R37 + R36 + R35 + R29 + RepositoryHygiene).
- Suíte integral (base **1561** = 1559 + 2 fatos): **1560 aprovados + 1 falha triada** —
  `ProvisionamentoB5Tests.P3_CorridaMesmoCnpj_ExatamenteUmVence_Outro409`, mesma família já documentada
  na R35; protocolo aplicado: **isolate 5× verde** (classe inteira, 11 testes por execução).
  Sem relação com esta rodada (nem UI, nem BFF).

## Fechamento do epic E3

As **29 páginas Adm360** do inventário do r35a0 estão cobertas: 7 (lote 1) + 12 (lote 2) + 8 (lote 3)
com guias novos; exclusões documentadas: `OrcamentoImprimir`, `ValeImprimir` (layouts de impressão) e
`_AlertasAcao` (parcial reutilizável). Portal Saúde 360 continuou com seus blocos FormHelp; AdminSaas
fora de escopo por design (D-R24-3).

## Estado de casa

- Apps PARADOS, portas livres. Rodada 37 fechada neste push.
- Recon E2 (R38) **já iniciado e documentado no r38a0**: triagem de 161 ocorrências de "tenant" em ~95
  views fora de AdminSaas separadas por audiência — instituição (reescrever), super-admin (keep),
  marketing (whitelist) e código não-renderizado (keep). Ajuste de público descoberto no meio do recon:
  `Usuarios`, `WhiteLabel`, `Perfis` e `Permissoes` aceitam `ADMINISTRADOR_CLIENTE` → o admin da
  instituição **vê** essas telas, então "tenant" nelas entra no escopo de reescrita (r38b1/b2).
