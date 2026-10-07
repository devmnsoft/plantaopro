# R4-J — Matriz de homologação da Rodada 4 (plano × módulo × perfil × ação × escopo)

**Entrega canônica (d1 §10).** Data: 2026-10-07 · branch `main` · base de origem `c84afc5` (`origin/main`) · HEAD `aff37c5` (22 commits locais).
Ambiente desta máquina: stack dev local (Web `https://localhost:52977`/`http://52976`, API `http://localhost:51976`/`51977`), PostgreSQL 18 local, banco de homologação `plantaopro_test` com migrations aplicados até **v2321**. Não há IIS nesta máquina → o que depende de servidor vivo fica NÃO EXECUTADO aqui (roteiro P0–P8: `docs/deploy/guia-implantacao-iis.md`).

## 0. Legenda

**Vereditos** (mesmo padrão de `docs/homologacao/vereditos-homologacao-2026-10-03.md`):
- **APROVADO** — executado nesta máquina com evidência reproduzível (suíte/browser/psql) e critério definido no doc-fonte.
- **FALHOU** — executado e o resultado não atendeu ao critério; exige correção antes da liberação.
- **BLOQUEADO** — executável apenas com dependência externa declarada (credencial real, decisão comercial, ambiente IIS).
- **NÃO EXECUTADO** — ainda não executado; caminho de destravamento registrado.

**Estados de prontidão** (separação obrigatória — nunca confundir):
- **Implementado** — código versionado (commit citado).
- **Testado** — suíte/verificação executada verde nesta máquina.
- **Homologado** — aceite do dono do domínio/usuário do módulo. **Esta matriz NÃO é aceite funcional**: habilita e organiza a homologação do usuário (`r4j-roteiro-homologacao.md`). Exceções com veredito próprio: GATE técnico (r4g) e homologação externa IA (r4f, 3/3).
- **Liberado p/ produção** — só quando o checklist §7 está inteiro.

## 1. Referências canônicas (verificadas em `plantaopro_test` em 2026-10-07)

### 1.1 Mecânica de controle
- **Sessão v2149**: a API calcula os claims `module`/`permissions` a partir dos módulos contratados (`tenant_modulos`) + vigência do contrato (regra por relógio, B6 S2) + permissões do perfil. A Web MVC checa os claims na requisição (`PermissionService`/`ModuleAccessService`, `backend/PlantaoPro.Web/Services/Security/AccessServices.cs`); falha de catálogo → `CATALOGO_NAO_CONFIGURADO` (log Error).
- **Exceção declarada (R4-F2)**: as telas fiscais do ADM360 são as únicas da Web que tocam o banco direto (repositórios Application+Infrastructure criados por request via `ConnectionStrings:Default`). Sem isso, nenhuma outra tela Web usa banco.
- **Módulos núcleo (sempre ativos para qualquer sessão)**: MEU_DIA, AJUDA, LGPD, CONTA, TREINAMENTO, USUARIOS, PERFIS, PERMISSOES, CONFIGURACOES, SEGURANCA, ASSINATURAS, CLIENTE_PORTAL.
- **Perfis globais admin (B6 S1, fonte única)**: `ADMIN_GLOBAL`, `ADMINISTRADOR_GLOBAL`, `SUPER_ADMIN`, `SUPER_ADMINISTRADOR` (`backend/PlantaoPro.CrossCutting/GlobalAdminProfiles.cs`). O superadmin de teste usa o perfil `ADMINISTRADOR_GLOBAL`.
- **Bloqueio por plano**: `perfil_permissoes.bloqueado_por_plano` existe e está intacto (em teste: 1635 vínculos ativos, **0** com a flag usada). Mecânica aprovada; uso comercial ainda não exercitado.
- **Catálogo controller→módulo** (dimensão "módulo" da matriz): `SaasRouteGuardFilter.ControllerModules` (~50 entradas); controllers públicos: Account, Error, Home, PlanosPublicos, Cadastro, Demo. Fonte única em código — não duplicar nesta matriz.

### 1.2 Planos ativos (tabela `plantaopro.planos`, 8 linhas)

| Plano | Preço | `permite_api` | `permite_white_label` | Notas |
|---|---:|---|---|---|
| essencial | 399 | - | - | base SaaS |
| profissional | 899 | - | - | |
| enterprise | 1999 | - | - | |
| enterprise-white-label | 2490 | sim | sim | |
| custom | 0 | - | sim | |
| revendedor | 3990 | sim | sim | |
| Plano demonstração local | 0 | - | - | slug vazio (uso local) |
| Plano Homologação Saúde 360 | 0 | - | - | slug vazio (uso local) |

