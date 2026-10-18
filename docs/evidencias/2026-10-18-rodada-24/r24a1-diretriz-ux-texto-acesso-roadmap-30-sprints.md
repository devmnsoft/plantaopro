# R24 · r24a1 — Diretriz UX / Texto / Acesso (epic de 30 sprints, R24→R53)

Data: 2026-10-18 · Sessão: `opencode`

## Diretriz do usuário (texto original, verbatim)

> "continue pelas proximas 30 sprints sem parar, evolua o desgin do sistema, revise o texto todo do sistema tbm, coloque
> todos em linguagem que qualquer usuario entenda, coloque instruções de como a pagina funciona, veja que uma area escura
> nao pode ter uma letra escura se nao confude e nao da pra ler, verifique se todos os formularios tem validação, as
> inserções tem id incremental, todas as listas buscam por nome e salvam id nenhuma lista deve buscar por id, chickbox,
> checkbox list tbm veja se todos os formualrios estão no padrão e tem validação, o cadastro nao pode confundir com demais,
> um usuario acessa só a sua area por cpf ou cnpj, email e senha pra nao confundir, o tenant deve ser visivel apenas para
> o super administrador, continue evoluindo o saas, seja critivo, vamos ser o maior saas deste segmento, tire todos os
> textos que sugerem homologação"

## Interpretação em epics (ids estáveis para os relatórios das próximas rodadas)

| Epic | Tema | Interpretação operacional |
|---|---|---|
| **E1** | Linguagem simples | Revisar TODO texto de UI (resx — valores são livres, chaves travadas em 653 pelo contrato R7DF6 — e literais pt-BR das zonas pré-conversão) e colocar em linguagem que qualquer usuário entenda: sem jargão técnico (BFF, scoping, seed, endpoint, GUID…), frases curtas e com verbo. |
| **E2** | Acesso e isolamento | Usuário acessa SÓ a sua área: login por e-mail+senha já aceita CPF/CNPJ/e-mail como identificador (`LoginViewModel`) — auditar/consolidar; **tenant visível apenas ao super administrador** (sweeps de toda a UI: breadcrumbs, tabelas, relatórios — incluindo os do backoffice SaaS do F-R14-B parte 2, D-R24-3); garantir zero mistura entre dados de tenants diferentes para papéis não-globais. |
| **E3** | Instruções por página | Cada tela de operação precisa dizer COMO funciona: bloco informativo curto (passos da regra de negócio, como registrar, o que acontece ao enviar). Padronizar o componente e cobrir todas as telas sem ele. |
| **E4** | Contraste (leitura) | Áreas escuras (`bg-dark`, `card bg-dark`, zonas noturnas do portal) não podem ter letra escura/fraca: varredura de `text-muted`/`text-secondary`/classes próprias em contexto escuro; corrigir; fixar regra em fato-scanner (padrão R16) com whitelist. |
| **E5** | Formulários no padrão + validação | Auditoria de TODOS os formulários: campos obrigatórios marcados, validação no cliente (required/estilo da casa) + validação na API (regras com mensagem amigável), `checkbox`/`checkbox list` consistentes, padrões de layout iguais entre telas (label, ajuda, erros). |
| **E6** | Listas: buscam por nome, salvam id | Todo `<select>`: label = nome legível, value = id; buscas/filtros de UI filtram por NOME (ILIKE nome), nunca por id; nenhuma lista exibe id cru ou código técnico ao usuário; **números visuais progressivos/incrementais** (PC-70, nº da NF, nº de orçamento/ticket) — o id interno segue UUID, o número que o usuário vê é sequencial. |
| **E7** | Cadastro ≠ operação | Telas de cadastro com identidade visual/textual própria (título + breadcrumb claros: "Cadastro de …"), separadas das telas operacionais, para o usuário nunca confundir onde cadastra e onde opera. |
| **E8** | Sem vestígio de homologação | Remover de todo texto visível (resx + literals + seeds exibidas + títulos) palavras que sugerem ambiente de teste/homologação: "homolog", "demo/demonstra", "smoke", "seed", "teste/testar", códigos de rodada (R8B…/a3610000…) em nomes exibidos, "Santacasa-demo" etc. Cuidado: strings de dados de teste (códigos `R8B4L-`) que APARECEM em telas também contam — renomear para dados neutros. |
| **E9** | Evolução do SaaS | Continuar amadurecendo o produto SaaS em si: planos e limites por plano, faturamento/cobrança, onboarding de novo cliente, uso e risco (relatórios já existem — polir para linguagem simples), notificações de ciclo de cobrança. Ser o maior SaaS do segmento: decidir prioridade por rodada, com critério registrado. |
| **E0** | Design do sistema (contínuo) | Manter a evolução de design já iniciada (portais, dark mode, componentes); revisões de navegação/menu, estados vazios/carregando, responsividade, acessibilidade básica. |

