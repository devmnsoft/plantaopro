# Assistente IA (camada canônica) — PlantãoPro

- **Data**: 2026-10-03 (rodada 1) · atualizado 2026-10-04 (rodada 2 — em curso) · **Baseline**: commit `82b8c26` (`main` = `origin/main`, pushado; contém WP-S1/S2/S3 e migrações v2305/v2306/v2307) · **Suíte**: 794/794 green (reproduzida em 2026-10-04 sobre árvore limpa em `82b8c26`)
- **Prontidão técnica**: implementada e validada por suíte de testes + smoke funcional com stack real no ar (3 perfis).
- **Homologação com provedor real**: **NAO EXECUTADA** nesta rodada — sem chave de provedor, cada ponto de IA devolve o estado explícito `NAO_CONFIGURADO` (comprovado por probe). A integração com o provedor externo só fica comprovada em ambiente de homologação com chaves reais (mocks/unitários não contam como prova de integração, por regra da pauta).

## 1. O que é

Camada canônica reutilizável de assistentes IA. A Web (BFF) chama os endpoints `api/ai/*`; a camada monta contexto, aplica governança e chama um adaptador de provedor. **Entrega inicial** (escopo fechado desta homologação): 3 adaptadores e 2 jornadas.

| Tarefa (código canônico) | Jornada | Ponto na UI |
|---|---|---|
| `MEU_DIA_RESUMO` | Resumo das pendências do Meu Dia | painel em `/Pendencias` |
| `COTACAO_ANALISE` | Análise de cotação/documento | painel nos `Detalhes` da cotação (Administrativo 360) |

Novas tarefas entram no backlog (exigem registro em `AiTaskCodes` + ajuste do CHECK da migração v2307).

**Princípios inegociáveis implementados:**
- **IA sugere, humano confirma.** Nenhuma ação de negócio é executada pela IA; nada aprova, transmite, altera, baixa, prescreve ou finaliza. Toda ação continua passando pelos serviços de negócio existentes com a validação deles.
- **Determinístico primeiro.** Pré-checagens (sem pendências, tarefa desabilitada, cota esgotada, ID inexistente) respondem SEM gastar chamada de LLM.
- **Contexto montado no servidor, pós-autorização.** A UI nunca monta prompt nem escolhe dados.

## 2. Arquitetura

```
UI (painéis, fetch c/ token CSRF)
  → Web BFF: Pendencias (GET ResumoIa) · Adm360CotacoesWebController (POST {id}/AnaliseIa) · AssistenteIa (config)
    → API /api/ai/* (AiController) — sessão do app
      → AiGateway — governança: habilitação por tenant, chave, cota mensal, timeout, fallback, auditoria
        → adaptadores Groq / Gemini / DeepSeek (wire OpenAI-compatible / nativos)
```

**Persistência** — migração idempotente `database/migrations/2026_10_v2307_ai_camada_canonica.sql` (manifest **v2.21.6**, ordem 76):
- `plantaopro.ai_config` — `tenant_id × task_code` (UNIQUE), `habilitada`, `provedor`, `modelo`, `fallback_provedor`, `api_key_cifrada bytea`, `chave_mascara`, `limite_tokens_entrada/saida`, `timeout_s`, `cota_mensal_usos`, `orcamento_mensal`. CHECKs no banco para tarefa, provedores e faixas. Sem FK de tenant (convenção das tabelas operacionais).
- `plantaopro.ai_usos` — auditoria de usos: tenant, usuário, tarefa, contexto, provedor/modelo, tokens, duração, fallback usado, classe de erro. **Sem segredos, sem prompt, sem trecho de dado clínico.**

## 3. Rotas

### API — `backend/PlantaoPro.Api/Controllers/AiController.cs`
| Rota | Uso |
|---|---|
| `GET api/ai/config[/{task}]` | Visão mascarada da configuração (chave só como `…1234`) |
| `PUT api/ai/config/{task}` | Atualização (admin do tenant). **`ApiKey` vazia PRESERVA a chave já gravada** |
| `POST api/ai/test-conexao` | Teste de conexão autorizado (body: `provedor` opcional) |
| `POST api/ai/tarefas/meu-dia-resumo` | Resumo das pendências do usuário autenticado |
| `POST api/ai/tarefas/analise-cotacao` | Análise de cotação (body: `cotacaoId` — validado no escopo do tenant) |

