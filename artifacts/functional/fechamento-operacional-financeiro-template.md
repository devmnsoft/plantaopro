# Fechamento operacional e financeiro — implementação e evidências

## Base comprovada

A árvore estava limpa em `e84117b`; o baseline foi registrado no commit `fe9debf`. Não há `AGENTS.md` no repositório, em seu ascendente imediato ou até profundidade 2 da raiz. Foram lidos os workflows, o histórico recente e os relatórios de execução/conferência (`execucao-conferencia-apuracao-design.md`) e fechamento/demonstrativo (`fechamento-demonstrativos-design.md`). Relatórios foram usados como inventário, não como prova de execução.

O ambiente desta rodada não fornece `dotnet`, PostgreSQL, Docker ou navegador Chromium/Chrome. Logo, restore, compilação, testes .NET, banco descartável, E2E autenticado e screenshots reais ficaram bloqueados. Não houve imagem simulada e este documento **não declara homologação**.

## Fontes canônicas reutilizadas

| Responsabilidade | Implementação preservada |
|---|---|
| execução e conferência | `escalas`, `medico_checkins`, `medico_presenca_correcoes` e seus serviços existentes |
| regra de remuneração | `plantoes.valor` + única implementação `PlantaoPaymentCalculator` (base de 12 horas, decimal e arredondamento final) |
| apuração/fechamento | `fechamento_plantao`, snapshots em `fechamento_plantao_escalas`, divergências e histórico |
| demonstrativo/obrigação | `pagamentos`, `historico_pagamento` e `financeiro_pagamento_origem` |
| ajuste/contestação | `pagamento_contestacoes`; nenhum valor consolidado é sobrescrito por correção operacional |
| pagamentos | consultas e baixa manual existentes; nenhum gateway, banco ou cobrança SaaS foi adicionado |
| interface | tela existente `/fechamentos`, pagamentos do profissional e design system PlantãoPro |

## Funcionalidades entregues

1. A prévia agora expõe critério de competência, itens elegíveis, duração prevista/conferida, memória com base, quantidade, regra, vigência, arredondamento e total.
2. Valor ou regra ausente deixou de ser um zero silenciosamente apto ao fechamento: vira bloqueio `REGRA_REMUNERACAO_AUSENTE`.
3. Uma validação comum verifica item vazio, conferência revogada, origem cancelada/alterada, regra ausente e correções abertas. A mesma validação alimenta a tela e é repetida sob locks na aprovação e geração financeira.
4. Mudanças de status, intervalo ou valor da origem depois da prévia impedem confirmação com conflito explícito. O snapshot fechado continua sendo a memória histórica.
5. Aprovação repetida consulta o fechamento persistido; geração financeira já era idempotente e continua protegida por lock, unicidade por escala e vínculo único de origem.
6. A tela separa total elegível, ajustes consolidados e total final; propostas de divergência não viram ajuste financeiro automaticamente.
7. Bloqueios permanecem visíveis, apontam para a conferência de origem, e desabilitam a ação visual sem substituir a validação do servidor.
8. Impressão usa os mesmos dados da consulta autenticada, CSS específico e texto que a identifica como impressão do navegador, não PDF assinado/comprovante.
9. Layout mobile empilha ações, mantém alternativa em blocos para tabela, valores à direita e foco visível.

## Matriz efetiva de estados

| Processo | Origem | Comando | Destino | Perfil | Pré-condições e efeito persistido | Ajuste/reversão |
|---|---|---|---|---|---|---|
| execução | escala confirmada | check-in/check-out | incompleta/pendente | profissional vinculado | vínculo, tenant e atribuição; persiste presença | correção versionada, sem apagar original |
| conferência | pendente/correção pendente | aprovar/recusar | aprovada/recusada | gestor operacional autorizado | versões e autoria válidas; histórico persistido | nova correção; após consolidação cria pendência, não diferença financeira |
| apuração | plantão realizado + execução conferida | gerar prévia | `ABERTO` | gestão de escalas | transação, locks, snapshots de item e histórico | divergência; fechamento ativo único por tenant/plantão |
| conferência da apuração | `ABERTO`/`DEVOLVIDO` | iniciar | `EM_CONFERENCIA` | gestão de escalas | transição condicional e histórico | pode abrir divergência |
| pendência | `EM_CONFERENCIA` | abrir divergência | `COM_DIVERGENCIA` | gestão de escalas | item pertence ao fechamento; divergência persistida | resolução auditada retorna a conferência |
| apuração | `EM_CONFERENCIA` | concluir | `AGUARDANDO_APROVACAO` | gestão de escalas | nenhum bloqueio: conferência, origem, regra e correções válidas | administrador pode devolver/rejeitar |
| fechamento | aguardando aprovação | aprovar | `APROVADO` | administrador autorizado | revalidação transacional sob lock; snapshot preservado | não edita consolidado; contestação/ajuste canônico |
| demonstrativo/obrigação | `APROVADO` | gerar financeiro | `FINANCEIRO_GERADO` | financeiro | revalidação, pagamento por escala, origem e histórico na transação | retry devolve persistido; contestação não apaga histórico |
| demonstrativo profissional | obrigação autorizada | consultar | sem mutação | próprio profissional | vínculo por usuário e tenant no serviço existente | contestação existente |
| pagamento | obrigação aprovada | baixa manual | pago | financeiro | valor/data/referência e autoria pelo serviço existente | cancelamento/contestação conforme estados canônicos; sem estorno novo |

