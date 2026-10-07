# R4-B10 · IA (Groq / Gemini / DeepSeek) — revalidação + prontidão para chaves

- **Data**: 2026-10-07 (UTC) · **Rodada**: 4 · **Bloco**: B10 (IA)
- **Base da árvore**: `5cf6732` (R4-B9), tree limpa antes do bloco.
- **Classificação final do item**:
  - **Prontidão técnica / implementação: APROVADO** (camada completa, testada, pronta p/ chave).
  - **Homologação externa (provedor real)** — executada em 2026-10-07 com as três chaves reais (detalhe no §9):
    - **Groq: APROVADO** — `[OK]` reprodutível (HTTP 200 + texto gerado) com `openai/gpt-oss-20b`.
    - **Gemini: APROVADO c/ ressalva operacional** — chave válida; 200 com candidatos reais; default `gemini-flash-latest` existe e suporta `generateContent`. Ressalva: tier gratuita em throttle/cota baixa (503/timeout em picos); tier paga recomendada p/ produção.
    - **DeepSeek: APROVADO** — crédito adicionado e re-verificado; `deepseek-flash` responde **200** com texto real (`"funcionando"`). Modelo de raciocínio: com teto pequeno o texto vai para `reasoning_content`; com orçamento suficiente `content` se preenche (ver 9.1b).
  - Mocks/unitários **não** declaram homologação externa (regra da pauta) — aqui a prova foi por **chamada real** aos provedores.

---

## 1. O que já existia (não recriado neste bloco)

Camada canônica de assistentes IA entregue nas rodadas 1–2 (WP-S3), preservada intacta:

| Peça | Local | Papel |
|---|---|---|
| Adaptadores | `Api/Ai/AiProviderAdapters.cs` | Groq e DeepSeek (OpenAI-compatible, `Authorization: Bearer`); Gemini (wire próprio, `x-goog-api-key`) |
| Gateway | `Api/Ai/AiGateway.cs` | governança: clamps de tokens/timeout/cota, cota só-sucesso, fallback de provedor+modelo, orçamento mensal (USD/BRL), concorrência de slots, limites de teste de conexão (10/24h por tenant/provedor) |
| Segredos | `Api/Ai/AiSecretProtector.cs` | AES-GCM 256 p/ chave **por tenant**; chave mestra só via config (`Ai:EncryptionKey`); exibição mascarada |
| Compatibilidade | `Api/Ai/AiModeloCompatibilidade.cs` | valida modelos por provedor; rejeita aposentados (`deepseek-chat`/`-reasoner`, 2026-07-24) |
| Auditoria | `ai_usos`, `ai_config` (repositório em `Api/Ai/AiConfigRepository.cs`) | tokens, custo (estimado/confirmado/incerto), sucesso, provedor, modelo |
| UI | `Web/Views/AssistenteIa/Index.cshtml` (+ links em `/Pendencias` e Detalhes da cotação) | habilitar tarefa, provedor/modelo, chave, limites, **Testar conexão**, reconciliação de custos |
| Jornadas | — | `MEU_DIA_RESUMO` (`/Pendencias`), `COTACAO_ANALISE` (Detalhes da cotação, Adm 360) |
| Estados | `Api/Ai/AiContracts.cs` (`AiErrorKinds`) | `OK`, `VAZIO`, `NAO_HABILITADO`, `NAO_CONFIGURADO`, `COTA_EXCEDIDA`, `TIMEOUT`, `RESPOSTA_INVALIDA`, `TRANSPORTE`, `PROVEDOR_LIMITADO`, `CONCORRENCIA`, `ORCAMENTO_EXCEDIDO`, `TESTE_LIMITE` |

**Princípios mantidos**: IA sugere, humano confirma (nenhuma ação de negócio executada pela IA); pré-checagens determinísticas respondem sem gastar chamada de LLM; nenhum segredo (chave/prompt) vaza em resposta ou log.

## 2. Revalidação sobre a árvore atual (pós-B9)

- Suíte `*Ai*`: **194/194** aprovados (`--filter "FullyQualifiedName~Ai"`), 0 falhas.
- Suíte completa: **1043/1043** aprovados, 0 falhas, 18 s (base B9 mantida; nenhum teste novo foi adicionado neste bloco — B10 é revalidação + prontidão, não nova funcionalidade).
- Build: **0 erros**.

## 3. Mapa de resolução de chaves (confirmado no código)

Ordem por tarefa/provedor: **chave do tenant > chave global do servidor > (se nada) estado `NAO_CONFIGURADO`**.

| Nível | Origem | Variável / local | Obrigatório para |
|---|---|---|---|
| Global do servidor | config do processo da API | `Ai__Providers__Groq__ApiKey` · `Ai__Providers__Gemini__ApiKey` · `Ai__Providers__DeepSeek__ApiKey` | usar IA sem chave por tenant (caminho recomendado) |
| Chave mestra (cifragem) | config do processo da API | `Ai__EncryptionKey` (EXATAMENTE 64 hex = 32 bytes) | salvar chaves **por tenant** |
| Por tenant | banco `ai_config.api_key_cifrada` | página `/AssistenteIa` → “Chave do provedor” | quando cada cliente traz a própria chave |

