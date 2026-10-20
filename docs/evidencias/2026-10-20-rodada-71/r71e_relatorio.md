# R71E — Chave global da Drael registrada + prova ponta a ponta (pedido do usuário, fora do plano)

Data: 2026-10-20 · Série: rodada 71 (lote extra r71e) · Estado: FECHADA (provas verdes, pushado em main)

## Pedido do usuário

Chave da Drael (IA da casa) fornecida pelo usuário com o exemplo de chamada canônico:

> `dk-…` em `curl https://drael.sh/v1/chat/completions -H "Authorization: Bearer dk-…" -d '{"model":"drael-v1",…}'`

A R71C deixou a Drael pronta como provider esperando a chave; este lote faz o cadastro dela
como **chave global do servidor** e prova o caminho completo, do arquivo local até a
chamada real ao provedor.

## O que mudou

1. **`.env` (git-ignorado)** — `Ai__Providers__Drael__ApiKey=dk-Rl…pRds` registrada ao lado
   da chave Gemini existente. Convenção do repo: segredos fora do repo; nunca em appsettings.
2. **`scripts/local/run-dev-start.ps1`** — bootstrap novo: carrega o `.env` como fonte de
   variáveis de ambiente dos processos API/Web (não sobrescreve variável já definida no
   terminal; valores entram na lista de redação do diagnóstico). Antes, só os scripts de
   e2e injetavam as chaves manualmente; agora o procedimento único de desenvolvimento
   (parar → compilar → iniciar) já incorpora as IAs configuradas.
3. Evidências versionadas desta pasta: `r71e_drael_e2e.ps1` (+ `r71e_drael_e2e_saida.txt`) e
   `r71e_launcher_probe_saida.txt`.

## Provas (todas em 2026-10-20)

### 1 · Wire format (chamada direta, sem o sistema)

`POST https://drael.sh/v1/chat/completions` com `Bearer dk-…` e `model=drael-v1`:
**HTTP 200**, `model=drael-v1`, resposta útil recebida (prompt de 1638 tokens → 113 de saída).

### 2 · E2E pelo sistema (API Release + banco de teste) — `r71e_drael_e2e.ps1`

Fluxo espelhando o r66i (Gemini): sobe a API com a chave injetada a partir do `.env`,
login do gestor demo, lê a configuração de IA e dispara o teste de conexão oficial:

```
API_OK 10:57:10 chave=dk-Rl...pRds
LOGIN_OK tenantRequired=False roles=ADMINISTRADOR_CLIENTE
TEST_CONEXAO_STATUS=200
TEST_RESULTADO success=True kind=OK
TEST_MENSAGEM Conexão confirmada com o provedor drael. Usando a chave do servidor.
E2E_RESULTADO=SUCESSO
```

Notas semânticas observadas no log:
- A linha `CHAVE_MESTRA_SERVIDOR=False` é a chave de CRIPTOGRAFIA dos segredos do tenant
  (`AiSecretProtector.HasMasterKey`), não a chave do provedor — esperada ausente neste
  cenário e sem efeito na prova (o teste usou "a chave do servidor" da Drael).
- Quota de testes de conexão vigendo: 10 por tenant/provedor em 24h, auditados em `ai_usos`.

### 3 · Pelo fluxo normal de desenvolvimento (stack Debug via launcher, banco dev)

`run-dev-stop → run-dev-build → run-dev-start` (build head `0d2cd82`), depois probe HTTP:

```
HEALTH=200
LOGIN_OK tenantRequired=False roles=ADMINISTRADOR_CLIENTE
CFG MEU_DIA_RESUMO prov=gemini hab=True chaveTenant=False chaveGlobal=True
CFG COTACAO_ANALISE prov=gemini hab=True chaveTenant=False chaveGlobal=True
TEST_DRAEL_STATUS=200
TEST_DRAEL success=True kind=OK
MSG Conexão confirmada com o provedor drael. Usando a chave do servidor.
PROBE_RESULTADO=SUCESSO
```

- `chaveGlobal=True` nas tarefas confirma que o `.env` foi carregado pelo launcher
  (a chave Gemini também passou a estar disponível no processo).
- A Drael respondeu de verdade pelo endpoint oficial `/api/ai/test-conexao`.

## Efeito prático para o sistema

- Qualquer tarefa de IA pode usar **Drael sem chave por cliente**: o gateway usa a chave
  global quando o tenant não forneceu a própria (chave do tenant sempre tem prioridade).
- Na tela Assistente IA, Drael aparece com chave global disponível e "Testar conexão"
  respondendo com o provedor real.
- Gemini continua registrado com a chave do usuário; seu 403 segue sendo externo (projeto
  Google), sem ação no repo. Groq/Mistral seguem prontos para chave gratuita do cliente
  (R71C).

## Gates

| Gate | Resultado |
|---|---|
| Build Release `--no-incremental` | EXIT=0 · 81 avisos (idêntico à base r71c) · 0 erros |
| Suíte completa (apps parados antes) | **1671 aprovados / 0 falhas** (nenhum código .cs mudou neste lote) |
| Ciclo dev `run-dev-stop/build/start` | BUILD OK (0 erros) · readiness + liveness OK · parada limpa |
| Docscan U+00C3 (arquivos alterados/novos) | BAD=0 |

## Commit

`scripts docs (rodada 71 r71e): …` — `.env` não é versionado (segredo); versionados ficam o
launcher, o script de e2e e as saídas de prova.
