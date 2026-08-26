# Evidência funcional — v2.08.0 Área do Profissional mobile-first

## Decisões de implementação

- A base recebida já contém a evolução v2.07.0 (`NotificationOperationsV2070ContractTests`, central de notificações, preferências e alertas). Ela foi preservada e reutilizada no resumo de notificações recentes; não foi criado um segundo mecanismo.
- O produto já possuía `MedicoAreaService`, APIs mobile de convite e a automação de disponibilidade. A v2.08.0 consolida essas capacidades, em vez de duplicá-las, no portal `MinhaAgenda` e em um serviço especializado para dashboard e presença.
- As tabelas `medico_checkins` e `medico_disponibilidade_regras` já existiam no esquema. Check-in/check-out usa a estrutura local existente; latitude/longitude continuam opcionais. Nenhuma integração de localização externa foi inventada.
- O checkout financeiro existente continua sendo a fonte dos pagamentos. A nova visão é individual, visual e somente leitura.
- O checkout de código não possuía remoto Git configurado. Assim, a atualização de `main` não pôde consultar a origem; a branch foi criada sobre `aed648c`, merge da rodada v2.07.0 já presente no repositório local.

## Entrega

- **Meu Dia:** saudação, confirmações, próximos plantões, notificações, resumo financeiro, convites e atalhos.
- **Plantões:** oportunidades em cards responsivos, indicação de conflito/solicitação existente e ação contextual sem IDs digitados.
- **Disponibilidade:** atalho para o fluxo real já existente, que oferece disponibilidade, indisponibilidades e preferências.
- **Presença:** lista apenas escalas do profissional e tenant autenticados; bloqueia duplicidade, checkout sem check-in e acesso a escala alheia; registra auditoria.
- **Financeiro:** cards totalizadores e lista compacta de valores previstos/pagos por status.
- **Mobile e acessibilidade:** grid adaptativo, CTAs em largura total em telas pequenas, foco visível, estados vazio/erro e ausência de tabelas largas.

## Segurança

Consultas críticas combinam `usuario_id`, `medico_id` e `cliente_id`. A gravação de presença revalida o vínculo da escala no servidor e a unicidade `(tenant_id, escala_id)` torna check-in idempotente. A UI nunca envia um médico/tenant escolhido pelo usuário.

## Validação esperada

Os testes `ProfessionalPortalV2080ContractTests` cobrem contratos de dashboard, pagamentos, presença, isolamento e UX. Os comandos obrigatórios da rodada devem ser executados no pipeline com .NET SDK e PostgreSQL disponíveis. Neste ambiente, o executável `dotnet` não está instalado; as verificações Python, busca de padrões e `git diff --check` permanecem executáveis localmente.
