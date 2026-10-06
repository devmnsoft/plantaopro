# R4-GATE — Gate de entrada dos blocos B/C (pré-requisitos verificados por execução)

Data: 2026-10-06 · Branch: `main` · Base: `89c85b4` (R4-A5) · Banco: `plantaopro_test`

## 1. Decisão do GATE

O GATE exige que todos os pré-requisitos estejam provados antes de iniciar o bloco B
(governança/funcional) e o bloco C (design + fiscal). Matriz final:

| # | Critério | Status | Evidência |
|---|---|---|---|
| G1 | API/Web sobem | **APROVADO** | Smoke vivo desta janela (§2) + validadores fail-fast de startup (R4-A2) + suíte canônica de 970 testes que sobe hosts Testing/Development/Production reais a cada rodada (R4-A5) |
| G2 | Login/autorização | **APROVADO** | Suíte canônica (testes de auth incluídos nos 970) + rota BFF de login (R4-A3) + endpoint de teste `TestSignin` sob flag explícita, indisponível em Dev/Prod (R4-A5) |
| G3 | Banco instala/atualiza | **APROVADO** | R4-A4: Cenário L (instalação limpa v2.21.8, `IDENTITY_SCHEMA_READY`, 84 fontes success) e Cenário U (upgrade representativo 84/84 idempotentes, checksum `5075bfe6…` idêntico, upgrade #3 no-op) |
| G4 | Precisão monetária | **APROVADO** | WS-A2 (correções commitadas na base `c84afc5`) + inventário R4-A4 (250 colunas monetárias mapeadas, 65 com dados reais, sem contaminação ÷100; rerun T1–T8 = 8/8) |
| G5 | Validação sem efeitos no banco | **APROVADO** | R4-A3: BFF responde JSON 400 (`BffDadosInvalidosResult`); POST form valida destino local e 302; GET preserva URL; cenários sem Referer/destino externo/inputs inválidos sem efeitos no banco |
| G6 | Isolamento entre tenants — DOIS tenants, MESMO módulo contratado (Q7) | **APROVADO (nova prova)** | Suíte `Administrativo360IsolamentoBidirecionalQ7Tests`: **8/8 aprovados** (§3) |

**Veredicto do GATE: APROVADO para iniciar o bloco B nesta máquina.**

Pendências que não pertencem a esta máquina e NÃO bloqueiam o início do B, mas bloqueiam a
liberação pública: validação viva IIS (roteiro P0–P7 do guia, R4-A2), auditoria do banco
produtivo (finding A4/F3), homologação externa de IA e fiscal (credenciais/decisão comercial, P2).

## 2. Smoke vivo da janela GATE (processos reais, fora do TestServer)

Subidos como processos `dotnet run` em Development, contra `plantaopro_test`:

| Host | Requisição | Resultado |
|---|---|---|
| API (`http://127.0.0.1:51976`) | `GET /` | **302 → `/swagger`** (mapeamento Development do `Program.cs`) |
| API | `GET /swagger/index.html` | **200** (log Kestrel: `Request finished ... 200`) |
| Web (`http://127.0.0.1:52976`) | `GET /` | **200** |
| Web | `GET /Account/Login` | **200** |

Notas honestas:
- O `Program.cs` da API não tem rota `/health` dedicada; em Production a `GET /` retorna o
  `HealthDto` (mapeamento verificado no código). No roteiro IIS de produção esse é o checkpoint.
- A porta 51976 da API é também o valor de `ApiSettings:BaseUrl` que a Web de dev consome —
  o smoke provou os dois hosts isolados, com configuração idêntica à publicação.
- Homologação viva em IIS continua **NÃO EXECUTADA aqui** (IIS ausente nesta máquina); segue o
  roteiro P0–P7 de `docs/deploy/guia-implantacao-iis.md`.

## 3. Q7 — Isolamento bidirecional com o MESMO módulo ATIVO nos dois tenants

**Problema (auditoria rodada 3):** todos os testes de isolamento existentes comparavam um
tenant contratado contra um tenant **sem** contrato. O cenário "dois vizinhos ambos com
ADM360 ativo" — onde vazamentos entre tenants seriam mais prováveis — estava sem prova.

**Implementação:** `backend/PlantaoPro.Tests/Administrativo360IsolamentoBidirecionalQ7Tests.cs`
(8 testes), repositórios REAIS contra PostgreSQL:

- Tenants dedicados e determinísticos `61a9e001…`/`61a9e002…`; cada um com **contrato ADM360
  ATIVO habilitado** — pré-condição verificada na semente (`2/2 contratos`, falha explícita senão);
- Semente idempotente (purge + insert numa transação, marcadores `Q7-`) — mesmo padrão dos
  testes A4/A5: reexecuções paralelas não deixam estado residual;
- Cobertura:
  | Superfície | Asserção | Sentido |
  |---|---|---|
  | Escrita — parceiro | criado em A não aparece na listagem de B | A→B e B→A |
  | Escrita — produto | criado em A não aparece na listagem de B | A→B e B→A |
  | Escrita cruzada | `AlternarStatus*` com id do outro tenant → `KeyNotFound` e valor preservado | A→B e B→A |
  | Leitura — cadastros | listagens por busca retornam apenas o registro próprio | A→B e B→A |
  | Leitura — documento | `ObterDocumentoPorIdAsync` com id do outro → null; próprio → presente com chave correta | A→B e B→A |
  | Arquivo — XML | `ObterXmlBytesAsync` cruzado → null; próprio → bytes+hash SHA-256 corretos | 4 leituras cruzadas |
  | Probe SQL final | 8 triplas (tabela × id × tenant): zero linhas com id do X armazenadas sob Y (parceiros, produtos, documentos, estabelecimentos) | A→B e B→A |

**Resultado: 8/8 aprovados — nenhum vazamento encontrado nas superfícies testadas.**

Limitação declarada: a prova cobre o repositório (superfície de dados real, sem layer HTTP).
Exportações via serviço de relatórios/cotações não entram nesta suíte (backlog §5).

## 4. Varredura canônica pós-GATE

Três execuções consecutivas da suíte integral com a nova suíte Q7:

| Rodada | Resultado | Duração |
|---|---|---|
| 1 | **970/970 aprovados, 0 falhas** | 15 s |
| 2 | **970/970 aprovados, 0 falhas** | 15 s |
| 3 | **970/970 aprovados, 0 falhas** | 13 s |

Contagem canônica atual: **970** (944 da base original + 18 do R4-A5 + 8 do GATE/Q7).
Sem MSB4166, sem retry, nenhum flake.

## 5. Classificação e backlog

- **GATE: APROVADO** (por execução, nesta máquina) para iniciar o bloco B (ordem B6→B7→B8→B9→B10 → C11 → fiscal).
- Backlog aberto neste GATE:
  1. Estender a prova Q7 à superfície HTTP completa e às exportações (relatórios/cotações) — o mapa de governança pedia leitura/escrita/exclusão/**arquivo/exportação**; exportação ainda coberta só indiretamente.
  2. Roteiro vivo P0–P7 no ambiente de homologação IIS (pendência ambiental, não técnica).
  3. Auditoria do banco produtivo (F3/A4) — exige acesso fora desta máquina.
