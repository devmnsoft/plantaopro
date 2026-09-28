# Relatório BLOCO C — Matriz de acesso, jornadas A–G, cross-tenant, auditoria, cotações e Central XML

| Campo | Valor |
|---|---|
| Data | 2026-09-28 |
| Ambiente | Local dev (Windows) — API `https://localhost:51977` · Web `https://localhost:52977` |
| Build em teste | HEAD `5cb43fc` + rename `_SuprimentosNav.cshtml` (staged) — compilado 2026-09-27 22:58 UTC-3, servers no ar 22:59 |
| Banco | PostgreSQL 18 — banco `postgres`, schema `plantaopro` |
| Tenant A (principal) | Santa Casa Demonstração `d3f6584c-2c64-4e5a-9ea9-4e1428647502` |
| Tenant B (negativo) | Clínica Modelo PlantãoPro `8b0c8e74-a81b-4ea2-b499-94755a1ca001` (+ Hospital São Lucas `…7599` sem usuários) |
| Usuários | `gestor@santacasa-demo.example` (Admin. do cliente, id `…7511`) · `consulta@santacasa-demo.example` (Auditor) · `admin.clinica@plantaopro.local` (Admin. Clínica Modelo, seed 122) · `superadmin@mnsoft.example` (Global) |
| Cenários automatizados | `sc1_jornada_ab` · `sc2_leituras_escrita` · `sc3_jornada_e` · `sc4_jornada_f` · `sc5_cross_tenant` · `sc6_jornada_g` · `sc7_m9_financeiro` (helper `bc_common.ps1`: curl + psql) |

Status: **COMPLETO** — matriz M1–M17 e jornadas A–G APROVADAS; cross-tenant X1–X4 APROVADO; valores esperados definidos em §1/§4 ANTES das execuções (regra do brief) e os caminhos reais documentados onde diferiram da forma prevista (gate de contrato antes dos dados, §5).

---

## 1. Ground truth do banco (valores esperados pré-definidos)

Consultado via SQL antes das jornadas (tenant Santa Casa `…7502`):

### 1.1 Cotações (`adm360_cotacoes`) — ESPERADO: 5 linhas exatamente estas

| id | provedor (origem) | procedimento | status_interno | status_externo |
|---|---|---|---|---|
| `4a74e5ae-34fb-4cee-8c77-74f2637b980c` | OPMENEXO | Laminectomia Lombar | RECEBIDA | SUBSTITUIDA |
| `a3600000-0000-4000-8000-000000000030` | OPMENEXO | Angioplastia Coronária | PRONTA_PARA_ENVIO | RECEBIDA |
| `a3600000-0000-4000-8000-000000000031` | INPART | Artroplastia Total de Quadril | EM_RELACIONAMENTO | RECEBIDA |
| `a3600000-0000-4000-8000-000000000032` | OPMENEXO | Cirurgia Neurológica | EXPIRADA | EXPIRADA |
| `a3600000-0000-4000-8000-000000000033` | OPMENEXO | Laminectomia Lombar com Artrodese | EM_RELACIONAMENTO | RECEBIDA |

### 1.2 Central XML (`adm360_documentos_recebidos`) — ESPERADO: 2 linhas

| id | tipo | nº | manifestação | conferência | quarentena |
|---|---|---|---|---|---|
| `a3600000-0000-4000-8000-000000000050` | NFE_COMPLETA | 1 | CONFIRMACAO_DA_OPERACAO | CONFERIDO | f |
| `a3600000-0000-4000-8000-000000000051` | NFE_COMPLETA | 2 | SEM_MANIFESTACAO | PENDENTE | t |

### 1.3 Mapeamentos (`adm360_mapeamentos_de_para`) — ESPERADO PRÉ-EXECUÇÃO: 4 linhas ATIVAS

| tipo_entidade | código externo | entidade interna |
|---|---|---|
| PRODUTO | OPME-MAT-991 | Stent Coronário Farmacológico de Sirolimus |
| PRODUTO | MAT9001-HOMOLOG | Produto interno homologacao |
| HOSPITAL | HOSP-EXT-01 | Hospital Regional Parceiro |
| UNIDADE_MEDIDA | CX-10 | UN |

**Estado pós-execução (confirmado por SQL no encerramento):** 5 linhas ATIVAS — as 4 de seed + a escrita da Jornada C/D (`CX-BC-20260927230937 → Teste Jornada C interno`, PRODUTO, `criado_por` = gestor `…7511`). Zero linhas `CX-BC%` fora do tenant A (X4).

