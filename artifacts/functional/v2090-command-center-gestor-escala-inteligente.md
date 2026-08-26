# PlantãoPro v2.09.0 — Command Center do Gestor e escala inteligente

## Entrega

- Command Center responsivo e protegido para Admin/Gestor/Administrador, com indicadores do tenant, filtros de período/status, estados de erro/vazio e cobertura em cards.
- Endpoint multi-tenant que consolida plantões, profissionais, financeiro e notificações sem executar DDL ou produzir dados simulados.
- Risco operacional determinístico (vagas, proximidade, confirmação, presença e ocorrências) e ranking explicável de profissionais.
- O ranking bloqueia conflito, indisponibilidade, bloqueio, perfil inativo ou não autorizado; especialidade, disponibilidade, confirmação, unidade preferencial e carga recente compõem a pontuação.

## Estrutura anterior confirmada

Foram reutilizados os módulos reais de plantões/escalas, financeiro, notificações, portal profissional, RBAC/tenant, white label e Design System. A Central de Escala existente permanece responsável por convite, substituição e transições auditadas; esta versão não duplica esses fluxos.

## Decisões técnicas

- Mantidos ASP.NET Core, PostgreSQL e Dapper.
- Consulta limitada a 200 plantões e período máximo de 93 dias.
- Tenant vem exclusivamente do token autenticado; não é aceito em formulário ou query string.
- O algoritmo fica no domínio, puro e testável, sem dependência externa.

## Limitações reais

- Distância geográfica não possui fonte canônica confiável; usa-se apenas unidade preferencial quando fornecida ao algoritmo.
- O custo participa da explicação, mas não reduz a nota: uma política de custo por tenant ainda não existe.
- Check-ins, ocorrências e substituições possuem implementações históricas heterogêneas; o resumo preserva zero até sua consolidação em fonte canônica, evitando SQL frágil ou números artificiais.
- Ações críticas continuam nos serviços existentes de Plantões/Central de Escala, que já validam transição, permissão e auditoria. O Command Center direciona ao detalhe real do plantão.

## Arquivos centrais

- `PlantaoPro.Domain/Escalas/SmartScheduleScoring.cs`
- `PlantaoPro.Api/ManagerCommandCenterService.cs`
- `PlantaoPro.Api/Controllers/ManagerCommandCenterController.cs`
- `PlantaoPro.Web/Controllers/CommandCenterController.cs`
- `PlantaoPro.Web/Views/CommandCenter/Index.cshtml`
- `PlantaoPro.Web/wwwroot/css/pages/command-center.css`
- `PlantaoPro.Tests/ManagerCommandCenterV2090Tests.cs`
