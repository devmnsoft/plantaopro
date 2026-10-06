# Entrega Final — Rodada de Homologação PlantãoPro (2026-10-05)

**Consolidação das 8 entregas da rodada.** Este documento é o índice executivo: aponta para a evidência primária de cada item e não a substitui.

- **Baseline**: `82b8c26` · **HEAD entregue**: `6cd815b` (`main` local) · `origin/main` = `2eaf57b` (pushado até WP-B); **3 commits locais aguardando push** (`67b4f91` WP-C, `619b069` segurança, `6cd815b` entrega final) — decisão de push pendente com o cliente.
- **Contratos preservados**: MVC/Razor, API REST `api/*`, Dapper, PostgreSQL schema `plantaopro`, envelope de resposta. Nenhuma versão alterada por orientação antiga.
- **Documentos de apoio** (todos nesta árvore): `docs/homologacao/vereditos-homologacao-2026-10-03.md` (vereditos por item, FALHOU = 0) · `docs/homologacao/roteiro-homologacao-perfis-2026-10-03.md` · `docs/homologacao/matriz-modulos-homologacao-2026-10-03.md` · `docs/homologacao/checklist-homologacao-final.md` · `docs/ia/README.md` · `docs/seguranca/p0-seguranca-homologacao-2026-10-03.md` · `docs/usuarios-teste.md`.

---

## 1. Mudanças (vs. baseline `82b8c26`)

| Commit | Áreas | Resumo |
|---|---|---|
| `31ba1e1` | docs | Vereditos/roteiro/matriz e docs IA+segurança atualizados para o baseline |
| `76f91b5` | código+UI (IA) | **WP-A2**: governança de modelos/orçamento/cota por provedor com fallback, custo incerto + reconciliação, validação de compatibilidade, UI expandida; suíte 810/810 na época |
| `2004955` | testes | **WP-A4**: flake FK 23503 corrigido com limpeza topológica por tenant + validação pré-destrutiva no reset do banco de teste |
| `605b31d` | banco | **WP-A4**: checksum v2301 realinhado no migration-manifest; drift v2301–v2308 reconciliado e documentado |
| `aabac92` | docs | **WP-A4**: evidência imutabilidade v2305 por papel, ciclo de vida do banco, CREATEROLE fora da operacional |
| `207eeab` | código (Web) | **WP-A1**: contrato JSON do BFF com 401/403 adequados, guard de tenant bloqueado em APIs, landing médico em Meu Dia |
| `acc076f` | docs | **WP-A1**: evidência contrato BFF JSON + guard 403 + landing médico; suíte 830/830 |
| `0ed01d7` | código (XML) | **WP-A3**: Centro de XML — parsing por bytes, quarentena tipada, conferência autorizada v2310; suíte 839/839 |
| `c15456d` | docs (gate) | Gate rodada 2: build da solução, ciclo de vida do banco em banco descartável com v2309+v2310, integridade do schema |
| `2eaf57b` | código+evidência | **WP-B**: jornadas M1–M4 com persistência real + fix `created_at` em `VincularRecebimento` |
| `67b4f91` | CSS/views/docs | **WP-C**: design system v2151 (fundo claro, azul AA, semânticas só para estado), "Como usar", validação junto ao campo, 9 confirmações `data-confirm` em ações críticas, fix contrato de toast, matriz + capturas |
| `619b069` | código+docs | **Segurança**: 4 UPDATEs ADM360 com filtro de tenant no WHERE + antiforgery no `RefreshContext`; relatório arquivo+linha+evidência+correção+teste |

Área de código tocada: `PlantaoPro.Api` (IA, guards), `PlantaoPro.Web` (BFF/JSON, Account, views ADM360, design system), `PlantaoPro.Infrastructure/Administrativo360` (XML, Compras, Qualidade), `backend/PlantaoPro.Tests`, `database/migrations` (checksums), `wwwroot/css/design-system/v2151-*.css`.

## 2. Regras de negócio/reglas de sistema corrigidas nesta rodada

