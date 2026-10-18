# R41 · r41a0 — E9 (rodada 2): onboarding de novo cliente + primeira experiência

Data: 2026-10-18 · Sessão: `opencode` · Base: R40 (`8ce28b9`, suíte 1569).

## O que o recon encontrou

A área de onboarding já é madura: checklist materializado do catálogo canônico adaptado ao contrato
do cliente (`tenant_onboarding_checklist` + `OnboardingJornadaService`), reavaliação contra dados
persistidos a cada leitura, concluir/pular/restaurar, ações comerciais reais, módulo `ONBOARDING`
sempre disponível para o admin da conta (membro de `TenantAdministrationModules`, R5-D9) e a página
de "Sem onboarding" como ponto de partida real com o botão de iniciar.

Lacunas fechadas nesta rodada:

| # | Lacuna | Correção |
|---|---|---|
| A | As descrições canônicas dos critérios da jornada exibiam "tenant" em 4 pontos visíveis ao admin da instituição (`PRIMEIRO_PLANTAO_CRIADO`, `ESCALA_PREENCHIDA`, `PRIMEIRO_PACIENTE_CADASTRADO`, `PRIMEIRA_TRIAGEM_REALIZADA`) | Reescritas para "da sua instituição" / "na sua instituição" em `CriterioDescricoes` (`OnboardingJornadaService.cs`) — sem pin em teste antigo |
| B | A primeira tela do admin da conta (**MeuDia**) não apontava para o checklist de implantação, e `/Onboarding` não tem atalho no menu lateral — uma conta recém-provisionada podia nunca descobrir a jornada | Banner no MeuDia para `ADMINISTRADOR_CLIENTE`/`ADMINISTRADOR`: se a implantação ainda não existe (404) convida a ver o passo a passo; se está em andamento mostra "X% pronta" + próxima ação + "Continuar de onde parou"; se FINALIZADO some |

## Decisões

- **D-R41-1**: a leitura do status é **best-effort** — transporte/401/403 oculta o aviso e a página
  segue normal (o Meu Dia é a fila pessoal de todos os dias; a jornada não pode derrubar o dia).
  Só um 404 legítimo (implantação não iniciada) vira convite; erro técnico não vira convite fake.
- **D-R41-2**: o aviso aparece apenas para quem administra a conta (admin da instituição). Médica vê
  sua fila; a visão global já cai no Command Center. Não se polui público que não decide a implantação.
- **D-R41-3**: o estado chega ao model do Meu Dia como propriedade extra do VM
  (`Onboarding`/`OnboardingCarregado`) com o tipo já fixado no dicionário BFF
  (`OnboardingStatusWebModel`, mesmo tipo da rota `api/onboarding/status` — ADR-0003). O contrato
  espelho R8B11 monitora a direção Api→Web, então campos extras de estado de view no VM são legítimos
  (comportamento declarado no próprio contrato).
- O menu lateral continua sem item fixo de Onboarding: enquanto não finalizado, o banner é o ponto de
  entrada; depois de finalizado, a página permanece acessível por URL (mesma política de rotas sem
  item de menu fixo, ex.: Minha Assinatura).

## Gates e suíte

- Rebuild real → multiset **160/160 idêntico ao pristino**, 0 erros.
- Classes: R41 + R7OB + OnboardingD9 + R8B11 + higiene.
- Suíte integral base **1572** = 1569 + 3 fatos R41.
