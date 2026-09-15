# Login Web, PostgreSQL e sessão

## Escopo e diagnóstico

- **Commit inicial preservado:** `fc487c1a25ee8b50a45c246cfc010c7ac2d9a178` (árvore limpa no início desta rodada).
- **Web:** `http://localhost:52976/Account/Login` ou `https://localhost:52977/Account/Login`.
- **API/Swagger:** `http://localhost:51976` ou `https://localhost:51977`. Swagger não hospeda a interface Web.
- **Caminho real:** `Login.cshtml` faz `POST /Account/Login` com antiforgery; `AccountController` envia JSON para `POST /api/auth/login`; `AuthController` chama `AuthService`; o serviço abre `ConnectionStrings:Default`, consulta o schema `plantaopro` com Dapper parametrizado, verifica `senha_hash` com BCrypt e carrega perfis, vínculos, permissões e módulos. Só após sucesso a API persiste a sessão e a Web emite `PlantaoPro.Auth` e redireciona.

O diagnóstico de código confirmou que recuperação (`POST /api/auth/forgot-password`) e login são formulários, ações Web e chamadas API diferentes. Um `200` da recuperação não comprova autenticação nem entrega de e-mail.

### Primeira falha encontrada nesta rodada

O formulário executava o POST nativo correto, mas descartava `returnUrl` porque a view não o serializava. Além disso, uma transição `offline` durante o POST limpava o estado de submissão; ao voltar `online`, o botão podia ser habilitado enquanto a requisição original ainda estava em andamento. Isso permitia submissão concorrente, sessão duplicada e tentativas adicionais de bloqueio. A evidência estática foi a ausência do campo `returnUrl` em `Login.cshtml` e a chamada a `resetSubmission()` no handler `offline` de `auth-login.js`.

A correção serializa e preserva `returnUrl` (a action continua aceitando apenas URL local via `Url.IsLocalUrl`) e mantém o botão indisponível enquanto `data-request-started` indicar um POST real. Apenas uma resposta renderizada ou `pageshow` restaura a interface.

## Arquivos alterados

- `backend/PlantaoPro.Web/Views/Account/Login.cshtml`: preserva o destino local no POST.
- `backend/PlantaoPro.Web/wwwroot/js/auth-login.js`: impede reenvio durante oscilação de conectividade.
- `backend/PlantaoPro.Web/Controllers/AccountController.cs`: preserva `returnUrl` após erro e não registra amostras de respostas potencialmente contendo token.
- `backend/PlantaoPro.Api/Controllers/AuthController.cs`: normaliza o e-mail de recuperação, gera token apenas para conta existente, persiste somente o hash e registra o resultado interno sem endereço completo; a resposta pública não expõe token.
- `backend/PlantaoPro.Tests/V2164CompilationAndJourneyContractTests.cs`: contratos de submit, retorno, estado concorrente e recuperação segura.

## Inicialização e provisionamento local

Use segredos locais, sem versioná-los:

```bash
docker compose up -d postgres
ASPNETCORE_ENVIRONMENT=Development \
DemoSeed__Enabled=true \
DemoSeed__DevelopmentDatabase=plantaopro \
DemoSeed__SuperAdminPassword='<senha-local-superadmin>' \
DemoSeed__ManagerPassword='<senha-local-gestor>' \
ConnectionStrings__Default='Host=127.0.0.1;Port=5432;Database=plantaopro;Username=postgres;Password=<senha-local>' \
dotnet run --project backend/PlantaoPro.Api -- --provision-demo
```

O provisionamento explícito cria, se ausentes, `superadmin@mnsoft.example` e `gestor@santacasa-demo.example`, preservando hashes existentes em reexecuções. Para redefinição administrativa local das contas `.example`, repita a configuração segura e acrescente `--reset-demo-passwords`; essa operação não ocorre no startup comum.

Inicie os processos separadamente:

```bash
ConnectionStrings__Default='<conexao-local>' dotnet run --project backend/PlantaoPro.Api --launch-profile http
dotnet run --project backend/PlantaoPro.Web --launch-profile http
```

## Recuperação de senha

O endpoint cria token aleatório de uso único, persiste apenas SHA-256, expira em 30 minutos e a redefinição marca o token utilizado. Não há provedor de e-mail conectado nesse fluxo: o estado interno é `PENDENTE_SEM_PROVEDOR`; portanto, HTTP 200 significa somente resposta pública aceita, **não entrega**. Tokens não são mais devolvidos pela API nem exibidos pela Web.

## Resultado e limitações desta execução

| Cenário obrigatório | Resultado nesta máquina |
|---|---|
| Submit/antiforgery/estado de loading | Verificação estática adicionada; execução .NET indisponível |
| Superadministrador persistido e área global | Não executado: sem SDK .NET, Docker e PostgreSQL |
| Gestor Santa Casa, módulos e isolamento | Não executado: sem SDK .NET, Docker e PostgreSQL |
| Senha correta/incorreta, inexistente, bloqueio e hash do seed | Não executado pelo mesmo bloqueio ambiental |
| Banco indisponível e timeout sem sessão | Fluxos e contratos inspecionados; E2E não executado |
| Cookie, refresh, página protegida e logout | Não executado pelo mesmo bloqueio ambiental |

Não há comprovação runtime nesta máquina e este artefato não transforma build ou HTTP 200 de recuperação em evidência de login. A homologação E2E deve usar `npm run diagnose:login` e `scripts/local/run-smoke-authenticated.sh` com as credenciais fornecidas por ambiente, além dos testes PostgreSQL, em um executor com .NET 10 e Docker/PostgreSQL.