Regras de casa aplicadas: cada fatia termina com fatos versionados; scanner-fatos permanentes para as classes de regra que
permitirem verificação estática (E4 contraste, E6 labels/values dos selects, E8 palavras banidas em zonas convertidas, E5
presença de validação); gates build/suíte como sempre (base 1529 crescendo).

## Roadmap 30 rodadas (R24→R53)

Ajustável conforme progresso real — o que muda entre rodadas é o detalhamento, não os epics.

| Rodada | Conteúdo |
|---|---|
| **R24** (hoje) | F-R14-D + F-R14-B fechados (D-R24-1..3) + registro da diretriz (este doc) |
| **R25** | r25a0 **inventário-mestre**: scan de TODAS as views/resx classificando achados por epic (file:line); quick-win E8 (palavras de homologação removidas) |
| **R26** | E4 contraste: varredura + correção zonas escuras + fato-scanner |
| **R27** | E1 portal: textos do portal do cliente (linguagem simples) — resx values |
| **R28** | E1 Adm360 parte 1 (suprimentos/compras) — literais pt-BR in-place |
| **R29** | E1 Adm360 parte 2 + demais módulos — resx values |
| **R30** | E5 formulários parte 1: auditoria + validação (cadastros) |
| **R31** | E5 formulários parte 2: operações + checkbox/checklist padrão |
| **R32** | E6 selects e buscas: nome × id (sweeps + fato-scanner de option label/value) |
| **R33** | E6 números visuais progressivos (inventário de sequências + hidratação de GUIDs em UI) |
| **R34** | E7 cadastro vs operação: identidade de telas (títulos/breadcrumbs) |
| **R35** | E3 instruções por página — portal |
| **R36** | E3 instruções por página — Adm360 parte 1 |
| **R37** | E3 instruções por página — Adm360 parte 2 + padrão do componente |
| **R38** | E2 acesso parte 1: login (CPF/CNPJ/e-mail+senha) + isolamento por área |
| **R39** | E2 acesso parte 2: tenant visível só p/ super admin (inclui backoffice SaaS, D-R24-3 revisitada) |
| **R40** | E9 SaaS: planos e limites por plano (produto) |
| **R41** | E9 SaaS: onboarding de novo cliente + primeira experiência |
| **R42** | Consolidação de scanner-fatos permanentes (regras E4/E5/E6/E8 em um suíte dedicado) |
| **R43** | E0 design: navegação e menus (clareza, agrupamento, profundidade) |
| **R44** | E0 design: estados vazios, carregamento, erros (linguagem simples + E1 revisitada) |
| **R45** | E0 design: responsividade (mobile/field) |
| **R46** | Passada crítica geral: regressão de UX + limpeza de pendências acumuladas |
| **R47** | E9 SaaS: faturamento e cobrança (fluxo completo em linguagem simples) |
| **R48** | E9 SaaS: alertas de inadimplência/risco + comunicação |
| **R49** | Acessibilidade básica (aria/labels/foco) + contraste revisitado |
| **R50** | Passada final de copy (tom de voz único, glossário) |
| **R51** | Ajuda central: "como usar" por área (extensão do E3) |
| **R52** | Hardening: smoke e2e completa + performance percebida |
| **R53** | Consolidação final: documentação de design + relatório da diretriz (antes/depois por epic) |