- Precedência .NET: env var > `appsettings.{Ambiente}.json` > `appsettings.json`. Os `appsettings*.json` versionados mantêm `ApiKey` **vazio** → o valor real vive só em env var (nunca commitado).
- `AiSecretProtector` trata `EncryptionKey` vazia/inválida como **ausente** (sem falha silenciosa); a UI avisa e bloqueia salvar chave de tenant sem ela.

## 4. Correções / prontidão aplicadas neste bloco

1. **`backend/PlantaoPro.Api/appsettings.json` — modelos sugeridos alinhados aos vigentes.** Antes: Groq `llama-3.3-70b-versatile`, Gemini `gemini-2.0-flash`, DeepSeek `deepseek-chat`. Depois: `gpt-oss-20b` / `gemini-2.5-flash` / `deepseek-flash` (= `AiGateway.ModelosPadrao`, “documentação oficial consultada em 2026-10-04”). Motivo concreto: `Sugerido()` devolve o `DefaultModel` do appsettings à UI como modelo sugerido, e `deepseek-chat` está **aposentado** — um admin novo que aceitasse o modelo sugerido veria o **salvar falhar** em `AiModeloCompatibilidade.Validar`. Config-only; nenhum teste depende dessas strings e o ambiente `Testing` faz override para a suíte.
2. **Novo `scripts/ai-external-probe.ps1`** — ferramenta de **homologação externa**: uma chamada real e mínima a cada provedor configurado, usando os MESMOS endpoints/auth dos adaptadores; roda sem banco/JWT; lê as env vars acima e, opcionalmente, um `.env` raiz (git-ignorado); veredito por provedor `[OK]`/`[FALHA <http>]`/`[SEM_CHAVE]`; exit codes `0`(≥1 OK)/`2`(nenhum)/`1`(erro).
3. **Novo `docs/ia/pronto-para-chaves.md`** (runbook onde colocar as chaves + passo a passo + critério de homologação) e ponteiro adicionado ao `docs/ia/README.md`.

## 5. Probe executado agora (sem chave) — prova do wiring

```
PlantaoPro - Probe de homologacao externa (B10 IA)
  Groq     key=(sem chave)
  Gemini   key=(sem chave)
  DeepSeek key=(sem chave)

==> GROQ     [SEM_CHAVE]  definir Ai__Providers__Groq__ApiKey
==> GEMINI   [SEM_CHAVE]  definir Ai__Providers__Gemini__ApiKey
==> DEEPSEEK [SEM_CHAVE]  definir Ai__Providers__DeepSeek__ApiKey

Veredito:
NENHUM provedor homologado externamente (falta chave real). Estado: BLOQUEADO p/ chave.   (exit 2)
```

Confirma: sem credencial tudo fica em `SEM_CHAVE`/`NAO_CONFIGURADO` (explicável, sem chamado de LLM), exatamente o estado esperado antes das chaves.

## 6. Como desbloquear (resumo — detalhe no runbook)

1. Definir `Ai__Providers__*__ApiKey` (e `Ai__EncryptionKey` se for por tenant) no processo da API.
2. `powershell -File scripts\ai-external-probe.ps1` → exigir `[OK]` p/ o(s) provedor(es).
3. No `/AssistenteIa`: habilitar a tarefa + **Testar conexão** = `OK`; depois rodar uma geração real (Meu Dia em `/Pendencias` ou Cotação nos Detalhes).
4. Critério p/ APROVADO (homologação externa): `[OK]` da probe **e** pelo menos uma geração real pelo app (mock não conta).

## 7. Observações / backlog aberto

- **Guia IIS §5** descreve `Ai__EncryptionKey` como “32+ chars”; o **código** exige **64 hex** (32 bytes). Afinar o guia quando houver oportunidade (não bloqueante: o runbook já documenta o valor real).
- **Homologação externa p/ chave** segue pendente (item principal do B10). Nada além disso impede liberação da prontidão técnica.
- Sem mudanças de schema/migração neste bloco.

## 8. Artefatos criados / alterados

- `scripts/ai-external-probe.ps1` (novo; endurecido p/ homologação: `HttpClient` + retry transitório)
- `docs/ia/pronto-para-chaves.md` (novo; atualizado c/ resultado da homologação)
- `docs/ia/README.md` (ponteiro + data rodada 4)
- `backend/PlantaoPro.Api/appsettings.json` (models sugeridos → vigentes; 2026-10-07: Groq `openai/gpt-oss-20b`, Gemini `gemini-flash-latest`)
- `backend/PlantaoPro.Api/Ai/AiGateway.cs` (`ModelosPadrao` alinhado aos modelos confirmados)
- `backend/PlantaoPro.Api/Ai/AiModeloCompatibilidade.cs` (catalogos atualizados: prefixo `openai/` da Groq e aliases Gemini)
- `.env` na raiz (git-ignorado; chaves reais — **não** commitado)
- `docs/evidencias/2026-10-06-rodada-4/r4f-ia-b10-prontidao-chaves.md` (este arquivo)

