# Checklist Beta Comercial Controlada

## Objetivo
Consolidar o PlantãoPro como **Beta Comercial Controlada**: demonstrável para diretoria, homologável com um cliente real, operável com acompanhamento assistido e pronto para iniciar o app mobile.

## Usuários de teste
| Perfil | Usuário sugerido | Validação principal |
| --- | --- | --- |
| ADMINISTRADOR_GLOBAL | admin.global@plantaopro.local | Visão executiva, clientes, planos, assinaturas, faturamento SaaS, auditoria e observabilidade. |
| ADMINISTRADOR | admin.cliente@hospital.local | Operação do próprio cliente, usuários, cadastros e relatórios. |
| COORDENACAO | coordenacao@hospital.local | Plantões, escalas, convites, Central de Escala, agenda e alertas. |
| FINANCEIRO | financeiro@hospital.local | Pagamentos médicos, faturas SaaS, contestação e confirmação. |
| MEDICO | medico@hospital.local | Minha Agenda, plantões disponíveis, convites, pagamentos, notificações e suporte. |
| HOSPITAL | hospital@hospital.local | Plantões e escalas da unidade/hospital autorizado. |

## Checklist técnico obrigatório
- [ ] `git status` sem alterações inesperadas antes do início.
- [ ] Sem binários versionados: dll, exe, pdb, cache, zip, apk, aab, db, sqlite, bin, obj ou .vs.
- [ ] `dotnet clean` API/Web executado no ambiente com SDK instalado.
- [ ] `dotnet build` API/Web verde.
- [ ] Varredura sem `@page`, `asp-page`, `@model dynamic`, `href="#"`, `alert()`, `confirm()` ou `ADD CONSTRAINT IF NOT EXISTS`.
- [ ] Swagger abre e autentica com JWT.
- [ ] `/api/health` retorna saudável.
- [ ] Login Web mantém Cookie Authentication e redirecionamento pós-login.

## Checklist operacional médico
- [ ] Criar cliente, hospital, especialidade, médico e plantão em RASCUNHO.
- [ ] Publicar plantão somente com hospital/especialidade ativos e cliente apto.
- [ ] Médico visualiza somente seus dados e solicita plantão sem duplicidade/conflito.
- [ ] Coordenação confirma escala e reduz vaga disponível sem ficar negativa.
- [ ] Coordenação marca escala como REALIZADA.
- [ ] Financeiro gera pagamento apenas para escala REALIZADA.
- [ ] Financeiro confirma pagamento com valor, data e forma.
- [ ] Médico visualiza pagamento confirmado e notificação.
- [ ] Dashboard, Central de Escala, Agenda e relatório financeiro refletem o fluxo.
- [ ] Auditoria registra solicitação, confirmação, realização e pagamento.

## Checklist SaaS básico
- [ ] Criar plano ativo com limites comerciais.
- [ ] Criar assinatura ativa para cliente, com no máximo uma assinatura ativa por cliente.
- [ ] Bloquear nova assinatura em plano inativo.
- [ ] Bloquear cadastro quando limite de médicos/hospitais for atingido.
- [ ] Bloquear publicação quando limite mensal de plantões for atingido.
- [ ] Bloquear operação de cliente SUSPENSO ou CANCELADO.
- [ ] Bloquear API Mobile quando plano não permitir mobile.
- [ ] Bloquear BI quando plano não permitir BI.
- [ ] Gerar fatura SaaS, marcar como paga com valor/data/forma, cancelar/contestar com justificativa.
- [ ] Inadimplência, suspensão, reativação e notificação ao admin do cliente validadas.

## Checklist UX por perfil
- [ ] Coordenação: Central de Escala com KPIs, pendências, badges, filtros, EmptyState, toast e modais críticos.
- [ ] Médico: Minha Agenda mobile-first com próximo plantão, convites, escalas, pagamentos, feed de notificações e botões grandes.
- [ ] Financeiro: cards de pendente/pago/atrasado/contestado, filtros, timeline, modais e EmptyState.
- [ ] Admin global: visão executiva com clientes ativos/teste/suspensos/risco, faturas vencidas, receita prevista, chamados críticos e erros.
- [ ] Hospital: acesso restrito aos dados da unidade/hospital.

## Checklist segurança, auditoria e observabilidade
- [ ] Admin global vê todos os clientes; usuários comuns respeitam `cliente_id`; médico vê apenas dados próprios.
- [ ] Acessos negados retornam 403 amigável e registram auditoria.
- [ ] Erros não expõem stack trace, SQL, senha, hash, token ou segredo.
- [ ] Exportações/downloads registram auditoria.
- [ ] Observabilidade mostra erros do dia, endpoints lentos, últimos erros, últimos logins, acessos negados, faturas vencidas e chamados críticos.

