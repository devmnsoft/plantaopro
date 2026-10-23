# R98 (B6) — Assistente Drael em linguagem simples: resumo do pendente + orientação ao usuário final

## Objetivo

Linha 92 do plano (r66a0): "assistente Drael em linguagem simples (resumo do pendente,
orientação ao usuário final)" — o dono da organização (não um perfil técnico) pergunta o
que precisa saber agora e recebe, em português simples e sem jargão: como está o contrato
ou teste grátis, o que falta configurar e o que exige atenção na operação, com a ordem
prática de passos numerados.

Lacunas tratadas nesta rodada:
1. a camada canônica de assistentes (P2/Rodada 2 + R62) tinha apenas tarefas de perfil
   (resumo do Meu Dia, análise de cotação) — nenhum ponto de entrada para o **usuário
   final** na tela que ele efetivamente abre (`MinhaAssinatura`);
2. não havia tarefa que resumisse o pendente das três áreas de uma vez (contrato,
   configuração inicial, operação) em linguagem simples com teto de palavras.

Entregas: tarefa `RESUMO_PENDENTE` (jornada com contexto montado no servidor, prompt de
linguagem simples travado, regra VAZIO sem gastar provedor), rota nova, migração v2377
(checks de task_code), BFF + painel na tela do dono, drill E2E com stub local do provedor
Drael e contrato de 9 fatos.

## Recon

- Camada IA canônica pronta: gateway com configuração por tenant/tarefa, cota mensal
  atômica, slots distribuídos, orçamento, saída sanitizada e auditoria imutável em
  `ai_usos`; provedor Drael desde a R62 (wire OpenAI-compatível, chave do tenant tem
  prioridade sobre a global, `BaseUrl` sobrepõe o default `https://drael.sh/v1/`).
- Os códigos de tarefa `MEU_DIA_RESUMO`/`COTACAO_ANALISE` estavam fixados em `AiTaskCodes`
  (const + TryParse), na lista hardcodada de `ObterConfiguracoesAsync` e nas etiquetas da
  tela de configuração; adicionar tarefa nova = const + TryParse + lista + checks do banco.
- O painel do assistente já existia como padrão reutilizável (`.pp-ai-panel` com
  `data-ai-panel`, `data-ai-url`, `data-ai-method`, `data-ai-run` + script
  `ai-assistant-panel.js` auto-bind) mas só vivia na tela de central de ações.
- `MinhaAssinatura/Index.cshtml` (tela do dono) não tinha painel nem as seções
  Styles/Scripts; o controller BFF dela ainda não injetava `AiWebService`.
- Onboarding: `OnboardingJornadaService.ReavaliarAsync(tenantId, clientId)` reavalia as
  etapas contra os dados reais e persiste transições (origem AUTOMATICO/MANUAL); etapa
  com critério segue os dados, etapa sem critério guarda a verdade humana.
- Operação: `IProductivityActionService.ListAsync(ProductivityQuery)` filtra por janela
  (`DueTo`) e pagina — mesma fonte da central de ações.

## Decisões

- **Tarefa nova `RESUMO_PENDENTE`** (`POST /api/ai/tarefas/resumo-pendente`, guard de
  tenant igual às demais). Contexto montado **no servidor** em três áreas minimizadas:
  `[contrato]` (SQL direto em `assinaturas join planos`; prioridade
  TRIAL>ATIVA>SUSPENSA>VENCIDA; variante por status — dias restantes do teste grátis,
  próximo vencimento, janela pós-trial); `[configuracao]` (onboarding **reavaliado** via
  `ReavaliarAsync`, rótulos de catálogo com módulo e natureza obrigatória/opcional, teto
  de 12 etapas + linha de estouro); `[operacao]` (produtividade com prazo na janela de
  7 dias, teto de 15 linhas + estouro, atrasadas marcadas `ATARDA`). Minimização:
  descrição livre, evidência e conteúdo clínico nunca entram nas linhas.
- **Contrato de linguagem simples** constante `InstrucaoLinguagemSimples` (travado pelo
  contrato): frases curtas, "sem jargão técnico e sem siglas", agrupamento por área,
  ordem prática numerada, "Máximo de 160 palavras", fechamento "cada passo é feito na
  tela indicada e o assistente não aplica nada sozinho".
- **Regra VAZIO**: a jornada devolve null (a rota responde VAZIO **sem chamar o provedor
  nem gastar cota**) somente quando contrato ATIVA e 0 etapas pendentes e 0 itens
  operacionais; TRIAL/SUSPENSA/VENCIDA/contrato ausente sempre gera, porque a orientação
  sobre o contrato é o propósito da tarefa. O escopo (período, fuso
  America/Sao_Paulo, contagens por área) viaja no outcome e é exibido na UI.