### 1.4 Contagens de apoio

| Métrica | Santa Casa (…7502) | Clínica Modelo (…ca001) |
|---|---|---|
| cotações | 5 | **0** |
| respostas de cotação | 3 | — |
| docs XML | 2 | — |
| parceiros | 5 | 1 |
| produtos | 3 | — |
| orçamentos | 4 | — |

**Regra cross-tenant derivada:** o usuário da Clínica Modelo NUNCA pode ver as 5 cotações nem os 2 docs XML da Santa Casa; ao acessar `/Administrativo360/Cotacoes`, ESPERADO lista vazia (0 linhas) ou recusa anterior a ela, e detalhe do id alheio → ESPERADO 403/404 (não 200). *Obs.: durante a execução foi confirmado que o gate de contrato (§5) atende essa regra de forma ainda mais estrita — o tenant B nem chega a listar.*

## 2. Investigação 2×LOGIN_SUCESSO — causa raiz e correção (RESOLVIDO neste bloco)

- **Causa raiz:** duplidade de responsabilidade — `AuthController.Login` gravava `LOGIN_SUCESSO`/`LOGIN_FALHA` **e** `AuthService.LoginAsync` (Data.cs L431) gravava outro `LOGIN_SUCESSO` em todo sucesso. MobileController também chamava o serviço e gravava → toda entrada gerava 2 linhas.
- **Correção:** fonte única no controller (única posição que vê sucesso **e** falha de cada ponto de entrada); payload enriquecido para preservar campos de observabilidade (`accessScope`, `primaryRole`, `tenantContextSelected`, `modulos`, `clienteStatus`). Commit `5cb43fc`.
- **Prova ao vivo (após rebuild 22:29):** 1 login ok + 1 login falho → delta exato **+1 LOGIN_SUCESSO** (220→221) e **+1 LOGIN_FALHA** (38→39); detalhes da linha nova contêm os campos enriquecidos.
- Consumidor verificado: `ObservabilidadeController/logins` lê apenas colunas genéricas (`perfil`, `acao`, `sucesso`, `ip_origem`) — nenhum campo específico da linha removida era consumido.
- Suíte após fix: 667/668; a única falha é o flake documentado `IsolamentoCadastros_ValidacoesDeNegocio_DuplicidadeEBloqueios`, que passa isolado (ver §6).

## 3. Matriz tela | ação | usuário | permissão/contrato | persistência | teste | resultado

Legenda: ✅ APROVADO · ❌ FALHOU · ⛔ BLOQUEADO · ⏸ NÃO EXECUTADO. Todos ✅ neste bloco.

