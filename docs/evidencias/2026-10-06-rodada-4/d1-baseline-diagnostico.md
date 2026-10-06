# Rodada 4 — D1 · Baseline e diagnóstico sobre `c84afc5`

> Entregue em 2026-10-06. Referência da análise: `c84afc5` (= `origin/main`, trabalho limpo, 0/0).
> Métodos: revisão dos 5 mapas da rodada 3 (`docs/evidencias/2026-10-06-rodada-3/mapas/`),
> reconfirmação dos pontos-chave contra o código atual, inspeção das saídas de publicação reais
> (`C:\Users\NCELL-DEV-020\Desktop\plantaopro[.api]`) e das configurações versionadas.
> Somente leitura nesta etapa — nenhuma mudança de código ou de banco.

## 0. Commit confirmado antes de iniciar

| Item | Valor |
|---|---|
| HEAD | `c84afc519411dd5a616844afe1af5633cb048a35` ("novos ajustes", marcelo, 2026-10-06 06:40 -03:00) |
| origin/main | idêntico — `ahead=0 / behind=0`; publicação prévia feita pelo usuário |
| Delta de `c84afc5` | 4 arquivos (+51): `FolderProfile.pubxml` ×2 (API e Web, FileSystem → `Desktop\plantaopro.api` / `Desktop\plantaopro`) + `scripts/local/find-tempdata-type.ps1` + `scripts/local/ws-a2-list-user-secrets.ps1`. Os `*.pubxml.user` seguem ignorados (`*.user`). |
| Sequência local | `7f43af2` (entrega rodada 2) → `6b6ca3d` (auditoria r3) → `d1b3736` (WS-A2) → `622cbfa` (WS-A3) → `c84afc5` |

Observação C11: os 4 artefatos que estavam deliberadamente **não versionados** agora são rastreados
(paste locais com caminhos absolutos `C:\Users\NCELL-DEV-020\...`). Não reverter aqui; registrar no
backlog como decisão do dono do repositório.

## 1. Correções com prova de execução (evidência executada, não só mock)

| Correção | Commit | Prova de execução |
|---|---|---|
| Gate monetário: parser pt-BR único, binder global, short-circuit de ModelState, canal IA, apresentação sem alterar valor | `d1b3736` | 25/25 unit (`ValorHumanoTests`) + 7/7 integração BFF real com stub determinístico (`Administrativo360GateMonetarioTests`) + suíte 902/902; reconciliação histórica executada em `plantaopro_test` (heurística inteiro ≥ 100, auditoria por linha) — `ws-a2-integridade-monetaria.md` |
| Identidade documental por família fiscal: migration v2311 (unicidade fiscal parcial, sequência única + hash canônico), import defensivo (advisory lock + ON CONFLICT + 23505), recebimento físico vs placeholder, retry serializable 40001/40P01 | `622cbfa` | v2311 aplicada **2×** em `plantaopro_test` (2ª 100% idempotente); T1–T8 em `Administrativo360DocumentosWsA3Tests` (concorrência 8 threads, bytes divergentes, reuso canônico); suíte **910/910 em 2 execuções consecutivas** (run 3 e 4) — `ws-a3-documental.md` |
| 4 UPDATEs ADM360 com filtro tenant + antiforgery no RefreshContext | `619b069` | coberto pela suíte canônica (executada nos runs 3–4) |
| Design system v2151, confirmações de ações críticas, contrato de toast | `67b4f91` | jornada M1–M4 executada (WP-B `2eaf57b`); verificação visual ainda não concluída nesta máquina (sem IIS/navegador instrumentado) |

## 2. Itens que dependem de validação adicional (plano desta rodada)

