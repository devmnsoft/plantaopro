# v1.57 — drawers e ações

## Componente canônico
`Views/Shared/_DetailDrawer.cshtml` define um único diálogo lateral. `wwwroot/js/detail-drawer.js` controla abertura, fechamento, loading/erro, foco e montagem segura. `drawers.css` fornece metadados, timeline, ações e modo full-screen.

## Contrato do gatilho
Um botão `type="button"` com `data-detail-open` fornece somente valores já renderizados pelo view model. Os campos aceitos são `kind`, `title`, `status`, `summary`, `origin`, `period`, `owner`, `composition`, `reference`, `history` e até duas rotas reais. Campos ausentes não geram conteúdo fictício.

## Matriz ativa
| Área | Gatilho | Resumo | Timeline | Ações reais |
|---|---|---|---|---|
| Plantões | Tabela e card mobile | Hospital, especialidade e observação | Estado/cobertura atual | Detalhe completo; duplicação permanece POST na lista |
| Escalas | Tabela | Médico/CRM, hospital, tipo, período e valor | Data real do registro | Detalhe da escala e detalhe do plantão |

## Matriz planejada (bloqueada por dados)
Fechamentos, Pendências, Pacientes, Agendamentos, Consultas, Pagamentos e Convites usarão o mesmo componente quando seus endpoints resumidos entregarem histórico, metadados e permissões suficientes. O drawer transacional de `work_items` permanece separado até a API retornar a projeção completa, para não quebrar comentários/versionamento.

## Segurança e acessibilidade
- Valores operacionais são inseridos com `textContent`, nunca com HTML interpolado.
- O painel tem nome e descrição acessíveis, região viva e estado ocupado.
- Escape e backdrop fecham; Tab permanece no diálogo; o foco retorna ao acionador.
- No mobile o painel ocupa `100vw × 100dvh`.
- Ações críticas não foram adicionadas sem endpoint, permissão e confirmação reais.