| # | Tela/Rota | Ação | Usuário | Permissão/contrato esperado | Persistência esperada | Teste | Resultado |
|---|---|---|---|---|---|---|---|
| M1 | `/Account/Login` | login válido | gestor | qualquer | `usuarios.ultimo_login` atualizado + 1× LOGIN_SUCESSO | POST form antiforgery (sc1/A) | ✅ POST 302 → dashboard; `ultimo_login` do gestor atualizado; delta LOGIN_SUCESSO **+1 exato** |
| M2 | Home pós-login | redirecionamento | gestor | ADM360 contratado | — | GET (sc1/A) | ✅ URL final `/Administrativo360/Gestao/Dashboard` (não Login); `<title>` contém "Administrativo 360"; página completa (>20 KB) |
| M3 | `/Administrativo360/Gestao/Dashboard` | ver dashboard | gestor | módulo ADM360 ATIVO | leitura | GET (sc1/B) | ✅ 200 persistente na sessão |
| M4 | `/Administrativo360/Cotacoes` | listar | gestor | ADM360 contratado | leitura | GET (sc2) | ✅ 200 com 5 linhas do §1.1 — 9 marcadores conferidos (procedimentos, provedores, labels de status) |
| M5 | `Cotacoes/Detalhes/{id}` | detalhe | gestor | ADM360 contratado | leitura | GET (sc2 para `…0030`; sc4/M5b para `…0031`) | ✅ `…0030` = Angioplastia/OPMENEXO/PRONTA_PARA_ENVIO·RECEBIDA; `…0031` = Artroplastia/INPART/EM_RELACIONAMENTO — ambos 200 conforme §1.1 |
| M6 | `Cotacoes/SalvarMapeamento` | escrever mapeamento | gestor | ADM360 (escrita) + antiforgery | +1 linha `adm360_mapeamentos_de_para` com `criado_por`=gestor | POST form (sc2) | ✅ 302; contagem 4→5; linha nova `CX-BC-20260927230937 → Teste Jornada C interno` (PRODUTO, ATIVO, `criado_por`=`…7511`) |
| M7 | `/Administrativo360/DocumentosXml` | listar | gestor | ADM360 contratado | leitura | GET (sc2) | ✅ 200 com 2 linhas do §1.2 (marcadores NFE_COMPLETA, Conferido, Pendente, Quarentena) |
| M8 | `DocumentosXml/Detalhes/…0050` | detalhe | gestor | ADM360 contratado | leitura | GET (sc2) | ✅ 200, NFE nº1 com CONFIRMACAO_DA_OPERACAO |
| M9 | `/Financeiro/Index` | ver financeiro | gestor | FINANCEIRO.VER (BLOCO B) + contrato FINANCEIRO ATIVO | leitura | GET (sc7) | ✅ 200 em `/Financeiro/Index` (página completa, 36 KB), sem AccessDenied/Login |
| M10 | M4 + M5 | leitura | consulta (Auditor) | somente leitura | — | GET (sc3/M10a·b) | ✅ lista 200 (5 cotações do próprio tenant) + detalhe da angioplastia 200 |
| M11 | M6 (escrita) | escrita | consulta (Auditor) | sem `ADM360:MAPEAR_CADASTROS` | nenhuma linha nova | GET+POST (sc3/M11a·b) | ✅ recusa na camada API (403 policy `Adm360.MapearCadastros`), cada chamada gera **+1 ACESSO_NEGADO**, erro renderizado na tela no GET seguinte, zero novas linhas nas tabelas de destino |
| M12 | Revogação ao vivo (módulo) | desabilitar ADM360 → página | superadmin altera; gestor consome | contrato ATIVO→desabilitado | +1 `tenant_modulos_historico` por ação | API toggle + RefreshContext (sc4/F3–F5) | ✅ após desabilitar, a MESMA sessão (refresh de contexto, sem novo login) cai em `AccessDenied?module=ADM360&reason=MODULO_NAO_CONTRATADO`; reativar → 200 de volta (detalhes em §4/F) |
| M13 | `/Account/Logout` GET e POST | logout | gestor | sessão viva | sessão expirada imediatamente | GET+POST (sc6/G1·G3) | ✅ ambos 302; resposta do logout zera os 4 cookies de auth (Set-Cookie expired); dashboard → `/Account/Login`; mensagem "Sessão encerrada" exibida |
| M14 | `/AdminSaas/Index` | área superadmin | superadmin | global | leitura | login web + GET (sc4/M14) | ✅ 200 fora de Login/AccessDenied |
| M15 | M4 | listar | admin.clinica (Tenant B) | dados apenas do Tenant B | leitura | GET (sc5/X1) | ✅ **caminho real: gate de contrato primeiro** — ADM360 NÃO contratado na Clínica Modelo → 302 `AccessDenied?module=ADM360&reason=MODULO_NAO_CONTRATADO`; nem se chega à lista (que seria vazia: 0 cotações, §1.4). Isolamento garantido por contrato + tenant |
| M16 | `Cotacoes/Detalhes/{id Santa Casa}` | detalhe alheio | admin.clinica (Tenant B) | isolamento por tenant | leitura | GET (sc5/X2) | ✅ 302 `AccessDenied?module=ADM360&reason=MODULO_NAO_CONTRATADO` (nunca 200) — id alheio não renderiza |
| M17 | `/Administrativo360/Cotacoes/Relacionamento` | listar mapeamentos | gestor | ADM360 contratado | leitura | GET (sc2, cobertura extra) | ✅ 200 com os 4 mapeamentos de seed + contador PORTAL_DEMO |

## 4. Jornadas A–G — valores esperados (definidos antes) e veredito

### A — Login e home (gestor) — ✅ APROVADO (sc1)
- Esperado: POST do form (Email/Senha/`__RequestVerificationToken`) → 302 para home; home exibe Administrativo 360; **exatamente 1** linha LOGIN_SUCESSO.
- Real: POST 302 → `/Administrativo360/Gestao/Dashboard`; título "Administrativo 360"; `usuarios.ultimo_login` atualizado; delta LOGIN_SUCESSO **+1** (confirma o fix de §2 — sem dupla gravação).

