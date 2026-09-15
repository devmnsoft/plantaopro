# Acesso real, portal, perfis e design — auditoria e correções

## Escopo e evidência inicial

- Repositório: `devmnsoft/plantaopro`.
- Branch recebida: `work`.
- Commit inicial preservado: `ce476a48e945b731b8aa395a0146add05bec2e2c`.
- A árvore estava limpa no início desta rodada.
- Os registros informados mostram a API em HTTP `51976`/HTTPS `51977`, Swagger disponível e uma resposta `200` de recuperação. Eles **não contêm um POST de login**. Recuperação e login são ações independentes; o `200` genérico de recuperação não prova conta existente, envio de e-mail ou alteração de senha.

## Fluxo identificado

1. `Views/Account/Login.cshtml` renderiza um único formulário `POST /Account/Login`, com antiforgery e campos `Email` e `Senha`. `auth-login.js` usa submissão nativa, bloqueia repetição apenas depois da validação, restaura a interface em uma nova resposta/`pageshow` e mantém mostrar-senha como `type="button"`.
2. `AccountController.Login` preserva apenas `returnUrl` local e usa o `HttpClient` nomeado `PlantaoProApi`, com base em `ApiSettings:BaseUrl`/`PlantaoProApi:BaseUrl` e timeout configurável, para chamar `POST api/auth/login`.
3. `AuthController.Login` chama `AuthService.LoginAsync`. Este abre `ConnectionStrings:Default`, consulta `plantaopro.usuarios` por identificador normalizado, lê `senha_hash` e valida a senha original com `BCrypt.Verify`. Não há comparação de senha em texto puro nem sucesso alternativo por ambiente.
4. Após identidade/status válidos, o serviço lê `usuarios_perfis`, permissões efetivas e `tenant_modulos`, emite JWT, registra tentativa/auditoria e persiste a sessão da API. A Web cria o cookie HTTP-only `PlantaoPro.Auth` por oito horas, adiciona claims do catálogo efetivo e somente então redireciona.
5. O middleware Web está na ordem `UseSession` → `UseAuthentication` → `UseAuthorization`. A API valida JWT e a sessão persistida. O filtro Web `SaasRouteGuardFilter` exige módulo contratado e permissão para controllers catalogados; esconder menus não é a barreira de autorização.
6. `AccountController.Logout` encerra o cookie e limpa a sessão antes de voltar ao login.

## Matriz curta

| Etapa | Implementação existente | Problema comprovado nesta revisão | Correção | Teste/evidência |
|---|---|---|---|---|
| Clique/submit | POST MVC nativo, antiforgery, validação e bloqueio de duplicidade | Os logs fornecidos não exercitam este caminho; não se comprovou chamada indevida à recuperação | Fluxo único preservado | Contratos existentes e inspeção; navegador indisponível |
| Web → API | `HttpClient` nomeado com timeout e cancelamento | Mensagem pública orientava verificar “backend”, detalhe operacional inadequado | Mensagem pública neutra de indisponibilidade | Novo contrato estático; build pendente |
| PostgreSQL/senha | Dapper parametrizado + hash BCrypt persistido | Nenhuma senha fixa/fallback encontrada no caminho canônico | Sem substituição do fluxo | Security scan encontrou somente configurações inseguras preexistentes; PostgreSQL indisponível |
| Cookie/contexto | Cookie criado após sucesso da API | Usuário autenticado com perfil que exige tenant, mas sem tenant, era devolvido ao login como se as credenciais falhassem | Cookie é mantido e o usuário vai para página autenticada explicativa, com logout | Novo contrato estático; runtime pendente |
| Destino/autorização | Prioridade de perfil, `returnUrl` local, claims, guard de módulo/permissão | Seleção de múltiplas organizações não está integrada ao login canônico | Não ampliado sem teste/runtime; limitação registrada | Inspeção de `usuario_tenant_acessos` e fluxo atual |
| Design | Layout responsivo, labels, autocomplete, foco, Caps Lock detectado e senha revelável | Título do formulário repetia o título promocional, contra o texto solicitado | Título reduzido para “Acesse sua conta” e apoio objetivo | `check-form-experience.py` aprovado |
| Logout | Sign-out do cookie + limpeza da sessão | Execução navegada não disponível | Sem alteração | Revisão estática; E2E pendente |

## Alterações desta rodada

- `backend/PlantaoPro.Web/Controllers/AccountController.cs`: mantém uma sessão validada quando falta contexto elegível; encaminha a uma página sem loop; não expõe orientação interna na falha de conexão.
- `backend/PlantaoPro.Web/Views/Account/Login.cshtml`: título curto solicitado e texto de apoio objetivo.
- `backend/PlantaoPro.Web/Views/Account/NoEligibleContext.cshtml`: estado autenticado sem organização, orientação e saída clara.
- `backend/PlantaoPro.Tests/V2164CompilationAndJourneyContractTests.cs`: contratos para o destino autenticado sem tenant, logout disponível, título e mensagem pública.
- Este relatório.

