# Auditoria do repositório PlantãoPro

## Diagnóstico executivo
PlantãoPro contém backend ASP.NET Core, Web MVC/Razor, PostgreSQL/Dapper, app mobile, scripts SQL e documentação extensa. A auditoria encontrou módulos clínicos Saúde 360 já implementados em controllers Web e API, mas com dependência residual de tela genérica e actions que redirecionavam para `Index()` em fluxos específicos.

## Módulos encontrados
- Autenticação, usuários, perfis e permissões.
- Admin SaaS, clientes/tenants, planos, assinaturas, billing, marketplace e white label.
- Plantões, escalas, convites, médicos, hospitais e especialidades.
- Saúde 360: pacientes, agendamentos, painel, triagem, consultas, CID, prescrições, financeiro clínica, convênios e planos de saúde.
- Ajuda, manual, LGPD, auditoria, observabilidade, suporte, customer success e mobile.

## Controllers API encontrados
Controllers principais em `backend/PlantaoPro.Api/Controllers`, incluindo Auth, Health, Dashboard, hospitais, especialidades, plantões, escalas, financeiro, clientes, usuários, Saúde 360 clínico, Workflow e suporte.

## Controllers Web encontrados
Controllers MVC em `backend/PlantaoPro.Web/Controllers`, incluindo Account, Home, Saúde 360 consolidado, operação, SaaS, suporte, manual e LGPD.

## Views encontradas
A árvore `backend/PlantaoPro.Web/Views` possui views específicas para módulos centrais e fallback genérico em `Views/Saude360/Modulo.cshtml` e `Views/Saude360/Formulario.cshtml`.

## Views genéricas em uso
O controller Saúde 360 mantém fallback genérico para listagem e formulário, agora com actions específicas para fluxos prioritários em vez de redirecionamento para `Index()`.

## Actions que retornavam Index ou outra action
Foram identificadas actions em Agendamentos, Triagem, Consultas, CID, Prescrições, Financeiro Clínica, Convênios e Planos de Saúde. Nesta rodada, as rotas prioritárias passaram a ter `ModuloAsync` com título, descrição, endpoint e ações próprias.

## Menus existentes e risco de 404
O menu global está centralizado em `MenuBuilderService`. O risco principal era excesso de grupos e links para actions genéricas. A recomendação é manter a jornada curta: Início, Atendimento, Plantões, Financeiro, Convênios, Gestão do Cliente, Admin SaaS, Relatórios, Ajuda e Governança.

## Formulários
Formulários genéricos existem para create/edit Saúde 360. Priorização futura: ViewModels específicos por cadastro, lookups reais e POST com preservação completa de dados.

## Selects sem lookup
Risco médio em forms genéricos: selects precisam consumir lookups globais por tenant antes de homologação comercial completa.

## Endpoints integrados e não consumidos
Há endpoints clínicos e SaaS expostos/documentados. A matriz mestra lista status por área para guiar consumo real no Web e Mobile.

## Migrations e seeds
Banco contém scripts em `database/`, `backend/sql/` e `backend/PlantaoPro.Web/Database`. Risco: manter ordem idempotente e validar seeds contra tabelas existentes.

## Riscos de segurança
- Evitar logs de senha, token, hash, API key e dados clínicos sensíveis.
- Auditar acesso a resumo clínico, impressão, exportação e dados financeiros.
- Garantir tenant_id/cliente_id em queries.

## Pendências críticas
- Ambiente desta execução não possui SDK `dotnet`; build/test precisam rodar em runner com .NET instalado.

## Pendências médias
- Substituir progressivamente fallback genérico por ViewModels e views fortemente tipadas.
- Consolidar lookups reais em todos os selects.

## Pendências futuras
- E2E de browser para login, menu sem 404 e jornada clínica completa.
