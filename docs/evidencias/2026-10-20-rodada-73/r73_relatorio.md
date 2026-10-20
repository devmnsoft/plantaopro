# R73 — Linguagem simples v2: varredura completa de jargão nos resx/views e contratos travando as strings

- Data: 2026-10-20 · Plano: `docs/evidencias/2026-10-19-rodada-66/r66a0-plano-50-sprints.md` (epic B2)
- Commits: `web (rodada 73 r73a)` · `tests docs (rodada 73 r73b)`
- Base do início da rodada: `main == origin/main == e30bdf2` · suíte 1681 aprovados · build 81 avisos / 0 erros.

## 1. O que foi entregue

| Item | Resultado | Onde |
|------|-----------|------|
| R73 do plano: varredura de jargão nos resx/views restantes, substituição por linguagem de leigo e contratos com as strings exatas | Feito — varredura integral (todos os valores dos 4 resx + todas as views), 33 trocas de texto em 15 arquivos e contrato permanente novo | §2-§4 |

## 2. Varredura e triagem

Scanner versionado e reexecutável neste diretório (`r73_scan.ps1` → saída bruta em `r73_gap_scan.txt`).
Vocabulário v2 = lista estendida da R34 (tenants, outbox, payload, antiforgery, server, logs, fallback,
cache, webhook, hash, checksum, seed, bootstrap, exceção, backend/frontend, migração, schema, stack, blob,
multipart, cookie, debug, uuid/guid, api/bff/endpoint + acrónimos em caixa alta SQL, JSON, CSRF, JWT etc.).

Resultado pré-mudança: **165 candidatos** (58 nos valores dos 4 resx + 107 nas views), cada um triado
individualmente por arquivo:linha/chave e contexto. Decisão completa (antes → depois, com a razão de cada
KEEP) em `r73a0-varredura-jargao-v2-plano.md`. Resumo das decisões:

- **Corrigido (21 valores de resx em 6 chaves × até 4 culturas + 13 edições em 11 views)**: todo texto
  visível a usuário geral que continha jargão técnico — "da API", "em SQL", "antiforgery", "logs técnicos",
  "no servidor", "banco de dados", "payload", "endpoint transacional" e afins. Sem chave criada ou removida
  (R7DF6 segue válido).
- **Mantido por decisão de público/contexto**: 7 chaves de resx na KEEP documentada do contrato (família
  `RelFin.Ind*` fiel ao SQL — KEEP herdado da R34; `Nav.LogsESaude` — link restrito à seção MNSOFT
  administracao para ADMINISTRADOR_GLOBAL); identificadores de código nas views (vendor `bootstrap`,
  atributos `data-endpoint`, `JSON.parse`, `enctype multipart`, comentários); áreas de público técnico
  declarado (Observabilidade e visualizador V112); módulo de integração e B2B (API Key/webhooks); nomes de
  recurso do produto em páginas comerciais (API Enterprise, API Mobile).

## 3. Contratos atualizados

- **Novo** — `backend/PlantaoPro.Tests/R73LinguagemSimplesV2ContratoTests.cs` (3 fatos):
  1. anti-regressão por vocabulário sobre todos os valores dos 4 resx fora da KEEP documentada;
  2. frases-jargão proibidas em qualquer `.cshtml` (lista exata no fato — escolhida por não bater em nome
     de código, mesmo critério da R50);
  3. strings exatas da reescrita travadas por igualdade/contenção (12 trechos de views + 5 valores pt-BR do
     resx).
- **Atualizado** — `R28LinguagemSimplesAdm360ContratoTests`: o literal travado de
  `Fiscal/Configurar.cshtml` acompanhou a troca ("nunca no banco de dados nem nas telas." → "nunca nos
  registros do sistema nem nas telas."), padrão de casa para copy tratada (mesmo tratamento do R57). A
  própria varredura v2 é o que expôs essa âncora: ela roda sobre todo o texto, não só sobre o já travado.

## 4. Gates desta rodada

| Gate | Resultado | Referência |
|------|-----------|------------|
| Build A/B Release `--no-incremental` | BASE=81 / NOVA=81 avisos, 0 erros, **DIFF_AVISOS=NENHUMA** | `r73_build_base.txt`, `r73_build_nova.txt` |
| Suíte integral | **1684/1684 aprovados** (baseline 1681 + 3 novos da R73), apps parados nas portas 51976/51977/52976 antes, incluindo a reexecução pós-documentos (lição R8B25) | `r73_tests_suites_completa.txt` |
| Docscan (`r62_docscan.ps1`) | Passada 1 (19 arquivos): **DOCSCAN_BAD=0**; passada final após a criação deste relatório: **DOCSCAN_BAD=0** — zero ocorrências introduzidas pela R73 | `r73_docscan.txt` |
| Higiene | `RepositoryHygieneContractTests` (8 fatos) verde na suíte final, que rodou depois deste diretório existir | `r73_tests_suites_completa.txt` |

## 5. Arquivos tocados

- **r73a (web)**: `Localization/UiTexts.resx` · `UiTexts.en.resx` · `UiTexts.es.resx` · `UiTexts.fr.resx` ·
  `Views/Financeiro/Index.cshtml` · `Administrativo360/FechamentoCaixa.cshtml` · `Administrativo360/Fiscal/Configurar.cshtml` ·
  `Administrativo360/FluxoCaixa.cshtml` (comentário de código) · `Auditoria/Index.cshtml` ·
  `Comercial/Funil.cshtml` · `MinhaAssinatura/Index.cshtml` · `Seguranca/Index.cshtml` ·
  `V112/OperationalPage.cshtml` · `Integracoes/Logs.cshtml` · `Lgpd/Consentimentos.cshtml`
- **r73b (tests docs)**: `R73LinguagemSimplesV2ContratoTests.cs` (novo) · `R28LinguagemSimplesAdm360ContratoTests.cs` ·
  `docs/evidencias/2026-10-20-rodada-73/*`

## 6. Lições para rodadas seguintes

1. Contrato que trava string exata precisa acompanhar mudança de texto: a varredura v2 encontrou o literal
   antigo ancorado no contrato R28 — ao reescrever copy tratada, atualizar os contratos que a fixam na mesma
   rodada (mesma mecânica do R57).
2. Varredura de frase sobre views cruas também enxerga comentário C# dentro do Razor (`// ... a API não
   envia ...` em `FluxoCaixa.cshtml`) — frase proibida pelo fato anti-regressão obriga repensar o comentário
   também, não só o HTML.
3. Aviso de autocrlf do git no stderr aborta script PS 5.1 com `ErrorActionPreference='Stop'`
   (NativeCommandError) antes do docscan terminar — normalizar CRLF nos `.cs` reescritos antes de rodar o
   script canônico (mesma família da lição 5 da R72, agora em arquivos de teste criados pelo editor).

## 7. Pendências mantidas do escopo

- Varredura de markup de formulários arquivo a arquivo (~177 arquivos, ~539 campos sem label explícito,
  ~729 sem erro inline — referência `r72_gap_scan.txt`) segue agendada para rodadas seguintes.
- Próxima do plano: **R74** (guardas de design: contrato travando o mapa de tokens + varredura).
