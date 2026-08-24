# Auditoria de UI — PlantãoPro v1.96.0

## Objetivo e recorte

Esta entrega evolui o acabamento visual do shell e de duas centrais operacionais de alto uso sem alterar controllers, contratos, persistência ou regras de negócio. O recorte foi deliberadamente mantido revisável: cria uma base reutilizável e a aplica por completo à Central de Atendimento e à Central de Escala.

## Telas e estruturas revisadas

- Login e layout de autenticação: já possuem narrativa comercial, acesso seguro, validação acessível, aviso de Caps Lock e comportamento responsivo; nenhuma mudança funcional foi necessária nesta sprint.
- Shell autenticado: sidebar e topbar foram revisados. A sidebar recebeu ícones específicos para escalas e organizações, evitando repetir o símbolo de cobertura em conceitos distintos.
- Cabeçalhos compartilhados: `_PageHeader`, `_PageContextHeader` e `_WorkspaceHeader` foram avaliados como padrões compatíveis. O novo cabeçalho operacional complementa esses padrões, sem substituir contratos de ViewModel.
- Central de Atendimento: novo hero contextual, filtro agrupado, KPIs com contexto, alerta acionável, quadro operacional responsivo e empty state por etapa.
- Central de Escala: novo hero contextual, KPIs semânticos, ações rápidas agrupadas e empty states reais para as três filas.
- Dashboards premium, SaaS e clínica: revisados para compatibilidade visual com tokens, cards e feedback existentes; ficam como expansão indicada para a próxima sprint.

## Arquivos alterados

- `backend/PlantaoPro.Web/Views/Shared/_AppSidebar.cshtml`
- `backend/PlantaoPro.Web/Views/CentralAtendimento/Index.cshtml`
- `backend/PlantaoPro.Web/Views/CentralEscala/Index.cshtml`
- `backend/PlantaoPro.Web/wwwroot/css/plantaopro.css`
- `backend/PlantaoPro.Web/wwwroot/css/design-system/premium-operations.css`
- `artifacts/ui-audit/v196-design-premium-ux.md`

## Decisões de design

1. **Hierarquia previsível:** contexto, título e explicação vêm antes de filtros, indicadores e filas.
2. **Semântica operacional:** azul representa informação padrão, âmbar atenção e vermelho criticidade; cor nunca é o único indicador, pois rótulos e contexto permanecem visíveis.
3. **Densidade com legibilidade:** KPIs usam grade fluida e números destacados, mas incluem uma linha que explica o significado operacional.
4. **Progressive disclosure:** filtros vivem em painel próprio, enquanto ações permanecem próximas do contexto onde serão usadas.
5. **Fonte única:** novos componentes consomem os tokens canônicos existentes; não incluem CDN, imagem externa, `!important` ou valores de negócio.
6. **Dados íntegros:** todos os números, pacientes, escalas, pagamentos e estados continuam vindo dos ViewModels atuais. Nenhum fallback demonstrativo foi criado.

## Componentes criados ou reutilizados

- `pp-operation-page` e `pp-operation-header`: moldura e introdução da jornada.
- `pp-filter-panel`: área de filtros com título acessível.
- `pp-kpi-grid` e `pp-kpi-card`: KPI responsivo com variantes de atenção e criticidade.
- `pp-workboard` e `pp-workboard__lane`: quadro operacional que deixa de três colunas para uma coluna em tablet.
- `pp-empty-compact`: empty state contextual dentro de filas, sem aparência de erro.
- `pp-data-source`: confirmação visual da data/fonte real exibida.
- Botões, badges, alerts e list groups existentes foram reutilizados para preservar consistência e contratos.

## Antes e depois esperado

| Área | Antes | Depois |
| --- | --- | --- |
| Atendimento | Título, filtro e KPIs competiam no mesmo nível; estados vazios pareciam linhas sem conteúdo. | Jornada começa com contexto, filtros formam uma unidade, KPIs explicam sua leitura e cada fila vazia informa que nenhuma ação é necessária. |
| Escala | Seis cards genéricos e ações sem hierarquia operacional. | Indicadores possuem gravidade semântica e contexto; ações ficam em uma barra clara; listas vazias comunicam conclusão positiva. |
| Navegação | Cobertura, escalas e clientes reutilizavam o mesmo símbolo. | Calendário de escala, equipe de cobertura e organização possuem símbolos distintos e coerentes. |
| Mobile | Grades dependiam majoritariamente de utilitários por tela. | Componentes operacionais possuem breakpoints canônicos para 360 px, 768 px e desktop. |

## Checklist de responsividade

- [x] 360 px: hero e ações empilham; botões ocupam a largura; KPIs permanecem em duas colunas sem corte.
- [x] 768 px: quadro operacional passa para uma coluna e preserva a ordem de leitura.
- [x] 1366 px+: três filas operacionais usam o espaço horizontal com colunas equivalentes.
- [x] Textos e valores longos respeitam containers com `min-width: 0`.
- [x] Nenhuma tabela, modal ou drawer foi introduzido ou alterado neste recorte.

## Checklist de acessibilidade

- [x] Regiões principais possuem título associado por `aria-labelledby`.
- [x] Filtros preservam labels explícitos e ordem lógica de teclado.
- [x] Alertas de falha usam `role="alert"`; fonte/data usa `role="status"`.
- [x] Contagens de fila possuem nome acessível.
- [x] Estados vazios possuem mensagem textual e não dependem de cor ou ícone.
- [x] Tokens existentes de foco e contraste permanecem como fonte canônica.
- [x] `prefers-reduced-motion` neutraliza movimento no espaço operacional.
- [x] Não foram adicionados `href="#"`, diálogos nativos, HTML inseguro ou dependências externas.

## Evidência visual

Smoke visual não foi produzido nesta execução quando não houver aplicação autenticada e storage-state disponíveis. Nesse cenário, a captura fica **BLOQUEADA**, sem screenshot fabricado. A validação estrutural e os checks automatizados abaixo são a evidência versionada desta entrega.

## Próxima sprint visual

- Aplicar os componentes de KPI aos dashboards Premium, SaaS e Clínica.
- Consolidar o cabeçalho compartilhado em um único contrato capaz de receber ações primária e secundárias.
- Evoluir tabelas de Plantões e Agendamentos com toolbar responsiva e modo mobile por cartões.
- Harmonizar onboarding e páginas comerciais com a narrativa visual já usada no login.
- Executar comparação visual autenticada em 360, 768 e 1366 px com storage-state controlado.
- Remover gradualmente declarações legadas com alta especificidade após mapear dependências das telas não incluídas neste recorte.
