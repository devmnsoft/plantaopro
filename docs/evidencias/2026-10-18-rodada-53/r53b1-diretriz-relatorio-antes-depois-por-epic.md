# R53 · r53b1 — Relatório da diretriz UX/texto/acesso — antes/depois por epic

Data: 2026-10-18 · Sessão: `opencode` · Consolidação final do roadmap r24a1 (R24→R53, 30 sprints).
Fontes: relatórios finais versionados `r{N}z-relatorio-final-consolidado.md` (N=24..52), inventário
mestre `r25a0`, contratos permanentes em `PlantaoPro.Tests`, recon do estado atual (R53) e auditoria
de tom executada nesta rodada.

## Visão geral

- **As 30 rodadas R24→R53 foram executadas e fechadas** com evidência versionada, contrato
  permanente e push em cada uma.
- Suíte de fatos: **1529** (base R23) → **1605** (consolidação), crescimento inteiro versionado por
  contrato (progressão completa na seção 5).
- Cada regra de design/languagem virou **teste anti-regressão** — a diretriz não depende de memória:
  `dotnet test --filter "FullyQualifiedName~R42ScannersPermanentes"` cobre sozinha E4/E5/E6/E8.
- Build gate de casa (multiset 160 chaves vs pristino) saiu `DIFF=NENHUMA` em todas as rodadas que
  tocaram código; smoke e2e completa verde na R52 (`R52_E2E_OK`, 13/13 verificações).

### Mapa das 4 diretrizes do usuário

| Diretriz (r24a1) | Epics | Rodadas | Evidência principal |
|---|---|---|---|
| Evoluir o design | E0, E4, E6, R49 | R26, R43–R46, R49 | `docs/ux/design-system-consolidado.md`; contratos R26/R43–R46/R49 |
| Linguagem simples + instruções por página | E1, E3, E5, E8, R50 | R25–R31, R34, R35–R37, R50 | contratos R25/R27/R28/R29/R30/R31/R34/R35/R36/R37/R50; glossário D-R50-1 |
| Acesso isolado por CPF/CNPJ+email/senha | E2 (parte 1) | R38 | D-R38-1/2; triagem de 161 ocorrências com 70 keeps classificados |
| Tenant visível só para super-admin | E2 (parte 2) | R39 | `R39TenantSoParaSuperAdminContratoTests` (papéis de plataforma + whitelist de 25 views) |

## Antes (estado R24/R25, antes da varredura)

Estado registrado no inventário-mestre R25 sobre o produto pré-diretriz:

- **Jargão renderizado**: "Atualizar BFF", URLs de endpoint crua, "retornado pela API", GUIDs como
  label/placeholder, "Fila e Transmissões Outbox", "billing", `SHA-256` sem contexto.
- **Homologação visível ao usuário**: banner demo, rotas "Compatibilidade 1.12" com texto de
  homologação, placeholders vazando hospital demo (15 pontos classificados).
- **Contraste**: 192 linhas violadoras (`text-muted` sobre fundo escuro) em 35 views Adm360
  concentradas (ex.: Cotacoes/Detalhes 27, Gestao/Dashboard 35).
- **Formulários**: 342 inputs em 59 views com só 164 `required`, 43 `maxlength`, 1 `pattern`;
  checkbox list sem componente padronizado em lugar nenhum (32 checkboxes, zero `asp-for`).
- **Listas**: campos pediam colar UUID; GUID truncado exibido em UI (ex.: `FechamentoId[.8]`).
- **Instruções**: apenas 65 views com `pp-screen-guide` contra dezenas de telas operacionais sem
  "como usar"; 29 páginas Adm360 sem guia.
- **Acesso/isolamento**: 161 ocorrências de "tenant" em ~95 views, sem classificação;
  `AssistenteIa` exibindo `TenantId` truncado; relatórios SaaS globais sem decisão registrada
  (resolvida no D-R24-3).
- **SaaS**: nenhum endpoint checou limite de plano ao criar usuário; tela de faturas do cliente era
  estática, sem pagamento; MNSOFT cego para faturas vencidas (sem alerta/notificação).
- **Acessibilidade**: nenhum nome acessível auditado; `accessibility.css` existia mas **nenhum layout
  a carregava**; azul de marca em FAIL de AA (`#1f73f1`).
