# PlantãoPro v2.16.6 — Central de Escalas e Cobertura

## Base confirmada

- Commit inicial preservado: `b3bd2a0d478bcc1d245ef368ac9b6ddd5c589698`.
- A solução mantém .NET/C# com MVC/Razor, Dapper e PostgreSQL. Não foi criado um segundo domínio de escala ou convite.
- `ModuleContractingService` mantém contratação separada da operação; convidar ou aceitar não escreve contratos, preços ou pagamentos.
- A regressão CS0122 está corrigida: `V2160ClinicalJourneyContractTests` usa `RepositoryPathResolver.RepoRoot`; o `Lazy<string> Root` do resolver continua privado.
- Convite operacional é `plantao_convites`. Convites de identidade/vínculo não participam deste fluxo.

## Matriz curta de capacidade e lacuna

| Capacidade existente | Lacuna encontrada | Regra aplicada em v2.16.6 | Arquivo responsável | Cenário |
|---|---|---|---|---|
| Central lista riscos e abre lista/calendário | aceite mobile reservava convite em conexão separada | convite, vaga e escala mudam em uma transação | `Data.cs`, `MobileController.cs` | convite → aceite → cobertura |
| `vagas_disponiveis` representa capacidade | duas pessoas podiam observar a última vaga | `FOR UPDATE` serializa o plantão | `Data.cs` | dois aceites para última vaga |
| conflito usa intervalos semiabertos | dois plantões distintos não compartilhavam lock | advisory transaction lock pela identidade canônica `medico_id` | `Data.cs` | mesmo profissional aceita horários sobrepostos |
| índice ativo evita escala duplicada | repetição devolvia erro genérico | convite `ACEITO` + escala ativa devolve resultado consolidado | `Data.cs` | repetição do mesmo aceite |
| convite possui `expira_em` | emissão deixava validade indefinida | emissão operacional expira em 24 horas | `BusinessRulesServices.cs` | convite expirado |
| histórico de escala já existe | aceite do convite criava solicitação, não confirmação | aceite cria escala `confirmado`, histórico e reduz capacidade | `Data.cs` | gestor e profissional veem a mesma confirmação |

## Estados canônicos e transições usadas

| Agregado | Estados usados pelo projeto | Transição | Ator e condições | Efeito |
|---|---|---|---|---|
| plantão | `aberto`, `em_escala`, `preenchido`, `realizado`, `cancelado` | `aberto/em_escala → preenchido` | aceite autenticado, última vaga | decrementa `vagas_disponiveis` uma única vez |
| convite operacional | `ENVIADO`, `PENDENTE`, `ACEITO`, `RECUSADO` | `ENVIADO/PENDENTE → ACEITO` | o próprio profissional, antes de `expira_em` | registra resposta na mesma transação da escala |
| escala/alocação | `solicitado`, `confirmado`, `realizado`, `cancelado`, `substituido` | inexistente → `confirmado` | aceite válido de convite | cria compromisso e histórico; não cria pagamento |
| escala/alocação | `confirmado → cancelado` | gestor/profissional conforme endpoint vigente, com justificativa | preserva linha e histórico, reabre capacidade |

Convites pendentes não ocupam capacidade. A cobertura é a diferença entre `vagas` previstas e `vagas_disponiveis`, alterada somente por escala confirmada/cancelada. Notificação persistida, entrega externa e aceite continuam eventos distintos; a mensagem da API afirma apenas persistência.

## Revalidação e concorrência

O aceite não reutiliza a fotografia de elegibilidade da emissão. Ele revalida: cliente ativo da requisição, dono autenticado do cadastro profissional, vínculo/cadastro ativo, bloqueio, CRM/UF, especialidade, indisponibilidade, limite configurado, validade, estado do plantão, vaga e conflito corrente. O filtro por `cliente_id` faz identificador de outro cliente parecer inexistente.

1. `pg_advisory_xact_lock(hashtextextended(medico_id, 2166))` protege a agenda do profissional inclusive entre plantões/clientes processados por esta jornada.
2. `FOR UPDATE` no convite impede duas respostas simultâneas ao mesmo convite.
3. `FOR UPDATE` no plantão e o check constraint de vagas protegem capacidade e a última vaga.
4. O índice parcial `ux_escala_ocupacao_ativa` permanece a defesa final contra duplicidade da pessoa no plantão.
5. Sobreposição usa `inicio < fim_existente AND fim > inicio_existente`: plantões adjacentes são permitidos e plantões que atravessam meia-noite são comparados pelo instante completo.

Após timeout/erro a resposta orienta consultar a agenda; uma repetição observa `ACEITO` e retorna a escala persistida sem nova escala, capacidade ou notificação. Uma violação única concorrente vira HTTP 409 compreensível.

## Interface e navegação preservadas

A Central existente segue sendo a entrada do gestor, com indicadores, fila de risco e alternativas por calendário e lista. O detalhe apresenta unidade, especialidade, intervalo, capacidade e links para escalas/agenda. A Central Meu Dia segue agregando itens de convite e agenda conforme perfil. O layout já usa os tokens/componentes responsivos do design system e não depende de arrastar, `alert()` ou `confirm()`.

Limitação consciente: os indicadores atuais vêm do resumo operacional existente e alguns representam somente “hoje”; eles não devem ser apresentados como contagem arbitrária filtrada. A evolução não inventa valor contratual, ranking profissional, multa, regra de reconfirmação ou entrega por provedor. Alterações de compromisso já confirmado continuam no fluxo vigente; política adicional de reconfirmação permanece decisão de produto.

## Evidências e testes

- Contratos v2.16.6 verificam locks, isolamento por cliente, idempotência, validade, consumo de capacidade e a regressão do resolver.
- A suíte PostgreSQL deve executar em banco descartável para demonstrar disputa da última vaga, conflito cruzado, expiração, revogação e cancelamento. Neste ambiente, o SDK e um PostgreSQL de teste não estavam disponíveis; esses checks não são declarados aprovados.
- Não foram enviadas mensagens a profissionais reais. A implementação somente cria a notificação persistida já existente.

## Limitações da entrega

Não há alegação de ausência absoluta de defeitos. Validação visual desktop/mobile, teclado, console, screenshot da aplicação real, instalação limpa e upgrade exigem runtime .NET, navegador e PostgreSQL com dados isolados. Esses itens devem permanecer visíveis no PR/CI como não executados até haver evidência real.
