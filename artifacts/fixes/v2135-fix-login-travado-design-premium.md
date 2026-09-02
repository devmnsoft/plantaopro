# v2.13.5 — login travado e design premium

Data da inspeção: 2026-09-02.

## Resultado e bloqueio do ambiente

Esta rodada foi executada no contêiner Linux disponível em `/workspace/plantaopro`, e não no ambiente Windows recomendado (`C:\MNSOFT\PlantaoPro`). O executável `dotnet` não está instalado: tanto `dotnet --info` quanto `dotnet --list-sdks` terminaram com `dotnet: command not found`.

Por determinação expressa da tarefa ("se `dotnet` não estiver disponível, não alterar backend"), nenhum arquivo sob `backend/` foi modificado. Isso também impede afirmar que a falha foi definitivamente corrigida: sem iniciar Web, API e banco não é possível executar o clique real, observar o Network do navegador, colher os logs dos dois servidores, validar a credencial ou comprovar cookie/JWT e redirect. Swagger `200` não foi tratado como evidência de autenticação.

## URLs e topologia identificadas estaticamente

Os perfis HTTP de `launchSettings.json` definem:

- **API:** `http://localhost:51976` (Swagger em `http://localhost:51976/swagger`);
- **Web:** `http://localhost:52976`;
- **login que deve ser aberto:** `http://localhost:52976/Account/Login`.

Os perfis HTTPS são, respectivamente, `https://localhost:51977` e `https://localhost:52977`. API e Web são projetos separados e devem permanecer em terminais separados. Abrir `http://localhost:51976/swagger` abre somente a documentação da API; não abre nem valida o login da aplicação Web.

Em Development, a configuração Web aponta o cliente HTTP para `http://localhost:51976`. Portanto, a validação local deve iniciar primeiro a API e depois a Web, sem confundir as portas.

## Inspeção estática do fluxo existente

A inspeção estática encontrou o seguinte encadeamento já presente na revisão de origem:

1. `Views/Account/Login.cshtml` contém um formulário MVC para `Account/Login`, com `method="post"`, token antiforgery e botão `type="submit"`.
2. `auth-login.js` não cancela um formulário válido; ele ativa o loading e permite o POST tradicional. Para submissão inválida, mostra resumo visível. Um temporizador de 15 segundos e o evento `pageshow` reabilitam o botão e removem o estado de processamento.
3. `AccountController.Login` registra o início do POST Web e chama `POST api/auth/login` por `PlantaoProApi`.
4. `AuthController.Login` chama o serviço de autenticação, registra auditoria/log e devolve o status produzido pelo serviço.
5. Em sucesso, o controller Web normaliza perfis e contexto, cria a identidade/cookie, guarda o JWT na sessão e executa redirect local ou por perfil.

Esses achados demonstram apenas a intenção implementada no código; **não são evidência de execução**. Sem runtime não houve POST observado no Network, POST observado no log, status HTTP antes/depois, acesso ao banco, emissão de cookie/JWT ou redirect comprovado. Por isso, não foi atribuída uma causa raiz sem evidência e não foi mascarado o bloqueio como sucesso.

## Super Admin, tenant e identificadores

- Não foi encontrada no código inspecionado uma credencial fixa para `comercial@mnsoft.com.br` ou `18160057000113`.
- O seed de desenvolvimento exige habilitação por configuração e senha externa ao Git; esta decisão evita gravar a senha inicial no repositório. Sua execução, idempotência, hash e auditoria não puderam ser verificadas contra um banco real.
- Claims de perfil, escopo e `tenant_id` existem no fluxo Web, mas isolamento multi-tenant e autorização efetiva precisam de testes de integração com banco e perfis reais.
- O contrato atual de login é baseado em `Email` e a tela informa somente e-mail. CPF/CNPJ não deve ser anunciado antes de o backend, schema e testes reais suportarem normalização e consulta segura.

## Interface encontrada

A tela existente já apresenta composição PlantãoPro/MNSOFT, labels visíveis, CTA forte, spinner, resumo de erros acessível, aviso de Caps Lock, mostrar/ocultar senha, recuperação de senha, ajuda de acesso seguro e estilos responsivos. Os formulários do sistema não foram alterados porque estão sob `backend/` e o SDK obrigatório está ausente. Uma avaliação visual navegada também não foi possível sem iniciar a aplicação; consequentemente, não há screenshot desta rodada.

## Comandos executados e resultados

### Executados

- `git status --short --branch` — executado; árvore inicialmente limpa na branch `work`.
- `git remote -v || true` — executado; nenhum remote foi exibido no contêiner.
- `dotnet --info` — falhou: `dotnet: command not found`.
- `dotnet --list-sdks` — falhou: `dotnet: command not found`.
- `find backend -maxdepth 5 -name "launchSettings.json" -print` — encontrou configurações da API e Web.
- `rg -n "applicationUrl|localhost|PlantaoPro.Api|PlantaoPro.Web|Swagger|Login|Auth|Account|SignIn" backend` — executado para mapear URLs e fluxo.
- buscas focadas por `PlantaoProApi`, seed, Super Admin e padrões proibidos — executadas estaticamente.

### Não executados por indisponibilidade do SDK/runtime

- restore, clean, builds Debug/Release e `dotnet test`;
- `dotnet run` da API e da Web;
- teste E2E/Playwright contra a aplicação real;
- conexão e consulta ao PostgreSQL;
- captura de Network/log/status/cookie/JWT/redirect.

## Roteiro obrigatório para desbloqueio no Windows

No host com .NET 10 e PostgreSQL configurado, executar em `C:\MNSOFT\PlantaoPro`:

```powershell
dotnet --info
dotnet --list-sdks
dotnet restore backend/PlantaoPro.sln
dotnet build backend/PlantaoPro.sln -c Debug --no-restore
dotnet run --project backend/PlantaoPro.Api/PlantaoPro.Api.csproj
```

Em outro terminal:

```powershell
dotnet run --project backend/PlantaoPro.Web/PlantaoPro.Web.csproj
```

Abrir `http://localhost:52976/Account/Login`, preencher uma conta real e manter o DevTools em **Network** com Preserve log. O aceite exige registrar: POST Web para `/Account/Login`, chamada Web → API para `/api/auth/login` no log, status HTTP, acesso ao banco, cookie emitido, JWT armazenado no servidor, redirect final e retorno do botão ao estado normal em qualquer falha. Repetir com login inválido, Super Admin, administrador de tenant e usuário comum.

Depois, executar integralmente os comandos da Fase 12. Qualquer correção no backend ou redesign adicional somente deve ser realizado após essa reprodução, preservando a causa e os status reais nos logs.

## Limitações restantes

Todos os critérios dependentes de runtime permanecem **não comprovados**, inclusive login válido, mensagem de login inválido em execução, Super Admin global, isolamento de tenant, menus/permissões, banco, cookie/JWT, redirect, builds e testes. A varredura textual também encontrou ocorrências do padrão proibido em fixtures/testes e documentação; elas não foram modificadas devido à proibição de alterar `backend/` sem SDK e porque parte das ocorrências representa assertions ou exemplos, não comportamento Web ativo.
