# v2.12.7 — correção do login e redesign premium

## Modo de execução

**MODO LOGIN ESTÁTICO.** O executável `dotnet` não está instalado no ambiente. Conforme a restrição da rodada, nenhum arquivo C#, contrato, banco, migration, projeto ou solução foi alterado.

## Diagnóstico e causa

O fluxo real usa um POST MVC tradicional em `Views/Account/Login.cshtml`, protegido por antiforgery, e o feedback de envio fica em `wwwroot/js/auth-login.js`. O script desabilitava o botão e mudava seu rótulo assim que o formulário era submetido, mas não possuía rotina única de restauração, tratamento de retorno pelo bfcache nem limite de recuperação caso a navegação fosse interrompida. Assim, uma validação tardia, uma navegação cancelada ou a restauração da página podia manter o controle desabilitado indefinidamente.

Também foram revisados estaticamente `Controllers/AccountController.cs`, `Models/ViewModels.cs`, `Views/Shared/_AuthLayout.cshtml` e os estilos de autenticação já consolidados. O backend existente faz autenticação real via API, valida `ModelState`, cria identidade/claims, preserva escopo global ou de tenant e rejeita conta que exige tenant sem contexto. Ele não foi modificado porque o SDK está ausente.

## Correção aplicada

- O POST continua tradicional; não foi introduzido AJAX, mock ou desvio de autenticação.
- O estado de processamento só começa após a validação nativa do formulário.
- Uma função idempotente restaura texto, spinner, `disabled` e `aria-busy` em falha de validação, retorno pelo evento `pageshow`, erro renderizado pelo servidor e após 15 segundos sem navegação.
- Submissões simultâneas continuam bloqueadas enquanto o estado está ocupado.
- A recuperação por demora informa, via região `aria-live`, que o usuário pode tentar novamente.
- O resumo de validação recebe mensagem humana, foco programático e anúncio assertivo.

## Validação, segurança e multi-cliente

A tela mantém e-mail individual e senha, antiforgery, `autocomplete`, validação por campo e rota real de recuperação de senha. O CNPJ não foi criado como login compartilhado: o texto esclarece que a instituição apenas determina o contexto. A resolução de perfil, permissões, Super Admin e tenant permanece integralmente no fluxo real existente do servidor. Nenhuma credencial, token, senha fixa, bypass ou dado de demonstração foi adicionado.

As respostas de credencial inválida, usuário bloqueado e tenant ausente continuam sendo produzidas pelo controller/API existentes e retornam em resumo acessível. Como o backend não pôde ser executado, os cenários de cliente ou módulo bloqueado não foram alterados nem simulados.

## Design e acessibilidade

Foi criada uma camada CSS isolada para v2.12.7, carregada por último no layout de autenticação. Ela equilibra as duas colunas, reduz e limita o título, clareia o card, alinha campos e ações, organiza erros e ajuda, remove clipping/scroll forçado e oferece layouts específicos para tablet, 360 px e desktop. A composição inclui marca MNSOFT, três diferenciais, ajuda recolhível, versão e rota real de privacidade.

Labels visíveis, foco de alto contraste, navegação por teclado, mostrar/ocultar senha com estado acessível, aviso de Caps Lock, `aria-live`, `aria-atomic` e `aria-busy` foram preservados ou reforçados.

## Comandos e verificações

Foram executados os comandos de diagnóstico (`pwd`, status/branch/remotes, localização do SDK, buscas `find`/`rg`), inspeção estática do fluxo, `git diff --check`, varredura de padrões proibidos e os scripts de segurança/compatibilidade disponíveis. Também foi feita validação sintática do JavaScript com Node quando disponível.

## Limitações reais

- Build, restore, testes .NET, inicialização da aplicação e autenticação manual não puderam ser realizados porque `dotnet` não existe no ambiente.
- Sem aplicação executável, não foi possível produzir screenshot real no navegador nem validar credenciais, redirects, menus, auditoria ou bloqueios contra uma API ativa.
- A correção não modifica backend C#, conforme exigido para o modo estático.
