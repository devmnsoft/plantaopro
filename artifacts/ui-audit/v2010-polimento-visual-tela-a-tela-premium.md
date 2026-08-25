# Auditoria visual — PlantãoPro v2.01.0

## Escopo e método

Rodada de QA visual realizada sobre as **rotas e dados reais** já existentes, sem alterar regras de negócio, banco ou autenticação. A camada `v2010-screen-polish.css` complementa o Design System 2.0; não cria outro AppShell nem duplica a identidade. Foram revisados os breakpoints-alvo de 360, 390, 768, 1024, 1366, 1440 e 1920 px por inspeção responsiva das regras. Evidência navegável ficou pendente porque o ambiente não fornece credenciais/dados de uma instância autenticada nem conexão configurada com a API.

## Matriz tela a tela

| Rota/tela analisada | Problema encontrado | Ajuste realizado | Componente usado | Estado validado | Pendência/evidência |
|---|---|---|---|---|---|
| `/Account/Login` | Primeira impressão perdia espaço no mobile estreito; foco e controles precisavam manter hierarquia | Ritmo responsivo em 360/390 px, controle mínimo, foco visível e painel sem overflow | Auth layout, form control, botão primário | inválido, erro, processando | Screenshot pendente: execução exige API de autenticação |
| `/Dashboard`, `/MinhaCentral`, `/Operacoes` | Densidade e espaçamento variavam entre cards | Grade com gaps fluidos, largura máxima em telas amplas e superfícies alinhadas | page, KPI card, section | carregando, vazio, alerta | Validado por CSS compartilhado; print autenticado pendente |
| `/Escalas`, `/Plantoes`, `/MinhaAgenda` | Tabelas largas e ações podiam comprimir filtros | Cabeçalho sticky, área rolável, alvos de ação e filtros flexíveis | data table, filter bar, confirm modal | vazio, sem resultado, processando, sucesso/erro | Alternativa mobile preserva scroll acessível sem quebrar colunas |
| `/Medicos`, `/Hospitais` (profissionais/unidades) | Ações iconográficas e linhas tinham densidade irregular | Altura de linha, coluna final organizada, foco e status padronizados | table, status badge, empty state | vazio, filtro sem resultado, bloqueado | Conteúdo continua vindo dos controllers reais |
| `/Relatorios`, `/Observabilidade` | Blocos analíticos e filtros não compartilhavam o mesmo ritmo | Seções e toolbars responsivas, superfícies e títulos consistentes | section header, toolbar, card | carregando, erro, sem permissão | Validação visual com dados reais pendente |
| `/Usuario`, `/Perfis`, `/Permissoes` | Ações administrativas sensíveis precisavam de confirmação e feedback | Modal acessível com título/descrição, retorno de foco e estado `aria-busy` | confirm modal, access notice | sem permissão, bloqueado, processando, erro | Permissões não foram alteradas |
| `/Configuracoes`, `/Parametrizacoes` | Formulários longos variavam em label, controle e ações | Labels, altura de controle, validação e barra de ações uniformes | form field, card actions | inválido, sucesso, erro | Regras de validação existentes preservadas |
| `/Auditoria`, `/Observabilidade/Acessos` | Tabela crua em larguras intermediárias | Cabeçalho legível, divisores sutis, paginação e overflow intencional | premium table, pagination | vazio, erro, sem resultados | Print autenticado pendente |
| `/Planos` | Cards comerciais precisavam destacar disponibilidade e leitura de benefícios | Grid auto-fit, plano ativo, preço, benefícios e CTAs com hierarquia SaaS | plan card, feature list, badge | ativo/inativo, vazio, bloqueado | Nenhum preço ou benefício foi inventado |
| `/Assinaturas/Details` | Tela era um bloco administrativo cru; ações destrutivas se misturavam | Hero, resumo em KPIs e zona sensível em três cards com justificativas rotuladas | page hero, KPI card, confirm modal | sucesso, alerta, inválido, processando | Dados permanecem oriundos do modelo da API |
| `/Assinaturas/AlterarPlano` | Campo técnico e confirmação tinham pouco contexto comercial | Contexto do contrato, ajuda do campo, validação, antiforgery e confirmação descritiva | form section, confirm modal | inválido, alerta, processando | Seleção continua seguindo o contrato atual do controller |
| `/MinhaAssinatura`, `/MinhaAssinatura/Upgrade` | Sidebar contratual podia competir com conteúdo e quebrar no tablet | Layout 2:1, sidebar sticky no desktop e coluna única até 1024 px | subscription layout, inline empty, access notice | sem assinatura, módulo não contratado, limite, erro | Sem estimativas ou mocks adicionados |
| `/WhiteLabel`, `/WhiteLabel/Preview`, `/WhiteLabel/Assets` | Preview e formulários precisavam respeitar largura e contraste do DS | Contenção responsiva, foco visível e cards uniformes; fallback existente preservado | card, form control, status notice | vazio, inválido, erro, sucesso | Revisão de contraste final depende das cores reais escolhidas pelo tenant |
| Área profissional: `/MinhaAgenda`, `/MeusPagamentos` | Tabela larga e ações primárias perdiam prioridade no telefone | Scroll horizontal contido, ações full-width quando necessário e estados vazios consistentes | table responsive, action bar, empty state | vazio, alerta, processando | Sem dados fixos adicionados |
| `/Shared/NotFound`, `/Shared/Error` | Estados precisavam manter leitura e foco no mesmo produto | Largura, espaçamento e borda de estado padronizados | friendly error state | 404, erro genérico | Ilustrações existentes preservadas |

## Estados obrigatórios

- **Carregando / ação em processamento:** elementos com `aria-busy`, skeleton existente e confirmação agora anunciam “Processando ação. Aguarde.”, desabilitam a ação e exibem spinner.
- **Vazio / sem resultados:** `EmptyState` e `inline-empty` mantêm título, explicação e ação real; tabelas recebem superfície consistente.
- **Erro / sucesso / alerta:** alerts, toasts e error state compartilham raio, contraste e hierarquia; mensagens continuam vindas dos fluxos reais.
- **Bloqueado / sem permissão / módulo não contratado:** notices distinguem indisponibilidade do plano e limite atingido, com CTA real para upgrade.
- **Formulário inválido:** foco visível, labels explícitos, summary e controles `required` foram preservados/aprimorados nas telas alteradas.

## QA responsivo e acessibilidade

- **360/390 px:** padding reduzido, modais dentro da viewport, botões de ação quebram linha e controles mantêm alvo mínimo.
- **768 px:** filtros empilham sem compressão; tabelas largas usam scroll contido em vez de reduzir texto até ficar ilegível.
- **1024 px:** assinatura passa para uma coluna e remove sticky lateral.
- **1366/1440 px:** grid e largura de leitura permanecem equilibrados; planos usam colunas automáticas.
- **1920 px:** conteúdo ganha respiro lateral sem linhas excessivamente longas.
- Foco `:focus-visible`, labels, `aria-live`, `aria-busy`, `role`, status textual e `prefers-reduced-motion`/forced colors existentes foram respeitados.

## Riscos e pendências

1. Prints de rotas autenticadas devem ser anexados quando uma instância com API e credenciais de demonstração estiver disponível.
2. A prévia white label deve passar por verificação de contraste em runtime para cada cor informada pelo tenant.
3. Tabelas muito específicas podem evoluir futuramente para cards mobile sem ocultar colunas essenciais; nesta rodada foi escolhida rolagem explícita e segura.
