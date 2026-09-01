# v2.13.0 — clique do login, design e fallback frontend

## Modo de execução

- Data: 2026-09-01.
- `dotnet` não está instalado (`dotnet: command not found`).
- Modo seguido: **B — ambiente sem .NET**.
- As alterações desta rodada ficaram limitadas a frontend/design/diagnóstico estático porque o SDK .NET não está disponível neste ambiente. A validação final deve ser feita na máquina Windows com SDK .NET 10.
- Nenhum controller, service, model, DTO, migration, SQL, projeto ou solução foi alterado.

## Fluxo encontrado

- Tela real: `PlantaoPro.Web/Views/Account/Login.cshtml`, layout `_AuthLayout.cshtml`.
- URL Web: `/Account/Login` no host iniciado por `PlantaoPro.Web` (o `launchSettings.json` da Web define essa rota inicial). O Swagger pertence à API e **não é a tela de login**.
- Formulário: POST MVC para `AccountController.Login`, protegido por antiforgery.
- Integração esperada, apenas inspecionada: a action Web chama `POST api/auth/login` usando o cliente `PlantaoProApi`.
- JavaScript da tela: `wwwroot/js/auth-login.js`.

## Causa provável e correção

A tela possuía dois controladores independentes de loading: o específico de autenticação e o global de `plantaopro-ui.js`. Ambos reagiam ao mesmo `submit`, alteravam `aria-busy`, desabilitavam o botão e substituíam seu conteúdo. Essa disputa torna o comportamento dependente da ordem de registro dos listeners e pode fazer o fluxo específico interpretar o formulário como já enviado, cancelar um evento ou deixar o botão travado.

A correção:

1. declara explicitamente `asp-controller="Account"`, `asp-action="Login"`, `method="post"` e mantém `type="submit"`;
2. marca o login com `data-submit-loading="manual"`;
3. faz o controlador global ignorar formulários com loading manual;
4. mantém um único controlador para o estado do botão, ativado somente no evento `submit` válido;
5. registra `data-request-started="true"` apenas quando o POST nativo vai começar;
6. restaura botão/spinner no `pageshow`, em validação inválida e após o limite defensivo de 15 segundos;
7. mantém mensagem visível e foco no resumo quando a validação bloqueia o envio.

## Design e experiência

- Composição SaaS responsiva com painel institucional, contraste reforçado e escala tipográfica contida.
- Título e orientação de acesso revisados.
- Label visível “E-mail”, sem usar placeholder como label. CPF/CNPJ não foram anunciados porque o contrato atual valida endereço de e-mail.
- Senha com mostrar/ocultar, aviso de Caps Lock, autocomplete adequado e ajuda contextual.
- Botão primário com loading acessível e sem competição entre scripts.
- Bloco compacto “Como acessar com segurança”, recuperação de senha e mensagem de erro no topo.
- Camada visual comum `v2130-forms.css`: labels, foco, obrigatoriedade, validações por campo, texto auxiliar, botões e preferência de movimento reduzido.

## Formulários revisados estaticamente

Foram localizados/revisados os fluxos frontend de usuários/segurança, clientes/tenants, perfis, médicos, hospitais/unidades, escalas, plantões, financeiro e parametrizações. Nesta rodada sem SDK, a mudança transversal ficou restrita à folha visual comum carregada pelos layouts; não foram alterados bindings Razor ou contratos desses módulos. A única página funcional tocada foi o login e ela contém a ajuda compacta “Como acessar com segurança” (equivalente contextual a “Como usar esta tela”).

## Diagnóstico no navegador

Foi criado `npm run diagnose:login`, que abre a **Web**, preenche o formulário com credenciais fornecidas por variáveis de ambiente, captura erros de console e respostas POST, informa status HTTP e falha se não encontrar `POST /Account/Login`, se o botão permanecer bloqueado ou se houver erro no console.

Na máquina Windows:

```powershell
$env:PLANTAOPRO_WEB_URL = "http://localhost:<porta-web>"
$env:PLANTAOPRO_LOGIN_IDENTIFIER = "<usuario-de-homologacao>"
$env:PLANTAOPRO_LOGIN_PASSWORD = "<senha-de-homologacao>"
npm run diagnose:login
```

Não use `http://localhost:51976/swagger`: essa é a API. Use a porta indicada no console de **PlantaoPro.Web** e abra `/Account/Login`.

Validação manual alternativa:

1. Abrir a Web em `/Account/Login` e DevTools > Network.
2. Marcar **Preserve log** e limpar a lista.
3. Preencher os campos e clicar em **Entrar com segurança**.
4. Confirmar um POST para `/Account/Login` e anotar o status HTTP.
5. Se não houver POST, inspecionar validação/console frontend.
6. Se houver POST 400/401/403/404/500, seguir a integração Web/API conforme o status.
7. Confirmar que, ao retornar erro, o botão está habilitado e a mensagem aparece no topo.

## Comandos e resultados

- `pwd`: `/workspace/plantaopro`.
- `git status --short --branch`: branch inicial `work`, limpa.
- `git remote -v`: nenhum remote configurado no checkout.
- `which dotnet`, `dotnet --info`, `dotnet --list-sdks`: SDK ausente.
- Buscas `rg` e `find` solicitadas: login, handlers, views/pages/wwwroot e endpoint foram localizados.
- Validações estáticas finais e `git diff --check`: registradas no fechamento desta rodada.

## Limitações restantes

- Build, testes C# e integração real Web → API não puderam ser executados sem SDK.
- O diagnóstico Playwright real exige que Web e API estejam iniciadas e credenciais válidas de homologação sejam fornecidas; nenhuma credencial, segredo ou mock foi incluído.
- Na validação Windows, executar `dotnet clean`, `restore`, builds Debug/Release e testes conforme o checklist do projeto.
