# PlantãoPro v2.14.1 — design premium e Cockpit MNSOFT

## Escopo entregue

### Telas e superfícies alteradas

- **Login:** composição visual, microcopy, identificação, mensagens contextuais, ajuda e comportamento de recuperação.
- **Admin SaaS / Cockpit MNSOFT:** banner global, busca explícita de cliente/tenant, visão executiva por fontes reais e atalhos de governança.
- **Shell visual:** inclusão do acabamento v2.14.1 nos layouts autenticado e de autenticação.
- **Mensagens:** toasts existentes também são renderizados no layout de autenticação, preservando os estados `Success`, `Error`, `Warning` e `Info`.

## Login crítico

- O formulário continua sendo um `POST` tradicional para `Account/Login`, com antiforgery; não foi criado fetch, bypass ou rota paralela.
- O rótulo visível passou a comunicar **E-mail, CPF ou CNPJ**, sem mostrar exemplos de documentos ou credenciais. A aceitação efetiva de CPF/CNPJ depende da validação do contrato de autenticação no backend; ela não pôde ser modificada nesta rodada sem SDK .NET.
- Senha mantém autocomplete adequado, alternância acessível de visibilidade e aviso de Caps Lock.
- O botão usa o texto direto **Entrar**, anuncia `aria-busy`, exibe progresso e volta ao estado normal após 15 segundos se a navegação não ocorrer.
- Perda de conexão durante o envio agora recupera o botão, move foco para uma mensagem humana e permite nova tentativa.
- O parâmetro `reason` pode apresentar mensagens não técnicas para sessão expirada, tenant ausente, usuário/cliente/funcionalidade bloqueados e acesso negado. Não revela se um identificador existe.
- Erros do ModelState permanecem próximos aos campos e no resumo; avisos e mensagens do servidor usam regiões vivas/toasts, sem `alert()`.
- O disclosure **Como usar esta tela** explica campos obrigatórios, destino por perfil e quando procurar a administração.

## Cockpit do Super Administrador

- O banner persistente identifica o contexto global e reforça que consultar ou atuar em outro tenant depende de seleção explícita, autorização no servidor e auditoria.
- A busca de cliente possui label, método GET e rota real para a fonte de clientes. Pesquisar não simula troca de contexto.
- Seis cartões executivos cobrem clientes ativos/bloqueados, usuários por cliente, módulos/funcionalidades, cobranças pendentes/vencidas, saúde operacional e eventos de auditoria.
- Os cartões exibem **Consultar**, **Ver saúde** ou **Ver trilha** em vez de números fictícios. Cada um leva a uma fonte real já protegida pelas autorizações existentes.
- A central já existente continua oferecendo contrato/plano, configuração, usuários/perfis, LGPD, relatórios e estados indisponíveis explícitos. Nenhuma tela CRUD ou dado inventado foi adicionado.
- A experiência responde em três, duas ou uma coluna e mantém foco visível e preferência de movimento reduzido.

## Formulários e mensagens

- Labels permanecem visíveis e obrigatórios recebem marca visual sem depender de placeholder.
- A busca de tenant usa relacionamento por pesquisa, nunca solicita um ID manual e informa que não altera contexto automaticamente.
- Erros de campo, falha de comunicação, demora e perda de conexão têm tratamentos inline recuperáveis.
- O padrão compartilhado de toast cobre sucesso, erro, atenção e informação. Estados específicos de sessão/acesso usam banner contextual no login.
- Não há `href="#"`, `alert()`, `confirm()` ou `prompt()` nos arquivos alterados.

## Visibilidade por perfil

- **Administrador Global MNSOFT:** acessa a central global, clientes, módulos, registros, faturamento e auditoria pelas rotas autorizadas; qualquer contexto assistido deve permanecer explícito e auditável.
- **Admin do cliente:** permanece direcionado ao portal do próprio cliente, onde administra os usuários e perfis permitidos pelo servidor; não recebe navegação para o Cockpit MNSOFT.
- **Usuário comum:** continua vendo somente módulos autorizados pelas claims, policies e tenant efetivo.
- **Suporte/Auditoria:** a autorização atual da rota Admin SaaS também admite esses papéis. A interface não eleva privilégios e toda capacidade efetiva continua dependente do servidor.