- **Migração v2377** (padrão v2308: DROP/ADD dos CHECKs): `ai_config_task_code_check`
  passa a 3 códigos (`MEU_DIA_RESUMO`, `COTACAO_ANALISE`, `RESUMO_PENDENTE`) e
  `ai_usos_task_code_check` a 4 (mantém `TESTAR_CONEXAO`, que não tem linha de
  configuração por tenant). Manifest: entrada ativa dependendo de v2376, sha_lf
  `eb358852d1a71b2839a9fcfb1a001dfb7fafbc1b152c61a6e2e84502bdae7287`.
- **Web**: BFF `AiWebService.ResumoPendenteAsync` (POST sem payload, mesmo contrato de
  outcome das demais tarefas); ação `GET /MinhaAssinatura/ResumoIa` no controller da
  tela do dono (token da sessão; 401 com mensagem quando expirada); painel opcional
  `.pp-ai-panel` somente no branch com contrato vinculado (reaproveita CSS/script do
  painel padrão; botão "Pedir resumo"); etiqueta do código novo no mapa de tarefas da
  tela de configuração (administradores passam a ver o cartão).
- **Drill**: API standalone na porta 52079 contra o banco vivo + **stub local do Drael**
  (HttpListener na porta 52078 devolvendo JSON OpenAI-compatível em `chat/completions`);
  env `Ai__EncryptionKey` (64 hex) e `Ai__Providers:Drael__BaseUrl` apontando para o
  stub; mundos determinísticos pelo funil self-service com plano `R98_PLANO_DRILL`
  (trial 5 dias / ATIVA mensal); limpeza idempotente por CNPJ + linhas `ai_*` no início
  (rerun previsível); os dois mundos permanecem no banco ao fim.

## Achados

1. **Etapa de onboarding com critério segue os dados**: forçar `concluido=true` por SQL
   persiste a linha (a origem MANUAL não é revertida), mas o estado efetivo da
   reavalição continua PENDENTE/BLOQUEADO porque o critério avalia os dados reais
   (convite jamais enviado etc.). O mundo limpo do VAZIO só é determinístico removendo o
   `criterio_codigo` e concluindo MANUAL (a etapa cai na branca humana, onde
   `atendida = concluido`) — feito na preparação do drill e documentado aqui.
2. **NAO_HABILITADO também audita**: `FalhaAsync` grava linha em `ai_usos` (status
   FALHA) antes de qualquer chamada — o marcador "não gastou provedor" do drill conta
   apenas SUCESSO.
3. **`DateOnly - DateOnly` indisponível nesta toolchain** (CS0019): a diferença de dias
   da jornada vai por `ToDateTime(TimeOnly.MinValue)` (regra de calendário local
   `DateTime.Today` preservada).
4. **`CommandDefinition(sql, params, ct)` sem a sobrecarga de CancellationToken nesta
   versão do Dapper** (CS1503): as duas consultas internas da jornada omitem o CT no
   definitor (o `OpenAsync(ct)` segue honrando cancelamento).
5. **PowerShell 5.1 não aceita `$env:'Nome:Com:DoisPontos'`** (referência de variável
   inválida no parse): chave `Ai__Providers:Drael__BaseUrl` injetada via
   `[System.Environment]::SetEnvironmentVariable` (escopo de processo, herdado pela API).

## Evidência (execução canônica)

Linha a linha de `r98a_drill_saida.txt` (API porta 52079, stub local do Drael porta
52078, banco vivo `plantaopro_test`):

```
R98A_DRILL data=20261023-085819 db=plantaopro_test porta=52079 stub=52078
PORTAS_LIVRES_INICIO=SIM
ESTAGEM_1_STUB_DRAEL ok=SIM porta=52078
ESTAGEM_1_API_NO_AR ok=SIM pid=44136
PRE_LIMPEZA_RERUN ok=SIM
PA_MUNDOS_OK trial_tenant=5f30b8c2-4ad3-4cfb-9d2b-d64d12dea729 ativa_tenant=51e58744-bb97-4751-a66b-7ebd23e12cd6
AI_TAREFA_NAO_HABILITADA_OK http=200 status=NAO_HABILITADO
AI_CONFIG_OK http=200 task=RESUMO_PENDENTE chave=SIM
AI_TESTE_CONEXAO_OK http=200 status=OK
AI_GERACAO_OK http=200 sucesso=SIM provedor=drael texto=SIM
AI_USO_REGISTRADO_OK task=RESUMO_PENDENTE sucesso=1 provedor=drael
AI_COTA_EXCEDIDA_OK http=200 status=COTA_EXCEDIDA sucesso=1
AI_VAZIO_OK http=200 status=VAZIO
PORTAS_LIVRES=SIM
RESULTADO=PASSEI
```

