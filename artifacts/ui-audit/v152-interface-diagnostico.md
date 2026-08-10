# Diagnóstico de interface v1.52

Data da auditoria por código: 2026-08-10. Escopo: views Razor, componentes, CSS e JavaScript existentes. Esta auditoria **não equivale a validação visual em navegador**: o SDK .NET não está instalado no ambiente, portanto o runtime autenticado não pôde ser iniciado nem capturado.

## Critérios

Em cada área foram observados: composição visual, dependência de Bootstrap cru, hero e ação principal, indicadores ligados a modelos/API, filtros, detalhe/drawer, estado vazio, comportamento mobile e contrato de navegação. Nenhum número operacional novo foi introduzido nesta rodada.

| Área | Leitura encontrada | Lacuna / decisão v1.52 |
|---|---|---|
| Shell global | Layout tem skip link, sidebar/topbar/mobile nav, portal de overlays e live region. O shell já é responsivo e componentizado. | Preservar estrutura canônica; validar CTAs das três navegações e não empilhar nova camada cosmética. |
| Login | Narrativa, marca local, toggle de senha e região de erro já existem; há layout mobile e respeito a redução de movimento. | Manter a composição v1.51; depende de runtime para confirmar contraste e foco em sessão real. |
| Dashboard / Minha Central | Dashboard, central, resumo do dia, prioridades, kanban, feed e drawer já usam modelos reais e estados vazios. | Prioridade é manter fonte real; não substituir ausência por contadores estáticos. |
| Meu Dia | Timeline, recomendação, filtros e drawer possuem CSS/JS próprios. | Confirmar por navegador o retorno de foco e os filtros rápidos com dados reais. |
| Agenda | Central operacional tem calendário, filtros e drawer enriquecido. | Ações devem continuar condicionadas às URLs reais; impressão e overflow mobile requerem runtime. |
| Plantões | Listagem já possui workspace, indicadores derivados do modelo e ações de domínio. | Evolução restante é validar publicação/cobertura conforme permissão em integração. |
| Escalas | Index e detalhe existem, com vínculo à operação. | Timeline completa de presença/pagamento depende dos eventos retornados pela API. |
| Fechamentos | Workspace explicita Realizado → Divergências → Conferência → Aprovação → Financeiro e usa empty state. | Sem dados carregados no ambiente; validar endpoints `/bff/fechamentos` no runtime. |
| Pendências | Central e pendências clínicas coexistem; Minha Central usa `work_items`, kanban e drawer. | Ações de atribuir/adiar dependem do contrato real do work item; não criar CTA especulativo. |
| Saúde 360 | Fluxo clínico e módulos Paciente → Financeiro têm superfícies dedicadas. | Quantidades e tempo médio só devem aparecer quando retornados pelo BFF; validar continuidade por perfil. |
| Pacientes | Busca, cadastro, detalhe, histórico e resumo clínico existem. | Consolidar timeline e próximo agendamento quando API entregar relação; manter LGPD explícita. |
| Agendamentos | Agenda dia/médico, calendário, check-in e páginas de detalhe existem. | Confirmar no runtime quais transições são permitidas por status antes de ampliar CTAs. |
| Triagem | Fila, atendimento e classificação possuem views clínicas. | Layout de duas colunas deve ser testado em 360–768 px com dados extensos. |
| Consultas | Atendimento, histórico, resumo e impressão existem. | Salvar/finalizar/prescrição precisam continuar ligados aos forms reais e à autorização. |
| Financeiro / Pagamentos | Há central financeira, clínica, pagamentos e jornadas de glosa/recebimento. | Evitar consolidado inventado; composição e timeline dependem do retorno real. |
| Relatórios | Index era uma lista hardcoded de dez títulos, Bootstrap cru e botões futuros repetidos. Relatórios reais tinham rotas próprias. | **Refatorado:** biblioteca responsiva aponta apenas para actions existentes; favoritos e agendamento aparecem como informação indisponível, não botão falso. |
| Configurações | Index era uma grade Bootstrap focada apenas na conta e Swagger. | **Refatorado:** landing por responsabilidade liga conta, usuários, assinatura, marca, notificações, LGPD, integrações, parâmetros e saúde a controllers existentes. Swagger técnico saiu da ação principal. |
| Convites | Central já foi enriquecida na v1.51 com filtros e detalhes. | Seleção de plantão deve permanecer baseada na fonte real; ID manual não deve voltar como caminho principal. |
| Mobile | Design system contém breakpoints e navegação inferior; drawers possuem infraestrutura compartilhada. | Novo padrão `pp-*` empilha hero/ações, reduz cards e elimina grade rígida em telas pequenas. Teste visual segue bloqueado sem runtime. |

## Componentes e dívida identificada

- Já existem `_KpiCard`, `_FilterPanel`, `_DataTable`, `_StatusBadge`, `_EmptyState`, cabeçalhos e overlays. Duplicá-los aumentaria divergência.
- A v1.52 adiciona padrões estruturais `pp-page`, `pp-hero`, `pp-hero-actions`, `pp-section-header` e `pp-action-card` para landings reais. Eles usam tokens existentes e não codificam dados operacionais.
- Ainda existem views legadas dominadas por `row`, `card` e `alert`. A migração deve ocorrer conforme os endpoints e modelos de cada jornada forem verificados, em vez de uma substituição global insegura.
- A validação estática de rotas cobre shell, navegação mobile, jornada clínica e os CTAs alterados. Ela detecta `href="#"`, botão sem contrato e controller inexistente.

## Bloqueios

- `dotnet --info`, restore, build e testes: bloqueados porque `dotnet` não está disponível.
- Runtime e screenshots: não produzidos, pois a aplicação ASP.NET não pode ser iniciada neste ambiente.
- Validação visual em 360, 390, 430, 768 e 1024 px: não declarada como concluída; apenas regras responsivas foram revisadas estaticamente.