- Todos com `permite_relatorios=false` (coluna criada no B6 v2313, default fechado em bancos existentes).
- **`plano_modulos` está VAZIO no banco de teste**: a alocação módulo↔plano por tabela não é usada na linhagem de homologação; o acesso efetivo nasce dos contratos `tenant_modulos` + colunas de capacidade do plano (`permite_api`, white label, limites em `plano_limites`/`plano_precos`).

### 1.3 Contratos dos tenants de teste (`tenant_modulos` ATIVO)

| Tenant | Módulo | Origem |
|---|---|---|
| Santa Casa Demonstração | ADM360, AUDITORIA, CONFERENCIA, ESCALAS, EXECUCAO | seed demo / migração demo |
| Clínica Modelo PlantãoPro | AGENDAMENTOS, CONSULTAS, ESCALAS, FINANCEIRO, NOTIFICACOES, PACIENTES, PLANTOES, RELATORIOS, USUARIOS, WHITE_LABEL | seed homologação |
| Tenant Q7 A / B | ADM360 | DEMO_MIGRATION (isolamento) |
| A360 Isolacao Tenant A / B | (módulo nulo — contrato de isolamento) | CONTRATO (B7 Q7) |

Nenhum tenant de teste tem `planos.id` vinculado em `tenants.plano_id` (todos "(sem plano)") — coerente com a mecânica por contrato acima.

### 1.4 Usuários de teste (dimensão perfil × ação)

Fontes: `docs/usuarios-teste.md` (seed idempotente) e seed demo Santa Casa (credentials em `r4i-fiscal.md`). Perfis efetivos verificados no banco em 2026-10-07.

| Usuário | Tenant | Perfil | Ações-chave exercidas nesta rodada |
|---|---|---|---|
| `superadmin@plantaopro.local` | (global) | ADMINISTRADOR_GLOBAL | governança SaaS (criar tenant/plano/assinatura), último-admin (B6) |
| `admin.clinica@plantaopro.local` | Clínica Modelo PlantãoPro | ADMINISTRADOR | usuários/perfis, BI, plantões, financeiro |
| `recepcao@plantaopro.local` | Clínica Modelo PlantãoPro | RECEPCAO | agendamentos, painel de chamada |
| `medico@plantaopro.local` | Clínica Modelo PlantãoPro | MEDICO | consultas, triagem, Meu Dia, IA |
| `financeiro@plantaopro.local` | Clínica Modelo PlantãoPro | FINANCEIRO | baixa/estorno/fechar-caixa, estorno de pre-autorização |
| `gestor@santacasa-demo.example` | Santa Casa Demonstração | ADMINISTRADOR_CLIENTE | ADM360: cotações, conferência, XML, fiscal pré-emissão |
| `medico@santacasa-demo.example` | Santa Casa Demonstração | MEDICO | E2E fiscal 11 passos (r4i) |
| `consulta@santacasa-demo.example` | Santa Casa Demonstração | AUDITOR | leitura de conferência/triagem (somente leitura) |

### 1.5 Achados de qualidade de dados (pré-existentes, não introduzidos pela rodada)

- `modulos_sistema` mistura os ~45 módulos reais (ordem 10–290) com centenas de linhas-legado formato par `MÓDULO.AÇÃO` (ordem 0) — anexo da dimensão módulo; não corrigir nesta rodada.
- Duplicidades ativas em `perfis` (FINANCEIRO ×2, MEDICO ×3, RECEPCAO ×2, ADMINISTRADOR_CLIENTE ×2) e vínculos duplicados ativos em `usuarios_perfis` (gestor@/medico@ da Santa Casa ×2) — sem efeito funcional (checagens deduplicam); limpar em rodada de higiene.
- `IDENTITY_SCHEMA_INCOMPLETE` / `MISSING_ADMIN_ROLE_LINK` count=1 (preexistente, backlog).

## 2. Matriz por módulo — execução da Rodada 4

Colunas: **Impl.** = código versionado · **Test.** = suíte/verificação verde nesta máquina · **Hom.** = homologação/aceite (tec. = técnica com critério definido; com.= comercial pendente; ext.= externa) · **Lib.** = liberado p/ produção. Veredito final do item conforme §0.

