# R94 (B5) — Carga/Sustentação: harness de carga da API com thresholds documentados

## Objetivo

Linha 86 do plano (r66a0): "carga/sustentação (script PS/k6): thresholds documentados".

Antes desta rodada o repositório não tinha nenhum harness de carga — as únicas referências de
desempenho eram medições frias manuais (R91: listas densas com cliente único, n=3; R92: EXPLAIN
e índices nos mesmos caminhos). Não era possível responder "a API sustenta clientes simultâneos
estáveis ao longo de uma janela de dois minutos, sem degradar, sem vazar conexão e sem estourar
memória". Entregas da rodada:

1. `r94a_carga_api.ps1` — harness autocontido: sobe somente a API (DLLs Release, banco
   `plantaopro_test`, porta 51976), logina o gestor demo, aquece e sustenta 8 clientes
   concorrentes reais por 120 s em mix somente leitura dos caminhos quentes do módulo
   Administrativo 360;
2. `r94a_carga_saida.txt` — execução canônica com thresholds aplicados: **RESULTADO=PASSEI**;
3. Contrato travado: `R94CargaSustentacaoContratoTests` (3 fatos novos → suíte 1786 → **1789**).

k6 não está instalado na máquina; o harness é PowerShell (house style, zero binário novo) e o
mix fica documentado no próprio script para eventual port.

## Recon

- **Referências frias** (R91/R92, cliente único, n=3): `/api/administrativo360/orcamentos` ~100 ms,
  `/api/administrativo360/estoque` ~265 ms. Sob concorrência os percentis ficam acima da média
  fria — isso define a lógica dos limites abaixo.
- **Rotas de carga**: as quatro já instrumentadas/otimizadas nas R91/R92 (orcamentos, orcamentos
  paginado pág. 2, estoque, fiscal/notas) + `GET /api/health` como rota controle leve. Tudo
  somente leitura com bearer do gestor demo (`POST /api/auth/login`) — segure-executável sobre o
  banco de teste.
- **Volume do banco no momento da medição**: `adm360_orcamentos=11436`, `adm360_saldos=9370`,
  `adm360_movimentos=18038`, `adm360_reservas=2648`, `adm360_produtos=1295` (linha `VOLUME` do txt).
- **Máquina**: DESKTOP-CK86BSC, `cpu=12` — máquina de desenvolvimento; os limites são calibrados
  para este perfil e declarados como tal no cabeçalho do script (edição 1).
- **`GET /api/observabilidade/digest-diario` excluído do mix**: endpoint de papel
  `ADMINISTRADOR_GLOBAL`; o gestor demo não tem esse papel e viraria 403 sob carga, poluindo a
  taxa de erro.

## Decisões (design)

- **Concorrência real de I/O sem threads extras**: um agendador single-thread mantém exatamente
  N requests em voo o tempo todo (`SendAsync` + `GetAwaiter().GetResult()` por lote). Motivo:
  scriptblock executado em thread .NET pura falha em PS 5.1 sem Runspace; `Task.Run` é ambíguo
  entre `Action` e `Func<T>`; `TaskFactory.StartNew` com `param($x)` não liga argumentos.
- **Coletas como parâmetros explícitos da função** (`List[object]`/`List[string]`): mutação de
  variável de escopo externo dentro de função mostrou-se insconfiável neste PS 5.1.
- **Warmup separado** (15 s, requests descartados) + seed fixada do `System.Random`
  (warmup 9400, janela 9500) → mix de rotas reprodutível corrida a corrida.
- **Saída máquina-legível e cultura-invariante** (decimais com ponto): o contrato C# faz parse
  determinístico em qualquer máquina (`double.Parse` invariant; vírgula aceita como
  normalização defensiva).
- **Sustentação monitorada inline**: amostrador a cada 3 s dentro da janela lê
  `pg_stat_activity` (conexões abertas contra o banco de teste via psql) e `WorkingSet64` do
  processo da API; as duas primeiras/últimas amostras entram no txt (`AMOSTRA_t=…`).
- **Mix de carga** (pesos sobre 100): 40 % orcamentos · 20 % orcamentos?pagina=2 · 25 % estoque ·
  10 % fiscal/notas · 5 % health — espelha a proporção de uso das listagens quentes e inclui a
  rota paginada validada na R91.
