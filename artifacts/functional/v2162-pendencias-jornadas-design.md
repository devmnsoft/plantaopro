# PlantãoPro v2.16.2 — Central de Pendências e consolidação de jornadas

## Estado real e base

- Commit inicial: `9030660`, merge da v2.16.1, árvore de trabalho limpa. A branch de entrega é `codex/v2162-pendencias-jornadas-design`.
- Foram lidos o histórico recente, os workflows `dotnet-ci.yml` e `database-one-click.yml` e os relatórios v2.16.0/v2.16.1. A v2.16.1 corrigiu no código o trigger inválido, o vínculo tenant/paciente e a atomicidade clínica; nesta máquina, porém, essas correções não podem ser declaradas homologadas porque não há `dotnet`, `psql` nem Docker.
- A inspeção encontrou uma Central de Ações real já compartilhada com Meu Dia. Ela foi evoluída; não foi criado outro dashboard. A Central Operacional, Minha Central e notificações permanecem consumidores especializados, sem ganhar uma segunda máquina de estados.

## Matriz curta do que existia

| Capacidade | Estado inicial | Origem/consumidor | Regressão v2.16.2 |
|---|---|---|---|
| Convites pendentes | existente | `cobertura_convites` / Central | Mantido, tenant e médico na consulta |
| Escalas aguardando confirmação | ausente na Central | `escalas` | Incluído pelos estados abertos da origem |
| Pagamentos em conferência | parcial (fechamento, não pagamento) | `pagamentos` | Incluído com vínculo do médico |
| Contestações | existente | `pagamento_contestacoes` | Mantido somente para financeiro autorizado |
| Chegada/check-in do paciente | existente | `agendamentos` + ausência de `checkins` | Mantido somente para perfil clínico operacional |
| Atendimento em rascunho | ausente na Central | `consultas` | Incluído com vínculo do profissional |
| Plantão sem cobertura | sobreposição fora do escopo aceito | `plantoes` | Removido desta Central; continua na origem operacional |
| Resumo do conjunto filtrável | parcial, métricas sobre no máximo 100 linhas | consulta paginada | Agregação SQL independente da página |
| Falha, vazio e acesso negado | parcial | Web service/view | Estados separados; 403 não é vazio |

## Contrato das pendências

| Categoria / identidade estável | Abre quando | Encerra quando | Destino canônico |
|---|---|---|---|
| `OPERACAO:CONVITE:{id}:RESPONDER` | convite `PENDENTE` | aceito, recusado, cancelado ou expirado | Convites |
| `OPERACAO:ESCALA:{id}:CONFIRMAR` | escala solicitada/pendente/aguardando confirmação | status da escala deixa o conjunto aberto | detalhe da Escala |
| `FINANCEIRO:PAGAMENTO:{id}:CONFERIR` | pagamento ativo pendente/em conferência/atrasado | confirmação, pagamento, cancelamento ou inativação na origem | detalhe Financeiro |
| `FINANCEIRO:CONTESTACAO:{id}:RESOLVER` | contestação `ABERTA` | resolução/cancelamento na origem | Contestações financeiras |
| `CLINICO:AGENDAMENTO:{id}:CHECKIN` | agendamento de hoje sem check-in ativo | check-in, cancelamento ou mudança de estado | Agenda |
| `CLINICO:CONSULTA:{id}:CONTINUAR` | consulta em atendimento/rascunho | finalização/cancelamento na origem | workspace da Consulta |

A atualização sempre refaz a consulta das entidades de origem. Não existe “concluir” genérico. O adiamento grava somente estado de apresentação por `(tenant, usuário, chave)` e não muda a data relevante. Antes de adiar, a API relê a origem e a autorização; o `upsert` torna repetição idempotente. Troca de tenant muda todos os parâmetros e a chave do estado, impedindo uma ação antiga de atravessar o contexto.

## Autorização, consulta e experiência

- A API decide as categorias antes do SQL por perfil e permissão financeira; toda origem é limitada por tenant, e médico recebe apenas seus registros vinculados. Recepção não recebe evolução clínica em rascunho de outro profissional; financeiro não recebe texto assistencial.
- Itens são ordenados por faixa operacional existente, data relevante, criação e identidade derivada. A Central não cria prazo clínico ou contratual; mostra explicitamente a data utilizada. Paginação permanece no servidor e o resumo agora agrega todo o conjunto visível, não a página.
- Os indicadores são links para suas listas. Módulo virou seleção fechada e período é enviado como intervalo parametrizado. Cada linha explicita situação, origem, motivo, responsável funcional e ação real, sem mostrar GUID.
- Falha HTTP/conectividade apresenta indisponibilidade; 403 apresenta acesso negado; apenas resposta bem-sucedida sem itens apresenta vazio. Requisições usam `CancellationToken`, e navegação GET preserva filtros na URL e no histórico do navegador.
- O diálogo acessível de adiamento continua validando antes do loading, preserva foco e não usa `alert()`, `confirm()`, `href="#"` ou armazenamento local.

## Regressões e evidências

- Adicionado contrato automatizado para as seis fontes, escopo tenant, resumo integral, revalidação antes de adiar, ausência de conclusão genérica e estados completos da tela.
- `python3 scripts/check-csharp10-compatibility.py`: passou.
- `python3 -m unittest discover -s scripts/tests -v`: 11 testes passaram.
- `git diff --check`: passou antes do commit.
- Builds Debug/Release, xUnit, PostgreSQL isolado, instalação limpa, upgrade e E2E autenticado não foram executados localmente: SDK .NET, `psql` e Docker não estão instalados. Isso é limitação do ambiente, não sucesso presumido.
- Screenshot atual não foi produzida porque a aplicação real não pode ser compilada/iniciada neste executor. Imagens antigas não foram reutilizadas.

## Gates obrigatórios antes de homologação

O CI deve executar restore, builds Debug/Release, xUnit e os jobs PostgreSQL dos workflows. Ainda devem ser comprovados em aplicação real: dois tenants, dois usuários, módulo não contratado, revogação e acesso direto; duplo clique e mudança concorrente; troca de contexto e resposta atrasada; falha parcial por categoria; paginação estável; e E2E login → contexto → filtro → origem → resolução → retorno → atualização, com teclado, foco, console, desktop/mobile e screenshots atuais. Até essas evidências passarem, este relatório não declara homologação nem recomenda merge automático.