| Módulo / bloco | Escopo na rodada (resumo) | Impl. (commit-base) | Test. | Hom. | Lib. | Veredito | Evidência | Pendência registrada |
|---|---|---|---|---|---|---|---|---|
| Diagnóstico de baseline (D1) | escopo canônico da rodada | cdd517bc | ok | tec. | - | APROVADO | d1-baseline-diagnostico.md | - |
| Publicação/IIS (A2) | script `publish-iis.ps1`, validadores fail-fast de startup (porta dev, BaseUrl, DataProtection) | c875362e | ok (suíte base) | tec. (local); IIS vivo **fora desta máquina** | **não** | NÃO EXECUTADO (ambiente) | r4a2-publicacao-iis.md | executar P0–P8 no servidor de homologação |
| MVC seguro (A3) | `SaasRouteGuardFilter` (3 ramos: catálogo / público / bloqueado-por-tenant) | 5166bb6f | ok | tec.; UX de reidratação pendente revisão | **não** | APROVADO (técnico) | r4a3-mvc-seguro.md | revisão UX das telas de reidratação |
| Reconciliação monetária (A4) | inventário 250 colunas monetárias + regras pt-BR | b9011447 | ok (G4 no GATE) | tec.; **auditoria do banco produtivo F3 pendente** | **não** | APROVADO (técnico) | r4a4-reconciliacao.md | auditoria F3 + janela de upgrade do prod local |
| TestAuth anti-flake (A5) | flag `TestAuth:Enabled` somente Testing; `/MvcSeguroTest/TestSignin` 404 fora | 40b18dbb | ok | tec. | **não** | APROVADO (técnico) | r4a5-test-auth.md | - |
| Governança SaaS (B6) | S1 fontes únicas (GlobalAdminProfiles), S2 vigência por relógio, S3 máquina TRIAL/ATIVA, v2312/v2313, DateOnly | 0cd03ab0 (+b3d057d) | ok | tec.; **aceite comercial MNSOFT pendente** | **não** | APROVADO (técnico) | r4b-governanca-b6.md | v2312/v2313 no prod local ("JANELA A2/F3"); auditoria DateOnly Npgsql 10 |
| ADM360 core (B7) | T01–T18: cotações (cancelar/estorno), conferência, XML, isolamento tenanted, v2314 | a97a8cd0 | ok | tec.; portais reais OPMENEXO/INPART pendentes | **não** | APROVADO (técnico) | r4c-adm360-b7.md | v2314 na janela; portais reais |
| Saúde 360 financeiro (B8) | T01–T15: `clinica_*` (baixa/estorno/fechar-caixa), pre-autorização na finalização, v2315/v2316, `fila_atendimento` legada criada | 628d311f | ok (flake aceito sob observação) | tec.; **UI real/IIS viva pendente** | **não** | APROVADO (técnico) | r4d-saude360-b8.md | v2315/v2316 na janela; flake Aceite12 sob observação |
| Plantões / Meu Dia / BI (B9) | plantões até encerrado/contestação c/ estorno, Meu Dia real, BI por competência/fuso/escopo, relatórios por `data_negocio`, v2317–v2320 (bug latente de catálogo corrigido) | 5cf67328 | ok (32/32) | tec.; **UI real/IIS viva pendente** | **não** | APROVADO (técnico) | r4e-plantoes-meu-dia-bi.md | v2317–v2320 na janela; flake Aceite17 sob observação |
| IA (B10) | pronta p/ chaves Groq/Gemini/DeepSeek; AES-GCM por tenant; fallbacks; probes | 292d3560 (+5eb75176, 39ecb741) | ok (194/194) | **externa 3/3 executada** (Groq OK; Gemini com ressalva free tier; DeepSeek OK pós-crédito) | **não** | APROVADO (técnico + externa) | r4f-ia-b10-prontidao-chaves.md | nota de produção p/ modelos de raciocínio (limite de saída / content vazio) |
| Design (C11) | C11.1–C11.4: tokens de z-index/contraste AA, breadcrumb canônico único, viewports reais, botões triagem/estorno | ead665ca (cadeia 11ea69fc→ead665ca) | ok (build + suite; CSS = build verde basta) | **visual pendente do usuário** (roteiro §8) | **não** | APROVADO (técnico) | r4h-c11-design.md | aceite visual |
| Fiscal pré-emissão (F1+F2) | A29 MVP: v2321, parâmetros por tenant, pré-documento interno, conferência de referências; Web MVC (configurar/notas/nova/detalhes), gate ADM360, credencial A33 | aff37c5 (F1 48a600c, F2 aff37c5) | ok (57/57 F1; 1106/1106 completa; E2E 11 passos verbatim) | tec.; **emissão autorizada = N/A por design (A29)** | **não** | BLOQUEADO (escopo P1) | r4i-fiscal.md | P2: credenciais reais + decisão comercial operação/UF/provedor → desbloqueia emitir/assinar/transmitir/cancelar |
| GATE de homologação (r4g) | G1 startup, G2 auth/TestSignin, G3 migrações L/U, G4 monetário 250 colunas, G5 validação sem efeitos, G6 isolamento 8/8 (Q7) | 89c85b4 | ok | tec. | **não** | APROVADO 6/6 | r4g-gate-homologacao.md | - |
| Módulos fora da rodada (PACIENTES, AGENDAMENTOS, CONVÊNIOS, PARCEIRO, COMERCIAL, SUPORTE, LGPD, etc.) | não tocados | herdam `docs/homologacao/*` de 2026-10-03 | sem regressão (suítes completas verdes na contagem final) | herdam veredito de 2026-10-03 (APROVADO 39 / FALHOU 0 / BLOQUEADO 4 / NÃO EXECUTADO 3) | **não** | HERDA 2026-10-03 | docs/homologacao/matriz-modulos-homologacao-2026-10-03.md | os BLOQUEADOS de lá permanecem (IIS/credenciais) |