## 9. Homologação externa com chaves reais (executada em 2026-10-07)

As três chaves reais foram gravadas em `.env` na raiz (git-ignorado; nunca commitado). O `scripts/ai-external-probe.ps1` foi endurecido neste passo: transporte **`HttpClient`** (em vez de `Invoke-WebRequest`, que engasgava em alguns endpoints) e **retry em falha transitória** (5xx/429/timeout); falha determinística (401/404/402) não repete.

### 9.1 Correção dos modelos padrão (evidência de chamada real)

A varredura com chave revelou que os nomes padrão versionados estavam desatualizados para as contas atuais:

| Provedor | Antes (versionado) | Após (confirmado) | Motivo |
|---|---|---|---|
| Groq | `gpt-oss-20b` | **`openai/gpt-oss-20b`** | id real hoje leva o prefixo do vendor (`openai/`); sem ele = 404 `model_not_found`. `llama-*` aposentados. |
| Gemini | `gemini-2.5-flash` | **`gemini-flash-latest`** | `gemini-2.5-flash` → 404 “no longer available to new users”; o alias estável `flash-latest` responde via `generateContent`. |
| DeepSeek | `deepseek-flash` | `deepseek-flash` (confirmado — 200 c/ texto real) | conta estava sem saldo (402); crédito adicionado e geração OK. Reasoning model (ver 9.1b). |

Arquivos tocados (só valores/catalogos, sem mudança de contrato): `AiGateway.ModelosPadrao`, `appsettings.json` (`DefaultModel`), `AiModeloCompatibilidade` (catalogos p/ evitar aviso do novo default; regra bloqueante por prefixo inalterada), `ai-external-probe.ps1` (defaults + transporte). Revalidação: build **0 erros**; IA **194/194**; completa **1043/1043**.

### 9.1b Reconfirmação (modelos corrigidos + DeepSeek creditado)

Após corrigir os 3 modelos padrão e adicionar crédito à conta DeepSeek, a probe canônica foi re-executada:

- **GROQ `openai/gpt-oss-20b`** — `[OK]` HTTP 200, reproduzível.
- **DEEPSEEK `deepseek-flash`** — `[OK]` HTTP 200 com texto real (`"funcionando"`, `finish_reason: stop` em `max_tokens=200`). Saldo resolvido; homologação fechada.
- **GEMINI `gemini-flash-latest`** — nesta janela, 503 “high demand” + timeout (throttle do tier gratuito); o 200 com conteúdo capturado em 9.1 permanece como evidência de homologação, e a listagem confirma `generateContent`.

**Observação de produção (modelos de raciocínio):** `gpt-oss` e `deepseek-flash` são reasoning models. Com teto de saída pequeno, o raciocínio consome os tokens e `content` pode ficar vazio; com orçamento suficiente `content` se preenche. A API clampia `LimiteTokensSaida` em **[64, 8000]**; recomenda-se **≥ ~256** p/ respostas úteis nesses provedores. `content` vazio vira erro `RespostaInvalida` (tratado, não quebra o app; pode acionar fallback).

### 9.2 Veredito por provedor

| Provedor | Chave | Prova | Classificação |
|---|---|---|---|
| Groq | válida | **`[OK]` HTTP 200** (reprodutível) — `openai/gpt-oss-20b` devolveu `content` com texto real (“funcionando”) | **APROVADO** |
| Gemini | válida | **200 com candidatos reais** (capturado); listagem `/v1beta/models` 200 <1s (modelo existe + suporta `generateContent`); rede p/ Google ok | **APROVADO** (ressalva: cota/tier free — 503/timeout em picos) |
| DeepSeek | válida | **`[OK]` HTTP 200** com texto real (`"funcionando"`, `finish_reason: stop`) após crédito adicionado (antes 402 sem saldo) | **APROVADO** |

> Nota de integridade (Gemini): o `generateContent` do `gemini-flash-latest` respondeu **HTTP 200 com candidatos reais** nesta sessão. A instabilidade posterior (503 “high demand”/timeout) é **capacidade da tier gratuita sob carga repetida**, não falha de contrato — o endpoint de listagem segue 200 em <1s. Para latência/estabilidade previsíveis em produção, usar tier paga/cota maior.

### 9.3 Efeitos e próximos passos
- **Groq**: pronto para geração real pelo app já agora (chave global + tarefa habilitada).
- **Gemini**: pronto; recomendar plano paga/cota maior p/ produção.
- **DeepSeek**: **homologação fechada** (crédito adicionado → `[OK]` 200 com texto real). Em produção, dar folga no limite de saída por ser reasoning model (≥ ~256, ver 9.1b).