### B — Sessão sem re-login — ✅ APROVADO (sc1)
- Esperado: navegação + refresh de contexto → 200 persistente; 0 novas LOGIN_SUCESSO.
- Real: Dashboard 200 → Cotacoes 200 → `GET /Account/RefreshContext` 200 (reassinatura do cookie, sem LOGIN_SUCESSO) → Dashboard 200; contador inalterado durante toda a navegação.

### C — Leitura cotações (gestor) — ✅ APROVADO (sc2 + sc4/M5b)
- Esperado: lista = 5 linhas do §1.1 conferidas uma a uma; detalhe `…0031` = Artroplastia/INPART/EM_RELACIONAMENTO.
- Real: lista 200 com os 5 procedimentos/provedores/status do §1.1; detalhe `…0030` 200 (Angioplastia) e detalhe `…0031` 200 (Artroplastia/INPART/EM_RELACIONAMENTO). Nenhum dado do §1.1 foi alterado pelas jornadas (apenas leitura).

### D — Escrita mapeamento (gestor) — ✅ APROVADO (sc2/M6)
- Esperado: salvar mapeamento novo → 200/302 e +1 linha com `criado_por` = gestor e situação ATIVA.
- Real: POST antiforgery (token da própria página Relacionamento) → 302; contagem 4→5; linha criada `CX-BC-20260927230937 → Teste Jornada C interno` (PRODUTO, ATIVO, `criado_por`=`…7511`). *Nota:* o valor executado seguiu o padrão `CX-BC-<timestamp>`/PRODUTO do cenário (não `CX-BLOCO-C`/UNIDADE_MEDIDA do rascunho original) — critério de aceitação mantido: escrita única, rastreável por autor, reversível por exclusão em manutenção.

### E — Somente leitura (consulta) — ✅ APROVADO (sc3)
- Esperado: reads 200; POST de escrita recusado (403/permissão) com **+1 ACESSO_NEGADO**; zero novas linhas.
- Real: E0 login do auditor → +1 LOGIN_SUCESSO (escrita única); M10a lista 200; M10b detalhe 200; M11a GET Relacionamento → recusa 403 na camada API (policy `Adm360.MapearCadastros`) +1 ACESSO_NEGADO e marcador de erro renderizado; M11b POST SalvarMapeamento → recusado (302 sem persistência) +1 ACESSO_NEGADO, erro visível no GET seguinte; nenhuma linha nova em `adm360_mapeamentos_de_para`.

### F — Revogação ao vivo (módulo ADM360) — ✅ APROVADO (sc4, 8/8)
- Esperado: desabilitar via API (mesmo mecanismo P6) → página do gestor vira 403/`MODULO_NAO_CONTRATADO` **sem novo login**; linha nova em `tenant_modulos_historico`; reativar → 200.
- Real (passos): M5b detalhe 0031 200 · F1 baseline contrato+historico · F2 toggle pelo gestor (sem ADMINISTRADOR_GLOBAL) → 403 +1 ACESSO_NEGADO · F3 superadmin desabilita → estado `habilitado=f/BLOQUEADO` +1 historico · F4 refresh de contexto na MESMA sessão → `AccessDenied?module=ADM360&reason=MODULO_NAO_CONTRATADO` · F5 reativa → estado final `t|ATIVO` +1 historico · F6 mesma sessão volta a 200 · F7 nenhum passo reautenticou a sessão web do gestor · M14 área superadmin 200.
- **Nuance documentada (F4):** o guard web lê as claims `module` do cookie emitidas no login (`SaasRouteGuardFilter`/`ModuleAccessService.IsModuleEnabled`) e não consulta o banco a cada request — por isso o passo intermediário "dashboard antes do refresh" continua 200 (claims antigas), e é o `RefreshContext` (propagação oficial) que aplica a revogação na mesma sessão. O comportamento final (revogado sem novo login) é o esperado e está coberto pela assert F4b.