| Item | Dependência | Onde |
|---|---|---|
| v2311 em instalação limpa + upgrade representativo | executar `Tools.Database install` em banco novo + upgrade de base representativa | Bloco A item 4 |
| Inventário monetário completo (tabelas/colunas ainda não verificadas) | varredura do schema `numeric` fora de `adm360_*` | Bloco A item 4 |
| Erros reais do IIS | root causes identificados estáticamente (§3.1); falta validação em servidor IIS vivo — **não há IIS nesta máquina** (`inetsrv\appcmd.exe` ausente) | Bloco A item 2 |
| Destino do redirecionamento de ModelState inválido | ainda baseado em header `Referer` — confirmado em `ModelStateInvalidoFiltro.cs:40-43` (vivo em `c84afc5`) | Bloco A item 3 |
| TestSignin | guard só por ambiente confirmado em `TestSigninController.cs:45` (Q8 do mapa governança segue válido); falta flag explícita desabilitada por padrão + prova de indisponibilidade em Production **e** Development operacional | Bloco A item 5 |
| IA (Groq/Gemini/DeepSeek) | sem chave real em ambiente acessível → inferência real continua **não homologada** (bloqueado por credencial) | Bloco B item 10 |
| Emissão fiscal | decisão comercial pendente (operação, UF/município, provedor, certificado) — registrar pendência e avançar nas partes independentes | Complemento items 6–9 |

## 3. Diagnóstico por módulo (mapas rodados sobre `7f43af2`, reconfirmados em `c84afc5`)

Classe: **impl** implementado · **val** validado com prova de execução · **par** parcial · **queb** quebrado/inconsistente · **nae** não executado.

### 3.1 Publicação e operação no IIS — QUEBRADO/NAE
- `Web\appsettings.json:9` fixa `PlantaoProApi:BaseUrl = https://localhost:51977/` (porta de **dev**).
  Precedência confirmada em `Program.cs:107`: `ApiSettings:BaseUrl` → `PlantaoProApi:BaseUrl` → variáveis de ambiente.
  Em IIS sem variável definida, o Web tenta localhost:51977 → **conexão recusada** quando a API roda em
  outra porta/host ou está fora do ar. Root cause identificado estaticamente.
- `Api\appsettings.json`: `Jwt:Key = ""` vazio → sem segredo injetado (env/secret manager) o token falha em
  Production. Não existe validação de configuração com mensagem clara na inicialização.
- Portas dev (`51976/51977` API, `52976/52977` Web) vêm de `launchSettings.json`; em IIS (AspNetCoreModuleV2
  inprocess, `web.config` confirmado na saída publicada) a escuta vem dos bindings do site — o "Web ocupando
  porta 5000" é sintoma de binding/ASPNETCORE_URLS mal definido, não de código.
- **Achado novo**: as pastas publicadas no Desktop contêm `appsettings.json` **com credential real de
  banco** (`plantaopro_test` / senha em claro) e `appsettings.Development.json` (que ativa `DemoSeed.Enabled=true`
  e `ApiSettings:BaseUrl=http://localhost:51976`) — em deploy com `ASPNETCORE_ENVIRONMENT=Development` o
  TestSignin também reativa (sem flag independente). O publish carrega segredos por estar fora do repositório
  mas com dados reais.
- Roteiro API→health→Web→login→autorizado→persistência: **nae**. Persistência de Data Protection em reciclagem
  / múltiplas instâncias: **nae**.

### 3.2 Validação MVC segura e utilizável — PAR
- Parser/binder/short-circuit: **val** (WS-A2). Preservação de valores digitados e listas: mantida (PRG + ModelState).
- Redirecionamento por `Referer` **sem validação de destino local**: aberto (item 3) — `Referer` externo =
  open redirect + perda da mensagem TempData (redirect para outra origem descarta o cookie TempData).
- Diferenciação formulário vs `/bff/*` vs GET: BFF responde envelope JSON com status ≥ 400
  (`BffContracts`, `Program.cs:86-91,:137`) — **par**; faltam testes dedicados de ausência de Referer, destino
  externo e efeitos no banco.

### 3.3 Reconciliação monetária/documental — VAL + PAR
- Gate monetário: **val** (§1). Inventário de colunas fora de `adm360_*`: **par** (heurística rodou apenas em
  `adm360_*` no banco de teste; produção não acessível).
- v2311: **val** em `plantaopro_test`; **par** até revalidar limpa+upgrade (item 4).