## 3. Dimensão plano × mudanças desta rodada

| Plano | Mudança efetiva desta rodada | Exercício feito | Estado |
|---|---|---|---|
| todos os 8 | `permite_relatorios=false` default (v2313) — relatórios fechados onde a flag não abrir | GATE G4/G5 + relatórios B9 por escopo | APROVADO (mecânica); efeito comercial por plano = pendente de cliente real |
| todos os 8 | máquina de assinatura TRIAL→ATIVA→EXPIRADO por relógio (B6 S3) | testes de governança + contratos demo | APROVADO (mecânica) |
| todos os 8 | vigência de contrato por relógio no catálogo da sessão (B6 S2) | testes de governança + acesso por módulo | APROVADO (mecânica) |
| enterprise-white-label / revendedor / custom | `permite_api` / white label intactos | BFF/API verdes nas suítes completas | APROVADO (sem regressão) |
| (nenhum em teste) | `bloqueado_por_plano` por permissão | mecânica verificada intacta; 0 usos no banco de teste | APROVADO (mecânica); uso comercial não exercitado |

Conclusão: nenhum plano foi "homologado comercialmente" — a diferenciação por plano foi **validada como mecânica**; o efeito em clientes reais só existe com IIS + assinaturas reais (checklist §7).

## 4. Veredito consolidado da rodada

| Veredito | Qtd | Itens |
|---|---:|---|
| APROVADO | 13 | D1, A3, A4, B6, B7, B8, B9, B10, C11 (téc.), F1+F2 (tec.), A5, GATE 6/6 (+ D1 baseline) |
| FALHOU | 0 | - |
| BLOQUEADO | 1 | Fiscal: emissão autorizada (P2 — credenciais reais + decisão comercial op/UF/provedor) |
| NÃO EXECUTADO | 1 | IIS vivo (A2: P0–P8 no servidor) — dependência de ambiente fora desta máquina |

Separação por estado (leitura diagonal da matriz):
- **Implementado**: tudo acima (22 commits `cdd517bc`..`aff37c5`).
- **Testado**: tudo, exceto o que depende de IIS/credenciais reais.
- **Homologado (técnico)**: todos os APROVADOS, com exceção do aceite visual C11 (pendente do usuário, roteiro §8).
- **Homologado (comercial/funcional)**: **nenhum módulo** — este é o papel do roteiro r4j-roteiro-homologacao.md + eventual IIS.
- **Liberado p/ produção**: **nada** até o checklist §7 estar inteiro.

## 5. Backlog consolidado (ponta p/ detalhe nos docs-fonte)

Bloqueia liberação? **N** = não (tratar em rotina) · **J** = sim/janela.

