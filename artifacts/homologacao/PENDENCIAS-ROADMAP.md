# Pendências e Roadmap — Administrativo 360 (pós-homologação)

Data: 2026-09-26 · Ambiente: local dev · Banco: `postgres`/schema `plantaopro`

> Atualização 2026-09-28: execuções BLOCO A+B concluídas. P1, P3, P5 e P6 desta seção foram resolvidos; novos achados da execução estão no §3.

Nenhuma pendência abaixo bloqueia o MVP do Administrativo 360 nem o acesso do gestor. Classificação: **Alto** (bloqueia uso real), **Médio** (confusão/risco operacional), **Baixo** (higiene/técnica).

---

## 1. Pendências por impacto

### Médio

| # | Item | Onde | Descrição | Mitigação atual | Ação sugerida |
|---|---|---|---|---|---|
| P1 | Assimetria de papéis Web × API | `backend/PlantaoPro.Web` (atributos `[Authorize(Roles=...)]` dos controladores ADM360) × `CrossCutting\AccessScope.cs` (RoleCatalog) | Os controladores ADM360 no Web admitem papéis (`GESTOR_OPERACIONAL`, `CONSULTA_CLIENTE`, `AUDITOR`) que não constam no RoleCatalog usado pela API; a API usa um conjunto menor de papéis | Permissão (grant) continua sendo o gate efetivo em ambos os lados; a homologação provou o comportamento por permissão | Unificar catálogo: registrar os papéis ADM360 no RoleCatalog e usar a mesma lista nos dois lados; adicionar teste que compara as duas listas |
| P2 | Anomalia de roteamento: todo `POST /Account/*` → 405 `Allow: GET` | `backend/PlantaoPro.Web\Program.cs` (template único `{controller=Account}/{action=Login}/{id?}`) + `AccountController.cs` | Comportamento vivo reproduzível: `POST /` funciona (é onde o form aponta → 302 login OK); `POST /Account/Login` e `POST /Account/RefreshContext` (declarações `[HttpPost]`) retornam 405. GET nas mesmas rotas funciona. Build fresco confirmada; sem código customizado produzindo esse 405 (busca por "Allow" vazia). Suspeita: interação default-value de segmento + seleção de endpoint | Funcional: login e renovação de contexto funcionam pelas variantes existentes (form no `/`; RefreshContext via `GET /Account/RefreshContext?returnUrl=…`) | Investigar com route mapper log (DI `IActionDescriptorCollectionProvider`) num ambiente isolado; decidir se normaliza para `[Route("Account/Login")]` explícito; registrar teste de contrato `POST /Account/Login → 200/302` |
| P3 | Home padrão do AUDITOR cai em AccessDenied | Web — perfil AUDITOR | Login como consulta/AUDITOR redireciona para `/Account/AccessDenied?ReturnUrl=%2FAuditoria%2FIndex`; o AUDITOR não abre sua própria página `/Auditoria/Index` | Fora do caminho crítico ADM360 (ele abre a home ADM360 normalmente — screenshot) | Verificar grant/catálogo da rota Auditoria para AUDITOR; definir home correta por perfil |
| P4 | Rotas de ações de `Adm360CadastrosController` possivelmente inalcançáveis via attribute routing | `backend/PlantaoPro.Api\Controllers\Adm360CadastrosController.cs` | Ações com `[Http*("...")]` podem não casar com o template de rota do controller; as rotas usadas na matriz (`gestao/dashboard`, `cotacoes`, `cotacoes/mapeamentos`) estão validadas, mas o mapa completo das ações não foi exercitado uma a uma | Escrita crítica coberta pela matriz M3 com persistência | Percorrer todas as ações do controller e executar 1 request cada (teste de fumaça por rota) |
| P5 | `RefreshContext` reemitir claims de permissão **sem normalização** | `Program.cs` (mapa política → código) + fluxo refresh | No refresh os claims voltam na forma dois-pontos do DB (`ADM360:VER`) enquanto o login normaliza para pontual maiúsculo (`ADM360.MAPEAR_CADASTROS`); as policies resolvem por request, então o impacto é cosmético/defensivo | Homologação passou após refresh (R4b/R5: 302 + ticket novo + página 200) | Fazer o refresh passar pelo mesmo pipeline de normalização do login |

### Baixo

