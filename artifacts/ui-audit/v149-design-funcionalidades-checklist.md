# PlantãoPro v1.49.0 — auditoria de design e jornadas operacionais

Data da revisão: 2026-08-09

## Critério

- **Visual**: `Premium`, `Revisado` ou `Pendente`.
- **Funcional**: `Real`, quando usa controller/API/BFF existente; `Parcial`, quando a jornada ainda exige aprofundamento; nunca foram inseridos dados demonstrativos.
- A validação visual automatizada foi estrutural. A captura em navegador ficou impedida porque o SDK .NET não está instalado no ambiente e, portanto, a aplicação não pôde ser iniciada.

| Tela / viewport | Status visual | Status funcional | Problemas encontrados | Ações corrigidas | Pendências reais |
|---|---|---|---|---|---|
| Minha Central | Premium | Real (`api/minha-central` e `work_items`) | Entrada ainda orientada principalmente a pendências | Mantida central Kanban, resumo e drawer conectados ao BFF | Ampliar agregados por perfil quando APIs fornecerem esses recortes |
| Meu Dia | Premium | Real (`api/minha-central`) | Era um placeholder estático em estado de carregamento | Criada priorização por criticidade/prazo, KPIs reais, próxima ação, timeline, atalhos e drawer de `work_item` | Acrescentar agenda pessoal quando houver agregado próprio no BFF |
| Dashboard | Revisado | Real | Indicadores distribuídos entre dashboards por perfil | Design system existente preservado | Consolidar drilldowns em um único painel executivo |
| Agenda | Premium | Real (Agenda BFF) | Detalhe exigia navegação imediata | Adicionado drawer acessível, status, contexto, retorno de foco e impressão | Edição inline depende de contrato de autorização/status no BFF |
| Plantões | Premium | Real | Aparência CRUD e cobertura pouco evidente | Preservados KPIs calculados da página, filtros server-side, cards mobile, duplicação e central operacional | Ações em lote e filtros favoritos precisam de persistência/API |
| Escalas | Revisado | Real | Histórico fragmentado | Fluxos reais existentes preservados | Unificar presença, pagamento e conflito no mesmo drawer |
| Fechamentos | Revisado | Real | Etapas pouco explícitas em alguns estados | Fluxo existente preservado sem geração indevida de pagamento | Completar stepper com divergências vindas da API |
| Pendências | Premium | Real (`work_items`) | Próxima ação pessoal não aparecia em Meu Dia | Reuso do drawer e ordenação operacional real no Meu Dia | Adiamento e atribuição dependem dos comandos autorizados do backend |
| Saúde 360 | Premium | Real | Módulos pareciam páginas isoladas e empty state sugeria dados demo | Criada jornada navegável Paciente → Financeiro e removida sugestão de demo | Métricas por etapa exigem endpoint agregado clínico |
| Pacientes | Revisado | Real | Navegação clínica dispersa | Jornada Saúde 360 passou a oferecer acesso contextual | Consolidar timeline longitudinal com minimização LGPD |
| Agendamentos | Revisado | Real | Transições distribuídas por telas | Check-in e agenda conectados na jornada visual | Drawer único para reagendar/cancelar requer comandos do backend |
| Painel de Chamada | Revisado | Real | Baixa conexão visual com a jornada | Etapa “Chamada” incorporada ao fluxo Saúde 360 | Medir espera por etapa via API |
| Triagem | Revisado | Real | Fila isolada do fluxo global | Acesso direto pela jornada clínica | Persistência explícita de rascunho precisa ser auditada em navegador |
| Consultas | Revisado | Real | Contexto operacional fragmentado | Acesso direto e sequência clínica explícita | Consolidar anamnese/CID/prescrição sem ampliar exposição de dados |
| Prescrições | Revisado | Real | Etapa pouco visível no fluxo | Incluída na jornada assistencial | Drawer de histórico requer contrato clínico específico |
| Financeiro | Revisado | Real | Separado do desfecho clínico | Incluído como desfecho da jornada e atalho de Meu Dia | Agregado de caixa/convênios/glosas depende de endpoints reais |
| Convites | Premium | Real | Precisava de acesso operacional mais curto | Incluído nos atalhos de Meu Dia | Nenhuma regressão identificada estruturalmente |
| Pagamentos | Premium | Real | Precisava de integração com trabalho diário | Financeiro incluído nos atalhos e no fluxo clínico | Composição em drawer pode evoluir após endpoint próprio |
| Relatórios | Revisado | Real | Catálogo ainda heterogêneo | Estrutura atual preservada | Modelos salvos/favoritos exigem persistência |
| Configurações | Revisado | Real | Grupos distribuídos entre parametrizações e segurança | Navegação existente preservada | Consolidar landing page sem duplicar regras de autorização |
| Mobile 360/390/430 px | Premium estrutural | Parcial | Hero, KPIs e timeline poderiam comprimir | Breakpoints específicos, cards em duas colunas, drawer full-screen herdado e jornada horizontal com scroll-snap | Validar visualmente em navegador quando runtime estiver disponível |
| Tablet 768/1024 px | Premium estrutural | Parcial | Jornada de oito etapas excede largura | Scroll horizontal e workspace em coluna | Validar toque e rotação em dispositivo real |
| Desktop 1366/1920 px | Premium estrutural | Parcial | Hierarquia de próxima ação era fraca | Hero contextual, spotlight e layout de duas colunas | Executar regressão visual quando a aplicação puder iniciar |

## Acessibilidade e segurança

- Drawers usam `role="dialog"`, `aria-modal`, título associado, fechamento por `Escape` e devolução de foco na Agenda.
- Indicadores possuem região nomeada e os estados não dependem apenas de cor: texto, rótulo e ícone continuam disponíveis.
- Os atalhos são links reais; ações mutáveis de `work_items` continuam no BFF existente.
- Nenhum nome, valor, paciente, médico ou total fictício foi adicionado.
- A sugestão de cadastrar “dados demo” foi removida do empty state clínico.

## Evidência do ambiente

`dotnet restore backend/PlantaoPro.sln` retorna `/bin/bash: dotnet: command not found`. Restore, build, testes .NET e screenshot não podem ser produzidos nesta imagem até a instalação do SDK .NET 10 declarado pelo projeto.
