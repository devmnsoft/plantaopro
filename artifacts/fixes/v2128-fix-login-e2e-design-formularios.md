# v2.12.8 — Login E2E, design e formulários

## Resultado da rodada

**Modo de execução: MODO ESTÁTICO.** O executável `dotnet` não está instalado no
ambiente desta rodada (`dotnet: command not found`). Conforme a regra de segurança
da tarefa, nenhum arquivo de backend ou frontend foi alterado sem a possibilidade
de compilar e executar o fluxo real.

Consequentemente, esta rodada **não declara o login corrigido nem homologado**. A
mudança entregue é este registro diagnóstico, que evita produzir uma correção ou
um PR vazio sem evidência e deixa explícito o bloqueio para a próxima execução em
um ambiente completo.

## Diagnóstico estático

- Existem aplicações distintas. A API usa `http://localhost:51976` e abre o
  Swagger; a Web usa `http://localhost:52976` e abre `Account/Login`.
- Portanto, abrir apenas `http://localhost:51976/swagger/index.html` não abre o
  sistema Web e não pode gerar o `POST` MVC de login. A URL de entrada esperada em
  Development é `http://localhost:52976/Account/Login`.
- A configuração Development da Web aponta o cliente HTTP para
  `http://localhost:51976`, compatível com o perfil HTTP da API. A configuração
  base aponta para HTTPS (`https://localhost:51977`); o ambiente deve ser
  `Development` ou deve fornecer `ApiSettings__BaseUrl`/`PlantaoProApi__BaseUrl`
  coerente com a URL efetivamente publicada.
- A tela contém um formulário MVC tradicional com antiforgery e
  `method="post"`. O controller Web envia `POST api/auth/login` à API, cria o
  principal/cookie e a sessão JWT e aplica redirect local ou por perfil.
- O JavaScript atual não usa `fetch`, bloqueia submissão duplicada somente depois
  de a validação nativa passar e restaura o botão em validação, `pageshow` ou após
  15 segundos. Isso reduz o risco do estado preso, mas só um teste em navegador
  pode comprová-lo.
- A API possui endpoint `POST /api/auth/login`; o serviço consulta PostgreSQL,
  valida o hash e carrega perfis/escopo. Há seed de Development com BCrypt e
  inserções idempotentes. Sua execução e as contas resultantes não foram
  verificadas nesta rodada.
- Há cookie Web chamado `PlantaoPro.Auth`, com `HttpOnly`, `SameSite=Lax` e
  `SecurePolicy=SameAsRequest`. A chamada Web → API é server-side; assim, CORS não
  participa do POST MVC descrito acima.

## Causa real

Os dados disponíveis comprovam uma diferença operacional importante: a evidência
relatada observa somente o processo da API/Swagger, enquanto a tela e o POST de
login pertencem ao processo Web, em outra porta. Isso explica a ausência do POST
de login nos logs apresentados.

Não foi possível afirmar se existe uma segunda falha em banco, seed, hash,
permissões, cookie ou redirect, pois a falta do SDK impede iniciar os dois
processos e reproduzir Web → API → PostgreSQL → cookie → redirect → menus. Tratar
qualquer dessas hipóteses como causa confirmada seria produzir evidência falsa.

## Credenciais e seed de desenvolvimento

Nenhuma credencial foi criada, alterada ou exposta. Antes de homologar, configure
as variáveis seguras esperadas pelo ambiente de Development e confirme a rotina
de seed existente. A senha nunca deve ser registrada em log, documentação ou
controle de versão; somente seu hash BCrypt deve persistir no banco.

O acesso global deve permanecer restrito ao perfil global/Super Admin. Usuários de
cliente devem possuir tenant válido e acessar somente esse escopo. Esses contratos
foram identificados estaticamente, mas não foram validados contra uma instância
real do PostgreSQL.

## Design, formulários e mensagens

