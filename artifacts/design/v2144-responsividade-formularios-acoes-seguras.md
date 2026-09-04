# PlantãoPro v2.14.4 — responsividade, formulários e ações seguras

## Escopo e premissas

Rodada de acabamento exclusivamente frontend (Razor, CSS e JavaScript). O SDK `dotnet` não está disponível no ambiente; por segurança, não foram alterados backend, banco, migrations, DTOs, regras de tenant, autorização, auditoria ou LGPD. Nenhum mock, credencial, secret, bypass ou arquivo binário foi adicionado.

## Telas e componentes auditados

- shell autenticado, sidebar, topbar, área de conteúdo e navegação móvel;
- login, recuperação visual de submissão, Caps Lock, mensagens de sessão/bloqueio e ajuda contextual;
- formulários e tabelas compartilhados, com amostragem das jornadas de médicos, hospitais e plantões;
- modal compartilhado de confirmação, overlay, toasts e formulários AJAX;
- cockpit Super Admin e visibilidade condicional do menu global.

## Alterações realizadas

### Responsividade em zoom de 100%

- Nova camada CSS v2.14.4 carregada nos layouts autenticado e público.
- Shell usa coluna lateral fluida e conteúdo com `minmax(0, 1fr)`, evitando que cards, grids e textos ampliem a viewport.
- Conteúdo ganhou limite consistente de 100 rem, espaçamento fluido e proteção global de mídia e textos.
- Topbar pode redistribuir ações e oculta apenas contextos secundários nos breakpoints menores.
- Tabelas compartilhadas têm scroll horizontal contido, área máxima previsível e rolagem suave em touch.
- Modais respeitam largura e altura útil da viewport; botões passam para uma coluna no mobile.
- Rodapés de formulários, barras de ação, cards e títulos quebram de forma controlada.

### Formulários e feedback

- Labels, ajuda, obrigatoriedade, erro por campo e sumário de validação receberam hierarquia visual comum.
- Formulários ficam limitados à área útil e campos não ultrapassam o container.
- Ações de formulário reorganizam-se em telas estreitas e preservam alvos de toque adequados.
- O fluxo AJAX mantém toast de sucesso/erro, erro geral junto ao formulário e sempre libera o estado ocupado no bloco `finally`.

### Pop-ups e ações seguras

- O modal compartilhado continua substituindo confirmações nativas para elementos `data-confirm`.
- Foi incluído confinamento de foco por teclado, foco inicial previsível, retorno ao acionador, fechamento por Escape e backdrop.
- O modal bloqueia rolagem de fundo, possui altura limitada à viewport e feedback acessível durante processamento.

### Login

- Mantidos identificador “E-mail, CPF ou CNPJ”, senha, aviso de Caps Lock, recuperação, mensagens de sessão expirada e bloqueios.
- Mantido o controle manual de submissão: validação ou falha de conexão libera imediatamente o botão; espera superior a 15 segundos também o restaura e preserva os campos.
- Composição foi reforçada para 100% de zoom e viewports estreitas.
- Identificação visual atualizada para v2.14.4 e bloco “Como usar esta tela” preservado.

### Super Admin, tenant e ícones

- O cockpit e o menu global foram auditados; a seção Modo Global MNSOFT continua condicionada ao papel `ADMINISTRADOR_GLOBAL`.
- As condições existentes para módulos operacionais, clínicos, financeiros e de gestão foram preservadas, mantendo a navegação coerente com as permissões.
- Ícones continuam usando o componente `<app-icon>` e os recursos vetoriais existentes; nenhum binário foi criado.

## Comandos e resultados

- `pwd`, `git status --short --branch` e `git remote -v || true`: repositório confirmado em `/workspace/plantaopro`, branch inicial `work`, sem remoto configurado.
- `dotnet --info || true` e `dotnet --list-sdks || true`: `dotnet` não encontrado.
- scans `rg` solicitados de largura, overflow, altura, interações e “Processando”: concluídos; serviram para priorizar shell, login e infraestrutura compartilhada.
- scan `rg` de seções Razor, Bootstrap, ARIA e eventos: concluído, com 418 referências revisáveis; os arquivos alterados não introduzem referência quebrada.
- `node --check` nos JavaScripts tocados/auditados: aprovado.
- `git diff --check`: aprovado.
- scans finais de padrões proibidos e secrets: executados; nenhum padrão proibido nos arquivos tocados e nenhum secret novo.

## Limitações reais

- Sem SDK `dotnet`, não foi possível executar clean, restore, build, testes automatizados ou iniciar a aplicação para captura de tela.
- Sem remoto Git configurado, não foi possível executar fetch, pull/rebase ou push contra `origin` neste ambiente.
- A validação desta rodada é estática; recomenda-se executar a solução em CI e realizar QA visual autenticado em 360 px, 768 px, 1280 px e 1920 px, todos com zoom de 100%.