### 3.4 Autenticação de teste e regressões — PAR
- TestSignin vivo e funcional (usado pela suíte BFF): **impl**; restrição por flag: aberta (item 5).
- Flakes conhecidos sob paralelismo (A3/G5 histórico): causa raiz tratada no wrapper de retry de SSI
  (`ExecutarComRetrySerializableAsync`) — **val** nos runs 3–4 (910/910 ×2); varredura residual final pendente.

### 3.5 Governança SaaS — PAR (3 gaps críticos persistem)
Do mapa `mapa-governanca-saas.md` (reconfirmado: nenhuma das linhas foi alterada por WS-A2/A3):
1. **Crítico** — admin global concedido por correspondência de **nome** de perfil (`SecurityAdministrationServices.cs:93-97`).
2. **Crítico** — sessões Web operam com **claims congeladas no login** (revogação só chega em `RefreshContext`/erro BFF).
3. **Crítico** — módulo **AGENDADO nunca ativa** (sem job de runtime; gate exige `ATIVO`).
+ alto: downgrade sem revalidação de uso; corrida de última assinatura (23505→500 genérico, sem teste);
médio: fallback legado hardcoded, `bloqueado_por_plano` sem marcador, ator de auditoria hardcoded;
baixo: `tenant_modulos` sem unique. TRIAL divergente entre limites (aceito) e funcionalidades (rejeitado).
Testes de isolamento **bidirecional com o mesmo módulo contratado nos dois tenants: NÃO EXISTE** (Q7) —
o alvo do item 5/B6.

### 3.6 Administrativo 360 — IMPL/VAL no núcleo, PAR nos fins
Cadeia cotação→estorno implementada ponta a ponta com idempotência/lock/fora-de-transação por hash canônico
(mapa `mapa-adm360.md`). Gaps documentados e reconfirmados: manifestação SEFAZ sempre indisponível (honesto);
**estorno só integral**; `FecharCaixa` sem idempotency_key/retry serializable; devolução física inexistente em
compra/NF-e; rastreabilidade não faz o join fornecedor/NF-e de ponta a ponta; sem consolidador "próxima ação por
estado"; sem entidade de pendência responsável+prazo.

### 3.7 Saúde 360 e financeiro clínico — IMPL/VAL no núcleo, PAR nos fins
Jornada paciente→consulta→finalização→geração de conta implementada e testada (transições, conflito 409,
idempotência, tenant B não vê A). Gaps reconfirmados: reagendamento beco sem saída sem revalidação de conflito;
pré-autorização PENDENTE **não bloqueia** finalizar/faturar; recebimento **não baixa** `clinica_contas_receber`
nem alimenta `clinica_caixa` (conciliação manual); camada v115/v116 consolidada é estrutura separada e o
"recebimento" Saúde 360 não a alimenta (origem canônica por operação a definir — item 8); form genérico expõe
campos clínicos ao papel financeiro.

### 3.8 Plantões / Meu Dia / Indicadores — IMPL/VAL núcleo; BI PAR/QUEBRADO
Plantões: publicar→convite→aceite→escala→execução→fechamento→pagamento testado nos pontos críticos (última
vaga, idempotência, locks). Abertos: status `encerrado` **inalcançável pela API**; contestação abre estado que
não grava em `pagamento.status` e trava quando aberta em `pendente`; ausência sem reatribuição de vaga; dois
mecanismos paralelos de substituição. Meu Dia: fonte real (CTEs) mas prioridade estática, prazo nulo e
responsável **não renderizado**; agenda "fake" por comparação de datas. BI: KPIs acumulados disfarçados de
mensais, filtros de unidade/profissional **ignorados no SQL**, competência por `reg_date`, fuso tratado por
convenção implícita com colunas `timestamp`/`timestamptz` mistas → indicadores divergem dos registros.

### 3.9 IA — IMPL, homologação externa BLOQUEADA
Provedores preservados (Groq/Gemini/DeepSeek), `Ai:EncryptionKey` vazio no JSON versionado, chaves só via
segredo de servidor. Sem chave real: reserva/orçamento/fallback/custo incerto revalidados apenas em mock.

