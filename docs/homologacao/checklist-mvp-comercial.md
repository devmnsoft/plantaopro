# Checklist MVP comercial homologável — PlantãoPro

## Objetivo
Consolidar a validação final do PlantãoPro como MVP SaaS multiempresa pronto para homologação comercial, demonstrando operação médica ponta a ponta, faturamento SaaS básico, suporte, API Mobile, segurança por perfil, auditoria, observabilidade e documentação operacional.

## Pré-condições técnicas
- [ ] API inicia sem erro e responde `/api/health`.
- [ ] Web inicia sem erro e abre a tela de login.
- [ ] Swagger carrega os endpoints de API, incluindo a tag `Mobile`.
- [ ] Login Web com Cookie Authentication funciona para perfis de demonstração.
- [ ] Login API/Mobile com JWT funciona e endpoint sem token retorna `401` amigável.
- [ ] Banco PostgreSQL está com schema `plantaopro` e scripts incrementais aplicados.
- [ ] Build API e Web executado antes da entrega.
- [ ] Varredura obrigatória sem `@page`, `asp-page`, `href="#"`, `alert()`, `confirm()` ou collection expressions incompatíveis.
- [ ] Nenhum artefato `bin`, `obj`, `.vs`, `.dll`, `.exe`, `.pdb`, `.zip`, `.apk` ou `.aab` versionado.

## Fluxo operacional médico
- [ ] Criar cliente, hospital, especialidade e médico de homologação.
- [ ] Criar plantão em `rascunho` com data final maior que inicial, valor >= 0 e vagas > 0.
- [ ] Publicar plantão somente com hospital, especialidade e cliente ativos.
- [ ] Médico visualiza somente plantões permitidos do próprio cliente.
- [ ] Médico solicita plantão sem duplicidade e sem conflito crítico.
- [ ] Coordenação confirma escala e vagas disponíveis são reduzidas sem ficarem negativas.
- [ ] Coordenação marca escala como realizada.
- [ ] Financeiro gera pagamento apenas para escala realizada e não gera duplicado.
- [ ] Financeiro confirma pagamento com valor, data e forma.
- [ ] Médico visualiza pagamento confirmado, notificação e histórico relacionado.
- [ ] Dashboard, Central de Escala, Agenda e Relatório Financeiro refletem o fluxo.

## Fluxo SaaS básico
- [ ] Admin global cria cliente, plano e assinatura ativa.
- [ ] Cliente possui no máximo uma assinatura ativa.
- [ ] Plano inativo não permite nova assinatura.
- [ ] Limites de médicos, hospitais e plantões/mês bloqueiam excessos.
- [ ] Plano sem mobile bloqueia API Mobile com `403` amigável.
- [ ] Plano sem BI bloqueia BI.
- [ ] Fatura SaaS é gerada, paga com valor/data/forma e auditada.
- [ ] Cancelamento de fatura exige justificativa.
- [ ] Cliente inadimplente/suspenso bloqueia publicação de plantão.
- [ ] Reativação libera operação e notifica administrador do cliente.

## Segurança e multiempresa
- [ ] `ADMINISTRADOR_GLOBAL` vê todos os clientes, auditoria global e observabilidade.
- [ ] `ADMINISTRADOR`, `COORDENACAO`, `OPERADOR` e `FINANCEIRO` veem apenas o cliente vinculado.
- [ ] `MEDICO` vê apenas agenda, convites, escalas, pagamentos, notificações e suporte próprios.
- [ ] `HOSPITAL` vê apenas unidade/hospital autorizado.
- [ ] Acesso negado não depende apenas de esconder botão: controller/API também bloqueiam.
- [ ] Exportações, relatórios e downloads respeitam `cliente_id` ou escopo hospital/médico.
- [ ] Tentativas de acesso negado são registradas em auditoria.

## UX, suporte e observabilidade
- [ ] Telas críticas possuem cabeçalho, estados vazios, badges, filtros e tabelas/cards responsivos.
- [ ] Área do Médico funciona em layout mobile-first com botões grandes e cards legíveis.
- [ ] POSTs relevantes exibem toast e ações sensíveis usam modal de confirmação.
- [ ] Erros 403/404/500 são amigáveis e não exibem stack trace nem SQL bruto.
- [ ] Chamado de suporte pode ser criado, respondido/resolvido e auditado.
- [ ] Observabilidade mostra erros recentes, acessos negados, endpoints lentos e chamados críticos apenas para admin global.

## API Mobile MVP
- [ ] `POST /api/mobile/auth/login` autentica e não retorna senha, hash ou segredo.
- [ ] `GET /api/mobile/me` e `GET /api/mobile/dashboard` retornam payload leve.
- [ ] Plantões disponíveis, detalhes e solicitação estão paginados/limitados.
- [ ] Convites podem ser listados, aceitos e recusados com motivo.
- [ ] Minhas escalas, meus pagamentos e notificações retornam somente dados próprios.
- [ ] Perfil, disponibilidade e preferências estão expostos como contrato MVP.
- [ ] Suporte mobile lista, detalha e cria chamados próprios do usuário autenticado.

## Teste manual final
Registrar evidências do roteiro de 35 passos descrito em `docs/homologacao/roteiro-operacao-ponta-a-ponta.md`, incluindo prints, CSV exportado, logs sem dados sensíveis e resultado dos testes de acesso negado.

## Critérios de aprovação comercial
- [ ] Demo comercial executável sem intervenção técnica durante a reunião.
- [ ] Operação ponta a ponta concluída em ambiente de homologação.
- [ ] SaaS básico validado com plano, assinatura, fatura e bloqueio/liberação.
- [ ] API Mobile pronta para início do app nativo.
- [ ] Pendências conhecidas registradas com prioridade, dono e impacto.