- **Performance**: sem linha de base medida (medida pela primeira vez na R52).

## Depois — por epic

### E1 · Linguagem simples — FECHADO (R27, R28, R29, R34; passada final R50)

Antes: jargão técnico em todas as zonas (exemplos acima). Depois:

- R27 portal: 19 pontos em 7 views ("Atualizar BFF"→"Atualizar cobertura", endpoint cru removido).
- R28 Adm360-1: 13 pontos em 8 views (UUID fora de todo label; "Outbox"→"Envios para os
  fornecedores").
- R29 demais módulos: 19 pontos em 9 views — padrão "X pela API" extinto da UI renderizada.
- R34 final: **108 valores reescritos** (27 chaves × 4 culturas) nos `UiTexts.*.resx` + 3 pontos em
  Observabilidade; anti-regressão por token nas 4 culturas.
- R50 passada final de tom: glossário canônico (D-R50-1) + 13 correções (fallback/token/endpoint
  banidos em copy; Convites sem id fragmentado).
- Travado por: `R27/R28/R29LinguagemSimples*`, `R34LinguagemSimplesResxContratoTests`,
  `R50CopyContratoTests`, `R25TextosSemHomologacaoContratoTests` + whitelist de domínio.
- Pendência: decisão "Dashboard"→"Painel" (termo de negócio, não jargão) e auditoria de exclamações
  (abaixo, seção 6).

### E2 · Acesso e isolamento — FECHADO (R38, R39)

Antes: "tenant" espalhado em ~95 views; modelo de acesso sem decisões registradas. Depois:

- R38: 161 ocorrências triadas → 70 keeps integralmente classificados; jornada clínica (20 views),
  Central de Segurança (11 views), partials, Ajuda, WhiteLabel ×5, Parametrizações ×5 limpos;
  **D-R38-1** (instituição por CNPJ; pessoa por email global único + senha com vínculo explícito por
  cliente; CPF opcional) e **D-R38-2** ("tenant" só como linguagem interna/plataforma) registrados.
- R39: `AdminSaas` confirmada restrita a `AdministradorGlobal`/`Suporte`/`Auditor` e contrato
  `R39TenantSoParaSuperAdminContratoTests` (2 fatos) travando que nenhum `.cshtml` fora de
  `AdminSaas/` + whitelist de 25 keeps contenha "tenant". Relatório R39 declara E1–E6 fechados.
- Pendência remanescente: login "escolher CPF/CNPJ/e-mail" como epic próprio de produto (o contrato
  atual aceita o identificador; a experiência de escolha é pendência de roadmap futuro).

### E3 · Instruções por página — FECHADO (R35, R36, R37) + extensão R51

Antes: 29 páginas Adm360 operacionais sem guia. Depois: 29 guias no padrão da casa
(para que serve / como utilizar / efeito / próxima ação) em três lotes (7+12+8), códigos internos
removidos das telas fiscais ("(A29)", "(MVP A29)", "(P1)"…), renome "Cadastros Mestre"→"Cadastros
Organizacionais". Excluídas documentadamente: OrcamentoImprimir, ValeImprimir, `_AlertasAcao`.
Travado por `R35/R36/R37InstrucoesPorPaginaLoteNContratoTests`.
Extensão (R51): **Ajuda central com "como usar" por área** — tópicos filtrados por perfil, família
administrador-cliente ganhou seu manual (D-R51-1/2), 7 artigos novos com link direto para a tela
real. Contrato `R51AjudaContratoTests`. Pendência: perfis `DIRETOR`/`RECEPCAO`/`PARCEIRO`.

### E4 · Contraste — FECHADO (R26) + revisitado (R49)

Antes: 192 linhas violadoras em 35 views. Depois: 214 trocas `text-muted`→`text-white-50` em
38 views (D-R26-1) + scanner de adjacência DOM permanente em **todas** as views. R49 revisitou e
**mediu** o contraste dos pares críticos (≥ 4,5:1 travado por fato) e corrigiu o azul de marca para
AA (`#1d6fe9`, 4,66:1).

### E5 · Formulários — FECHADO no escopo Adm360 (R30, R31)

Antes: 43 `maxlength` em 342 inputs, teto maior que a coluna era bug. Depois: régua **D-R30-1**
(HTML5 nativo; `maxlength` = tamanho exato da coluna via `information_schema`; `pattern` só formato
fechado; `min 0` monetário; servidor continua autoridade) aplicada em cadastros (11 pontos) e
operações (13 pontos em 11 views). Contratado nos dois lotes.

### E6 · Listas buscam por nome / números progressivos — FECHADO (R32, R33)

Antes: campos de UUID livres na tela, GUID truncado exibido. Depois: lookups nome×id nos fluxos de
orçamento (6 de 13 campos, raiz: arrays chegavam vazios) e lookups específicos por ação em
reserva/pedido usando **apenas endpoints existentes** (D-E6-1/D-E6-2; zero rota nova). KEeps auditados
(`ProdutoId` manual em Cotações etc.). Meta de número progressivo satisfeita por design (id nunca
renderizado como conteúdo). Pendência: inventário completo de fragmentos `[..8]` remanescentes.

### E0 · Design (navegação, estados, responsividade) — FECHADO (R43, R44, R45; passada crítica R46)

Antes: literal inglês "Command Center" fora do i18n; itens de menu sem permissão; ícones duplicados
(Coverage ×3, User ×4…); 4 telas de detalhe renderizando dump JSON cru; 3 tabelas fora do wrapper
responsivo. Depois: menus por resx com rótulo único e itens sob permissão (D-R43-1/2); detalhes em
campos rotulados com "Não informado" + contrato contra JSON cru; `.table-responsive` nas 3 falhas +
contrato "nenhuma tabela fora do wrapper" com keeps de impressão; deduplicação de ícones com
exceções declaradas (D-R46-1/2). Contratos R43/R44/R45/R46.

### E7 · Cadastro vs operação (identidade de telas) — não executado como epic autônomo

Registro honesto: o slot do roadmap (R34) foi ocupado na execução pela varredura final de E1 nos
resx, e nenhuma rodada executou o E7 como epic próprio (nem o inventário R25 o define). A
preocupação central dele (identidade de títulos/breadcrumbs) foi atendida **parcialmente e
pontualmente** dentro de outras rodadas: renome "Cadastros Mestre"→"Cadastros Organizacionais"
(h1+breadcrumb, R37), `Onboarding.PageTitle` unificado menu×página (D-R43-1, R43), subtítulos fiscais
limpos (R36). **Pendência**: auditoria sistemática de identidade cadastro×operação em roadmap futuro.

### E8 · Vestígios de homologação/demo — FECHADO no quick-win R25

15 pontos removidos/reescritos em 4 culturas com whitelist de domínio travada em fato (SEFAZ,
"Não Homologada", Operação Assistida, marketing, "demonstrativo financeiro"). Contrato com 5 fatos.

### E9 · SaaS (planos, onboarding, faturamento, inadimplência) — FECHADO (R40, R41, R47, R48)

Antes: limite de usuários do plano nunca checado na criação; primeira experiência sem orientar
implantação; fatura do cliente em tabela estática sem pagamento; vencimento sem alerta nem
notificação. Depois:

- **R40 — Planos e limites**: `POST api/seguranca/usuarios` aplica `PodeCadastrarUsuarioAsync`
  **antes do save** e registra uso; mapa de guards completo (médicos ✓ hospitais ✓ plantões ✓
  convites ✓ usuários ✗→✓); "no tenant permitido"→"no escopo de acesso".
- **R41 — Onboarding**: descrições canônicas em linguagem simples + banner de implantação no Meu Dia
  (best-effort, D-R41-1/2/3 — reutiliza endpoint existente, zero rota nova).
- **R47 — Faturamento/cobrança**: listagem unificada de faturas (UNION com campo `Origem`) +
  "Pagar agora" idempotente delegando ao motor de cobrança; smoke viva pegou e curou o bug latente
  B4 (cast `::timestamptz`) com assert permanente; fatura demo R$ 399,00 paga ponta a ponta, segundo
  "cobrar" devolve 409.
- **R48 — Alertas de inadimplência**: dono único `RegistrarVencimentoFaturaAsync` (evento
  `FATURA_VENCIDA` + alerta `INADIMPLENCIA`/`ALTA` + notificação exata para cada
  `ADMINISTRADOR_CLIENTE` ativo, mesmo transação); sweep identifica pré-flip (D-R48-1/2); mensagem
  em pt-BR simples (D-R48-3); alvo estrito (D-R48-4). Smoke: exatamente 1 notificação, sem duplicar
  (índice parcial confirma).

### Rodadas transversais (R42, R50, R51, R52)

- **R42** consolidou as regras E4/E5/E6/E8 num único ponto de entrada de scanners permanentes
  (invocando as classes canônicas, zero cópia de lógica).
- **R50** fixou o tom de voz e o glossário (seção E1).
- **R51** estendeu o E3 para a Ajuda central por área (seção E3).
- **R52** hardening: smoke e2e completa versionada (13 verificações, verde) + linha de base de
  performance (média 50 ms nas páginas padrão; HEAVY Orçamentos 17,2 MB e Estoque 8,75 MB).

## 5. Progressão cumulativa da suíte (fatos versionados)

| Rodada | Total | Rodada | Total |
|---|---|---|---|
| base R23 | 1529 | R37 | 1561 |
| R24 | 1533 | R38 | 1565 |
| R25 | 1538 | R39 | 1567 |
| R26 | 1540 | R40 | 1569 |
| R27 | 1542 | R41 | 1572 |
| R28 | 1544 | R42 | 1577 |
| R29 | 1546 | R43 | 1580 |
| R30 | 1548 | R44 | 1583 |
| R31 | 1550 | R45 | 1585 |
| R32 | 1552 | R46 | 1587 |
| R33 | 1554 | R47 | 1591 |
| R34 | 1556 | R48 | 1594 |
| R35 | 1557 | R49 | 1599 |
| R36 | 1559 | R50 | 1602 |
|  |  | R51 | 1605 |
|  |  | R52 | 1605 (estável, rodada docs/smoke) |

## 6. Auditoria de tom executada em R53 (nova, ainda sem contrato)

Contagem de exclamações em copy (script local desta rodada):

- **Views**: 31 `!` em strings literais, distribuídos em ~19 arquivos (maioria: Cadastros 2,
  Cirurgias 2, Orcamentos 2, Vales 2, Dashboard(Gestao) 3, Detalhes 2, Presencas 2,
  `_SuprimentosNav` 3, etc.).
- **Resx**: 4 valores de toast por cultura (total 16): "Vale expedido com sucesso!", "Vale
  reconciliado com sucesso!", "Orçamento aprovado com sucesso! …", "Vale valorizado com sucesso…"
  (pt-BR neutro + en/es/fr).

Inventário de pendência para passada de tom futura (decisão de produto sobre tom dos toasts de
sucesso). Nenhum item acima foi alterado nesta rodada (R53 é documentação).

## 7. Pendências finais consolidadas (pós-diretriz, para roadmap futuro)

1. **Performance de listas densas**: paginação/virtualização de Orçamentos (17,2 MB) e Estoque
   (8,75 MB) — reverter a linha de base R52 depois (R52).
2. **Decisão de produto "Dashboard"→"Painel"** junto da revisão de menus (R50).
3. **Passada de tom**: 31 exclamações em Views + 16 valores de toast resx (seção 6).
4. **Ajuda central**: artigos para `DIRETOR`/`RECEPCAO`/`PARCEIRO`; tour guiado consumindo os
   artigos (botão já existe) (R51).
5. **Acessibilidade avançada**: trap de foco em modais, `aria-live` em toasts, landmarks, `th scope`
   em tabelas densas, teclado em widgets custom (R49, D-R49-4).
6. **Inventário completo de GUIDs/fragmentos** remanescentes em UI (R33/R50, E6).
7. **Auditoria sistemática cadastro×operação** (o E7 não executado como epic autônomo).
8. **Experiência de login CPF/CNPJ/e-mail** como epic de produto (o contrato já aceita os três
   identificadores; a escolha guiada é futura) (R38).
9. **Smoke de escrita** (criar→aprovar→receber) quando existir seed demo idempotente em
   `plantaopro_test` (R52).
10. **Consolidação das famílias flake** (`R19DapperBindingGuardTests.Db_CentralClientes_…`, B5 P2/P3,
    R7O O1/O3) em snapshot/CTE única para suíte integral estável sob carga (documentado desde R29).