## Decisões de design e segurança

1. Não apresentar KPIs falsos na ausência de um view model agregado.
2. Não implementar troca de tenant somente no browser: a busca leva à fonte protegida, e a mudança real continua responsabilidade do fluxo auditado.
3. Diferenciar autenticação Web (cookie/sessão) da API (JWT armazenado na sessão) sem expor token ou detalhes técnicos ao usuário.
4. Tratar o loading do login como estado temporário e recuperável, não como confirmação de autenticação.
5. Reutilizar rotas, policies, claims e componentes de mensagem existentes; nenhuma autorização visual substitui a validação do servidor.

## Limitações reais

- O executável `dotnet` não está no `PATH`; por isso nenhum arquivo C#, banco, DTO, service, controller, `.csproj`, `.sln`, `TargetFramework`, `Directory.Build.props` ou `global.json` foi alterado.
- Build e testes dependem do SDK .NET 10 no `PATH` e não puderam ser executados.
- O backend Web atual valida a propriedade de login como endereço de e-mail. O rótulo multipropósito solicitado está pronto, porém CPF/CNPJ somente poderá autenticar após validação backend/API em uma rodada com SDK disponível.
- Não há agregado frontend disponível com totais globais. O cockpit oferece navegação honesta para as fontes em vez de inventar valores.
- Não foi possível iniciar a aplicação para captura visual porque a execução requer `dotnet`; a tentativa de preview estático com Playwright também encontrou o pacote sem o executável Chromium instalado. A responsividade foi revisada estaticamente nas media queries.
- A busca global encontrou 14 ocorrências de `Processando`/`Carregando` fora dos arquivos tocados, usadas principalmente como estados transitórios. Elas devem ser auditadas funcionalmente em rodada futura; nenhuma ocorrência proibida existe no patch desta rodada.
- A busca de segredos encontrou apenas exemplos parametrizados e credenciais padrão de testes já existentes; nenhum segredo foi adicionado.

## Comandos e resultados

| Comando | Resultado |
|---|---|
| `pwd` | `/workspace/plantaopro` |
| `git status --short --branch` | branch de trabalho inicialmente limpa |
| `git remote -v` | executado; nenhum remote configurado no contêiner |
| `dotnet --info` | indisponível: `dotnet: command not found` |
| `dotnet --list-sdks` | indisponível: `dotnet: command not found` |
| `find . -maxdepth 4 (...) -print` | solução e projetos identificados sob `backend/`; `Directory.Build.props` identificado |
| buscas `rg` de autenticação, tenant, Super Admin e JavaScript | executadas; fluxo Web POST, cookie, JWT em sessão, claims, tenants, menus e guards localizados |
| `git diff --check` | aprovado |
| busca global de padrões proibidos/loading | 14 ocorrências fora do patch, documentadas; zero nos arquivos alterados |
| busca `TODO/FIXME/HACK/console.log/debugger` | 10 correspondências, todas falso positivo do termo de perfil `TODOS`; nenhum marcador real encontrado |
| busca de roles/tenant/perfil | 1.865 correspondências revisáveis, confirmando ampla infraestrutura de autorização |
| `python3 scripts/repository-security-check.py` | aprovado: `repository-security ok` |
| `python3 scripts/check-csharp10-compatibility.py` | aprovado |
| `python3 scripts/validate-scrpt-completo.py` | aprovado: cobertura 100% |
| busca de padrões de segredo solicitada | somente templates parametrizados/documentação e defaults de teste preexistentes |

## Próxima validação com SDK .NET 10

Executar clean, restore, builds Debug/Release e testes da solução; depois validar em navegador o POST, os redirects por perfil, todos os motivos de sessão e a troca auditada de tenant. Também alinhar o contrato `LoginViewModel`/API para aceitar CPF/CNPJ sem enfraquecer mensagens antienumeração.