- **Thresholds (edição 1)** — declarados no topo do script e aplicados ao final:

  | Limite | Valor | Justificativa |
  |---|---|---|
  | `p50_max_ms` | 250 | Percentis sob 8 concorrentes acima da média fria (R91/R92: ~100–265 ms, n=3); margem > 30 % sobre o medido (p50 ≈ 184 ms) |
  | `p90_max_ms` | 700 | > 3× o p90 medido (≈ 226 ms) — folga para jitter de máquina de desenvolvimento |
  | `p99_max_ms` | 1500 | Cauda sob concorrência (GC, psql do amostrador) — ~6× o medido (≈ 251 ms) |
  | `erro_max_pct` | 1.0 | Tolerância transitória de ambiente de desenvolvimento; esperado zero em somente leitura |
  | `rps_min` | 8.0 | Capacidade sustentada medida ≈ 40 req/s; o mínimo pega degradação de saturação, não pico |
  | `degradacao_max_x` | 1.5 | p90 da segunda metade / p90 da primeira; > 1,5 = drift real na janela |
  | `conexao_delta_max` | 4 | (máx − mín) de conexões durante a janela; baseline 8 em voo + 1 amostrador — vazamento aparece como crescimento |

## Mudança (commit r94a)

| Arquivo | Papel |
|---|---|
| `docs/evidencias/2026-10-22-rodada-94/r94a_carga_api.ps1` | Harness de carga (thresholds documentados no topo, mix, amostrador, saida maquina-legivel, teardown da porta 51976 sempre) |
| `docs/evidencias/2026-10-22-rodada-94/r94a_carga_saida.txt` | Execução canônica 8×120 s + 15 s warmup — `RESULTADO=PASSEI` |
| `backend/PlantaoPro.Tests/R94CargaSustentacaoContratoTests.cs` | 3 fatos puros de arquivo (sem fixture/DB) |

Detalhes do contrato:

- **Fato 1** trava no script as sete constantes de threshold, o perfil padrão
  (`-Workers 8 -DuracaoSeg 120 -WarmupSeg 15`), as cinco rotas do mix, o amostrador
  (`pg_stat_activity` + `WorkingSet64`), a fixação de cultura invariante e os marcadores da saída.
- **Fato 2** lê a execução canônica e exige: `RESULTADO=PASSEI`, todos os sete `CHECK`=SIM, e cada
  valor medido da linha `GERAL` **dentro do próprio threshold declarado na linha `THRESHOLD` do
  mesmo arquivo** (mesma fonte — o contrato verifica o contrato declarado, sem repetir limites
  hardcoded); ≥ 4 linhas `POR_ROTA` todas com `err=0`; `n ≥ 2000` (volume sustentado);
  `PORTAS_LIVRES=SIM` (teardown efetivo).
- **Fato 3** exige sustentação monitorada: `degradacao_x ≤ degradacao_max_x`, metades com
  ≥ 1000 pedidos cada, `AMOSTRAS_CONEXAO n ≥ 5` com `delta ≤ conexao_delta_max` (sem vazamento),
  `MEMORIA_API_MB` coerente, ≥ 2 amostras inline, `LOGIN_OK`, `APPS api_pid > 0` e warmup com
  descarte real de requests.

### Lições PS 5.1 desta rodada (registradas para herdar)

1. **Colisão case-insensitive com `param`**: `param([string]$Out='')` liga `$out` como variável
   *tipada* string; todo `$out=@()` passa a coagir para string e o `+=` vira concatenação — o
   arquivo de saída colapsou numa linha única. Acumuladores não podem reutilizar nome de parâmetro.
2. `ConcurrentQueue<T>` **não tem `Clear()`** no .NET 4.8 (só .NET Core 3+).
3. `Thread.CurrentUICulture` não resolve neste PS 5.1/.NET 4.8; basta `CurrentCulture` para
   formatação numérica.
4. `[System.Net.Http.HttpClient]` exige `Add-Type -AssemblyName System.Net.Http`.
5. Round-trip `Get-Content`/`WriteAllLines` em arquivo UTF-8 sem BOM corrompe acentos
   (leitura ANSI cp1252 → reescrita UTF-8 = mojibake). O script final é **100 % ASCII** por
   propósito, imunizando contra esses round-trips.

## Evidência (execução canônica, 2026-10-22)

Duas execuções completas independentes, ambas `RESULTADO=PASSEI` (reprodutibilidade — os números
seguem da canônica, que também é o artefato travado pelo contrato):

