# Acesso demonstrável no banco e design SaaS

## Escopo e base confirmada

Commit inicial preservado: `11dd5fa` (`Merge pull request #472 from devmnsoft/codex/corrigir-cs0117-e-aprimorar-interface`). A identidade canônica usa `usuarios`, `perfis`, `usuarios_perfis` e `perfil_permissoes`; o contexto SaaS usa `clientes`, `tenants`, `unidades`, `assinaturas`, `modulos_sistema` e `tenant_modulos`. O login Web envia o formulário com antiforgery à API, que consulta PostgreSQL, valida BCrypt e cria JWT/sessão canônica. Também foram conferidos `RolesConstants.EscalasGestao`, `RepositoryPathResolver.RepoRoot` e os fluxos de execução/conferência existentes.

Não foi criada tabela de usuário, autorização alternativa, senha mestra ou exceção por e-mail. Foi removida a compatibilidade insegura que aceitava um valor legado em texto puro como senha.

## Configuração e provisionamento local

O banco precisa estar instalado antes deste passo. Configure por variável de ambiente ou `dotnet user-secrets`; o nome informado é comparado, com diferenciação exata, a `current_database()` antes da transação:

```bash
cd backend
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__Default='Host=localhost;Port=5432;Database=plantaopro_dev;Username=plantaopro_app;Password=LOCAL_ONLY'
export DemoSeed__Enabled=true
export DemoSeed__DevelopmentDatabase=plantaopro_dev
dotnet run --project PlantaoPro.Api -- --provision-demo
```

O comando é deliberado: o seed não roda em uma requisição nem na inicialização normal. Ele recusa ambiente diferente de `Development`, exige habilitação explícita e fixa o banco por nome. Tudo ocorre em uma transação protegida por advisory lock. IDs são estáveis e inserções são idempotentes; uma repetição preserva hashes e vínculos. Caso exista outro administrador global, a transação é recusada e preservada integralmente.

As senhas podem ser substituídas localmente com `DemoSeed__SuperAdminPassword` e `DemoSeed__ManagerPassword`. Não as inclua em JSON versionado, shell history compartilhado ou logs.

## Credenciais iniciais exclusivamente locais

| Contexto | Login | Senha inicial | Destino esperado |
|---|---|---|---|
| Administração da plataforma | `superadmin@mnsoft.example` | `MnSoft!Demo2026#Admin` | Administração global e seleção auditada de cliente |
| Santa Casa Demonstração | `gestor@santacasa-demo.example` | `SantaCasa!Demo2026#Gestor` | Área isolada do cliente, unidade e módulos contratados |

Os endereços `.example`, a organização e a unidade são fixtures sintéticas. Nenhum paciente, cobrança, mensagem ou destinatário real é criado.

## Redefinição local explícita

O provisionamento normal **não** redefine senha. Para restaurar somente as duas identidades reconhecidas, repita a mesma configuração e execute:

```bash
dotnet run --project PlantaoPro.Api -- --reset-demo-passwords
```

Esse comando tem as mesmas três barreiras (Development, habilitação e nome do banco), grava novos hashes BCrypt e revoga sessões abertas das contas. Como `.example` não possui caixa postal, não se deve anunciar recuperação por e-mail sem provedor funcional.

## Execução e URLs

Inicie API e Web em terminais distintos:

```bash
dotnet run --project backend/PlantaoPro.Api --launch-profile http
dotnet run --project backend/PlantaoPro.Web --launch-profile http
```

* Aplicação Web real: `http://localhost:52976/Account/Login`.
* Swagger da API (não é a aplicação): `http://localhost:51976/swagger`.

O indicador discreto de demonstração aparece apenas com `DemoSeed__Enabled=true` (ou a configuração legada visual `Demo__Enabled=true`). A página pública jamais exibe logins ou senhas.

## Segurança, isolamento e módulos

O administrador global recebe o perfil canônico global sem tenant. O gestor recebe `ADMINISTRADOR_CLIENTE` com `tenant_id` e `cliente_id` da Santa Casa Demonstração. O contrato local habilita somente Escalas, Execução e Conferência; contratação não substitui permissões. A API continua responsável por distinguir identidade, vínculo, contexto, módulo e permissão, e todas as sessões passam pela validação/revogação canônica.

## Evidências e limitações

Os testes de contrato cobrem acionamento explícito, barreiras ambientais, transação, concorrência, tabelas canônicas, BCrypt, idempotência, preservação de senha, conflito global, reset separado, revogação e requisitos do login. Os testes de integração PostgreSQL e E2E devem ser executados com um banco descartável e serviços de comunicação de teste antes de afirmar que as credenciais autenticam. Screenshots devem ser capturados nessa execução real, sem senha visível; não foi usado mock ou fallback para fabricar evidência.