### G — Logout (GET+POST) — ✅ APROVADO (sc6, 4/4)
- Esperado: ambos os verbos aceitos; após logout, página protegida → `/Account/Login`; novo login → 200 e LOGIN_SUCESSO **+1** (não +2).
- Real: G1 POST `/Account/Logout` → 302 para raiz (aplicação redireciona via rota de Login); a resposta zera os 4 cookies de auth (`PlantaoPro.Auth` + `AuthC1..C3`, expired 1970) e o dashboard seguinte cai em `/Account/Login` com a mensagem TempData "Sessão encerrada com sucesso" · G2 re-login → delta LOGIN_SUCESSO **+1 exato** (258→259 na corrida final) e dashboard 200 · G3 GET `/Account/Logout` → 302 e sessão morre · G4 auditoria consistente (delta total da corrida = 1).
- *Nota:* sem `[ValidateAntiForgeryToken]` em `Logout()` (só `[Authorize]`) — POST com body vazio funciona; comportamento idêntico nos dois verbos.

## 5. Cross-tenant (2 tenants por ID) — ✅ APROVADO (sc5, 6/6)

| Teste | Expectativa | Resultado |
|---|---|---|
| X1 — admin.clinica lista cotações (web) | 200 vazio OU recusa anterior | ✅ **302 `AccessDenied?module=ADM360&reason=MODULO_NAO_CONTRATADO`** — ADM360 não contratado no tenant B; as 5 cotações da Santa Casa jamais renderizam |
| X2 — admin.clinica abre detalhe `…0030` (web) | nunca 200 | ✅ 302 no mesmo gate; id alheio não renderiza |
| X3a — gestor lista parceiros via API | só os próprios | ✅ lista contém `a361…0001` (SC) e NÃO contém `a362…0001` (clínica) |
| X3b — gestor faz PATCH by-id no parceiro alheio | 403/404, sem efeito | ✅ 404 "Parceiro não encontrado ou não pertence a este cliente." (escrita no-op) |
| X3c — admin.clinica consome API ADM360 | 403 + auditoria | ✅ 403 `{"code":"MODULE_NOT_CONTRACTED","origem":"CONTRATO"}` + **delta ACESSO_NEGADO +1** |
| X4 — SQL: escritas da jornada D só no Tenant A | 0 fora / ≥1 dentro | ✅ `adm360_mapeamentos_de_para` com `codigo_externo like 'CX-BC%'`: 0 fora, 1 dentro; `adm360_cotacao_itens` com `CX-BC%`: 0 |

**Achado estrutural — gate de contrato em DOIS planos:** além do guard web (claims do cookie + `SaasRouteGuardFilter`), a API devolve 403 `MODULE_NOT_CONTRACTED` quando o módulo não está contratado no tenant, **independente de permissões** (o perfil admin.clinica tem 129 permissões, nenhuma `ADM360:*`, `modules=[]` vazio). Quando há contrato, a política por permissão segue valendo (mapeamento `Adm360.Ver`→`ADM360:VER` etc. em `Program.cs`). O isolamento cross-tenant portanto se dá em camadas: contrato → permissão → tenant_id nos queries.

## 6. Auditoria — deltas e contagens finais

Regra verificada em todas as corridas: **1 login real (API ou web) = exatamente 1 linha** (fix §2) e **cada recusa 403/404 gravada = +1 ACESSO_NEGADO**. Deltas por corrida (observados):

| Corrida | LOGIN_SUCESSO | ACESSO_NEGADO | Outras |
|---|---|---|---|
| sc1 (A+B) | +1 (login A) | 0 | `ultimo_login` atualizado |
| sc2 (M4–M9) | 0 (sessão reusada) | 0 | mapeamentos 4→5 (M6) |
| sc3 (E) | +1 (E0) | +2 (M11a, M11b) | zero novas linhas de negócio |
| sc4 (F, corrida final 23:54) | +3 informativos (logins reais da própria corrida: API superadmin, API gestor, web superadmin M14) | +1 (F2) | historico ADM360 +2 (F3, F5) |
| sc5 (cross-tenant) | +1 (admin.clinica) | +1 (X3c) | 0 escritas |
| sc6 (G, corrida final) | +1 F0 (re-login p/ testar logout) + 1 G2 = +2 | 0 | logout não grava LOGIN_* |
| sc7 (M9) | +1 | 0 | — |

Contagens finais (SQL, 2026-09-28 pós-execuções): LOGIN_SUCESSO **260** · LOGIN_FALHA **48** · ACESSO_NEGADO **124** · HABILITAR **6** / DESABILITAR **6** · OPERACAO_RESUMO **6** · SAUDE360_RESUMO **3** / SAUDE360_LISTAR_SEM_TENANT **3**.

