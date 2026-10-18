# R43 · r43z — Relatório final consolidado (escrito retroativamente na R46)

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R42 (`6c5fe91`, suíte 1577).

## Escopo da rodada

Roadmap R43: "E8 navegação/menus: rótulos únicos, itens novos só sob permissão".

## Commits

| Commit | Conteúdo |
|---|---|
| `ed80cfe` | r43b1: resx ×4 + `Nav.CommandCenter` e `Nav.MinhaAssinatura`; `Onboarding.PageTitle` renomeado para "Implantação" (pt/en/es/fr); sidebar: literal "Command Center" → `@UiText.L("Nav.CommandCenter")` + 2 itens novos (WorkItem/Payment) só sob `Can("ONBOARDING")`/`Can("ASSINATURAS")` && `!isGlobalAdmin`; Title+h1 de `CommandCenter/Index` pela mesma chave + fato `R43NavigacaoMenusContratoTests` (3 fatos) + r43a0 |
| (docs) | este r43z, retroativo |

## Decisões fixadas (D-R43-1/2)

- **D-R43-1**: o rótulo do item Implantação reutiliza `Onboarding.PageTitle` — página e menu
  nunca divergem (trava por contrato, 4 culturas).
- **D-R43-2**: itens novos só quando `Can("ONBOARDING")`/`Can("ASSINATURAS")` && `!isGlobalAdmin`
  (`HasPermission` retorna true para tenant-admin nesses módulos core — `TenantAdministrationModules`).

## Pendência aberta (fechada na R46)

- Ícones duplicados no menu lateral (Coverage ×3, User ×4, Report ×3, Finance ×2…) — recon
  documentado aqui; deduplicação com mapa único por item + pares residuais declarados fechou
  no **r46b1** (ver `2026-10-18-rodada-46`).

## Gates e suíte

- Rebuild real → multiset 160/160 idêntico ao pristino, 0 erros, gate limpo.
- i18n: `R7DF6Admin360I18nContratoTests` intacto (chaves novas são `Nav.*`, fora do escopo Adm360 = 653).
- Suíte integral (base **1580** = 1577 + 3 fatos): **1580/1580, exit 0, zero flakes**.