| # | Item | Onde | Descrição | Ação sugerida |
|---|---|---|---|---|
| P6 | INSERT sem coluna `nome` | `ModuleContractingService.cs` L128 | O caminho de contratação via serviço não preenche `nome` em `tenant_modulos` (as seeds/pgadmin agora preenchem); pode gerar contrato com nome nulo pela API | Incluir `nome = coalesce(m.nome, m.codigo)` |
| P7 | `schema_migrations` vazio (0 linhas) | DB `postgres`/`plantaopro` | Migrations aplicadas manualmente neste ambiente (decisão documentada); o runner `Tools.Database` não foi usado para não divergir checksums | Decidir política oficial: backfill de registro com checksums atuais, ou manter manual com manifest como fonte única — documentar |
| P8 | Cookie de autenticação grande (~14 KB em 3 chunks) | Sessão Web `PlantaoPro.AuthC1..C3` | Acima da diretriz de ~4 KB por cookie (mitigado pelo chunking); aumenta overhead por request | Avaliar mover claims pesadas (lista de 48 permissões) para store server-side; manter no cookie só id + tenant + hash |
| P9 | Referências desatualizadas em SQL | `scrpt_completo.sql` (header diz v2.1.9.7) e `121_acesso_demo_local.sql` (RAISE menciona "migration v2197") | Textos históricos inconsistentes com a versão real | Alinhar versões comentadas |

### Observações de ferramenta (não-produto)

- `curl -w '%{http_code}'` imprimiu artefatos tipo `200000` em alguns requests nesta sessão (curl 8.21.0, PS 5.1). O status-fonte em todos os casos foi o cabeçalho capturado em arquivo `-D`. Sem impacto no produto.

---

## 2. Roadmap atualizado

### Fase 1 — Estabilizar o que foi homologado (imediato, 1–3 dias)
- [ ] Fix P2 (anomalia POST `/Account/*`) com teste de contrato; é a fonte de mais confusão operacional (mitigada: form no `/` + Logout GET/POST; quirk documentado em §3.3)
- [x] Fix P1 (RoleCatalog único Web/API) + teste comparativo — BLOCO B
- [x] Fix P3 (home do AUDITOR) — BLOCO B (seed 121 + grupo AuditoriaAcesso)
- [x] Registrar em CI: execução dos seeds numa base fresh (prova de fresh-install) — BLOCO A (cenários A–E + guardiã de manifests)

### Fase 2 — Cobrir o NÃO EXECUTADO da matriz (próxima semana)
- [ ] E2E de escrita via UI Web (formulário de mapeamento/diário) com persistência verificada
- [ ] Teste negativo cross-tenant app-a-app (token do tenant A acessando dados do tenant B via API e Web)
- [ ] Fumaça por rota de `Adm360CadastrosController` (P4)
- [ ] Backfill/política de `schema_migrations` (P7)

### Fase 3 — Reforço de produção (2–4 semanas)
- [ ] Reduzir tamanho do cookie de sessão (P8)
- [ ] Auditoria de escrita (hoje apenas LOGIN_* e ACESSO_NEGADO; criar `OPERACAO_ESCRITA` ou estender `OPERACAO_RESUMO` para mutações ADM360)
- [ ] Contrato via `ModuleContractingService` com `nome` (P6) + idempotência testada contra base com contrato duplicado
- [ ] Limpeza de referências/versiones em SQL (P9)
- [ ] Documentar runbook de homologação neste ambiente (ordem de scripts já está em §4 do relatório)

### Critério de avanço de fase
Fase 1 concluída quando: P1–P3 com testes verdes em CI e sem regressão no fluxo gestor (reexecutar matriz M1–M7b). Fase 2 quando: itens da matriz 17/18 viram APROVADO com evidência commitada.

---

## 3. Achados e correções — execuções BLOCO A/B (2026-09-27/28)

### 3.1 Resolvidos nestas execuções

| Item | Resolução | Prova |
|---|---|---|
| P1 (assimetria de papéis Web×API) | Roles clínicos registradas no RoleCatalog (ENFERMAGEM, HOSPITAL, RECEPCAO, AUDITOR_CLINICO, TRIAGEM etc.) com prioridade abaixo do par-base; policies/menu/claims alinhados | Suíte 668/668 + testes `RoleCatalogConstantsParityTests` |
| P3 (home do AUDITOR) | Módulo AUDITORIA contratado no tenant via seed 121 (mesmo contrato canônico do ADM360) + grupo de permissões `AuditoriaAcesso` | Home `/Auditoria/Index` APROVADA ao vivo |
| P5 (claims de refresh sem normalização) | `SessionClaimsBuilder` único compartilhado por Login e RefreshContext (mesma resposta da API → cookie idêntico); V1200/V2153/V2156 apontam para o builder; normalização canônica `:` → `.` | Testes `SessionClaimsBuilderTests` + jornada P6 (recusa/liberação por RefreshContext sem novo login) |
| P6 (INSERT sem `nome`) | `nome = coalesce(m.nome, m.codigo)` no upsert de contratação | Jornada P6: linha criada com `nome='Financeiro'` pelo fluxo real (revisão→solicitação→aprovação) |
| B6 (Logout só GET) | Logout exposto em GET e POST | Matriz de rotas/sessão |

