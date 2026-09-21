# v2.17.1 — auditoria do baseline de autenticação e segurança

Data: 2026-09-21 (UTC)  
Base auditada: `7840671` (`work`), com árvore limpa no início da execução.

## Escopo verificado

- Fluxos de login API e Web, emissão/propagação de JWT, cookie, sessão, papéis, permissões, módulos e contexto.
- Serviço de sessão autenticada e contratos recentes de isolamento por tenant.
- Rotas/controllers, manifestos canônicos do banco, migrations recentes e relatórios técnicos anteriores.
- Configuração versionada para desenvolvimento e gates locais disponíveis em `scripts/`.

## Defeito bloqueante corrigido

`backend/PlantaoPro.Api/appsettings.Development.json` continha uma chave JWT utilizável e três senhas de demonstração versionadas, além de desabilitar lockout e habilitar criação automática/compatibilidade legada de banco. Esses defaults contrariavam o modelo seguro já documentado no projeto e faziam o scanner oficial falhar.

A configuração de desenvolvimento agora:

- deixa `Jwt:Key` vazio, exigindo variável de ambiente ou user-secret;
- mantém lockout de login habilitado;
- desabilita criação automática e banco legado por padrão;
- desabilita provisionamento/reset automático do demo;
- não contém nenhuma senha de demo.

Foram adicionados testes de regressão que leem o JSON como configuração estruturada e impedem a reintrodução desses defaults inseguros.

## Evidência executável

Os gates locais de unicidade de controllers/rotas, C# 10, manifestos do banco, segurança do repositório, feedback acessível, formulários, layout, UI premium, assets e rotas v1.52 passaram. O scanner de segurança passou após a correção.

Dois validadores de UX preexistentes continuam falhando: `check-operational-ux.py` não encontra o contrato textual “Gerar financeiro” em `Views/OperacaoPremium/Fechamentos.cshtml`; `check-saas-ui.py` não encontra marcadores esperados nas telas Admin SaaS e Onboarding. Eles não foram mascarados nem classificados como aprovados.

## Bloqueios reais

- `dotnet`, `docker` e `psql` não estão instalados no executor.
- Restore, build, testes .NET, inicialização Swagger, login com usuário de desenvolvimento, PostgreSQL limpo/upgrade e E2E Web não puderam ser executados.
- Não houve QA visual nem captura de tela porque a aplicação não pôde ser compilada/iniciada.

O produto permanece **não homologado para produção** nesta execução. A próxima validação deve começar por restore/build/test com .NET 10 e PostgreSQL descartável; depois executar login API/Web, sessão revogada, Super Admin global e cruzamento entre dois tenants antes de qualquer expansão funcional.