1. **Vinculação de recebimento quebrada** — INSERT em `adm360_recebimentos` usava coluna inexistente `criado_em` (schema: `created_at`); toda vinculação nova falhava em runtime. Corrigido (`2eaf57b`) com regressão na suíte e jornada M3.7 refeita.
2. **Contrato JSON do BFF** — rotas `/bff/*` respondiam 302→HTML (AccessDenied/Login) onde o cliente JS esperava JSON; agora 401/403 com envelope adequado (`207eeab`). Guard de tenant passa a bloquear as APIs com 403.
3. **Landing médico em módulo não contratado** — perfil MÉDICO_AREA sem `MEDICO_AREA` contratado cai em Acesso Negado com razão correta em vez de tela de operação (`207eeab`).
4. **Centro de XML** — parsing passa a operar **sobre bytes** (sem re-encoding/normalização que corrompia hash), quarentenas **tipadas** (destino fora / malformado) e conferência autorizada com permissão própria (v2310) (`0ed01d7`).
5. **Integridade de migrations** — drift de checksum v2301–v2308 reconciliado; manifest confere (`605b31d`).
6. **Isolamento de tenant — defense-in-depth** — 4 UPDATEs (`adm360_pedido_itens`, `adm360_pedidos`, `adm360_vale_eventos`, `adm360_recebimento_itens`) ganham `AND tenant_id = @tenantId` no WHERE (`619b069`). Linhas já eram travadas por id+tenant na mesma transação; o filtro torna o isolamento explícito na própria instrução.
7. **CSRF em `Account/RefreshContext`** — POST passa a exigir token antiforgery (GET inalterado; zero chamadores atuais) (`619b069`).
8. **Contrato de toast (notificações)** — 15 ações financeiras/comerciais gravavam `TempData["SuccessMessage"/"ErrorMessage"]` que nenhuma view consumia (ação gravada, usuário sem feedback). `_ToastMessages` agora lê chaves canônicas com fallback (`67b4f91`).
9. **Regras visuais de homologação (WP-C)** — azul único `#1d6fe9` (AA 4.5+ em texto pequeno), cores semânticas reservadas a estados, jornada/context bar desvinculados do teal, foco visível fora do ADM360, breadcrumb da topbar consistente, validação junto ao campo, confirmação nativa em 9 ações críticas destrutivas (9/9 validadas por DOM + 1 execução real: título `a3610000-…91` aprovado → APROVADO).

**Corrigidas também**: flake `IsolamentoCadastros` (FK 23503 — limpeza topológica, `2004955`) e contraste do link da marca (4,11:1 → 5,85:1, fecha item antigo do backlog).

## 3. Jornadas comprovadas (persistência real em `plantaopro_test`)

Matriz completa com cenários, esperados e evidências: `docs/evidencias/2026-10-05-wp-b-jornadas/jornadas.md`.

| Módulo | Resultado | Destaques |
|---|---|---|
| **M1 — Autenticação/perfis/módulos** | PASS (4/4 capturados) | Login gestor; senha inválida com mensagem segura; importação negada para Auditor; `AccessDenied?module=ADM360&reason=MODULO_NAO_CONTRATADO` para tenant Clínica |
| **M2 — Cadastros master** | PASS (parceiro/produto/local criados; 2 rejeições corretas) | Nome inválido rejeitado; CNPJ duplicado rejeitado; produto criado (com finding de decimal — ver §6) |
| **M3 — Documentos XML + cadeia de compras** | PASS (8/8 capturados) | NF-e válida importada e parseada; conferência autorizada; quarentena dest-fora; quarentena malformada; vinculação → documento VINCULADO com eventos de rastreabilidade; inspeção na fila com decisão 2/2 QUARENTENA; pedido PC-32 RECEBIDO 2/2 |
| **M4 — Concorrência e isolamento** | PASS (2/2) | M4.1: dois `VincularRecebimento` simultâneos → exatamente 1 sucesso + 1 negado pela regra (1 evento persistido); M4.2: tenant sem módulo abre por ID documento de outro tenant → negado, sem vazamento |

Outras jornadas executadas na rodada (evidências próprias):
- **IA (rodada S3/S4)**: smoke funcional 18 passos verdes em 3 perfis com stack real; estados da máquina de estados exercitados (VAZIO, NAO_ENCONTRADO, NAO_HABILITADO, NAO_CONFIGURADO) — `docs/ia/README.md` §9.
- **Ações críticas (WP-C)**: execução real de Aprovar Título a Pagar com diálogo `data-confirm` → estado APROVADO persistido.
- **Imutabilidade v2305 (WP-A4)**: cenário manutenção (superuser) vs. usuário comum com papel restrito — UPDATE negado sem GUC — `docs/evidencias/2026-10-04-rodada2/wpa4-evidencia-2305-*`.

Linhas persistidas para rastreamento: `jornadas.md` §"Linhas persistidas" (parceiros, produto, locais, documentos, pedido, recebimentos, lote, título).

## 4. Capturas (PNG sanitizado, em local durável no repositório)

Todas geradas por navegação real do navegador desktop contra a stack no ar; dados de tenant de demonstração (Santa Casa / Clínica Modelo), sem PII de produção.

