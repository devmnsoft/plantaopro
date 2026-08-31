# PlantãoPro v2.12.2 — onboarding, planos, cobrança e design

## Modo de execução e diagnóstico

- **Modo usado:** MODO DESIGN ESTÁTICO.
- **SDK:** `dotnet` não está instalado (`dotnet: command not found`). Conforme a restrição da rodada, não foram alterados C#, contratos, projetos, solução, banco ou migrations.
- **Git remoto:** o repositório local não possui remoto configurado; `git remote -v` não retornou entradas. Portanto, fetch, pull, push e abertura remota de PR dependem de configuração externa.
- **Base encontrada:** a versão anterior já contém estruturas reais para clientes/tenants, planos, assinatura, faturamento, onboarding, convites, perfis, usuários e orientação contextual. Esta rodada não substitui essas integrações por dados demonstrativos.

## Jornada SaaS preservada

O cadastro self-service existente conduz organização, plano, administrador, consentimentos e revisão. As telas administrativas existentes consomem os dados fornecidos pelos controllers/APIs; o refinamento visual desta rodada não adiciona valores, usuários, cobranças ou instituições fixas.

### Identidade e contexto

- CPF ou e-mail identifica a pessoa; a senha é individual.
- CNPJ identifica a instituição e não é uma credencial compartilhada.
- O contexto de tenant e as permissões continuam sendo decididos pelo backend.
- O Super Admin MNSOFT mantém o console global autorizado; o Admin do Cliente permanece restrito ao próprio tenant.
- Bloqueios de cliente, usuário, módulo e inadimplência não são simulados no navegador.

## Planos, cobrança e bloqueio

O catálogo real de planos permanece a fonte de limites, recursos, status e valores. A tela ganhou orientação explícita para revisão segura do catálogo e deixa claro que editar plano não altera diretamente uma assinatura. O faturamento SaaS existente mantém filtros, estados vazios e ações controladas pelo servidor, com orientação sobre auditoria e suspensão.

Valores comerciais definitivos devem ser definidos e mantidos pela MNSOFT no catálogo persistido; esta rodada não introduz preço fictício.

## Onboarding e convites

A experiência existente mantém implantação em etapas, consentimentos, administrador individual e revisão. Convites e onboarding continuam vinculados aos endpoints reais e ao tenant corrente. Como o SDK não está disponível, não foram criados novos fluxos server-side de convite, expiração, reenvio ou checklist persistido.

## Telas e design

Foi adicionada uma camada visual versionada (`v2122-saas-commerce.css`) aplicada globalmente pelo layout, com foco nas jornadas de:

- cadastro e onboarding;
- catálogo de planos;
- gestão de clientes;
- faturamento SaaS;
- convites e operação;
- cards, tabelas e formulários relacionados.

Melhorias incluem hierarquia comercial mais clara, superfícies sóbrias, cor médica, elevação consistente, estados de foco visíveis, tabela responsiva, stepper navegável em telas pequenas, respeito a movimento reduzido e modo de cores forçadas. A tela de planos também recebeu o bloco recolhível **Como usar esta tela** com passos curtos e alerta sobre controle server-side.

## Testes e verificações

Executados:

- diagnóstico solicitado de Git, SDK e referências SaaS;
- buscas de padrões frágeis/proibidos;
- `git diff --check`;
- `python3 scripts/repository-security-check.py`;
- `python3 scripts/check-csharp10-compatibility.py`;
- `python3 scripts/validate-scrpt-completo.py`.

Não executados por limitação real do ambiente:

- restore, build e testes .NET;
- screenshot da aplicação em execução, pois o servidor ASP.NET não pode ser iniciado sem `dotnet`.

## Limitações restantes

A implementação backend e os testes pedidos para cadastro ampliado, CPF, seleção multi-tenant, trial, ativação, auditoria, cobrança, bloqueios, convites e checklist persistido não puderam ser validados nem evoluídos nesta rodada. Essas partes devem ser retomadas em ambiente com .NET 10 disponível. Antes do deploy, também é necessária validação visual navegada com dados reais e perfis Super Admin/Admin do Cliente.