### 3.10 Design — VAL parcial (sistema v2151), verificação visual NAE
Sistema/tokens existem e foram usados nas jornadas WP-B/WP-C. Sobreposições de menu (z-index/contextos de
stacking), contraste computado e viewports reais 1440/1024/768/390: **nae** em `c84afc5` (exige navegador
instrumentado; plano do bloco C).

### 3.11 Emissão fiscal (complemento) — NAE
Sem fluxo fiscal emitido (NF-e/NFS-e) em ADM360 hoje: existe import/conferência/vínculo de documentos de
**entrada** + manifestação SEFAZ sempre indisponível. Decisões comerciais pendentes (operação, UF, provedor,
certificado) → homologação externa **bloqueada**; partes independentes (cadastro/segurança de parâmetros,
modelagem de estados, separação entrada×emissão) podem avançar sem adaptador fictício.

## 4. Plano de execução desta rodada (ordem do brief)

1. **D1 (este documento)** ✅ — commit de auditoria sobre `c84afc5`.
2. **A2 Publicação/IIS** — configuração de implantação (sites/pools/bindings/URL interna/secrets/HTTPS/
   logs), precedência do HttpClient documentada, validação de configuração na inicialização sem imprimir
   segredos, erro humano de "API indisponível", Data Protection por hospedagem; roteiro de publicação+validação.
   Sem IIS local: evidências por configuração + build + checklist executável.
3. **A3 MVC seguro** — destino local validado no filtro de ModelState (substituir confiança no Referer);
   diferenciação POST-form/MVC, `/bff/*` JSON, GET; testes: sem Referer, Referer externo, inputs inválidos,
   efeitos no banco nulos.
4. **A4 Reconciliação** — completar inventário monetário (todo schema `numeric`), correções só com evidência+
   auditoria; revalidar v2311 em instalação limpa + upgrade representativo; cenários concorrentes já cobertos
   por T1–T8 (rerun).
5. **A5 Test auth + regressão** — flag `TestAuth:Enabled` (desligada por padrão; ligada só pelo ambiente Testing
   via appsettings de teste), prova de 404 em Production e Development sem flag; varredura de flake residual
   com 3–5 rodadas canônicas consecutivas.
6. **GATE** — API/Web sobem, login/autorização, install+upgrade do banco, precisão monetária, validação sem
   efeitos, isolamento 2 tenants no MESMO módulo contratado (novo teste bidirecional — fecha Q7).
7. **B6 Governança** (escopos, fim do SUPER_ADMIN por nome, claims dinâmicas/Web, job AGENDADO, concorrência
   de última assinatura, downgrade sem perda) → **B7 ADM360** (divergências/devolução/pendências/rastreabilidade/
   próxima ação) → **B8 Saúde 360** (origem canônica financeira, baixa de recebimento, pré-autorização) →
   **B9 Plantões/Meu Dia/BI** (encerrado, contestação, Meu Dia real, BI por competência/fuso/escopo) →
   **B10 IA** (revalidação com mocks + homologação externa se houver chave).
8. **C11 Design** — escala de camadas (stacking context real, não z-index a esmo), tokens/contraste
   (4.5:1/3:1), breadcrumbs sem controller, viewports 1440/1024/768/390 reais.
9. **Fiscal** — decidir primeiro operação/UF/provedor (registro de pendência); avançar cadastro/segurança/
   modelagem de estados independentes; homologação externa BLOQUEADO até credenciais.
10. **Entrega** — diagnóstico, matriz plano×módulo×perfil×ação×escopo, guia IIS, evidências, roteiro de
    homologação, backlog; classificar APROVADO/FALHOU/BLOQUEADO/NÃO EXECUTADO; separar implementado/testado/
    homologado pelo usuário/liberado para produção.

Evidências desta rodada em `docs/evidencias/2026-10-06-rodada-4/`. Commits PT-BR ASCII, um por workspace.
Push somente por decisão explícita.
