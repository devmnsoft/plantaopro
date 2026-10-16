# R10-B4 — Decisões formais dos residuais **B16** (especialidade clínica text × id) e **B18** (tiles KPI manuais × parciais do design system)

- **Rodada:** 10 — fatia 4 de 4 (plano em `r10a0-auditoria-inicial-plano-sprint.md`, §1 D4/D5)
- **Data:** 2026-10-16
- **Status:** DECISÕES REGISTRADAS (aceite = decisão versionada em doc + painel; contratos-guarda inalterados e verdes).
- **Natureza da fatia:** só documento + painel. **Zero mudança de código** nesta fatia — ambas as dívidas já terminaram a R8 com validação por fonte canônica e contrato anti-drift; o que restava era a decisão formal que as próprias fatias R8 deixaram registrada como "fora do rito".

## B16 / D4 — Especialidade clínica: texto livre vs id

### Validação que está sendo decidida (fonte: `r8a0…matriz-estado.md` L234 + `R8B16EspecialidadeClinicaContratoTests` 3/3)

- A clínica escreve por **texto livre** `agendamentos.especialidade`: `Saude360ClinicalService` popula só o texto e NUNCA o id (o serviço inteiro não referencia `especialidade_id`).
- A exibição usa `coalesce(a.especialidade, e.nome, …)` — texto primário, nome-por-id como fallback defensivo.
- `agendamentos.especialidade_id` e `consultas.especialidade_id` são **dormentes (forward-schema)** — DDL-only, zero escrita em produção.
- A representação **por id** é canônica **apenas** no domínio plantões/médicos (`plantoes.especialidade_id`, `medicos`, `medico_especialidades`) — domínio distinto, sem colisão.

### Decisão

**Manter o texto livre como representação operacional da especialidade clínica.** A normalização texto→id fica registrada como **decisão de modelo de dados a ser feita em rodada própria**, quando houver pedido explícito (p. ex., relatórios transversais por especialidade padrão entre tenants).

| Opção | Avaliação | Decisão |
|---|---|---|
| (a) Normalizar agora (backfill nome→id + picker no formulário + backfill `agendamentos.especialidade_id`/`consultas.especialidade_id` + deprecar texto) | Escopo de migração com cutover, período dual-read/dual-write e validação de unicidade nome→id — rodada própria | rejeitada nesta rodada |
| (b) Híbrido (começar a gravar id junto do texto sem deprecar o texto) | Meio-termo que tornaria `especialidade_id` semi-canônico e criaria exatamente o drift que R8-B16 documentou como risco | rejeitada |
| (c) Manter status quo + guarda contratual | Custo zero; mudança futura é forçada a passar por decisão consciente | **adotada** |

### Consequências e gatilhos para rever

- O contrato `R8B16EspecialidadeClinicaContratoTests` segue **inalterado e verde**: qualquer escrita clínica em `especialidade_id`, ou mudança do fallback de exibição, faz o contrato falhar → decisão consciente obrigatória antes do merge.
- Coluna órfã `consultas.especialidade_id` **mantida** (forward-schema; remover coluna de produção tem custo de migração sem benefício hoje).
- Gatilhos para abrir a rodada de normalização: (i) demanda de relatório/filtro por especialidade canônica entre tenants; (ii) formulário de agendamento ganhar picker de especialidade; (iii) qualquer feature que precise de join clínico↔`especialidades`.

## B18 / D5 — Tiles KPI: unificação ou divergência

### Validação que está sendo decidida (fonte: `r8a0…matriz-estado.md` L236 + `R8B18KpiCardsParciaisContrat*Tests` 3/3)

- `_KpiCard` é o componente **estabelecido** de KPI server-side (consumido por ~7 views: Coordenacao, Marketplace, WhiteLabel×2, Permissoes×2, Inteligencia); `_CoverageCard` vive em CentralEscala. O snapshot antigo ("usado só em Inteligencia") estava desatualizado.
- Os 4 locais apontados na matriz (Bi/Operações hidratados por JS; MeuDia `day-kpis`; CommandCenter `cc-kpis`) **NÃO são duplicatas byte-idênticas** — são 4 renderizações distintas, nenhuma via `_KpiCard`. A redundância é **conceitual**, não cópia.
- O swap byte-equivalente à la B12 não se aplica aqui.

### Decisão

**Adiar a unificação para um refator de design em rodada futura**, com definição de alvo visual + paridade de comportamento JS prévia. Nenhum tile é tocado agora.

| Opção | Avaliação | Decisão |
|---|---|---|
| (a) Swap byte-equivalente estilo B12 | Não se aplica: renderizações distintas (partial server-side vs hidratação JS), nenhum par é cópia exata | rejeitada |
| (b) Unificar agora exigindo paridade de comportamento JS | Requer alvo visual definido + estados/tendências/links equivalentes (inclusive os drill-downs r10b3 entregues nesta rodada) — risco de regressão visual em 4 superfícies sem regra de produto por trás | adiada |
| (c) Adiar + guarda contratual | Mantém o anti-drift; a unificação futura parte de um spec de design próprio | **adotada** |

### Consequências e gatilhos para rever

- O contrato `R8B18KpiCardsParciaisContratoTests` segue **inalterado e verde** como guarda anti-drift.
- Nota de interface futura: os drill-downs adicionados pelo r10b3 (links "Ver registros" nos tiles de Bi/Premium) precisam entrar no **API do tile futuro** (p. ex., action-link opcional de `_KpiCard`) para a unificação não perder a navegação — registrado como requisito do spec de design.
- Gatilhos para abrir o refator: (i) design definir o alvo visual do tile KPI; (ii) segunda rodada com escopo de design system; (iii) nova superfície de KPI que force escolha entre os 5 padrões existentes.

## Painel de dívidas (estado pós-roda-10)

| Dívida R8 herdada | Estado após Rodada 10 | Fechado em |
|---|---|---|
| B16 — especialidade text × id | **DECIDIDO: manter texto livre; normalização = rodada própria** (guarda ativa) | este doc + plano §3 |
| B17 — facade Meu Dia | **CONSOLIDADO** (fachada removida, topologia única) | `r10b2-….md` |
| B18 — tiles KPI | **DECIDIDO: unificação = refator de design futuro** (guarda ativa) | este doc + plano §3 |
| §27-L208 — drill-down Bi/Premium | **ESTENDIDO onde há alvo real** (7 links honestos; 3 sem link documentado) | `r10b3-….md` |
| A12 — writer `adm360_ocorrencias` | **IMPLEMENTADO + TESTADO** | `r10b1-….md` |

## Evidência executada

- Build sln Release: `BUILD_EXIT:0` (0 erros).
- Guardas isoladas: filtro `R8B16|R8B18` = **6/6 aprovados** (`R8B16EspecialidadeClinicaContratoTests` 3/3 + `R8B18KpiCardsParciaisContratoTests` 3/3 — inalterados).
- Suíte full Release: **`Total de testes: 1421`**, **TEST_EXIT:0 — 1421/1421 limpo** (slice só-doc: código idêntico ao pós-r10b3; a execução confirma o estado de partida do relatório final).