### Web (BFF — CSRF obrigatório em tudo que escreve/executa)
| Rota | Uso / acesso |
|---|---|
| `GET /AssistenteIa` | Página de configuração — `ADMINISTRADOR_GLOBAL`, `ADMINISTRADOR`, `ADMINISTRADOR_CLIENTE` |
| `POST /AssistenteIa/Salvar/{task}` · `POST /AssistenteIa/TestarConexao` | Configurar tarefa / testar conexão |
| `GET /Pendencias/ResumoIa` | Painel Meu Dia (leitura; módulo comum MEU_DIA) |
| `POST /Administrativo360/Cotacoes/{id}/AnaliseIa` | Painel Administrativo 360 (gates ADM360 + escopo de tenant) |

## 4. Configuração de segredos (só no servidor)

Nenhuma chave vai para o cliente nem volta inteira para a UI. Chaves dos tenants são cifradas em repouso (AES-GCM) com a chave mestra do servidor.

| Variável (env / user-secrets do processo **API**) | Descrição |
|---|---|
| `Ai__EncryptionKey` | 64 chars hex (32 bytes) — cifra as chaves por tenant. Ausente ⇒ salvando **chave vazia** funciona; salvando **chave não-vazia** a API devolve erro claro ("Configure a chave mestra do servidor") e a página `/AssistenteIa` exibe o aviso. |
| `Ai__Providers__Groq__ApiKey` / `Ai__Providers__Gemini__ApiKey` / `Ai__Providers__DeepSeek__ApiKey` | Chaves **globais do servidor** (fallback aprovado quando o tenant não tem chave). |
| `Ai__TimeoutSeconds` | Timeout padrão global (por tarefa é clampado em 5–120 s). |
| `Ai__Providers__*__BaseUrl` / `__DefaultModel` | Endpoints e modelos padrão (Groq `llama-3.3-70b-versatile`, Gemini `gemini-2.0-flash`, DeepSeek `deepseek-chat`). |

**Passo a passo (tenant):**
1. (Opcional) defina a chave global do provedor no servidor; se o tenant vai usar chave própria, defina `Ai__EncryptionKey`.
2. Admin entra em `/AssistenteIa`, escolhe provedor/modelo/fallback, cola a chave do tenant (depois aparece apenas mascarada), ajusta limites e salva.
3. `TestarConexao` deve devolver `OK`. Falhas retornam classes explícitas (`NAO_CONFIGURADO`, `TRANSPORTE`, `TIMEOUT`, `RESPOSTA_INVALIDA`) com mensagem amigável — nunca "sucesso" fictício.

## 5. Governança (valores admitidos)

- Clamps aplicados pelo gateway: `LimiteTokensEntrada` ≤ 16000, `LimiteTokensSaida` ∈ [64, 8000], `TimeoutS` ∈ [5, 120], `CotaMensalUsos` ∈ [1, 100000] (CHECKs equivalentes existem no banco).
- **Cota mensal por tenant+tarefa**: só SUCESSOS contam. Estourou ⇒ `COTA_EXCEDIDA` (sem custo adicional).
- **Fallback**: apenas para o provedor aprovado configurado por tarefa (`fallback_provedor`); marca `fallbackUsado=true` na resposta e na auditoria.
- **Auditoria**: `ai_usos` registra cada uso (incluindo falhas classificadas) sem nenhum segredo ou dado clínico.

## 6. Máquina de estados (ordem de avaliação)