Execução conferida, fechamento aprovado, obrigação financeira, demonstrativo consultável e pagamento são estados diferentes. Nenhuma abertura de tela ou impressão registra pagamento.

## Cálculo, período e concorrência

A competência é determinada por `plantoes.data_inicio`. Plantões que atravessam meia-noite ou mês ficam integralmente na competência do início; não existe rateio implícito. A base atual é o valor do plantão para 12 horas; a duração é calculada em decimal, com mínimo existente de uma hora, e o valor é arredondado a duas casas com `MidpointRounding.AwayFromZero` somente ao final. A UI apenas apresenta a memória produzida pelo backend.

A prévia materializa status, intervalo, horas e valor calculado. Na confirmação, o serviço bloqueia fechamento e origens (`FOR UPDATE`), recalcula com a implementação canônica e compara com o snapshot. Restrições existentes `ux_fechamento_plantao_ativo`, unicidade do item e `unique(tenant_id, escala_id)` na origem financeira protegem dupla inclusão. A transição usa status esperado; retry de aprovação aprovada ou geração já realizada devolve o resultado persistido.

## Autorizações e auditoria

As rotas mantêm segregação: gestão de escalas gera/confere, administrador aprova/devolve/rejeita, financeiro gera obrigação e profissional consulta somente pagamentos ligados ao próprio usuário. Todas as consultas de fechamento incluem tenant e cliente. Geração, transições, divergências, origem financeira e histórico permanecem auditáveis com ator e instante. Nenhuma credencial, documento ou dado bancário foi adicionado a logs.

## Testes adicionados

Foram adicionados testes unitários para travessia de competência e arredondamento, mais contratos para revalidação de origem/conferência/regra/correção, memória visual, separação de pagamento e restrições persistidas de unicidade.

### Resultado dos gates

- `git diff --check`: aprovado.
- `python3 scripts/check-csharp10-compatibility.py`: aprovado.
- `python3 scripts/check-feedback-ui.py`: aprovado após remoção de `!important` da folha alterada.
- `python3 scripts/repository-security-check.py`: falhou em configurações preexistentes de connection string, banco legado e auto-create em `appsettings.json`; esta entrega não alterou esses arquivos.
- Comandos `dotnet restore/build/test`: bloqueados porque `dotnet` não está instalado.
- PostgreSQL limpo/upgrade, concorrência com duas conexões e E2E: bloqueados por ausência de runtime/banco/navegador.

## Decisões pendentes

- O domínio não possui ajuste monetário pós-fechamento plenamente modelado com aprovação e efeito no demonstrativo. A entrega mantém ajustes consolidados em zero e bloqueia a promessa de alteração automática; a política deve ser definida antes de habilitar esse caso.
- A regra atual é a base de 12 horas materializada no sistema. Tarifas por unidade/profissional e vigências próprias não existem no modelo canônico; não foi criado um segundo cadastro. Valor ausente bloqueia em vez de assumir zero.
- O fechamento permanece integral por plantão. Fechamento parcial por competência/profissional não foi inventado.
- A listagem existente limita o conjunto a 500 e ainda não fornece paginação SQL. Os totais são identificados como seleção retornada/fechada; paginação real exige contrato de consulta paginada numa próxima evolução.
- Publicação formal separada do demonstrativo não existe como estado próprio; `FINANCEIRO_GERADO` significa somente obrigação criada. A interface não o apresenta como transferência ou comprovante.
