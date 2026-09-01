# PlantãoPro v2.12.6 — validações, feedback, manual e design

## Ambiente e escopo seguro

- **Modo usado:** MODO DESIGN ESTÁTICO.
- **SDK:** `dotnet` não está instalado (`dotnet: command not found`). Conforme a regra da rodada, nenhum controller, model C#, contrato, banco, migration, projeto, props ou solution foi alterado.
- **Git remoto:** o checkout não possui remoto configurado. A branch local criada foi `codex/v2126-validacoes-feedback-manual-design`.

## Inventário da auditoria

A varredura encontrou **397 views Razor**, **91 arquivos com formulário** e **114 elementos `<form>`**. A matriz abaixo consolida o inventário por módulo; os campos e regras individuais permanecem definidos nas views e nos contratos existentes.

| Módulos / páginas | Formulários | Objetivo e perfis | Validação e feedback observados | Riscos / pendências |
|---|---:|---|---|---|
| Conta (login, recuperar e redefinir senha) | 3 | Acesso de todos os perfis | Tag Helpers, mensagens por campo e resumo em parte das telas | Validar regras server-side quando houver SDK |
| Agenda e agendamentos | 5 | Criar, editar, filtrar e confirmar; recepção, coordenação e médicos | Datas/horas e confirmação visual existentes | Revisar todos os endpoints POST e auditoria |
| Assinaturas, planos e faturamento SaaS | 25 | Cliente, plano, vigência e cobrança; Super Admin | Resumos, limites numéricos, datas e mensagens existentes | `ClienteId` e `PlanoId` ainda são entradas técnicas em `_Form`; substituir por lookup real exige fonte server-side |
| Clientes, onboarding e jornada | 10 | Cadastro e implantação; Super Admin e CS | Fluxos guiados, feedback e estados operacionais | Confirmar isolamento e validação no servidor |
| Usuários, perfis e permissões | 10 | Pessoas e controle de acesso; administradores | Matriz por checkbox e formulários básicos | Alguns botões são apenas visuais; confirmar handlers reais antes de convertê-los em POST |
| Médicos, hospitais e especialidades | 6 | Cadastros operacionais; administradores | Partials reutilizáveis e validação por campo | Conferir CPF, telefone e tenant no servidor |
| Escalas e plantões | 9 | Cobertura, confirmação e substituição; escala e coordenação | Confirmações via componente, filtros, datas e estados vazios | Auditar conflitos de horário e eventos com SDK |
| Financeiro e pagamentos | 5 | Valores, pagamentos e pendências; financeiro | Campos monetários, feedback e confirmação visual | Confirmar cultura monetária e permissão no servidor |
| Comunicação, notificações e suporte operacional | 13 | Mensagens, preferências, chamados e ocorrências | Filtros, toasts e confirmações reutilizáveis | Revisar retenção/LGPD em anexos e textos |
| Relatórios, auditoria, LGPD e inteligência | 8 | Consulta, filtro e exportação; gestores e auditores | Estados e filtros existentes | Exportação sensível deve confirmar e auditar no servidor |
| Demais áreas clínicas, comerciais e parametrizações | 20 | Fluxos especializados conforme permissão | Padrões variados, agora cobertos pela experiência global | Auditoria detalhada depende de execução com SDK |

## Evolução aplicada

### Validação e formulários

- Todo formulário renderizado recebe uma classe de experiência consistente, foco visível e marcação acessível dos campos HTML `required`.
- Formulários com campos ganham um resumo de validação no topo quando a página ainda não oferece um; em erro, o primeiro campo inválido recebe foco e é anunciado por toast.
- Datas `DataInicio` / `DataFim` mantêm a verificação de ordem cronológica já existente.
- Todo POST tradicional passa a exibir estado ocupado e impede envio duplicado, sem interferir nos fluxos AJAX ou nos formulários que aguardam confirmação.
- A solução aproveita validações HTML e Tag Helpers existentes, sem inventar regras ou dados.

### Mensagens e confirmação

- Foram consolidados os tipos sucesso, informação, atenção e erro no host de toast existente.
- O modal global existente continua responsável por ações com `data-confirm`, sem `alert()` ou `confirm()` nativos.
- O resumo novo usa `role="alert"`; o guia usa semântica nativa, navegação por teclado e foco visível.

### “Como usar esta tela”

- O layout autenticado inclui um guia curto e recolhível em todas as páginas principais.
- Há orientações específicas para usuários, perfis, plantões, escalas, financeiro, relatórios, Super Admin, onboarding, suporte e notificações.
- Demais módulos recebem orientação segura e genérica: finalidade, perfil, ação, cuidado e próximo passo.

### Design

- Guia em card discreto, responsivo e com animação reduzida quando solicitada pelo sistema.
- Formulários recebem estados válido/inválido, foco de alto contraste, indicador obrigatório e botão ocupado.
- O comportamento é progressivo: sem JavaScript, formulários e o `<details>` continuam funcionais.

## Achados e limitações reais

- A pesquisa técnica encontrou usos existentes de `Guid.Parse`, SQL interpolado e `SELECT *`. Eles foram registrados, mas não modificados porque o SDK está ausente e a rodada proíbe alterações backend nesse modo.
- O formulário de assinatura ainda expõe `ClienteId` e `PlanoId`. A correção correta requer carregar opções/autocomplete reais e autorizados pelo servidor; trocar apenas o elemento visual produziria um formulário quebrado ou dados fictícios.
- Validação server-side, autorização, isolamento de tenant, auditoria e testes de persistência não puderam ser comprovados sem SDK. Nenhuma alegação de cobertura backend foi feita.
- Não foram criados mocks, dados fixos, segredos, contratos ou atalhos de framework.

## Verificações executadas

- Diagnóstico Git, remoto e SDK.
- Inventário completo de Razor e formulários por `find`, `rg` e script Python.
- Pesquisas de parsers inseguros, SQL interpolado, `SELECT *`, `href="#"`, diálogos nativos, solicitação de ID, mocks e segredos.
- `git diff --check` e scripts de segurança/compatibilidade disponíveis (resultado registrado no fechamento da rodada).
- Builds e testes .NET: **não executados**, pois o comando `dotnet` não existe no ambiente.