Fluxo: NAO_HABILITADO antes de configurar (jornada TRIAL sempre gera; sem linha de
configuração nenhuma chamada sai); dono (ADMINISTRADOR_CLIENTE) habilita
`RESUMO_PENDENTE` com provedor drael, chave do tenant e **cota mensal de 1 uso**;
test-conexao usa a chave do tenant contra o stub; geração OK com texto sanitizado e
escopo explícito; auditoria registra exatamente 1 SUCESSO com o novo código; segunda
geração no mesmo mês ⇒ COTA_EXCEDIDA sem novo sucesso; mundo ATIVA limpo ⇒ VAZIO com
sucesso sem gastar provedor.

Banco vivo pós-upgrade: `aplicadas=151 esperadas=147 falhas=0 pendentes=0`. Manifest:
**147 migrações ativas** (v2377 nova; sha_lf
`eb358852d1a71b2839a9fcfb1a001dfb7fafbc1b152c61a6e2e84502bdae7287`). Checks vivos
verificados: as duas tabelas aceitam `RESUMO_PENDENTE`; `TESTAR_CONEXAO` permanece só na
auditoria.

## Contrato

`R98ResumoPendenteLinguagemSimplesContratoTests` — 9 fatos:
1. Saída do drill E2E: todos os marcadores máquina-legíveis (stub, API, limpeza, mundos,
   NAO_HABILITADO, config, test-conexao, geração drael, uso registrado, cota esgotada,
   VAZIO), `RESULTADO=PASSEI` e nenhum `FALHA=`;
2. v2377 ativa no manifest (`dependsOn` v2376, checksum coerente pela lógica CRLF→LF do
   runner, fragmentos do SQL) + checks **vivos** nas duas tabelas no banco de teste;
3. Tarefa registrada nos contratos de código: `AiTaskCodes.TryParse` (positivo/negativos/
   caixa), listagem de configuração expõe o terceiro cartão, DI em `Program.cs` e rota no
   source;
4. Jornada com mundo TRIAL: as três áreas no contexto (dias restantes do teste, etapas
   obrigatória/opcional com módulo, operação com `ATARDA`), tetos (15+estouro; página
   25; janela 7 dias), escopo America/Sao_Paulo com contagens e marcadores do prompt;
5. Regra VAZIO: ATIVA limpa ⇒ null; TRIAL limpo ⇒ gera com orientação do teste; ATIVA +
   1 item operacional ⇒ gera;
6. Minimização: descrição livre/evidência do onboarding nunca aparece nas linhas; 30
   etapas pendentes ⇒ 12 listadas + 1 de estouro;
7. Gateway ponta a ponta com stub do Drael: chave do tenant + cota 1, saída sanitizada
   (HTML escapado), auditoria com o novo código de tarefa, segunda geração ⇒
   COTA_EXCEDIDA **sem nova chamada ao provedor**;
8. Rota: sem tenant ⇒ NAO_HABILITADO antes de qualquer IO; ATIVA limpa ⇒ VAZIO com
   sucesso sem tocar no gateway; com pendências ⇒ taskCode exato e outcome passa íntegro;
9. Camada Web pinada no source: BFF com a rota da API, ação `ResumoIa`, painel com
   `data-ai-url`/`data-ai-run`/seções Styles+Scripts e etiqueta na tela de configuração.

Suíte: 1812 → 1821.

## Gates

- Build A/B Release `--no-incremental` (tag r98): `base-exit=0 nova-exit=0`; avisos únicos
  **76 por lado**, `DIFF=NENHUMA`.
- Suíte completa pré-doc (`--no-build -c Release`): **1821/1821 aprovados** (~39 s) —
  inclui higiene `RepositoryHygieneContractTests` 8/8.
- Docscan pós-doc: 0 defeitos.
- Suíte completa pós-doc (`--no-build -c Release`): **1821/1821 aprovados**.
- Sem `r75_gap` nesta rodada: nenhuma página/artigo novo (painel adicionado à view
  existente `MinhaAssinatura/Index.cshtml`).

## Pendências

- **Renovação ANUAL no `gerar-mensal`** (herdada R97): um ANUAL renovaria cobrando o
  preço anual todo mês — corrigir o ciclo de renovação em rodada futura.
- Fatura pós-trial (expiração sem conversão): TRIAL vencido segue 403 até conversão; o
  fluxo formal de solicitações de mudança/conversão de plano segue para a R101.
- Limpeza do resíduo WP-A1 (2 usuários sem link `ADMINISTRADOR_GLOBAL`, `tenant_id null`).
- (Herdadas) Órfãs adm360 vales/cotações (FKs NOT VALID); endurecer o wrapper B1 contra
  stderr benigno; conciliar `scrpt_completo.sql`/`instalar_no_banco_atual.sql` × manifest
  (inclui v2377 — mesma prática de não atualizá-los agora, herdada de R96/R97); política
  de retenção de backups.
