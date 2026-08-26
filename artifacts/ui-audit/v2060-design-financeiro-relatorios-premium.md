# Auditoria de UI — v2.06.0 Design financeiro e relatórios premium

## Escopo alterado
- **Apuração de plantões:** comando executivo, seis KPIs, filtros organizados, resumo ativo, pipeline clicável, tabela adaptativa e estados já vinculados à API.
- **Conferência:** jornada, composição de horário/valor/presença, ações condicionadas pelo status real e timeline de auditoria em painel contextual.
- **Pagamentos:** resumo previsto/pendente/pago/rejeitado, filtros de competência e seletores derivados da resposta autorizada, badges semânticos e tabela responsiva.
- **Relatórios:** catálogo executivo e card de filtros que explicita a indisponibilidade enquanto não existir endpoint financeiro auditável; nenhuma geração é simulada.

## Decisões visuais e componentes
- O acabamento foi concentrado em `design-system/financial.css`, sem CSS local nas páginas.
- Foram reutilizados `pp-kpi-card`, `pp-data-table`, `pp-action-card`, botões, empty states, alertas e tokens do design system.
- A esteira usa sete etapas com quantidade e valor calculados exclusivamente da resposta da API. Cada etapa é também um link de filtro.
- Valores monetários usam numerais tabulares; superfícies usam bordas, sombras suaves e tons semânticos sóbrios.

## Filtros revisados
- Período por datas, profissional por lista/autocomplete, unidade e especialidade por dropdown e status por dropdown.
- Aplicar e limpar permanecem explícitos, com resumo textual dos filtros ativos.
- Nenhum campo solicita ID ao usuário; identificadores internos aparecem apenas como valores de opções reais.

## Badges financeiros
- Normalização central em `_StatusBadge`: pendente, conferido, aprovado, rejeitado, divergente, pago e cancelado.
- Ícone e texto acompanham cada cor para que o significado não dependa apenas de percepção cromática.

## Responsividade validada
- Breakpoints cobrem 360/390 px (cards em duas colunas ou uma coluna para detalhes/filtros), 768/1024 px (três colunas) e 1366/1440/1920 px (grade executiva completa).
- Tabelas financeiras viram blocos rotulados por `data-label` no mobile; pipeline mantém rolagem localizada, sem ampliar a página.
- Controles e botões preservam alvo de toque do design system.

## Acessibilidade
- Cabeçalhos e regiões têm `aria-labelledby`/labels; resumo de filtros usa `aria-live`.
- Pipeline possui nomes de link descritivos, badge expõe `role=status`, ícone decorativo e rótulo textual.
- Foco visível usa o padrão global e estados vazios/erros mantêm mensagens compreensíveis.

## Limitações
- Não foi adicionada biblioteca de gráficos: os endpoints financeiros atuais não entregam série temporal própria e um gráfico sintético violaria a honestidade dos dados.
- Exportações e filtros que dependem de endpoints inexistentes permanecem indisponíveis e explicados, sem simular sucesso.
- Check-in/check-out e horário executado são exibidos somente quando a API de fechamento os representa; não foram inferidos dados ausentes.

## Comandos executados
Consulte a seção de validação do resumo final: verificações de segurança, compatibilidade C# 10, script completo, restore, build, testes, busca de padrões e `git diff --check`.