`tenant_modulos_historico` ADM360: **10 linhas** = 5 pares DESABILITAR/HABILITAR dos re-runs da jornada F (23:37, 23:46, 23:51, debug F4 23:53, final 23:54) — re-runs documentados; o audit HABILITAR/DESABILITAR=6 inclui ainda 1 par de estágio anterior (etapa 0/P6). Estado final do contrato: `habilitado=t, status=ATIVO, preco/limite NULL` (idem ao baseline — a jornada F termina restaurando).

Suíte `dotnet test --no-build`: **667/668** — falha única = flake documentado `IsolamentoCadastros_ValidacoesDeNegocio_DuplicidadeEBloqueios` (passa isolado 1/1; dependência de timing/concorrência entre testes, não regressão). Sem 5xx não tratados nos logs do período.

## 7. Cotações/documentos e integrações externas (classificação pela condição real)

- Cotações/relacionamento/mapeamentos: fluxo **funcional localmente** com dados de demonstração (seed 140 + script de demo transacional) — todas as operações de §3/§4 persistidas e verificadas por SQL.
- Provedores `OPMENEXO`/`INPART`: dados simulados recebidos em formato próprio — **integração externa NÃO VALIDADA** (sem ambiente de fornecedor real nesta execução) → classificados como simulação/recebimento, não integração comprovada.
- DF-e: tabela de sincronização existe, sem sincronização executada contra Sefaz real nesta execução → **NÃO EXECUTADO** como integração externa.

## 8. Defeitos encontrados e corrigidos neste bloco

1. **`_SuprimentosNav.cshtml` (defeito pré-existente, corrigido):** o partial ficava em `Views/Administrativo360/`, mas Razor resolve `<partial name="..."/>` a partir do diretório da view + `Shared/` → **HTTP 500 em 11 views** de Suprimentos/Administrativo. Corrigido com `git mv` para `Views/Shared/_SuprimentosNav.cshtml` (nenhuma mudança de conteúdo); rebuild 22:58 validou as views. Renomeação staged, vai ao commit deste bloco.
2. **2×LOGIN_SUCESSO** — causa raiz e fix em §2 (commit `5cb43fc`).

Nenhum outro defeito de produto: nenhum passo exigiu ajuste de código além desses dois (a jornada F, por exemplo, foi verde sem mudança alguma — o refresh de contexto já implementado cumpria o requisito).

## 9. Lições técnicas das corridas (para os blocos D/E)

- PS 5.1 descarta **argumento string vazia** para executável nativo (`--data ''` fazia o curl engolir o próximo argumento) → helpers omitem `--data` quando o body é vazio.
- PS 5.1 **não faz subscript de array dentro de interpolação**: `"$parts[0]"` renderiza o array inteiro + `[0]` literal (reproduzido em script mínimo; correção: atribuir a variável escalar primeiro).
- `-L` no curl repete o header Cookie inicial em todos os hops da cadeia → passo que reemite/remove cookie exige 2 etapas: chamada com `$follow=$false` + chamada seguinte com jar atualizado.
- Set-Cookie de respostas só é capturado com `-D` explícito → variantes `Bc-WebGetRefresh`/`Bc-WebPostRefresh` do helper.
- `\t` dentro de string PS simples é literal → linhas de jar construídas com variável de tab.
- Cookie de auth é ticket assinado: continua criptograficamente válido após SignOut se o client não aplicar os Set-Cookie de remoção (logout só "morre" quando os 4 cookies são zerados — comportamento correto do servidor).

## 10. Vereditos consolidados

| Bloco | Status |
|---|---|
| 2×LOGIN_SUCESSO | ✅ APROVADO (causa raiz + fix `5cb43fc` + prova delta=1) |
| Matriz M1–M16 (+M17 extra) | ✅ APROVADO — 17/17 (M15/M16 com caminho real de gate documentado) |
| Jornadas A–G | ✅ APROVADO — 7/7 |
| Cross-tenant X1–X4 (+X3c) | ✅ APROVADO — 6/6 assert |
| Auditoria | ✅ APROVADO — 1 linha por login real; deltas por recusa consistentes; histórico de módulo rastreável |
| Cotações/central XML | ✅ funcional local; integrações externas classificadas (§7) |
| Defeitos do bloco | `_SuprimentosNav` 500 (corrigido, staged) + 2×LOGIN_SUCESSO (commit `5cb43fc`) |