- **`docs/evidencias/2026-10-05-wp-b-jornadas/`** — 19 PNGs: `m1-1…m1-4` (login/gestão de acesso), `m2-1…m2-7` (cadastros), `m3-1…m3-8` (XML/compras), `m4-2` (isolamento) — nomes completos no diretório.
- **`docs/evidencias/2026-10-05-wp-c-design/`** — `cadastros-{1440,768,390}.png`, `detalhes-nfe-{1440,768,390}.png`, `wp-c-toast-login-sessao-encerrada.png` (pipeline de toast renderizando na login) + `matriz-wp-c.md`.
- **Método das capturas responsivas**: pane de captura 800×658 px; emulação de viewport por fator de zoom `800/alvo` (1440→0.556, 768→1.042, 390→2.051) — registrado em `matriz-wp-c.md` com limitações declaradas.
- **Rodada 2 (WP-A)**: logs e scripts de execução em `docs/evidencias/2026-10-04-rodada2/` (gate do banco, ciclo de vida descartável, v2305, reconciliação de manifest, suítes).
- **Segurança**: relatório em `docs/evidencias/2026-10-05-seguranca/auditoria-seguranca.md`.

## 5. Resultados e evidências

| Item | Resultado | Evidência |
|---|---|---|
| Suíte canônica | **839/839 aprovados** (55–59 s) — reproduzido após cada pacote, inclusive após os fixes de segurança | Execuições desta rodada + `docs/evidencias/2026-10-04-rodada2/*.log` (histórico: 794→810→815→830→839) |
| Gate rodada 2 | Fechado: build da solução limpo, ciclo de vida do banco em banco descartável (v2309+v2310), integridade de schema e manifest | Commit `c15456d` + logs do diretório acima |
| Auditoria de segurança (pré-homologação) | **0 vulneráveis**; 7 suspeitos analisados um a um → 4 corrigidos (SG-01..04), 1 corrigido (SG-05 antiforgery), resto documentado com decisão e risco residual R-1..R-9 | `docs/evidencias/2026-10-05-seguranca/auditoria-seguranca.md` (arquivo+linha+evidência+correção+teste) |
| Design (WP-C) | Contraste medido: `#1d6fe9` 5,85:1 / hover 5,48:1 sobre branco; teal 6,6:1 mantido só onde consistente; breadcrumb validado por estilo computado `rgb(26,95,208)` nas 2 páginas; jornada 100% `#1a5fd0`; `data-confirm` 9/9 por DOM; 1 ação executada de verdade | `docs/evidencias/2026-10-05-wp-c-design/matriz-wp-c.md` + 7 PNGs |
| Suíte — instabilidade conhecida | `Aceite12_XmlRepetido_NaoDuplica` e (novo, 1 ocorrência) `A3/G5 gate de recebimento`: falham sob paralelismo, passam isolado e em rerun. Suspeita de ordem/isolamento xUnit, não de produto | §6 risco R-9; monitorar |
| Stack pós-entrega | API `https://localhost:51977/api/health` Healthy; Web `https://localhost:52977/Account/Login` 200 + liveness pós-init OK (build com todos os fixes) | Saída de `run-dev-start.ps1` (2026-10-05 21:49) |

**Estado real dos provedores de IA** (verificado hoje em `plantaopro_test` + configuração do processo):

- **Nenhuma chave real de provedor está configurada** neste ambiente: `Ai.Providers.{Groq,Gemini,DeepSeek}.ApiKey` vazios no `appsettings.json` da API (globais do servidor) e o tenant de demonstração (`d3f6584c…`, Santa Casa) tem `COTACAO_ANALISE` habilitada **sem chave** (`tem_chave = f`) → estado efetivo **`NAO_CONFIGURADO`**, devolvido explicitamente pela camada (comprovado por probe e registrado em `ai_usos`).
- As demais linhas `ai_config` (23 linhas de outros tenants) contêm **chaves placeholder de teste** com modelo fictício `llama-teste` — artefatos da suíte/probes, não credenciais comerciais. 31 linhas em `ai_usos` = execuções de teste (falhas classificadas/estados), sem inferência real.
- Conclusão: caminho, governança (clamps, cota, fallback, auditoria sem segredos) e máquina de estados estão **comprovados**; a **inferência com provedor real permanece NÃO homologada** — requer ambiente com chave real + `TestarConexao` OK + uma geração real (item 8 do backlog, §8). Mocks não contam como prova de integração, por regra da pauta.

## 6. Pendências e riscos (em curso desta homologação)