### 3.2 Causas raízes identificadas e corrigidas no BLOCO B

| # | Achado | Causa raiz | Correção | Status |
|---|---|---|---|---|
| F1 | `42703 coluna m.funcionalidades não existe` no catálogo de módulos e no upsert AdminSaas | Migration v2149 órfã (ausente dos dois manifests) nunca se aplicou: colunas comerciais de `modulos_sistema` (categoria, essencial, funcionalidades, limite_padrao), tabela `tenant_modulos_historico`, índice e FK dela | `v2201_reconciliar_colunas_modulos_sistema_comercial.sql` (DDL idempotente copiado do v2149), registrada nos manifests, scrpt_completo regenerado/validado (100%) | APROVADO (aplicada; cenários de instalação continuam verdes) |
| F2 | Todo `GET` do ciclo de contratação 500 com `42883 operador não existe: uuid = bigint` | Drift estrutural: `tenants.cliente_id` BIGINT × `clientes.id` UUID; `GetAsync` fazia join direto entre os dois | Join removido do `GetAsync` (nome via `coalesce(t.nome,'')`) | APROVADO ao vivo; drift estrutural completo segue no §3.3 |
| F3 | `estadoContratual` sempre vazia no catálogo | Dapper não mapeia alias com sublinhado (`as estado_contratual`); padrão do repo é alias entre `""..""` | Alias citado `""EstadoContratual""` nas duas queries (catálogo + itens) | APROVADO (estado `BLOQUEADO`/`ATIVO`/`SOLICITADO` retornando na jornada) |
| F4 | Toggle de tenant `BLOQUEADO` falhava | `SaasCoreServices` L156 consultava `reg_status` em `plantaopro.clientes` para validar o tenant; a tabela `tenants` não tem `reg_status` (tem `status`) | Consulta alterada para `exists(select 1 from plantaopro.tenants where id=@tenantId)` | APROVADO |
| F5 | `desabilitar-tenant`/`habilitar-tenant` 500 com `42P10` (ON CONFLICT sem índice correspondente) **em qualquer ambiente** | `ToggleTenantAsync` inferia `on conflict (tenant_id,modulo_id) where reg_status='A'`; a cadeia oficial só possui `ux_tenant_modulos_contrato_ativo` com predicado `reg_status='A' and modulo_id is not null` | Alinhado ao mesmo target que o `DecideAsync` já usava (funcional desde a P3 da jornada) | APROVADO ao vivo (revogação→recusa→reativação) |
| F6 | Gestor negado em `/Financeiro/Index` com `PERMISSAO_NEGADA` mesmo com módulo contratado | Modelo v2.14.9 é 100% data-driven (claims `permission` do perfil); o perfil demo `ADMINISTRADOR_CLIENTE` tinha 48 permissões e zero de FINANCEIRO | Seed 121 concede apenas `FINANCEIRO.VER` ao perfil (idempotente, vínculo único) | APROVADO (acesso liberado pós-refresh; recusa mantém motivo correto pós-revogação) |

### 3.3 Documentado (fora do escopo desta execução)

- **Drift estrutural `tenants.cliente_id` (bigint) × `clientes.id` (uuid)**: além do `GetAsync` corrigido (F2), o mesmo padrão afeta `ProductivityActionServices.cs` L296 (API do Meu dia 500 com `42883 bigint = uuid`). Requer decisão canônica de tipo e migration dedicada.
- **Dapper/timestamptz sistêmico**: mapeamento de colunas `timestamptz` para `DateTime?` raw em outros DTOs pode apresentar o mesmo comportamento latente do F3 (só aparece quando o alias não está citado). Corrigir por DTO conforme consumo real, não cegamente.
- **Comportamento do toggle**: `habilitar/desabilitar-tenant` escreve `PrecoContratado`/`LimiteContratado` como recebidos; body sem preço zera o preço do contrato (observado na jornada: `250.00 → null` na revogação). Sugerido coalescer nulo→valor existente em ciclo futuro.
- **Sessões em memória**: store de sessões da API some no restart; o cookie mantém as claims (JWT embutido) e páginas normais funcionam, mas `/Account/RefreshContext` exige sessão viva → re-login após cada restart. Avaliar persistência para ambientes compartilhados.
- **Flake de teste**: `IsolamentoCadastros_ValidacoesDeNegocio_DuplicidadeEBloqueios` passa isolado/em build limpo; suíte final 668/668 no build da jornada.
- **`SaveAsync` do AdminSaas** (SaasCoreServices L122-126) também escrevia as 4 colunas órfãs do v2149 — coberto pela v2201 em qualquer base.

