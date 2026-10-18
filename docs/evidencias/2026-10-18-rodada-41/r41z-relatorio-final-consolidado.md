# R41 · r41z — Relatório final consolidado (escrito retroativamente na R46)

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R40 (`8ce28b9`, suíte 1569).

## Escopo da rodada

Roadmap R41: "E9 SaaS: onboarding / primeira experiência".

## Commits

| Commit | Conteúdo |
|---|---|
| `b9755d2` | r41b1: 4× `CriterioDescricoes` reescritas em linguagem simples ("da sua instituição", com acento, UTF-8) + banner de implantação no Meu Dia (best-effort) + fato `R41OnboardingPrimeiraExperienciaContratoTests` (3 fatos) + r41a0 |
| (docs) | este r41z, retroativo |

## O que entrou

- **Linguagem simples**: os textos de critérios que ainda falavam a língua de sistema foram
  reescritos para a língua da instituição (4 ocorrências, verificadas por contrato com acento).
- **Banner Meu Dia** (`ProductivityPageViewModel.Onboarding` / `OnboardingCarregado`):
  `ProductivityWebService.GetOnboardingStatusAsync(token, ct)` retorna `(bool Ok, OnboardingStatusWebModel? Status)`;
  só o admin da conta vê (`AdministradorCliente`/`Administrador`, nunca médico/global);
  banner "Prepare sua conta…" ou "Sua implantação está X% pronta" + link `/Onboarding`, oculto se FINALIZADO.
- **Tipos**: estado entra como propriedade extra do VM reutilizando `OnboardingStatusWebModel`
  (tipo já fixado no dicionário BFF para `api/onboarding/status`) — respeita ADR-0003.

## Decisões fixadas (D-R41-1/2/3)

- Leitura best-effort: transporte/401/403 oculta o banner sem barulho; apenas 404 legítimo
  vira convite para iniciar a implantação.
- Aviso restrito ao admin da conta.
- Sem novo endpoint: reutilização do existente (contrato BFF).

## Gates e suíte

- Rebuild real → multiset 160/160 idêntico ao pristino, 0 erros, gate limpo.
- Suíte integral (base **1572** = 1569 + 3 fatos): **1572/1572, exit 0, zero flakes**.
