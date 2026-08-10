# Diagnóstico visual — PlantãoPro v1.54

## Escopo e causa raiz

A auditoria partiu dos seis registros visuais fornecidos e da inspeção do shell Razor. O problema central era estrutural: componentes Bootstrap e classes históricas estavam sendo combinados sem um contrato único de layout. A v1.54 passa a usar tokens canônicos e classes `pp-*`, mantendo aliases apenas para migração.

## Print 1 — área autenticada / Admin SaaS

- Topbar desalinhada e sem contenção em larguras intermediárias.
- Breadcrumb renderizado como lista textual, usuário e perfil expostos como links soltos.
- Busca, contexto, plano e upgrade competiam pela mesma linha sem prioridades responsivas.
- Título e subtítulo não tinham limites; o footer podia subir em páginas curtas.
- Correção: shell flexível com conteúdo expansível, topbar em regiões semânticas, breadcrumb compacto e dropdown real de usuário.

## Print 2 — checklist operacional

- Excesso de espaço vazio, hierarquia fraca e checklist apresentado como lista simples.
- Ausência de contexto, ação e indicação de progresso; footer visualmente solto.
- Direção adotada: cards operacionais com status, explicação, progresso e próxima ação, apoiados pelo grid de cards do design system.

## Print 3 — landing SaaS

- Headline grande demais, cards superdimensionados e CTAs sem alinhamento.
- Proporção entre narrativa, prova de valor e públicos prejudicava a leitura.
- Direção adotada: tipografia fluida limitada, hero de duas colunas e grids responsivos sem altura fixa.

## Print 4 — planos

- Contraste insuficiente na seção escura, cards cortados/sobrepostos e CTAs pouco visíveis.
- O grid não acomodava a largura disponível e quebrava verticalmente.
- Direção adotada: superfícies claras, contraste AA, grid `minmax`, destaque do plano recomendado sem deslocar cards e comparação abaixo.

## Print 5 — cadastro / onboarding

- Formulário sem grid, labels colados, controles de larguras diferentes e selects desalinhados.
- Linhas horizontais brutas substituíam agrupamento; faltavam cards, ajuda e validação visual.
- Correção: contrato `pp-form-page/card/section/grid/field`, controles consistentes, cabeçalho de etapa, ajuda e erro associado por `aria-describedby`.

## Print 6 — login

- Composição pesada, benefícios cortados, logo desproporcional e painel esquerdo esmagando o formulário.
- Labels sem respiro, botão quebrando texto, toggle ambíguo, erros como marcadores isolados e Caps Lock fraco.
- Correção: shell 55/45, marca proporcional, benefícios sem recorte no desktop, formulário full-width, erro textual com ícone, aviso inline e botão com `white-space: nowrap`; no mobile a composição vira uma coluna.

## Riscos controlados

- Aliases antigos permanecem temporariamente para telas ainda não migradas, mas os tokens oficiais são a fonte de verdade.
- As folhas v1.54 são carregadas por último e têm responsabilidade explícita (reset, shell e formulários/feedback), evitando seletores de alta especificidade e `!important`.
- A validação de runtime depende do SDK .NET, indisponível no ambiente desta auditoria.