| # | Item | Origem | Tipo | Bloq.? |
|---|---|---|---|---|
| 1 | Upgrade do banco produtivo local `plantaopro` até v2321 (v2312–v2321 da rodada + v2311 A4/F1) | A4 F1 + F1 | ambiente | J — "JANELA A2/F3" |
| 2 | IIS P0–P8 no servidor de homologação/produção (novo: variáveis do pool Web p/ Fiscal) | A2 + F2 | ambiente | J |
| 3 | Auditoria das colunas monetárias no banco produtivo (F3) | A4 | risco | J |
| 4 | Fiscal P1: decisão comercial operação/UF/provedor + credenciais reais → emitir/assinar/transmitir/consultar/cancelar + XML/protocolo | F1/F2 (d1 §9, A29/A33) | decisão + ambiente | N (escopo registrado; MVP vigente) |
| 5 | Portais reais OPMENEXO/INPART no ADM360 | B7 | ambiente | N |
| 6 | UI real/IIS viva para Saude360 B8 e Plantões/B9 (aceite em tela real) | B8, B9 | ambiente | N |
| 7 | Aceite visual do usuário (C11: foco, contraste, breadcrumb, responsive, botões) | C11 | decisão | N (bloqueia apenas o "homologado" de design) |
| 8 | Auditoria DateOnly vs Npgsql 10 (caminho SaaS comercial) | B6 → B7 → B8 | risco técnico | N |
| 9 | Flake Aceite17 / Aceite12 sob observação | B8, B9 | estabilidade | N |
| 10 | `IDENTITY_SCHEMA_INCOMPLETE` / `MISSING_ADMIN_ROLE_LINK` count=1 | pré-existente | dado | N |
| 11 | Higiene de dados: linhas legado em `modulos_sistema`, perfis/vínculos duplicados (§1.5) | pré-existente | dado | N |
| 12 | `DecideAsync(:94)` AGENDADO; recovery ENVIANDO em IIS | pré-existente | risco técnico | N |
| 13 | CS86xx/CS0108/CS321 (20 avisos fora do Fiscal); `ChangeStatusAsync` sem filtro tenant; `'confirmado'` legado; `'REABERTO'` só no índice único; `ValidarPlanoBiAsync` 403; `fila_atendimento` sem escritor | pré-existente | dívida técnica | N |
| 14 | Guia IIS: `Ai__EncryptionKey` "32+ chars" → corrigido nesta entrega (64 hex) | B10 | doc | N (fechado) |
| 15 | xUnit1031 em `Administrativo360R4F1FiscalPreEmissoesTests.cs` (~L400) | F1 | qualidade de suíte | N |
| 16 | `PublishUrl` Desktop legado em `FolderProfile.pubxml` (Web) | pré-existente | config | N (o script `publish-iis.ps1` manda) |
| 17 | Exibição UTC nas views; accents decorativos; CSS legado duplicado; `@media (pointer:coarse)` ausente | pré-existente / C11 | polimento | N |
| 18 | Nota de produção p/ modelos de raciocínio (limite de saída; content vazio = RespostaInvalida) | B10 | decisão | N |
| 19 | Backlog herdado de 2026-10-03 (os 4 BLOQUEADO + 3 NÃO EXECUTADO de lá) | docs/homologacao/vereditos-homologacao-2026-10-03.md | herança | ver fonte |

## 6. O que destrava a liberação (checklist produção)

1. **Banco**: `Tools.Database` upgrade do destino até **v2321** (próxima versão livre: v2322) — inclui v2312–v2321 da rodada + v2311 (A4/F1).
2. **IIS**: P0–P8 do guia com as **novas variáveis do pool Web** (F2): `ConnectionStrings__Default` (obrigatório — Web toca o banco nas telas fiscais) e `Fiscal__Credenciais__{referencia}` (opcional; só para exercício de emissão).
3. **Auditoria monetária F3** no banco produtivo (A4).
4. **Aceite funcional do usuário** por módulo: `r4j-roteiro-homologacao.md` (este documento não substitui o aceite).
5. **Decisão comercial fiscal** (operação/UF/provedor + credenciais reais, P2) — apenas se emissão autorizada entrar na liberação atual; senão, registrar o bloqueio e manter o MVP A29.
6. **Push explícito**: 22 commits pendentes em `main` local sobre `origin/main` (`c84afc5`) — push só por decisão registrada.
7. Registrar evidência de cada passo em `docs/evidencias/<data>/<ambiente>-iis.md` (formato passo→esperado→obtido, como no guia).
