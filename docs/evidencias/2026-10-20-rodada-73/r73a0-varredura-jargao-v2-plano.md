# R73 (r73a0) — Linguagem simples v2: varredura de jargão nos resx/views restantes — plano e triagem

- Data: 2026-10-20 · Item do plano: `docs/evidencias/2026-10-19-rodada-66/r66a0-plano-50-sprints.md` (epic B2, R73:
  "linguagem simples v2: varredura de jargão nos resx/views restantes; substituição por textos que um leigo
  entende, e contratos atualizando as strings exatas").
- Antecedentes da casa: R27 (portal), R28 (adm360 parte 1), R29 (demais módulos), R34 (valores de resx em 4
  culturas), R50 (passada final de copy, contrato permanente de frases). Esta rodada estende a varredura ao
  vocabulário técnico inteiro sobre todos os valores de resx (não só chaves já mapeadas) e sobre todas as views.

## 1. Método

Scanner versionado neste diretório: `r73_scan.ps1` (reexecutável; saída em `r73_gap_scan.txt`).

- **Resx** — todos os `<value>` dos 4 arquivos `UiTexts.*.resx` (pt-BR/en/es/fr) contra duas listas:
  - palavras soltas (case-insensitive, fronteira de palavra): `tenant(s)`, `inquilino(s)`, `outbox`,
    `payload(s)`, `json`, `http`, `antiforgery`, `server(s)`, `log(s)`, `fallback`, `cache`, `webhook(s)`,
    `hash`, `checksum(s)`, `seed(s)`, `bootstrap`, `exceção(excecao)`, `backend`, `frontend`,
    `migração(migracao)`, `migration`, `schema`, `stack`, `blob`, `multipart`, `cookie(s)`, `debug`,
    `uuid`, `guid`, `api`, `bff`, `endpoint(s)`;
  - acrónimos em caixa alta (sensível a maiúscula, fronteira de palavra): `API`, `BFF`, `UUID`, `GUID`,
    `BLOB`, `CRUD`, `CSRF`, `JWT`, `OAuth`, `SQL`, `JSON`, `CHECKSUM`, `WEBHOOK`, `ANTIFORGERY`.
- **Views** — linhas cruas de todo `Views/**/*.cshtml` contra frases (case-insensitive: "banco de dados",
  "base de dados", "stack trace", "código de status", entre outras) + os acrónimos acima + palavras-chave
  (`payload`, `endpoint(s)`, `webhook(s)`, `outbox`, `bootstrap`, `multipart`, "stack trace").
- Frases (e não palavras soltas) nas views para não bater em nome de código — mesmo critério da R50.

Resultado da execução (pré-mudança): **58 candidatos nos resx + 107 nas views = 165 candidatos**, todos
triados individualmente (arquivo:linha/chave + contexto) em `r73_gap_scan.txt`.

## 2. Corrigidos (texto visível para usuário leigo)

### 2.1 Resx — 6 chaves reescritas em até 4 culturas (21 valores)

| Chave | Antes (pt-BR) | Depois (pt-BR) | Culturas |
|-------|---------------|----------------|----------|
| `Shift.TipFilter` | Filtre a operação no servidor. | O filtro é aplicado a todos os registros, não apenas à página atual. | 4 |
| `Clinical.PhTipSensitive` | Dados sensíveis não aparecem em logs técnicos. | Dados sensíveis não aparecem nos registros do sistema. | 4 |
| `Clinical.FormHiddenHint` | ...validação, antiforgery e preservação dos dados em caso de erro. | ...validação, proteção contra envio duplicado e preservação dos dados em caso de erro. | 4 |
| `Implantacao.Loading` | Carregando informações reais da API... | Carregando informações reais do sistema... | 4 |
| `Adm360.Dash.CapDashboardDesc` | Métricas e estatísticas apuradas via consultas reais em SQL. | Métricas e estatísticas apuradas com os dados reais do sistema. | 4 |
| `Adm360.Produtos.JsPrevLocal` | ...official calculation is recomputed on the server when saving. | ...official value is calculated when you save. | en (única cultura com "server") |

Sem chave criada ou removida (contrato R7DF6 segue válido — chaves são identidade, valor é redação).

### 2.2 Views — 13 edições em 11 arquivos

| Arquivo | Antes | Depois |
|---------|-------|--------|
| `Financeiro/Index.cshtml` (empty state) | A API não retornou lançamentos para os filtros atuais. | Não encontramos lançamentos para os filtros atuais. |
| `Financeiro/Index.cshtml` (tooltip do Confirmar) | ...exige status pendente, valor e endpoint transacional. | ...exige status pendente e valor informado. |
| `Administrativo360/FechamentoCaixa.cshtml` | ...são bloqueados pelo banco de dados. | ...são bloqueados pelo sistema. |
| `Administrativo360/Fiscal/Configurar.cshtml` | ...nunca no banco de dados nem nas telas. | ...nunca nos registros do sistema nem nas telas. |
| `MinhaAssinatura/Index.cshtml` (empty state) | A API não retornou um vínculo contratual... | Não encontramos um vínculo contratual... |
| `MinhaAssinatura/Index.cshtml` (consumo) | A API ainda não disponibilizou limites ou uso... | Limites e uso ainda não foram informados... |
| `Comercial/Funil.cshtml` (legenda dos cards) | API /api/comercial/funil | Dados por etapa |
| `Auditoria/Index.cshtml` (ajuda da busca) | A busca respeita o escopo e os dados disponíveis na API. | A busca respeita o seu perfil de acesso e os dados disponíveis. |
| `Seguranca/Index.cshtml` (badge dos cards) | Dados persistidos via API | Dados carregados do sistema |
| `V112/OperationalPage.cshtml` (estado vazio) | ...para consultar a API. | ...para carregar os dados. |
| `Integracoes/Logs.cshtml` | ...ou payload clínico sensível. | ...ou dados clínicos sensíveis. |
| `Lgpd/Consentimentos.cshtml` | Histórico completo disponível na API `/api/lgpd/consentimentos`. | O histórico completo dos consentimentos fica registrado no sistema e pode ser consultado pela área técnica. |
| `Administrativo360/FluxoCaixa.cshtml` (comentário de código) | // ...a API não envia "tipo" no item... | // ...a resposta não envia "tipo" no item... |

## 3. Mantidos (KEEP documentado — decisão de público/contexto)

- **Resx (7 chaves na KEEP do contrato novo)**
  - `Nav.LogsESaude` — link da barra lateral que só renderiza na seção MNSOFT administracao (apenas
    ADMINISTRADOR_GLOBAL) apontando para a área de Observabilidade, de público operacional técnico;
  - família `Adm360.RelFin.Ind*` (DreFiltros/DreFonte/MargemFiltros/VencFiltros/VendasFiltros/VendasFonte)
    — definições de indicadores fiéis ao SQL, KEEP já documentado na R34 (família R8-B27): referência
    técnica para analistas, sem paráfrase.
- **Views — código, não copy**: instâncias `bootstrap.Modal`/vendor (classes e assets), atributos
  `data-endpoint`/`data-lookup-endpoint`/`data-v112-page`, `JSON.parse`/`JSON.stringify`,
  `enctype="multipart/form-data"`, variável JS `payload` em `V112/OperationalPage.cshtml`, comentários
  `@* *@`/`<!-- -->` (definições fiéis ao SQL — R8-B22 a R8-B28, nota CSRF, "hidden com GUID vazio",
  "Anexos e Respostas Outbox") e propriedade C# `Endpoint = "/api/..."`.
- **Views — área restrita a ADMINISTRADOR_GLOBAL (público técnico declarada na própria tela)**:
  `Observabilidade/Index.cshtml` ("Endpoints lentos", "Stack traces e payloads sensíveis são mascarados") e
  o visualizador de resposta da `V112/OperationalPage.cshtml`.
- **Views — módulo de integração (público de consumidores de API)**: `Integracoes/*` (títulos
  "Integrações e API Keys", "Webhooks", "Detalhes do Webhook") e `B2BLaunch/Form.cshtml` ("API Keys",
  escopo `webhooks:write`).
- **Views — nome de recurso do produto em páginas comerciais**: "API Enterprise" e "API Mobile"
  (Marketplace/PlanosPublicos), "Deseja API?" (simulador comercial), "BI avançado, API e revenda"
  (card do Marketplace) e a especificação técnica `API: <code>caminho</code>` em `Shared/SaasComercialPage.cshtml`.
- **Auditoria**: identificadores técnicos dos datalist de filtro (perfis/entidades/ações) — a própria página
  já traz a ressalva "Identificadores técnicos aparecem apenas para permitir investigação e correlação", e o
  detalhe "Detalhes JSON" é o visor técnico do evento (seção restrita da barra lateral).

## 4. Contratos

- Novo: `backend/PlantaoPro.Tests/R73LinguagemSimplesV2ContratoTests.cs` (3 fatos):
  1. anti-regressão por vocabulário: nenhum `<value>` dos 4 resx fora da KEEP pode voltar a conter termo da
     lista (mesma lista do scanner);
  2. frases-jargão proibidas em qualquer `.cshtml` das views (lista exata do fato);
  3. strings exatas da reescrita travadas (12 trechos de views + 5 valores pt-BR do resx, igualdade total).
- Atualizado: `R28LinguagemSimplesAdm360ContratoTests` — o literal travado de `Fiscal/Configurar.cshtml`
  acompanhou a troca ("banco de dados" → "registros do sistema"), mesmo tratamento que o contrato R57 dá a
  copy tratada.