```text
R94A_CARGA data=20261022_204603 host=DESKTOP-CK86BSC cpu=12
GERAL n=4912 rps=40.91 erro_pct=0 media_ms=144.9 p50=183.9 p90=226.4 p99=250.6
METADES h1_n=2432 h1_p50=184.8 h1_p90=228.9 h2_n=2480 h2_p50=183.2 h2_p90=220.8 degradacao_x=0.96
AMOSTRAS_CONEXAO n=38 min=9 max=9 delta=0
MEMORIA_API_MB min=232 max=244
CHECK p50_ok=SIM p90_ok=SIM p99_ok=SIM erro_ok=SIM rps_ok=SIM degradacao_ok=SIM conexao_ok=SIM
RESULTADO=PASSEI
PORTAS_LIVRES=SIM
```

Por rota (janela medida):

| Rota | Peso | n | err | média ms | p50 ms | p90 ms | p99 ms |
|---|---|---|---|---|---|---|---|
| `/api/administrativo360/orcamentos` | 40 | 1990 | 0 | 126.1 | 171.3 | 217.3 | 247.7 |
| `/api/administrativo360/orcamentos?pagina=2` | 20 | 983 | 0 | 124.3 | 170.9 | 217.3 | 245.7 |
| `/api/administrativo360/estoque` | 25 | 1241 | 0 | 205.2 | 203.6 | 235.9 | 255.7 |
| `/api/administrativo360/fiscal/notas` | 10 | 450 | 0 | 121.8 | 169.1 | 217.3 | 247.4 |
| `/api/health` | 5 | 248 | 0 | 117.3 | 171.2 | 213.7 | 240.7 |

- **Sustentação**: `degradacao_x=0.96` — a segunda metade ficou *ligeiramente mais rápida* que a
  primeira (sem drift); 38 amostras de conexões **constantes em 9** (8 em voo + 1 psql do
  amostrador, delta=0 → sem vazamento); WorkingSet da API 232–244 MB estável.
- **Calibração** (execução anterior, antes da fixação de cultura — decimais com vírgula na época,
  números equivalentes): `n=4888 rps=40.72 erro_pct=0 p50=185.4 p90=226.6 p99=255.3
  degradacao_x=1.00 conns min=max=9 ws 191–227 MB` → Δ < 2 % entre as duas corridas.

## Gates

- **A/B Release** (`r87abuild.ps1 -Tag r94`): base-unicos=**76** / nova-unicos=**76** →
  **DIFF=NENHUMA**.
- **Suíte completa (pré-doc)**: 1ª execução 1788/1789 com 1 flake conhecido de concorrência entre
  classes (`R86TrilhosContratoTests.Remessa_cnab400…`: contagem de faturas candidatas muda entre o
  SELECT e o `GerarRemessaAsync` — aprovou 7/7 isolado); reexecução completa → **Aprovado! 1789/1789**
  (1786 canônicos + 3 fatos novos da R94).
- **docscan** (`r62_docscan.ps1`): `ARQ_VERIFICADOS=4`, `DOCSCAN_BAD=0` (script 100 % ASCII, txt
  ASCII, contrato ASCII, este md UTF-8 correto).
- **Suíte completa (pós-doc)**: **Aprovado! 1789/1789** — higiene `RepositoryHygieneContractTests`
  **8/8**.
- **Inventário** (`r75_gap.ps1`): N/A — nenhuma página/artigo novo.
- **Migrações**: sem mudança — manifest permanece em **144** (última v2374).

## Pendências

- **k6 ausente** na máquina: o harness PowerShell é a edição 1; se o k6 entrar, o mix/pesos já
  estão documentados no cabeçalho do script para port direto (JSON k6 script).
- **BFF Web fora do escopo v1**: apenas a API (:51976) foi carregada; carga mista API+Web fica
  para depois que o desenho da R95+ assentar o restante do bloco B5/B6.
- **Thresholds calibrados neste perfil** (cpu=12, máquina de desenvolvimento, banco de teste com
  ~11k orçamentos): outra máquina ou volume exige recalibração da edição 1 — procedimento é
  rodar o script e ajustar os 7 limites no topo, com justificativa nesta evidência.
- **Carga com escrita concorrente** (read-modify-write, p.ex. reservas) não faz parte do mix de
  v1 por mutar o banco de teste; avaliar em rodada futura com snapshot/rollback transacional.
- Retenção/monitoramento contínuo (rodar o harness em CI noturno): fora desta rodada.
