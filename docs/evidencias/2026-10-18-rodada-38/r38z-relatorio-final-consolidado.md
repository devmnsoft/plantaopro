# R38 · r38z — Relatório final consolidado

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R37 (`befb5b7`, suíte 1561).

## Escopo da rodada

Epic **E2** da diretriz (r24a1) — acesso/tenant. Nesta rodada: a palavra "tenant" saiu de todas as
telas de uso institucional (fatias b1+b2) e o modelo de acesso foi auditado com decisões registradas
(r38a1).

## Commits

| Commit | Conteúdo |
|---|---|
| `c311ba5` | r38a0: triagem completa de 161 ocorrências de "tenant" em ~95 views fora de AdminSaas, por audiência (instituição / super-admin / marketing / código) |
| `992b6d3` | r38b1: jornada clínica 20 views (mesmo subtítulo), Central de Segurança 11 views + capa (sem endpoint cru `/api/seguranca`, KPI "Sem instituição") + fato por grupo |
| `ae65b85` | r38b2: partials compartilhados, Ajuda ×3, Usuarios/Perfis/Permissoes, WhiteLabel ×5, Parametrizacoes ×5, Configuracoes, Relatorios (chip "Sua instituição"), ApiKeys, Financeiro, AccessDenied, Cadastro\Sucesso (sem "onboarding"), HospitalArea, Adm360 DocumentosXml (sem DOCTYPE/DTD/XXE), Triagens, ContratoDetalhes + mensagens de API renderizadas na tela (triagem/cotações, sem "tenant atual") + fato R4B7 ajustado + fato b2 (par view/trecho × ~50) |
| (docs) | r38a1: auditoria do modelo de acesso + decisões D-R38-1/D-R38-2 + inventário final |

## Decisões

- **D-R38-1**: a instituição opera por **CNPJ** (provisionamento, fiscal, convites) e a pessoa por
  **email global único + senha** com vínculo explícito por cliente — equivalência funcional do
  isolamento "CPF/CNPJ + email/senha". CPF segue como documento opcional de cadastros. Login por
  CPF/CNPJ vira epic próprio (migração + desambiguação) se houver demanda registrada.
- **D-R38-2**: "tenant"/"cliente" segue como linguagem de operador nas zonas `isGlobal`
  (`Clientes`, `CommandCenter`, `MeuDia` global, `AdminSaas`) — reconfirma D-R24-3.

## Resultado da varredura

161 ocorrências → **70**, integralmente classificadas como keep: código não renderizado (variáveis,
`Model.Tenant*`, claims, classes CSS, nomes de chaves UiText), zonas super-admin/`isGlobal`, nota
técnica de analista (`DashboardsPremium\Perfil`) e marketing (`CommercialDemoWeb`, `SaasComercialPage`).
Os valores pt-BR das 4 chaves de login/navegação já vinham limpos da R34 ("instituição",
"Organização"). Guias RelFin L3135–3216 (4 culturas) seguem keep (fiéis ao SQL, whitelist acumulada).

## Gates e suíte

- Rebuild real → multiset **160/160 idêntico ao pristino**, 0 erros (verificado em b1 e b2).
- Classes dependentes: 22/22 (b1) e 46/46 (b2) verdes, incluindo R38, R29, R34, R7DF6, R4B7, higiene.
- Suíte integral (base **1565** = 1561 + 4 fatos R38): **1565/1565, exit 0, zero flakes**.

## Estado de casa

- Apps PARADOS, portas livres. Rodada 38 fechada neste push.
- Fila: **R39 E2 parte 2** (tenant visível só p/ super-admin: revisão do backoffice AdminSaas,
  D-R24-3 revisitada, fato travando o Authorize) e depois **R40+ E9** (recon iniciado: limites de
  plano aplicados só na troca de plano — hipótese: uso-vs-limite visível + recusa amigável).