| # | Item | Severidade | Situação |
|---|---|---|---|
| 1 | Decimal pt-BR lido como milhar no custo do produto ("12,50" → 1250) — valor ×100 no banco | **Alto** | Aberto — regra de parse/format em backlog (§8, item 11); reproduzível em M2.5 |
| 2 | Sem UNIQUE em `chave_acesso`; dedup só por string exata em aplicação | Médio | Aberto — risco em escrita concorrente (jornada M3.6 passou por isso) |
| 3 | Sequência de eventos desalinhada (dois eventos com `sequencia_evento=2` no mesmo documento) | Médio | Aberto — audit trail |
| 4 | Duas linhas de recebimento por documento (placeholder vs. confirmação); `documento.recebimento_id` aponta para o placeholder | Médio | Aberto — decisão de design |
| 5 | Mensagem de validação vaza `(Parameter 'precoCusto')` | Baixo-médio | Aberto |
| 6 | UX: JSON cru de `#pp-toast-data` visível na página; label duplicado no modal de parceiro | Baixo | Aberto |
| 7 | Estado real dos provedores IA sem chave (NAO_CONFIGURADO) | Médio (credencial comercial) | Declarado — destrava com chave real (§5) |
| 8 | Riscos residuais de segurança R-1..R-9 (idempotency, GUC/deploy, chaves mortas, segredo em claro, TOCTOU caixa, SecurePolicy…) | Baixo-média | Nenhum crítico; cada item com recomendação na auditoria |
| 9 | Instabilidade de testes sob paralelismo (`Aceite12`, `A3/G5` 1×) | Informativo | Monitorar; reprovar se reaparecer em sequência |
| 10 | 3 commits locais sem push para `origin/main` (WP-C, segurança, entrega final) | Decisão | Pendente de aprovação para push |
| 11 | Cookie de sessão expira no restart do servidor (sliding expiration) | Informativo | Documentado no roteiro — requer re-login após ciclo de dev |

Nenhuma pendência **bloqueante** aberta: o item 1 (decimal) é o mais relevante para aceite financeiro e já possui reprodutor exato (M2.5).

## 7. Instruções de homologação

**Ambiente** (máquina de desenvolvimento/homologação local):

1. Banco: PostgreSQL 18 local, banco `plantaopro_test` (psql: `C:\Program Files\PostgreSQL\18\bin\psql.exe`, senha local padrão `123456`); secrets da aplicação via user-secrets (conn string + JWT), nunca versionados.
2. Ciclo canônico (scripts em `scripts/local/`):
   - `run-dev-stop.ps1` → para API/Web e libera artefatos
   - `run-dev-build.ps1` → compila a solução (estado em `artifacts/runtime-logs/dev/last-build.json`)
   - `dotnet test backend\PlantaoPro.Tests\PlantaoPro.Tests.csproj --nologo -v q` → **839/839** (~1 min)
   - `run-dev-start.ps1` → sobe API `https://localhost:51977` e Web `https://localhost:52977`, espera health/liveness
3. Usuários canônicos: `docs/usuarios-teste.md` (ex.: gestor `gestor@santacasa-demo.example` / `SantaCasa!Demo2026#Gestor`; admin, médico, coordenação e tenant Clínica para teste de isolamento). Re-login após qualquer restart (item 11 do §6).

**Ordem sugerida de aceitação funcional** (com o que já existe, sem depender de nada fora desta árvore):

1. **Prontidão técnica** — suíte verde + gate (`c15456d`) + health da stack. Vereditos por requisito em `docs/homologacao/vereditos-homologacao-2026-10-03.md` (FALHOU = 0).
2. **Módulos** — seguir `docs/homologacao/roteiro-homologacao-perfis-2026-10-03.md` perfis × telas; a matriz por módulo atual é `docs/evidencias/2026-10-05-wp-b-jornadas/jornadas.md` (cenários M1–M4 com os passos exatos e o que verificar no DOM/banco). Checklist histórico completo em `docs/homologacao/checklist-homologacao-final.md`.
3. **Design (WP-C)** — abrir Cadastros e Detalhes de NF-e em 1440/768/390 e comparar com as capturas de referência de `docs/evidencias/2026-10-05-wp-c-design/`; validar as 9 confirmações destrutivas (diálogo nativo) e o toast de sucesso/erro junto ao campo.
4. **Segurança** — conferir o relatório `docs/evidencias/2026-10-05-seguranca/auditoria-seguranca.md` (achados, correções e riscos residuais) e o P0 anterior `docs/seguranca/p0-seguranca-homologacao-2026-10-03.md`.
5. **IA** — `/AssistenteIa` (admins) e painéis de Meu Dia/Cotação: com o ambiente atual todos os pontos devem mostrar **não configurado** (sem chave) — comportamento correto e esperado; com chave real, `TestarConexao` OK + geração real. Detalhes: `docs/ia/README.md`.
6. **Banco/imutabilidade** — cenários v2305 com papel restrito (`SET plantao.bypass_imutabilidade_adm360 = on` só para a role de manutenção) — scripts em `docs/evidencias/2026-10-04-rodada2/wpa4-evidencia-2305-*`.

