# PlantãoPro — Release Candidate comercial, SaaS, white label e operacional

## Escopo desta rodada
Release Candidate focado em fechamento demonstrável e homologável dos módulos já existentes: comercial público, simulador, propostas, Admin SaaS, Portal Cliente, Portal Parceiro, white label, billing/marketplace, operação de plantões, LGPD, auditoria, observabilidade e demo mode.

## Comandos de validação
- `dotnet clean backend/PlantaoPro.Api/PlantaoPro.Api.csproj`
- `dotnet clean backend/PlantaoPro.Web/PlantaoPro.Web.csproj`
- `dotnet build backend/PlantaoPro.Api/PlantaoPro.Api.csproj`
- `dotnet build backend/PlantaoPro.Web/PlantaoPro.Web.csproj`
- `dotnet test`

> Pendência real desta execução: o ambiente do agente não possui SDK `dotnet`; as validações devem ser repetidas no runner de CI/homologação.

## Usuário admin padrão
- E-mail: `admin@plantaopro.com`
- Senha: `Admin@123`

## Mapa do Release Candidate
| Funcionalidade | Status | Correção realizada | Pendência real |
|---|---|---|---|
| Landing, planos e simulador | Parcial | Fluxo público mantém actions reais e simulador retorna plano recomendado. | Persistir leads/simulações em banco no ambiente final. |
| Propostas comerciais | Parcial | Conversão agora exige proposta aprovada, bloqueia duplicidade por empresa/CNPJ e retorna tenant, cliente, assinatura, admin e onboarding provisionados em modo demonstrável. | Integrar provisionamento transacional ao PostgreSQL definitivo. |
| Admin SaaS | Parcial | Views de dashboard e APIs de resumo seguem endpoints existentes. | Homologar widgets com massa real e claims por perfil. |
| Portal cliente | Parcial | Documentado escopo de plano, uso, faturas, white label, onboarding, suporte e treinamento. | Validar isolamento por tenant em todos os cards com banco real. |
| Portal parceiro | Parcial | Documentado escopo de leads, propostas, clientes vinculados, comissões e materiais. | Restringir dados clínicos e billing detalhado conforme permissão real. |
| White label | Parcial | Mantido sem deploy, com templates e fallback documentados. | Testar aplicação visual por host/tenant no ambiente Web publicado. |
| Billing e marketplace | Parcial | Gateway segue identificado como `MANUAL_SANDBOX` no fluxo comercial demonstrável. | Conectar cobrança real somente quando gateway homologado existir. |
| Fluxo operacional plantões | Parcial | Roteiro operacional documenta hospital -> plantão -> convite -> escala -> pagamento. | QA manual ponta a ponta depende de banco e SDK no ambiente de homologação. |
| Multi-tenant e segurança | Parcial | Roteiro de segurança lista validações por perfil e tenant. | Criar evidências com dois tenants reais e usuários de perfis distintos. |
| LGPD e auditoria | Parcial | Conversão, lead, simulador, módulos e demo registram auditoria protegida contra falha. | Validar exportação/anonimização em massa real. |
| Demo mode | Parcial | Status passou a expor contadores por entidade demo sem dados pessoais reais. | Persistir flag `is_demo` nas tabelas definitivas quando gerar massa em banco. |

## Pendências reais
- Executar build/testes em ambiente com SDK .NET.
- Rodar QA manual com PostgreSQL de homologação.
- Capturar evidências do isolamento entre dois tenants.
- Substituir armazenamento demonstrável em memória por persistência transacional apenas quando o fluxo comercial for ativado fora de demo/sandbox.
