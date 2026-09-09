# v2.15.3 - cockpit SaaS, cobranca, modulos e design

Data da entrega: 2026-09-09
Branch: `codex/v2153-cockpit-saas-cobranca-modulos-design`

## O que ja estava feito

- Autenticacao Web por cookie e API por JWT.
- Claims basicas de usuario, perfis, tenant, cliente, permissoes e modulos.
- Estrutura SaaS com clientes, planos, assinaturas, modulos, auditoria, portal do cliente e cobranca sem gateway externo.
- Views Razor e partials premium para KPIs, status, empty state, alertas e guias de tela.
- Testes de contrato cobrindo boa parte da arquitetura.

## O que faltava ou estava parcial

- `/AdminSaas` ainda era uma vitrine estatica/demo, nao um cockpit operacional baseado nas APIs reais.
- Cobranca SaaS aceitava perfil `Administrador` e podia listar/alterar dados globais se o tenant nao fosse imposto.
- O guard Web validava permissao, mas nao exigia modulo contratado.
- Cliente bloqueado/suspenso era bloqueado no login, contra a regra de permitir login restrito.
- Configuracao versionada continha senha padrao e flags inseguras.
- Script SQL consolidado estava com checksums desalinhados em ambiente Windows.

## Implementado nesta rodada

- `/AdminSaas` passou a consumir dados reais de `api/saas-dashboard/resumo`, `api/faturamento-saas/resumo`, `api/saas-dashboard/alertas` e `api/modulos`.
- Cockpit global ganhou KPIs de clientes, bloqueios, inadimplencia, faturamento previsto/recebido, faturas vencidas, modulos contratados, alertas e status operacional.
- Login de cliente suspenso/bloqueado agora permite autenticacao com claim `cliente_status`, mensagem restritiva e auditoria `BLOQUEIO_TENANT`.
- `SaasRouteGuardFilter` passou a aplicar a regra central: usuario autenticado + cliente ativo + perfil permitido + modulo contratado.
- Rotas normais sao bloqueadas para cliente nao ativo; ficam liberadas apenas areas de conta, ajuda, LGPD e `MinhaAssinatura`.
- Cobranca SaaS foi escopada por `ICurrentUserService`: usuario nao global so enxerga o proprio cliente.
- Geracao mensal de faturas SaaS ficou restrita a Super Admin global.
- Detalhe, atualizacao, notificacao, contestacao e inadimplencia de faturas passaram a respeitar escopo do cliente atual.
- Tela `AccessDenied` ganhou estados especificos para cliente bloqueado e modulo nao contratado, com caminhos para faturas, suporte e solicitacao comercial.
- `AssinaturaSaasViewModel` passou a validar vigencia invertida por atributo de propriedade, mesmo quando outros campos estao invalidos.
- Removidos segredos obvios e flags inseguras de appsettings versionados.
- Queries legadas v1.13 com `select *` foram substituidas por colunas explicitas nas rotinas tocadas.
- Gerador de `scrpt_completo.sql` agora grava LF fixo e calcula hash dos bytes reais dos arquivos fonte.

## Regras de negocio aplicadas

- Super Admin MNSOFT acessa visao global e nao depende de tenant selecionado.
- Cliente comum nunca recebe escopo global em cobranca SaaS.
- Cliente bloqueado pode fazer login, consultar regularizacao/faturas e acionar suporte, mas nao opera modulos comuns.
- Modulo nao contratado e removido do acesso real por guard Web e deve ser omitido do menu conforme claims.
- Acoes criticas de bloqueio, cobranca e contexto permanecem auditaveis.
- Configuracoes sensiveis ficam fora do Git e devem ser fornecidas por ambiente.

## Telas criadas ou alteradas

- `backend/PlantaoPro.Web/Views/AdminSaas/Index.cshtml`
- `backend/PlantaoPro.Web/Views/Account/AccessDenied.cshtml`
- `backend/PlantaoPro.Web/Views/Relatorios/Index.cshtml`

Todas as telas tocadas mantem layout limpo, estados informativos e/ou bloco discreto de orientacao. O cockpit traz KPIs, tabelas responsivas, badges e links governados.

## Testes criados ou ajustados

- `backend/PlantaoPro.Tests/V2153CockpitSaasContractTests.cs`
- Ajustes em contratos de validacao, seguranca e Dapper para remover literais proibidos e cobrir a nova regra.

Cobertura nova:

- Super Admin em `/AdminSaas` usa cockpit real.
- Login de cliente bloqueado autentica com status e audita bloqueio operacional.
- Guard Web exige modulo contratado e bloqueia operacao normal de cliente suspenso.
- Cobranca SaaS isola faturas por cliente e mantem geracao global restrita.

## Comandos executados

- `git status --short --branch`: sucesso.
- `dotnet --info`: SDK .NET 10.0.400 disponivel.
- `dotnet restore backend/PlantaoPro.sln`: sucesso.
- `dotnet build backend/PlantaoPro.Api/PlantaoPro.Api.csproj -c Debug`: sucesso.
- `dotnet build backend/PlantaoPro.sln -c Debug --no-restore`: sucesso.
- `dotnet build backend/PlantaoPro.sln -c Release --no-restore`: sucesso.
- `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Debug --no-build`: sucesso, 360 testes aprovados apos correcoes.
- `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release --no-build`: sucesso, 360 testes aprovados.
- `git diff --check`: sucesso.
- `python scripts/generate-scrpt-completo.py`: sucesso, hash gerado `da6b4452c35aaacb8b36cfe672201dd02a696759d487dc92e503a7d21e260a4f`.
- `python scripts/repository-security-check.py`: sucesso.
- `python scripts/check-csharp10-compatibility.py`: sucesso.
- `python scripts/validate-scrpt-completo.py`: sucesso, cobertura 100%.
- `rg` de padroes proibidos solicitado na Fase 10: sem achados.

## Pendencias reais

- Ainda falta smoke integrado com PostgreSQL real para comprovar fluxos de login, tenant bloqueado, fatura e modulo bloqueado com dados vivos.
- Ainda existem services legados com SQL dinamico ou filtros tenant incompletos fora do escopo tocado.
- Claims de modulo/status dependem de novo login; falta refresh centralizado apos mudanca contratual.
- Nao foi integrado gateway externo de pagamento nesta rodada por decisao de escopo.

## Proximo bloco recomendado

1. Criar testes de integracao com PostgreSQL para login, cobranca SaaS, bloqueio de cliente e bloqueio de modulo.
2. Revisar todos os endpoints antigos que retornam dados operacionais sem `tenant_id` obrigatorio.
3. Implementar refresh/revogacao de sessao quando contrato, modulo ou status do cliente mudar.
4. Evoluir portal do cliente para self-service completo de usuarios, perfis, permissoes, visual e suporte.
