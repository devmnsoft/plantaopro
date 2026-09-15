# Ocorrências operacionais — resolução e template

**Base registrada:** `e4e2b1b` (branch limpa no início). Relatórios e títulos anteriores foram usados apenas como inventário; não como evidência de execução.

## Matriz de capacidade

| capacidade | regra | implementação | lacuna | teste |
|---|---|---|---|---|
| ocorrência operacional | tenant, unidade, solicitante do token e plantão opcional revalidados | validada por SQL transacional e API tipada | E2E depende de PostgreSQL/SDK | workflow unitário; banco pendente |
| classificação | catálogo único com 7 categorias e prioridade separada | implementada sem execução | catálogo ainda não administrável | teste de domínio |
| atribuição | somente gestor; usuário ativo do tenant; versão otimista | implementada sem execução | UI de autocomplete pendente | concorrência coberta pela condição SQL |
| estados | aberta, atendimento, aguardando, resolvida, cancelada; resolução/motivo obrigatórios | validada por teste de domínio | teste integrado pendente | `OcorrenciaWorkflowTests` |
| histórico | eventos reais, ordenados e isolados por tenant; sem exclusão física | implementada sem execução | comentários/anexos não incluídos nesta evolução | consulta API |
| cobertura/conferência | ocorrência não altera escala, presença nem pagamento | parcial | encaminhamento canônico deve ser conectado em rodada com banco executável | não executado |
| central/filtros | pesquisa, período, unidade, categoria, situação, responsável, minhas, paginação e ordem estável | implementada sem execução | template Web permanece pendente | revisão SQL |
| SLA | prazo é nulo por padrão; vencidas só com prazo aplicável | implementada sem execução | configuração contratual ausente | revisão SQL |
| login/permissões | preservado login persistido; tenant/usuário derivados das claims validadas pela sessão | validada na base anterior, não reexecutada | SDK indisponível | build não executável |

## Matriz de estados e permissões

| origem → destino | perfil | condição/campo | efeito e auditoria |
|---|---|---|---|
| ABERTA → EM_ATENDIMENTO | gestor | responsável ativo e versão atual | atribui, incrementa versão, evento `ATRIBUIDA` |
| EM_ATENDIMENTO ↔ AGUARDANDO_INFORMACAO | gestor ou responsável | versão atual | incrementa versão e registra evento |
| EM_ATENDIMENTO → RESOLVIDA | gestor ou responsável | providência obrigatória | preserva providência e evento `RESOLVIDA`; não altera módulos relacionados |
| aberta/atendimento/aguardando → CANCELADA | gestor | motivo obrigatório | cancelamento lógico e evento |
| RESOLVIDA/CANCELADA → ABERTA | gestor | versão atual | reabre sem apagar resolução anterior e registra evento |

## Auditoria de formulários/contratos alterados

| operação | entrada | controller | serviço | persistência | releitura/resposta |
|---|---|---|---|---|---|
| criar | DataAnnotations + catálogo de domínio | `POST /api/ocorrencias` | contexto revalidado | ocorrência + evento na mesma transação | `201` após `ObterAsync` |
| atribuir | responsável selecionado + versão | endpoint tipado | elegibilidade e papel | update condicional + evento | conflito `STALE_VERSION` em disputa |
| resolver/cancelar/reabrir | estado, descrição, versão | endpoint tipado | matriz de transição | update + evento atômicos | sucesso somente após commit |
| consultar | filtros tipados | `GET /api/ocorrencias` | tenant das claims | lista e indicadores com o mesmo predicado | erro de banco propaga; não vira zero |

## Privacidade e limitações

O conteúdo é exclusivamente administrativo e não é canal de emergência ou prontuário. Descrições não devem conter identificação de pacientes. Anexos não foram expostos porque a base não apresentou armazenamento privado canônico completo; criar uma implementação pública improvisada violaria a regra de privacidade. Não houve alegação de SLA: sem prazo configurado, `prazo_resolucao` permanece nulo e a interface consumidora deve exibir **sem prazo definido**.

O contêiner não possui .NET SDK, PostgreSQL (`psql`) nem navegador configurado; por isso build, instalação limpa, upgrade, E2E e screenshots reais não foram produzidos nesta execução. A entrega não é declarada homologada. A próxima rodada deve concluir template MVC, comentários/anexos autorizados e adaptadores idempotentes aos serviços canônicos de cobertura e correção de presença.
