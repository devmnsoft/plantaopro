# PlantãoPro v2.10.0 — Portal da Unidade e contratos operacionais

## Resumo técnico

- O placeholder existente `HospitalArea` foi evoluído para um portal responsivo com indicadores, cobertura, notificações e formulário sem entrada manual de identificadores.
- A API ganhou dashboard isolado por tenant/unidade, criação auditada de solicitação e decisões restritas a gestores. Recusa exige motivo; conversão usa transação e bloqueia sobreposição de escala.
- Foram adicionadas tabelas idempotentes para solicitações, contratos e regras de preço. O DDL permanece no pipeline de migração e nunca é executado durante requisições.
- Cálculo de preço e cobertura foi mantido no domínio; assim, o financeiro pode consumir o valor apurado sem duplicar seu fluxo.

## Arquivos principais

- `backend/PlantaoPro.Api/UnitPortalServices.cs` e `Controllers/UnitPortalController.cs`
- `backend/PlantaoPro.Domain/Contratos/`
- `backend/PlantaoPro.Web/Views/HospitalArea/`
- `database/schema/150_portal_unidade_contratos_v2100.sql`
- `backend/PlantaoPro.Tests/UnitPortalV2100Tests.cs`

## Segurança e governança

As consultas críticas recebem o tenant exclusivamente das claims e aplicam também a unidade. A criação valida que unidade e especialidade pertencem ao tenant. Aprovação/recusa têm autorização específica, transação e auditoria. O formulário oferece apenas seleções autorizadas e controles nativos de data/hora.

## Validações executadas

Os comandos requeridos foram executados conforme registrados na entrega/PR. A imagem atual não contém o SDK `dotnet`, portanto restore/build/test ficaram limitados pelo ambiente. A atualização remota da `main` também foi tentada, mas o proxy bloqueou o GitHub com HTTP 403; a branch foi criada sobre o merge local da `main` (`b98354b`).

## Limitações reais

- O carregamento dos dropdowns depende dos catálogos reais de unidades/especialidades autorizadas já existentes no ambiente; deliberadamente não há fallback com dados fictícios.
- Feriados são considerados pelo calculador quando informados pelo calendário corporativo; esta entrega não cria um segundo cadastro de feriados.
- Notificação persiste por meio da infraestrutura existente; canais externos continuam sujeitos à configuração do tenant.
