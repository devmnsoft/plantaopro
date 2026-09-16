# Correção do conflito de submissão do login

## Escopo e conclusão

Esta alteração corrige exclusivamente autenticação e acesso. **O login não foi declarado validado ponta a ponta neste ambiente**, pois ele não contém o SDK .NET, PostgreSQL, Docker nem um executável Chromium e o download do navegador foi recusado pelo CDN (HTTP 403). Não houve uso de autenticação falsa como evidência de banco, cookie ou autorização.

## Causa comprovada antes da correção

No commit de base `26d14e9f`, `Login.cshtml` já marcava o formulário com `data-submit-loading="manual"` e `data-login-form`. `_AuthLayout.cshtml` carregava, uma única vez, `plantaopro-ui.js` e `form-experience.js`; a seção da view carregava depois, uma única vez, `auth-login.js` e a validação unobtrusive.

A inspeção da versão anterior mostrou a sequência determinística:

1. o listener de captura de `form-experience.js` validava o formulário;
2. o listener genérico de bubble de `wireSubmitFeedback` recebia o primeiro `submit`, atribuía `aria-busy="true"` e desabilitava o botão;
3. o listener específico, registrado posteriormente por `auth-login.js`, recebia o mesmo evento, encontrava `aria-busy="true"`, chamava `preventDefault()` e impedia o POST.

Assim, campos válidos podiam deixar a tela ocupada sem qualquer requisição ao controller. Não foi usado o log de Swagger como evidência de login.

## Arquivos corrigidos

- `wwwroot/js/form-experience.js`: `wireSubmitFeedback` agora retorna imediatamente para formulários com loading manual ou formulário de login. Os demais formulários preservam o comportamento anterior.
- `wwwroot/js/auth-login.js`: o responsável único pelo loading do login respeita `event.defaultPrevented` antes de alterar qualquer estado.
- `AccountController.cs` e `AuthController.cs`: o identificador não sensível `X-Correlation-ID`, originado em `HttpContext.TraceIdentifier`, acompanha Web → API e entra em escopo estruturado de log. A API devolve o identificador. Senha, token, cookie e body não são registrados.
- `LoginSubmitRegressionContractTests.cs`: protege a exclusão, a ordem da guarda, a ausência de `form.submit()`, as inclusões únicas e a correlação.
- `scripts/ui/login-submit-regression.mjs`: teste Playwright contra a página Web real e, portanto, os scripts e a ordem reais. Cobre clique, Enter, duplo clique, inválidos, cancelamento anterior e retorno pelo histórico. A resposta interceptada comprova somente o comportamento de submit, nunca autenticação no banco.

Não há campos de credencial desabilitados antes da serialização; somente o botão é bloqueado. O POST continua nativo, com validação e antiforgery, sem `form.submit()`, temporizador, bypass ou remoção da proteção contra envio duplo.

## Rastreamento do fluxo real existente

1. O navegador envia `POST /Account/Login` com antiforgery.
2. `AccountController.Login` valida o ModelState, cria o `HttpClient` nomeado `PlantaoProApi`, adiciona `X-Correlation-ID` e chama `POST api/auth/login`.
3. `AuthController.Login` mantém a correlação e chama `AuthService.LoginAsync`.
4. `AuthService` abre `ConnectionStrings:Default`, consulta `plantaopro.usuarios` e os vínculos, normaliza somente o identificador, verifica `senha_hash` com `BCrypt.Verify` sem modificar a senha, aplica situação/bloqueio/perfis/contexto e persiste tentativa/sessão.
5. A Web recebe a resposta, cria a identidade no esquema cookie, persiste a sessão e redireciona por `returnUrl` local ou pelo perfil. Administrador global não recebe tenant arbitrário; usuário autenticado sem vínculo elegível é enviado para uma página orientativa, sem loop.
6. Os logs `Login POST iniciado`, `Chamando API de login`, `Resposta da API de login`, `Cookie de autenticação criado` e `Redirecionando...` podem ser unidos pelo campo estruturado `CorrelationId`.

A configuração versionada aponta a API Web para `http://localhost:51976`. Os profiles reais são API HTTP `51976`/HTTPS `51977` e Web HTTP `52976`/HTTPS `52977`; `/swagger` é apenas o launch URL da API. A chamada API é feita pelo `HttpClient` no servidor Web, não pelo navegador, portanto CORS não participa desse fluxo. O timeout permanece no cliente nomeado e falha de transporte/timeout continua distinta de credenciais inválidas. TLS não foi enfraquecido.