## Configuração e provisionamento local seguro

Inicie API e Web em processos separados. Para o perfil HTTP versionado, a API usa `http://localhost:51976` e a Web `http://localhost:52976`; configure a URL efetiva sem desativar TLS ou validação de certificado. Forneça segredos por variáveis de ambiente, user-secrets ou arquivo local ignorado:

```bash
ConnectionStrings__Default='<conexao-postgresql-local>' \
Jwt__Key='<chave-local-forte>' Jwt__Issuer=PlantaoPro Jwt__Audience=PlantaoPro \
dotnet run --project backend/PlantaoPro.Api --launch-profile http

PlantaoProApi__BaseUrl='http://localhost:51976/' \
dotnet run --project backend/PlantaoPro.Web --launch-profile http
```

Para criar contas locais, use apenas o provisionamento administrativo explícito existente, restrito a Development. As entradas abaixo devem vir do ambiente e não devem ser commitadas:

```bash
ASPNETCORE_ENVIRONMENT=Development DemoSeed__Enabled=true \
DemoSeed__DevelopmentDatabase='<nome-do-banco-descartavel>' \
DemoSeed__SuperAdminPassword='<entrada-local>' \
DemoSeed__ManagerPassword='<entrada-local>' \
ConnectionStrings__Default='<conexao-postgresql-local>' \
dotnet run --project backend/PlantaoPro.Api -- --provision-demo
```

O provisionamento comum preserva hashes existentes. Para redefinir explicitamente apenas as contas demonstrativas `.example`, use as mesmas entradas e acrescente `--reset-demo-passwords`; isso revoga as sessões e não acontece no startup normal. Não se afirma que qualquer credencial antiga permanece válida.

## Regras efetivas observadas

- Identidade requer `reg_status='A'` e `status='ATIVO'`; bloqueio temporário de login é independente do status do cliente.
- Perfil/vínculo requer registros ativos; permissões especiais negativas prevalecem sobre concessões.
- Usuário tenant recebe somente módulos ativos e habilitados em `tenant_modulos`; o filtro Web volta a validar módulo e ação.
- Administrador global recebe escopo global e catálogo curinga, chegando à central `AdminSaas`; contexto de tenant precisa continuar sendo uma operação explícita, auditada e revalidada no servidor.
- Usuário tenant com contexto único segue ao portal correspondente. Sem contexto elegível, permanece autenticado na página explicativa e pode sair, sem ciclo de login.
- `returnUrl` só é usado quando `Url.IsLocalUrl` o aceita.

## Verificações executadas

| Comando | Resultado |
|---|---|
| `python3 scripts/check-csharp10-compatibility.py` | Aprovado |
| `python3 scripts/check-form-experience.py` | Aprovado |
| `python3 -m unittest discover -s scripts/tests -p 'test_*.py'` | Aprovado: 11 testes |
| `git diff --check` | Aprovado |
| `python3 scripts/repository-security-check.py` | Falhou: sinalizou connection strings/configurações inseguras já presentes em `appsettings.json`; não foram introduzidas nesta rodada |
| `dotnet restore backend/PlantaoPro.sln` | Não executado: comando `dotnet` ausente |
| builds Debug/Release e `dotnet test` | Não executados: comando `dotnet` ausente |
| PostgreSQL isolado e jornada navegada | Não executados: `psql`, Docker e runtime .NET ausentes |

## Limitações e conclusão honesta

Não foi possível autenticar uma conta persistida, observar Network/Console, validar cookie após refresh, abrir páginas protegidas e confirmar logout no navegador desta máquina. Consequentemente, esta entrega **não declara o login homologado/corrigido em runtime** e nenhuma screenshot artificial foi produzida.

O schema possui `usuario_tenant_acessos`, mas o login canônico ainda parte do tenant singular da identidade. Os cenários de dois clientes, seleção/revalidação de tenant e administrador global entrando/saindo do contexto de cliente precisam de uma rodada integrada com PostgreSQL e navegador; não seria seguro simular essa garantia apenas na interface. Também permanecem pendentes os cenários runtime de identidade/vínculo bloqueados, módulo não contratado, perfil sem permissão, API indisponível/timeout, refresh e logout.

Antes de homologar, execute o provisionamento em banco descartável e a sequência real para superadministrador e administrador de cliente: login → Network `POST /Account/Login` e API `POST /api/auth/login` → página protegida → refresh → logout → tentativa anônima bloqueada. Em seguida cubra os casos negativos e multiempresa sem mocks de autenticação.
