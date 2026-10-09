# R5-C7/C8 — Onboarding adaptado ao contrato, com conclusao derivada de dados

Data: 2026-10-09 (rodada 5)

## Objetivo
Acabar com o onboarding de lista generica fixa concluida por clique (sucesso ficticio). As etapas
passam a vir de um catalogo canonico com objetivo/responsavel/pre-requisito/criterio verificavel;
etapas de modulo so existem quando o contrato do modulo esta efetivo no tenant (mesmo predicado de
vigencia dos claims de login, B4/B6); a conclusao e derivada de dados persistidos e reversivel.

## O que foi implementado
- Catalogo canonico `plantaopro.onboarding_etapas_catalogo` (migration aditiva/idempotente
  `2026_10_v2333_c7_onboarding_contrato.sql`): nucleo geral (dados da empresa, convidar equipe,
  primeiro aceite, identidade opcional, revisao) + etapas por modulo (PLANTOES x3, SAUDE360 x3,
  ADM360 x1). Etapa so e materializada com contrato efetivo do modulo.
- `OnboardingJornadaService` (API): materializador idempotente por codigo (indice unico arbitra),
  avaliador que le os DADOS do tenant (clientes, convites de equipe, plantoes, escalas, unidades,
  pacientes, triagens, operacoes ADM360, white label) e persiste cada transicao com origem
  (AUTOMATICO/MANUAL) e evidencia legivel. Dado sumido => etapa volta a pendente; metricas do
  master (status/progresso/proxima_acao) sao SEMPRE recalculadas a partir das etapas efetivas.
- API `Controllers/OnboardingController.cs` fina: status/iniciar/checklist/reavaliar/sincronizar/
  concluir/pular/restaurar/reiniciar/proxima-acao + auditoria. Concluir por clique sem criterio
  atendido devolve 409 honesto com a evidencia; pular so vale para etapa opcional (justificativa
  persistida); obrigatoria nunca pula.
- Kernel B5 (`CriarOnboardingAsync`) passa a materializar pelo catalogo (nada de 11 fixas).
- Web BFF (`OnboardingController` tenant-aware) + view com checklist real agrupado (geral e por
  modulo), progresso, evidencia e acoes com antiforgery; contexto global sem tenant mostra nota
  honesta em vez de lista inventada; linha legada ETAPA_xx segue visivel como historico sem
  contaminar metricas.
- Correcao encontrada pelos testes: `IniciarAsync`/`SincronizarAsync` agora resolvem o id do mestre
  (linha `tenant_onboarding_checklist.onboarding_id` e NOT NULL) com insert `returning id`.

## Como foi verificado
- Testes de integracao `backend/PlantaoPro.Tests/OnboardingC7Tests.cs` (banco real `plantaopro_test`,
  molde do B5): C1 so materializa o valido e e idempotente; C2 sincronizar acompanha contratos
  (entrada ATIVA, exclusao AGENDADO futuro, saida soft preservando historico e etapas concluidas);
  C3 conclusao automatica pela dado e reversao quando o dado some; C4 clique desonesto = 409 sem
  gravar nada; C5 pulo so opcional, persistido, restauravel; C6 FINALIZADO 100% so quando todas as
  obrigatorias estao atendidas em dados (perder dado reverte o master).
- Suíte completa: 1169/1169 verdes (0 falhas) apos todas as mudancas de codigo.
- Hosts reais (API 51976 / Web 52976): rota `/Onboarding/Index` renderizada para acesso global
  auditado (MNSOFT) e porta honesta para o demo gestor legado (gate de assinatura canonica,
  consistencia com o diagnostico da fase GATE; operacionalizacao do demo fica em D9-D12).

## Pendencias deliberadas (sem inventar nada)
- Banco principal `plantaopro` ainda nao recebeu v2332/v2333 (upgrade externo planejado; a tentativa
  desta sessao foi revertida de forma limpa porque o banco principal esta em schema pre-B5).
- Passo de IA no onboarding depende de codigo canonico de modulo IA (catalogo ganha a linha quando
  o modulo existir).
- Demo Santa Casa precisa de assinatura canonica (fluxo B5) para operar o onboarding de verdade (D9).