O cookie existente permanece `PlantaoPro.Auth`, HttpOnly, `SecurePolicy=SameAsRequest`, SameSite Lax, sem Domain customizado, Path padrão, expiração de oito horas e sliding expiration. A ordem permanece `UseSession`, `UseAuthentication`, `UseAuthorization`.

## Evidências e resultados neste ambiente

| Verificação | Resultado |
|---|---|
| Causa anterior | Confirmada por inspeção dos listeners e da ordem real de scripts no commit de base. |
| POST após clique/Enter e deduplicação | Teste Playwright entregue, mas não executado: Chromium ausente e download bloqueado por HTTP 403. |
| Chamada real Web → API | Não executada: SDK .NET ausente. |
| Conexão e schema efetivamente usados | Caminho de configuração e SQL revisados; conexão runtime não aberta, portanto `Database=postgres` não foi tratado como prova de erro ou acerto. |
| Credenciais persistidas/hash | Não executado: PostgreSQL ausente; nenhuma senha foi solicitada, alterada ou inferida. |
| Cookie, sessão, página protegida e reload | Não executados; nenhuma afirmação de autenticação completa. |
| Build/testes .NET | Bloqueados: `dotnet` não está instalado. |

Os casos obrigatórios de conta persistida/senha correta, senha incorreta, inexistente, bloqueada, super administrador global, administrador de cliente, sem contexto elegível, API indisponível, timeout, reload protegido e logout/acesso negado continuam **bloqueados neste executor** e devem ser executados localmente conforme abaixo.

## Execução local real

1. Instale .NET SDK 10, PostgreSQL, Node e Chromium para Playwright.
2. Defina segredos somente no shell/local secret store (não grave no repositório):

   ```bash
   export PLANTAOPRO_CONNECTION_STRING='Host=127.0.0.1;Port=5432;Database=<base>;Username=<usuario>;Password=<segredo>'
   export PLANTAOPRO_JWT_KEY='<chave-local-com-32-ou-mais-caracteres>'
   ./scripts/configure-development.sh
   ```

3. Para instalar/provisionar o administrador global pelo procedimento administrativo existente, forneça a senha por variável local segura; a ferramenta gera e grava somente o hash e verifica se o administrador global já existe:

   ```bash
   read -rsp 'Senha local do bootstrap: ' PLANTAOPRO_BOOTSTRAP_PASSWORD; echo
   export PLANTAOPRO_BOOTSTRAP_PASSWORD
   ./scripts/database/install-local-with-superadmin.sh
   unset PLANTAOPRO_BOOTSTRAP_PASSWORD
   ```

   Isso é um procedimento, não uma afirmação de que qualquer senha sugerida já esteja cadastrada.

4. Inicie API e Web em terminais separados, usando os profiles reais:

   ```bash
   dotnet run --project backend/PlantaoPro.Api/PlantaoPro.Api.csproj --launch-profile http
   dotnet run --project backend/PlantaoPro.Web/PlantaoPro.Web.csproj --launch-profile http
   ```

5. Em outro terminal, valide o submit real (a resposta do POST é interceptada apenas neste teste):

   ```bash
   npx playwright install chromium
   PLANTAOPRO_WEB_URL=http://localhost:52976 npm run test:login-submit
   ```

6. Execute a autenticação real, sem interceptação, usando segredo local e depois os gates:

   ```bash
   read -rp 'Identificador: ' PLANTAOPRO_LOGIN_IDENTIFIER
   read -rsp 'Senha: ' PLANTAOPRO_LOGIN_PASSWORD; echo
   export PLANTAOPRO_LOGIN_IDENTIFIER PLANTAOPRO_LOGIN_PASSWORD
   PLANTAOPRO_WEB_URL=http://localhost:52976 npm run diagnose:login
   unset PLANTAOPRO_LOGIN_IDENTIFIER PLANTAOPRO_LOGIN_PASSWORD
   dotnet build backend/PlantaoPro.sln
   dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj
   ```

7. Complete a matriz obrigatória com contas persistidas apropriadas, confirmando em cada sucesso: um POST, log correlacionado da consulta/hash, `Set-Cookie`, primeiro GET protegido, reload ainda autenticado e logout seguido de redirecionamento/negação. Para indisponibilidade e timeout, pare a API ou use um endpoint local controlado que exceda o timeout; confirme a mensagem de serviço, distinta da mensagem genérica de credenciais.