1. **`VAZIO`** — pré-checagem determinística (usuário sem pendências no período): resposta imediata, sem consultar configuração nem chave.
2. **`NAO_ENCONTRADO`** — cotação inexistente **ou de outro tenant** (validação do ID relacionado precede a execução — resposta 200 com classe, nunca 500 nem vazamento).
3. **`NAO_HABILITADO`** — linha `ai_config` ausente ou `habilitada=false`.
4. **`NAO_CONFIGURADO`** — habilitada, mas sem chave (do tenant nem global) para o provedor escolhido.
5. Execução → **`OK`** (texto sanitizado) ou **`TIMEOUT` / `TRANSPORTE` / `RESPOSTA_INVALIDA` / `COTA_EXCEDIDA`**; com falha do primário e fallback aprovado: destino aprovado executado.

Mensagens são texto puro e amigável: sem causa raiz técnica, sem URL, sem segredo, sem trecho de dado clínico. O contrato de saída é `AiOutcome` (`statusKind` + `mensagem` + campos opcionais de geração).

## 7. Segurança (decisões implementadas)

- **Prompt injection**: o contexto chega ao modelo já minimizado (linhas truncadas pelo serviço da jornada, sem binário); a saída volta **validada como texto** — strip de caracteres de controle → truncamento em 4000 chars com elipse → HTML-escape. A UI renderiza com `textContent` (sem HTML executável).
- **Sem superfície livre**: a IA não tem acesso a banco/SQL/URLs arbitrárias e não escreve dados de negócio — só gera texto de apoio.
- **Logs/mensagens**: nome do provedor + classe de erro. Nunca chave, prompt, CPF, trecho de prontuário, senha ou token.
- **Autorização antes do contexto**: os dados do prompt vêm dos serviços canônicos da própria jornada (pendências do usuário autenticado; cotação escopada ao tenant), então dados de outro tenant não entram no prompt.
- **CSRF**: todos os POSTs de execução/configuração exigem token antiforgery (probe: POST sem token = 400).

## 8. Como usar (ajuda curta por tela)

- **`/Pendencias` → painel "Resumo com IA"**: clique em *Gerar resumo* para um resumo das suas pendências do período. Os estados (vazio / desabilitado / não configurado / cota) aparecem explicados na própria caixa. A IA não executa ação: para agir sobre uma pendência, use as ações da lista.
- **Administrativo 360 → Detalhes da cotação → "Analisar com IA"**: gera uma análise de apoio à cotação e aos documentos. É leitura de apoio — aceitar, rejeitar ou transmitir continua sendo decisão humana no fluxo normal.
- **`/AssistenteIa` (admins)**: dois cards, um por tarefa. Habilite, escolha provedor/modelo/fallback, informe a chave do tenant (fica mascarada), ajuste limites/cota e salve; use *Testar conexão* antes de considerar configurado. Sem chave nem global: os pontos de IA mostram "não configurado".

## 9. Evidência desta rodada (detalhes: `docs/seguranca/p0-seguranca-homologacao-2026-10-03.md`, Anexo B)

- Suíte **794/794** (`evidencias-a360/wps3b-suite.log`); fatos da camada: `backend/PlantaoPro.Tests/AiWps3RegressionTests.cs` (15 fatos: clamps, preservação de chave no update, ordem dos estados, fallback aprovado, cota só-sucesso, sanitização/truncamento/escape, auditoria sem segredo, NAO_ENCONTRADO por tenant).
- Smoke funcional com stack no ar: 18 passos verdes, 3 perfis, stack real (`evidencias-a360/wps3-web-smoke.log`).
- Persistência verificada via `psql`: linhas `ai_config` corretas nos tenants de teste, chaves ausentes (nenhuma exposta), `ai_usos` consistente.

## 10. Limitações declaradas

1. **Integração com provedor real NÃO homologada** (sem chave nesta rodada). Comprova-se caminho, governança e estados; inferência real requer chave + `TestarConexao` OK + uma geração real no ambiente de homologação.
2. `orcamento_mensal` é persistido e exibido; o enforcement financeiro por valor (vs. cota de usos) fica em backlog.
3. Apenas as 2 tarefas iniciais existem; novas tarefas exigem mudança de código + migração (CHECK `task_code`).
4. ABRASF/fixtures XML são do escopo Administrativo 360 (outro anexo), não desta camada.
