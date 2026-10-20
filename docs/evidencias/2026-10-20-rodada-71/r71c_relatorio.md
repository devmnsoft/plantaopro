# R71C — Provedor Mistral registrado + pesquisa de IAs gratuitas (pedido do usuário, fora do plano)

Data: 2026-10-20 · Série: rodada 71 (lotes extras r71c/r71d) · Estado: FECHADA (gates verdes, pushado em main)

## Pedido do usuário

> "a IA procure tbm uma IA gratis pra ajudar, coloque e faça o cadastro delas tbm pode usar o drael a minha chave pra incorporar a IA"

Entregue: (1) pesquisa de provedores com plano realmente gratuito; (2) cadastro de um novo
provedor gratuito (Mistral) na camada canônica de IA, com o mesmo contrato dos demais;
(3) caminho documentado para usar as chaves gratuitas do usuário (Groq/Mistral/Gemini)
e a chave Drael para incorporar a IA da casa.

## Pesquisa — o que existe de IA gratuita (consultado em 2026-10-20)

| Provedor | Plano gratuito | Cartão? | Compatível OpenAI | Decisão PlantaoPro |
|---|---|---|---|---|
| Groq (console.groq.com) | Sim, generoso (catálogo largo: llama, gpt-oss, qwen3) | Não | Sim | Já era provider; documentado na tela como fonte de chave grátis |
| Google Gemini (aistudio.google.com) | Sim, por modelo (flash funciona em chave grátis) | Não | Endpoint próprio (adapter próprio já existe) | Já era provider; chave do usuário já registrada globalmente (.env) — bloqueio 403 API_KEY_SERVICE_BLOCKED é EXTERNO ao sistema (projeto Google, constatado na R66) |
| Mistral (console.mistral.ai, La Plateforme) | Sim, plano gratuito anunciado em 2025-09-24 e vigente em 2026-10 | Não | Sim (https://api.mistral.ai/v1) | **CADASTRADO AGORA** como 5º provider |
| Cerebras (cloud.cerebras.ai) | Docs oficiais (2026-10): não há tier permanente grátis; trial de US$ 5 expira em 30 dias e exige pagamento verificado | Sim | Sim | Fora de escopo (não é "grátis" de fato) |
| Cohere | Trial 1.000 chamadas/mês, uso não comercial | Não | Sim | Fora de escopo (restrição de licença) |

Fontes: docs oficiais (inference-docs.cerebras.ai/support/rate-limits; docs.mistral.ai/models,
changelog docs.mistral.ai/resources/changelogs, modelo `mistral-small-2603`), mistral.ai/news
(september-24-release — "Free tier on la Plateforme"), snapshot de mercado ianlpaterson.com
(2026-05-31, atualizado 2026-08). Modelo padrão escolhido: **mistral-small-2603**
(híbrido instruções/raciocínio/código, 256k de contexto, id confirmado no changelog oficial).

## O que mudou

### Código (backend/PlantaoPro.Api)
- `Ai/AiContracts.cs`: código `mistral` + entra em `AiProviderCodes.All`.
- `Ai/AiProviderAdapters.cs`: `MistralAdapter` (OpenAI-compatível, cliente `AiMistral`,
  chave `Ai:Providers:Mistral:ApiKey`, tenant prioritário sobre a global).
- `Ai/AiModeloCompatibilidade.cs`: governança do Mistral — BLOQUEIA catálogo de outros
  provedores (gemini-/deepseek-/drael-/gpt-oss-/llama-) e famílias aposentadas
  (magistral/devstral/pixtral/open-mistral/openmixtral/mathstral, docs de 2026-10);
  AVISA (não bloqueia) quando o nome não é de família vigente (mistral-/ministral-/codestral-).
- `Ai/AiGateway.cs`: modelo padrão do provedor = `mistral-small-2603`.
- `Program.cs`: HttpClient `AiMistral` (base `https://api.mistral.ai/v1/`) + registro do adapter.

### Tela (backend/PlantaoPro.Web)
- `Views/AssistenteIa/Index.cshtml`: Mistral entra na lista de provedor/reserva
  (Drael continua liderando) e o manual da página ganha o parágrafo "Provedores gratuitos"
  em linguagem simples (onde criar a conta, colar a chave, testar conexão).

### Banco (database)
- `migrations/2026_10_v2361_r71c_ai_provedor_mistral.sql` (nova): estende os checks
  `ai_config.provedor`, `ai_config.fallback_provedor` e `ai_precos_modelos.provedor`
  com `'mistral'`. Idempotente (DROP/ADD CONSTRAINT nomeado), aditiva, sem dados.
  Preço não é semeado: sem preço vigente o sistema já trata custo como incerto (a
  aplicação não inventa preço — mesmo padrão do Drael na v2356).
- `scrpt_completo.sql` e `pgadmin/instalar_no_banco_atual.sql`: seção 210 com
  `SOURCE`/`SOURCE-SHA256` (SHA-256 `00b03ef445e457057aaefc567c36911eb684dfc6d886fa6b2daf9267c333b07a`).
- `migration-manifest.json`: versão ativa/transactional dependente da v2360.
- `source-checksums.json`: nova entrada coberta pelo teste de integridade.
- Aplicada em `plantaopro` e `plantaopro_test` (psql ON_ERROR_STOP=1, 12 ALTER TABLE limpos,
  os 3 constraints presentes após aplicação).

### Contratos (backend/PlantaoPro.Tests)
- `R71CProvedorMistralContratoTests.cs` (novo, 4 fatos, espelho do contrato R62/Drael):
  registro em servidor+UI+banco+manifest; wire format/endpoint/chave/classificação;
  governança bloqueante/aviso; gateway ponta a ponta no banco real de teste
  (salvar, executar, auditoria em ai_usos, testar conexão).
- `R62ProvedorDraelContratoTests.cs`: expectativa da lista da tela atualizada
  (5 provedores, Drael ainda primeiro).

### Ambiente
- `.env.example`: documentadas as chaves globais opcionais dos 5 provedores
  (`Ai__Providers__{Gemini|Groq|DeepSeek|Drael|Mistral}__ApiKey`).

## Gates (comandos canônicos)

| Gate | Resultado |
|---|---|
| Build Release `--no-incremental` (A/B via stash: base pré-mudanças vs nova, com a nova refeita após os ajustes finais) | EXIT=0 nos dois · 81 avisos na base e 81 na nova · 0 erros · **DIFF=NENHUMA** (logs copiados para esta pasta: `r71c_build_base.txt`, `r71c_build_nova.txt`) |
| Suíte completa (apps das portas 51976/51977/52976 livres antes) | **1671 aprovados / 0 falhas / 0 ignorados** (base anterior 1667 → +4 do contrato novo); log `r71c_tests.txt` nesta pasta |
| Aplicação psql da v2361 em `plantaopro` e `plantaopro_test` (aplicada 2x — prova da idempotência) | 6x ALTER TABLE por aplicação, ON_ERROR_STOP=1 sem erro; os 3 constraints confirmados após cada aplicação |
| Docscan U+00C3 (`r62_docscan.ps1` sobre arquivos alterados/novos) | ARQ_VERIFICADOS=14 · BAD=2 — somente os dois scripts completos, em conteúdo legado pré-existente (o mesmo falso positivo legítimo já documentado na R68: comentários e defaults com formas em caixa alta de `não`/`padrão`/`atenção` em seções antigas; valores de dados intocados). Zero ocorrências no conteúdo novo (seção 210, migration v2361, .cs, cshtml, relatório) |

## Como o usuário usa (resumo em linguagem simples)

1. Criar conta grátis (sem cartão): console.groq.com e/ou console.mistral.ai
   (o Gemini grátis sai em aistudio.google.com, se quiser trocar a chave atual).
2. Copiar a chave gerada e abrir **Assistente IA** no sistema.
3. Escolher a tarefa, ligar o habilitador, escolher o provedor (Groq/Mistral/…),
   colar a chave em "Chave do provedor" e clicar **Testar conexão**.
4. Pronto: a tarefa passa a usar aquele provedor, com limites e cota mensal
   vigendo no servidor. Para uso sem chave por cliente, a chave global vai no
   `.env` do servidor (variáveis documentadas no `.env.example`).

Pendências externas (sem ação no repo): o 403 do Gemini continua sendo problema do projeto
Google da chave atual; a chave Drael (`dk-…`) pode ser adicionada em
`.env` como `Ai__Providers__Drael__ApiKey` quando fornecida.
