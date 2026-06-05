# Checklist Beta Homologável Final — PlantãoPro

## Objetivo

Este checklist consolida o aceite final da **Beta Homologável Final** do PlantãoPro para demonstração profissional, homologação com cliente real, operação assistida controlada, preparação do app mobile e posterior deploy em homologação/produção controlada.

## Gate 0 — Higiene de branch e produto

- [ ] Branch atual confirmada com `git branch --show-current` e diferente da branch indevida de produto externo.
- [ ] Backup local criado antes da rodada com `git branch backup/antes-beta-homologavel-final-plantaopro`.
- [ ] `git status --short` revisado antes e depois das alterações.
- [ ] `git log --oneline --decorate -10` revisado para confirmar histórico recente esperado.
- [ ] Varredura de termos externos executada com `rg` no repositório completo.
- [ ] Nenhum módulo, rota, controller, script, porta, domínio de negócio ou diretório raiz externo permanece no PlantãoPro.
- [ ] Referências a app mobile permanecem somente como artefatos legítimos do PlantãoPro em `app móvel do PlantãoPro` e documentação mobile oficial.

## Gate 1 — Build, smoke test e UX global

- [ ] `dotnet clean backend/PlantaoPro.Api/PlantaoPro.Api.csproj` executado em ambiente com SDK .NET.
- [ ] `dotnet clean backend/PlantaoPro.Web/PlantaoPro.Web.csproj` executado em ambiente com SDK .NET.
- [ ] `dotnet build backend/PlantaoPro.Api/PlantaoPro.Api.csproj` verde.
- [ ] `dotnet build backend/PlantaoPro.Web/PlantaoPro.Web.csproj` verde.
- [ ] Swagger abre sem erro.
- [ ] `/api/health` retorna resposta saudável e tipada.
- [ ] Web abre `/Account/Login` sem loop ou redirecionamento para raiz da API.
- [ ] Login/logout ADMINISTRADOR_GLOBAL validado.
- [ ] Login/logout MEDICO validado.
- [ ] Dashboard abre sem erro de partial, layout, sidebar, topbar, toast ou modal.
- [ ] Páginas 403 e 404 amigáveis validadas.

## Gate 2 — Operação médica ponta a ponta

- [ ] ADMINISTRADOR_GLOBAL cria ou seleciona cliente piloto.
- [ ] Hospital/unidade ativo criado e vinculado ao cliente correto.
- [ ] Especialidade ativa criada.
- [ ] Médico ativo criado com vínculo de usuário, cliente e especialidade.
- [ ] Plantão nasce como RASCUNHO, com data final maior que data inicial, valor maior ou igual a zero e vagas maiores que zero.
- [ ] Publicação valida hospital ativo, especialidade ativa, assinatura e limites do plano quando aplicáveis.
- [ ] MEDICO visualiza somente plantões próprios/elegíveis e solicita plantão sem duplicidade ou conflito crítico.
- [ ] COORDENACAO confirma escala, reduz vaga disponível e impede vaga negativa.
- [ ] COORDENACAO marca escala como realizada.
- [ ] FINANCEIRO gera pagamento somente para escala realizada e impede duplicidade.
- [ ] FINANCEIRO confirma pagamento com valor, data e forma.
- [ ] MEDICO visualiza pagamento confirmado, notificação e agenda atualizada.
- [ ] Central de Escala, Agenda Operacional, Dashboard e Relatórios refletem o fluxo concluído.
- [ ] Ações críticas registram auditoria, geram toast e usam modal quando sensíveis.

## Gate 3 — SaaS, faturamento e operação assistida

- [ ] Cliente, plano e assinatura ativa criados com no máximo uma assinatura ativa por cliente.
- [ ] Uso do plano exibe limites de médicos, hospitais, plantões/mês, mobile e BI.
- [ ] Cliente suspenso ou cancelado não publica plantão.
- [ ] Plano inativo não gera nova assinatura.
- [ ] Fatura SaaS gerada, paga com valor/data/forma e auditada.
- [ ] Inadimplência simulada exibe cliente em risco e permite suspensão com justificativa.
- [ ] Reativação libera operação e registra auditoria/notificação.
- [ ] Operação Assistida exibe cards de clientes em implantação, progresso, checklist, ocorrências, treinamentos e timeline.
- [ ] Ocorrência crítica gera alerta operacional.
- [ ] Suporte permite criar, responder, resolver e cancelar chamado com timeline e notificação.
- [ ] Customer Success registra interação, saúde do cliente, risco e plano de ação simples.

