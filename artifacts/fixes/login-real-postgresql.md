# Correção do login real com PostgreSQL

## Estado inicial e aplicações envolvidas

- Commit inicial preservado: `1c02c9b45b5765ddc90881510ff0c6c9d9fa68cf`.
- A tela é servida por `PlantaoPro.Web` em `http://localhost:52976/Account/Login` (perfil HTTP de desenvolvimento). A API/Swagger é outro processo, em `http://localhost:51976`.
- O formulário faz `POST /Account/Login` com antiforgery. A Web envia JSON para `POST /api/auth/login`; a API consulta `plantaopro.usuarios`, vínculos de perfil/tenant e valida `senha_hash` com BCrypt antes de criar a sessão persistida e devolver o JWT. A Web só então cria o cookie e redireciona.
- A origem efetiva do banco é `ConnectionStrings:Default` da configuração da API. Em desenvolvimento ela deve vir de variável de ambiente, user-secrets ou configuração local ignorada pelo Git; a connection string não é reproduzida neste artefato.

## Causa-raiz e etapa da interrupção

A inspeção do caminho executado pelo clique encontrou duas políticas de duração concorrentes:

1. o `HttpClient` Web não definia timeout próprio para autenticação e, portanto, podia manter o POST MVC pendente pelo timeout padrão;
2. após 15 segundos, o JavaScript apenas reabilitava o botão, embora o POST nativo original continuasse em andamento.

A interrupção observável fica, portanto, entre `AccountController.Login` (Web) e a resposta de `api/auth/login`: o navegador permanece na view ocupada enquanto a chamada servidor-servidor aguarda. O temporizador não cancelava Web, API ou PostgreSQL e ainda permitia um segundo login concorrente. Swagger responder não exercita essa chamada.

A correção dá ao cliente Web um timeout de autenticação configurável (15 segundos por padrão), passa o cancelamento do request Web ao HTTP, do endpoint API ao serviço, e deste às operações Dapper/Npgsql críticas. Timeout e API indisponível voltam como mensagens diferentes. O script não usa mais um temporizador cosmético: mantém o botão bloqueado durante o único POST real e só restaura estado quando uma resposta renderiza a view ou quando `pageshow` restaura a página pelo histórico.

## Arquivos alterados

- `backend/PlantaoPro.Web/Program.cs` e `appsettings.json`: timeout explícito do cliente da API.
- `backend/PlantaoPro.Web/Controllers/AccountController.cs`: propagação de cancelamento, distinção de timeout/conexão, descarte da senha antes de renderizar falhas.
- `backend/PlantaoPro.Web/wwwroot/js/auth-login.js`: remoção do temporizador que liberava submissões concorrentes.
- `backend/PlantaoPro.Api/Controllers/AuthController.cs` e `Data.cs`: cancelamento até PostgreSQL e criação da sessão persistida somente após as validações e atualizações de sucesso.
- `backend/PlantaoPro.Api/DevelopmentSeed.cs`: remoção das senhas default compiladas.
- `backend/PlantaoPro.Tests/*`: contratos de timeout e provisionamento sem senha fixa.

## Autenticação e provisionamento

Não existe sucesso especial para e-mail conhecido, senha fixa, banco indisponível ou documento. A consulta é parametrizada, não escolhe silenciosamente identidades ambíguas, lê o hash persistido e usa `BCrypt.Verify`; perfil, cliente, tenant, permissões e módulos são materializados da persistência. Cookie Web e sessão só são criados depois do resultado positivo da API.

O provisionamento continua sendo um comando administrativo explícito, idempotente e restrito a `Development`. Agora as duas senhas são obrigatoriamente externas ao código:

```bash
ASPNETCORE_ENVIRONMENT=Development \
DemoSeed__Enabled=true \
DemoSeed__DevelopmentDatabase='<banco-local-descartavel>' \
DemoSeed__SuperAdminPassword='<entrada-local>' \
DemoSeed__ManagerPassword='<entrada-local>' \
ConnectionStrings__Default='<configuracao-local>' \
dotnet run --project backend/PlantaoPro.Api -- --provision-demo
```

`--provision-demo` preserva contas e hashes já existentes. `--reset-demo-passwords` é a operação separada e explícita para redefinir as contas demonstrativas e revogar suas sessões; ela não roda na inicialização comum.

## Verificações executadas

- `python3 scripts/check-csharp10-compatibility.py`: passou.
- `python3 scripts/check-form-experience.py`: passou.
- `python3 scripts/repository-security-check.py`: passou.
- `python3 -m unittest discover -s scripts/tests -p 'test_*.py'`: 11 testes passaram.
- `git diff --check`: passou.

## Limitações restantes

O contêiner não possui `dotnet`, `psql`, Docker/Podman nem credenciais autorizadas. A tentativa de obter o instalador oficial do SDK retornou HTTP 403. Assim, não foi possível executar restore/build Debug e Release, testes .NET, PostgreSQL descartável, Console/Network reais, provisionar/inspecionar contas ou o E2E autenticado (incluindo cookie, refresh, logout, super administrador e administrador de cliente). Essas etapas permanecem obrigatórias em CI ou numa estação com .NET 10 e PostgreSQL; este documento não declara uma homologação runtime que o ambiente não permitiu produzir.

Para a comprovação final, iniciar API e Web nas portas acima, provisionar o banco descartável pelo comando explícito e executar `npm run diagnose:login` com credenciais fornecidas por ambiente, seguido do conjunto .NET/PostgreSQL e do smoke autenticado do repositório.