## Critérios de aprovação
- Build verde no ambiente de homologação.
- Fluxo operacional ponta a ponta concluído sem exceção técnica.
- Fluxo SaaS básico concluído com bloqueios e auditoria.
- API Mobile MVP validada com JWT, paginação, payload leve, 401 sem token e 403 para plano sem mobile.
- Documentação de demo, deploy, mobile, suporte, Customer Success e go-live revisada.

## Pendências conhecidas aceitas para Beta
- Push notification real, publicação nas lojas e integrações externas ficam para pós-Sprint Zero mobile.
- Carga de performance deve ser repetida com dados reais do cliente piloto.
- Regras fiscais/contábeis definitivas dependem de validação do cliente e contador.

## Operação assistida com cliente real

- [ ] Cliente aparece no painel de operação assistida.
- [ ] Cards exibem status, risco, responsável, progresso e ocorrências críticas.
- [ ] Checklist padrão de implantação disponível para todos os clientes em homologação.
- [ ] Conclusão de item usa modal de confirmação, registra auditoria e atualiza progresso.
- [ ] Reabertura usa modal de confirmação, exige justificativa e registra auditoria.
- [ ] Registro de ocorrência usa modal de confirmação; ocorrência crítica gera alerta operacional.
- [ ] Resolução de ocorrência exige descrição da solução, usa modal de confirmação e registra auditoria.
- [ ] Treinamentos por perfil registrados com responsável, participantes e observações.
- [ ] Nenhuma ocorrência crítica aberta antes do go-live beta.

## Higiene de branch e produto

- [ ] Branch atual confirmada antes de iniciar alterações.
- [ ] Branch indevida de outro produto não foi mergeada novamente.
- [ ] Varredura sem termos, módulos, domínios de negócio e portas pertencentes ao produto incorreto.
- [ ] App mobile legítimo mantido somente em `mobile/PlantaoPro.App`.
- [ ] Nenhum diretório raiz de app externo, ignorado ou não versionado, permanece no workspace de release.

## Roteiro manual final da Beta Controlada

Execute em homologação, registrando evidência de tela, usuário, horário e resultado esperado em cada etapa:

1. Confirmar ausência de resíduos de produto externo com a varredura de higiene do repositório.
2. Entrar como ADMINISTRADOR_GLOBAL e validar Dashboard executivo.
3. Criar cliente real ou selecionar cliente piloto.
4. Criar plano ativo com limites comerciais.
5. Criar assinatura ativa e validar que não existe segunda assinatura ativa para o mesmo cliente.
6. Abrir Operação Assistida e confirmar card do cliente com status, risco, responsável e progresso.
7. Concluir itens do checklist de implantação usando modal de confirmação.
8. Reabrir item com justificativa e confirmar toast/auditoria.
9. Registrar ocorrência crítica e confirmar alerta operacional.
10. Resolver ocorrência com descrição da solução.
11. Registrar treinamento de ADMINISTRADOR, COORDENACAO, FINANCEIRO, MEDICO e HOSPITAL.
12. Criar hospital/unidade vinculada ao cliente.
13. Criar especialidade ativa.
14. Criar médico com vínculo correto ao cliente e especialidade.
15. Criar plantão em rascunho com data futura, vagas e valor.
16. Publicar plantão e confirmar bloqueios de cliente/plano quando aplicáveis.
17. Entrar como MEDICO e abrir área mobile-first Minha Agenda.
18. Visualizar plantões disponíveis próprios/elegíveis.
19. Solicitar plantão e confirmar prevenção de duplicidade/conflito.
20. Entrar como COORDENACAO e confirmar pendência na Central de Escala.
21. Confirmar escala e validar redução de vaga disponível.
22. Marcar escala como REALIZADA.
23. Entrar como FINANCEIRO e gerar pagamento médico.
24. Confirmar pagamento com valor, data e forma.
25. Entrar como MEDICO e validar pagamento confirmado, notificação e agenda.
26. Abrir comunicação/suporte e registrar chamado.
27. Resolver chamado e conferir Customer Success.
28. Gerar fatura SaaS mensal.
29. Marcar fatura como paga e validar inadimplência quando simulada.
30. Suspender e reativar cliente com justificativa, validando bloqueio/liberação operacional.
31. Exportar relatório CSV e conferir registro de auditoria.
32. Abrir Auditoria e validar ações críticas, acessos negados e exportações.
33. Abrir Observabilidade e validar erros do dia, endpoints lentos, últimos logins e acessos negados.
34. Abrir Swagger, testar `/api/health` e os endpoints da tag Mobile.
35. Testar API Mobile com plano permitido.
36. Testar API Mobile com plano bloqueado, quando aplicável, esperando 403 amigável.
37. Testar endpoints protegidos sem token, esperando 401 amigável.
38. Testar acesso negado entre clientes, médicos e hospitais.
39. Validar 404 amigável em rota inexistente Web.
40. Validar responsividade em largura de celular para Dashboard, Central de Escala, Médico e Financeiro.
41. Confirmar build verde no ambiente com SDK .NET instalado.
42. Confirmar `git status` limpo e PR sem arquivos binários/artefatos locais.