## Gate 4 — Segurança, auditoria, observabilidade, relatórios e mobile

- [ ] ADMINISTRADOR_GLOBAL visualiza todos os clientes.
- [ ] ADMINISTRADOR, COORDENACAO, OPERADOR e FINANCEIRO visualizam somente o próprio cliente.
- [ ] MEDICO visualiza somente dados próprios.
- [ ] HOSPITAL visualiza somente hospital/unidade autorizada.
- [ ] Acesso negado é amigável e auditado.
- [ ] Logs não exibem senha, hash, token, segredo, SQL bruto ou payload sensível.
- [ ] Observabilidade cobre erros do dia, endpoints lentos, últimos erros, últimos logins, falhas API/Web, integrações, acessos negados, faturas vencidas, chamados críticos e ocorrências críticas.
- [ ] Relatórios têm filtros, cards resumo, tabela, paginação, EmptyState, exportação CSV, auditoria de exportação e respeito a `cliente_id`.
- [ ] API Mobile MVP expõe endpoints documentados, exige JWT, usa `ApiResponse<T>`, payload leve, paginação e 403 amigável quando o plano não permite mobile.
- [ ] App Expo lista plantões, escalas e pagamentos com IDs normalizados (`plantaoId`/`escalaId`/`pagamentoId` -> `id`) e fallback amigável quando endpoint estiver indisponível.
- [ ] Sprint Zero do app está documentada em `docs/mobile/sprint-zero-app.md`, `docs/mobile/arquitetura-app.md`, `docs/mobile/telas-mvp-app.md`, `docs/mobile/mobile-api-endpoints.md` e `docs/mobile/mobile-fluxos.md`.

## Incremento 2026-06-05 — AJAX seguro em ações críticas Web

- [x] Escalas com confirmação, recusa, conclusão e substituição protegidas por antiforgery, modal, AJAX e toast.
- [x] Plantões com publicação/cancelamento usando confirmação contextual e envio AJAX com fallback seguro.
- [x] Pagamentos com confirmação/cancelamento protegidos por antiforgery, modal, AJAX e feedback visual.
- [x] Solicitação médica de plantão com confirmação explícita, validação visual e toast.
- [x] Relatório incremental registrado em `docs/homologacao/relatorio-crud-ajax-seguro-2026-06-05.md`.

## Roteiro manual final obrigatório

1. Confirmar ausência de resíduos externos.
2. Login admin global.
3. Criar cliente.
4. Criar plano.
5. Criar assinatura.
6. Criar operação assistida.
7. Concluir checklist.
8. Registrar ocorrência.
9. Criar hospital.
10. Criar especialidade.
11. Criar médico.
12. Criar plantão.
13. Publicar plantão.
14. Login médico.
15. Solicitar plantão.
16. Login coordenação.
17. Confirmar escala.
18. Marcar escala realizada.
19. Login financeiro.
20. Gerar pagamento.
21. Confirmar pagamento.
22. Login médico.
23. Ver pagamento confirmado.
24. Ver notificação.
25. Abrir comunicação.
26. Abrir agenda.
27. Abrir Central de Escala.
28. Gerar fatura SaaS.
29. Marcar fatura paga.
30. Abrir chamado.
31. Resolver chamado.
32. Ver Customer Success.
33. Baixar relatório CSV.
34. Ver auditoria.
35. Ver observabilidade.
36. Abrir API mobile no Swagger.
37. Testar API mobile com plano permitido.
38. Testar API mobile com plano bloqueado, se aplicável.
39. Testar acesso negado.
40. Testar responsividade.
41. Confirmar build verde.
42. Confirmar PR limpo.

## Pendências reais aceitáveis para Beta

- Execução de `dotnet build`, `dotnet test` e smoke test Web/API deve ocorrer no CI ou homologação quando o SDK .NET estiver disponível.
- Validar em dispositivo/emulador que a lista financeira renderiza `valorPrevisto`/`valorPago` como `valor` e usa `pagamentoId` como chave estável.
- Evidências visuais do teste manual devem ser anexadas pelo responsável de homologação, com usuário, data, horário e resultado.
- Teste de carga com massa real, push notification real, publicação em lojas e integrações externas permanecem como escopo pós-Beta.