Nenhuma alteração visual foi feita em MODO ESTÁTICO. A tela encontrada já inclui
logo PlantãoPro/MNSOFT, formulário com labels e validação, mostrar/ocultar senha,
aviso de Caps Lock, recuperação, ajuda recolhível, privacidade, mensagem de erro e
estado de carregamento. Os componentes compartilhados incluem estados vazios e de
erro, toast e modal de confirmação. Eles precisam ser revisados visualmente e com
tecnologia assistiva em uma execução completa antes de qualquer refinamento.

## Comandos executados

```bash
pwd
git status --short --branch
git branch --show-current
git remote -v || true
which dotnet || true
dotnet --info || true
dotnet --list-sdks || true
find .. -name AGENTS.md -print
find backend -maxdepth 4 \( -name '*.sln' -o -name '*.csproj' \) -print
find backend -maxdepth 5 -type f \( -name 'launchSettings.json' -o -name 'appsettings*.json' \) -print
rg -n "applicationUrl|launchUrl|ASPNETCORE_URLS|localhost|ApiBaseUrl|BaseAddress|PlantaoPro.Api|PlantaoPro.Web" backend/PlantaoPro.Api backend/PlantaoPro.Web
rg -l "Login|Entrar|SignIn|Authenticate|Logout|Processando|Caps Lock|PasswordHash|SuperAdmin" backend/PlantaoPro.Web backend/PlantaoPro.Api backend/PlantaoPro.Tests
rg -n -C 5 "PlantaoProApi|AddHttpClient|AddCookie|LoginPath|UseAuthentication|UseAuthorization" backend/PlantaoPro.Web/Program.cs
rg -n -C 6 "PLANTAOPRO_SUPERADMIN|SUPERADMIN|SuperAdmin|HashPassword|senha_hash|LoginAsync" backend/PlantaoPro.Api/DevelopmentSeed.cs backend/PlantaoPro.Api/Data.cs backend/PlantaoPro.Api/appsettings.Development.json
```

## Testes

Os comandos obrigatórios abaixo não puderam ser executados porque `dotnet` não
existe no ambiente:

```bash
dotnet restore backend/PlantaoPro.sln
dotnet build backend/PlantaoPro.Api/PlantaoPro.Api.csproj -c Debug
dotnet build backend/PlantaoPro.sln -c Debug --no-restore
dotnet build backend/PlantaoPro.sln -c Release --no-restore
dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release --no-build
```

Também não foram executados testes E2E, screenshot ou validação PostgreSQL, pois
eles exigem aplicações compiladas e em execução. Artefatos E2E antigos presentes
no repositório não foram reutilizados como prova desta versão.

## Próxima execução obrigatória

1. Disponibilizar o SDK .NET compatível e PostgreSQL com o schema do projeto.
2. Configurar segredos exclusivamente por variáveis de ambiente.
3. Iniciar API em `http://localhost:51976` e Web em
   `http://localhost:52976` com `ASPNETCORE_ENVIRONMENT=Development`.
4. Abrir a URL da Web, confirmar `GET /Account/Login`, submeter vazio, credencial
   inválida e contas válidas global/tenant, acompanhando os logs dos dois
   processos.
5. Verificar banco, hash, bloqueios, perfis e tenant; inspecionar `Set-Cookie`,
   redirect, menus, área protegida e logout.
6. Executar testes automatizados e navegador real, incluindo restauração do botão
   em todos os caminhos de erro, responsividade e acessibilidade.
7. Só então implementar e fotografar os refinamentos visuais necessários.

## Limitações restantes

- Login real, seed, PostgreSQL, claims, cookie/JWT, redirect, menus, isolamento de
  tenant e logout: **não homologados**.
- Builds e testes: **bloqueados pela ausência do SDK**.
- Revisão visual e screenshot: **bloqueados porque a Web não pode ser executada**.
- Remoto Git: nenhum remoto está configurado neste checkout; fetch, push e abertura
  remota do PR dependem da infraestrutura externa.