**Critério de aceite por nível** (separação exigida pela pauta):

- **Prontidão técnica**: ATENDIDA — suíte 839/839, gate fechado, segurança revalidada com correções aplicadas, design AA.
- **Homologação funcional**: EXECUTADA para M1–M4 + IA (estados) + WP-C com persistência real; o cliente roda os mesmos passos do item 2 com os usuários de `docs/usuarios-teste.md`.
- **Liberação de produção**: NÃO declarada — depende dos itens do §8 (mínimo: decimal pt-BR, chave real de IA opcional, papel de banco CREATEROLE/roles, TLS/`pg_hba`) da validação comercial.

## 8. Backlog pós-MVP (separado do escopo MVP)

Ordem sugerida por impacto. Itens marcados ✓ já tiveram avanço nesta rodada.

| # | Item | Severidade |
|---|---|---|
| 1 | **Decimal pt-BR**: regra central de parse/formato cultural (custo, quantidades, valores) — fecharia o finding alto do §6 | Alto |
| 2 | `chave_acesso` UNIQUE (tenant, chave) no banco — dedup à prova de escrita concorrente | Média |
| 3 | Gerador de `sequencia_evento` sequencial por documento (audit trail) | Média |
| 4 | Decisão de design: única linha de recebimento por documento (retirar placeholder ou repontar `documento.recebimento_id`) | Média |
| 5 | Idempotency-Key gerada pelo cliente (storage/hidden field) por ação em andamento — hoje `Guid.NewGuid()` em servidor/proxies (S-7a/b/c) | Média-baixa |
| 6 | Unificação das 15 chaves de toast (`SuccessMessage`/`ErrorMessage`) num único contrato | Baixa-média |
| 7 | IA: enforcement financeiro por valor (`orcamento_mensal` — contador tokens×preço + estado `COTA_ESGOTADA`); hoje cota de usos + exibição/reconciliação existem | Média |
| 8 | IA: homologação com chave real de provedor (credencial comercial) | Média |
| 9 | Segurança: papel de banco dedicado (CREATEROLE apenas no deploy) + GUC de imutabilidade por role; runbook | Média-baixa |
| 10 | Segurança: criptografia at rest de `segredo_referencia` (contas de portal) | Baixa |
| 11 | Segurança: remover/mover chaves mortas `LoginLockout*` da `appsettings.json` da Web (lockout real é da API) | Baixa |
| 12 | Segurança: `Session.CookieSecurePolicy = Always` no pipeline de produção; antiforgery no POST de Logout quando um form existir; unificação dos leitores de claim de tenant (`tenant_id` vs `cliente_id`) | Baixa |
| 13 | Caixa: consolidar snapshot do extrato na mesma transação (TOCTOU `FecharCaixaAsync`) | Baixa |
| 14 | BFF: registrar `OperationBff` no catálogo do guard (ou excluí-lo como BFF de API) com regressão do centro de notificações — item pré-existente ainda aberto | Baixa-média |
| 15 | Logs: máscarar corpo de resposta em caso de proxy-arem conteúdo de terceiros (hoje: envelope próprio, 400 chars) | Baixa |
| 16 | UX: esconder JSON cru de `#pp-toast-data`; label duplicado no modal de parceiro; mensagem `(Parameter 'precoCusto')` sanitizada | Baixa |
| 17 | QA: leitor de tela real (NVDA/JAWS/VoiceOver) sobre a estrutura semântica validada | Baixa |
| 18 | QA: XML ABRASF real anonimizado em fixtures (pipeline de bytes/hash já comprovado no sintético) | Baixa |
| 19 | Mobile (app): passadas por perfil com o app real — nada declarado concluído | Alto (escopo) |
| 20 | Produção (implantação): rede/TLS/`pg_hba`/papéis (S-08/S-09 do P0) | Média (deploy) |
| ✓ | Contrast link da marca (4,11:1) — **fechado no WP-C** (`#1d6fe9`, 5,85:1) | — |
| ✓ | Flake `IsolamentoCadastros` (FK 23503) — **fechado no WP-A4** (`2004955`) | — |
