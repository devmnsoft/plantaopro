# Rodada 11 — r11a0: Auditoria inicial / plano da sprint (P1 herdado da R10: bloqueio imediato de saldo do A12)

- **Data:** 2026-10-16 · **Base:** `1b87cb9` (ponta exata do push da Rodada 10; `origin/main` == HEAD) · Ambiente local/homologação · banco principal `plantaopro` + teste `plantaopro_test` (PG18).
- **Restrição:** commits locais por slice (`main`); **push/merge/publish pendentes de autorização ao final da rodada**.
- **Escopo** (herdado de `r10z-relatorio-final-consolidado.md` — dívidas ordenadas): **P1 — bloqueio imediato de saldo (A12)** como funcionalidade principal. P2 homologação webview entra como janela se houver disponibilidade. P2 gaps de drill-down, P3s e P5 seguem para rodadas futuras (fora do escopo desta).

## 0. Baseline executada (gate de partida)

- **Build sln completa (Release):** EXIT 0, 0 erros (80 avisos pré-existentes — mesmos da R10).
- **Full suite (Release, `--no-build`):** `Total de testes: 1421`, `Aprovados: 1420`, `Com falha: 1` — a única falha foi `R7OBOnboardingPreferenciasIdiomaTests.O3_PadraoDaOrganizacao_TambemConclui_NomeandoOIdioma_E_ReverteAoLimpar` com **`Npgsql 40P01: impasse detectado`** em `PurgarAsync()` (L256) — **classe do catálogo ① de flakes conhecidos** (mesma classe/assinatura já registrada em r8b10-full2 `O5`, r8b22-full3 `O2`, r8b23-pipeline2-full3 `O1`, r8b25-full1 `O2`: deadlock em teardown sob concorrência, método diferente a cada run, erro de banco não de assert). Reexecução isolada: **5× 5/5 aprovados, EXIT 0** (`r11_r7ob_isolate.bat`) — não-regressão. Nenhum teste Adm360/i18n/da R10 falhou.

## 1. Matriz da dívida (estado COM evidência)

### D1 — P1: **Bloqueio imediato de saldo ao abrir ocorrência** (spec A12 — linha que ficou fora do escopo r10b1)

| | |
|---|---|
| **Definição (spec)** | `PlantaoPro_Administrativo360_MVP_Requisitos_Prompts.md` **A12 L112**: "Aberta → em tratamento → resolvida → encerrada. **Bloqueio imediato autorizado pode abranger saldo interno e externo.** Encerrar exige destino dos itens e justificativa. Registro de ocorrência não apaga estoque." + **regra global L296**: "**Lote bloqueado/vencido não pode ser reservado ou expedido.** Mostre FEFO, quantidade disponível, validade e **motivo do bloqueio**. Não edite saldo diretamente." |
| **Gap (evidência)** | O mecanismo de bloqueio **existe no modelo desde v2191 mas NUNCA foi escrito em produção**: (a) `adm360_movimentos.condicao` aceita `'BLOQUEADO'` (DDL); (b) a view **`adm360_saldos`** deriva `fisico/reservado/disponivel` por (produto, lote, local, condicao) e `disponivel` só conta `condicao='LIBERADO'` (e lote não vencido) → quantidade BLOQUEADO fica **automatically fora** do saldo disponível; (c) os consumidores já validam contra `disponivel` (`EstoqueRepository.ReservarAsync` L177–184 "Quantidade solicitada excede o estoque disponível" e transferência L86–93) → **"não pode ser reservado" já funciona por construção** se houver writer de BLOQUEADO; (d) varredura do backend: zero writer de `condicao='BLOQUEADO'` em produção (os hits de "BLOQUEADO" são de outros domínios: usuários, SaaS, onboarding, pré-emissões fiscais). O writer A12 do r10b1 abriu o ciclo de estado com **zero toque em `adm360_movimentos`** por delimitação do plano (invariante "não apaga estoque" coberta por teste). |
| **Anatomia do saldo (fonte canônica)** | `adm360_locais.tipo ∈ ('INTERNO','EXTERNO')` → "saldo interno/externo" do spec = saldos por tipo de local. Saldo derivado (view), nunca editado ("Não edite saldo diretamente") — toda alteração é movimento com `idempotency_key` único `(tenant_id,idempotency_key)` (tabela master `adm360_operacoes` + pernas, padrão `EstoqueRepository`). |
| **Plano (slice `r11b1`)** | Ver §2 (decisões fixadas). Em resumo: abrir ⇒ movimento(s) `BLOQUEADO` na mesma transação da ocorrência; resolver ⇒ reversão `LIBERADO`; tratar/encerrar sem efeito em saldo; idempotency determinístico por (ocorrência, local); motivo cita a ocorrência (regra L296). Sem DDL (colunas existentes cobrem). Testes reescrevem o invariante do r10b1 para o novo comportamento + fatos novos (bloqueio multi-local, reserva recusada, liberação no Resolver, isolamento por tenant). |
| **Aceite** | Suíte full verde; bloqueio efetivo provado por teste (reserva/expedição recusada durante ABERTA/EM_TRATAMENTO e liberada após RESOLVIDA); motivo do bloqueio consultável nos movimentos; invariante "não apaga estoque" preservada (soma física por lote×local invariante no ciclo completo). |

## 2. Decisões de design fixadas (com opções consideradas) — para o slice r11b1

1. **Momento:** bloqueio no **ABRIR** (situacao ABERTA) — "bloqueio imediato"; o lote permanece bloqueado durante EM_TRATAMENTO.
2. **Alcance (internos × externos):** bloquear até `quantidade` a partir do `disponivel` (condicao LIBERADO) do **lote em TODOS os locais ativos** (INTERNO e EXTERNO) — é o que a palavra "pode abranger saldo interno e externo" autoriza. Ordem determinística: **primeiro o local da ocorrência** (onde a não conformidade foi detectada), depois os demais locais **ordenados por código de local** (zero invenção de prioridade de negócio além da ordem canônica de cadastro). Se `disponivel < quantidade`, bloqueia o que houver (bloqueio parcial) — a abertura não é rejeitada por saldo insuficiente (não conformidade documental pode exceder o saldo físico; a diferença fica visível na soma dos movimentos). Opção (apenas o local da ocorrência) rejeitada: restrição abaixo do alcance que o spec autoriza; opção (exigir saldo ≥ quantidade) rejeitada: inventa rejeição que o spec não manda.
3. **Liberação:** **RESOLVER** inverte o bloqueio (movimentos `LIBERADO` do mesmo lote×local, espelhando as quantidades bloqueadas) — "resolvida" = não conformidade tratada e itens de volta à circulação. **TRATAR e ENCERRAR não mexem em saldo** (encerrar = ato administrativo com destino+justificativa, conforme r10b1). Consequência direta: máquina estrita ABERTA→EM_TRATAMENTO→RESOLVIDA→ENCERRADA ⇒ todo ciclo completo termina com saldo zerado de efeitos → "Registro de ocorrência não apaga estoque" passa a valer também para o caminho COM bloqueio. Se a ocorrência for resolvida sem chegar a resolver o bloqueio (ex.: destino = descarte/adiante) — fora da máquina estrita — fica registrado como pendência P1b: comando futuro de baixa/devolução dirigido por `destino`.
4. **Identidade/auditoria:** movimentos com `origem_tipo='OCORRENCIA'`, `origem_id=<ocorrencia_id>`, `motivo` = "Ocorrência {tipo} — {descricao} (ocorrência {id})" (L296 exige motivo consultável); `idempotency_key` determinístico `{ocorrencia_id}:BLQ:{local_code}` no bloqueio e `…:DEL:{local_code}` na liberação (a tabela exige unique tenant+key).
5. **Trancas concorrentes:** mesmo padrão da casa — `BloquearChavesDeterministasAsync` por chave `tenant:produto:lote:local` (uma por local tocado, ordenada) + `ValidarBloqueioInventarioAsync` (inventário aberto bloqueia) — idêntico a `ReservarAsync`.
6. **Web/API:** rotas invariantes (as 4 do r10b1); a lista de ocorrências e o saldo (tela existente) passam a refletir o bloqueio por derivação — **sem novo endpoint** nesta fatia (se o saldo da view Adm360 não expuser motivo por origem, registrar como pendência visual, não inventar tela).
7. **Testes:** atualizar o fato do r10b1 "estoque invariante" para: soma física por (lote, local) invariante no ciclo completo E saldo LIBERADO restaurado após RESOLVER; fatos novos: abrir cria BLOQUEADO com origem/motivo corretos; bloqueio parcial quando saldo < quantidade; reserva recusada enquanto ABERTA/EM_TRATAMENTO; RESOLVER libera e reserva volta a ser possível; TRATAR/ENCERRAR sem efeito em saldo; ciclo completo net-zero; isolamento por tenant nos movimentos.

## 3. Fila ordenada da sprint

1. **`r11b1` — D1 bloqueio imediato A12** (única mudança funcional desta rodada; escopo fechado pelas decisões §2).
2. **`r11b2` — Janela de homologação (se disponível):** webview CRUD de Ocorrências (R10) + drill-downs (R10) + bloqueio A12 (esta fatia) — evidência visual só com janela.
3. **`r11z` — Relatório final consolidado** da rodada + painel/dívidas atualizado (gaps de drill-down P2, UTC, tiles KPI, normalização de especialidade, P5 pg_dump seguem abertos).

Cada slice termina com: evidência executada (testes isolados + suíte full), doc `r11bx-*.md` nesta pasta e linha no §4. Push somente quando autorizado.

## 4. Registro de fechamentos da fila (atualizado por slice)

| Slice | Escopo | Resultado | Evidência |
|---|---|---|---|
| `r11b1` | D1 P1 — bloqueio imediato de saldo (A12 L112/L296) | FECHADO — abrir gera pernas `BLOQUEIO_OCORRENCIA` (LIBERADO→BLOQUEADO, locais internos e externos, local da ocorrência primeiro, bloqueio parcial sem rejeitar); resolver inverte (`LIBERACAO_OCORRENCIA`); tratar/encerrar sem pernas; reserva fail-closed sobre saldo bloqueado; motivo/origem consultáveis; sem DDL/API/Web; invariante "não apaga estoque" agora vale com bloqueio (ciclo net-zero); **24/24** da classe A12; **full 1428/1428 limpo** | `r11b1-bloqueio-imediato-a12.md` |
| `r11b2` | Homologação webview — P1 ao vivo + drill-downs | FECHADO c/ achado — CRUD Ocorrências completo no tenant SV (ABERTA→TRATAMENTO→RESOLVIDA→ENCERRADA); bloqueio imediato cobriu saldo INTERNO+EXTERNO (DEP 6 + EXT 2) e o ciclo terminou net-zero; Estoque exibe/filtra BLOQUEADO; **ACHADO P2 reprodutível**: `/Bi` e `/DashboardsPremium` → `CATALOGO_NAO_CONFIGURADO` para todo autenticado (falta entrada no catálogo `SaasRouteGuardFilter.ControllerModules`; telas sem link de menu) — bloqueia o clique nos 7 drill-downs; evidência DOM versionada (4 artefatos); capturas de pixel pendentes de janela em 1º plano | `r11b2-homologacao-bloqueio-a12-drilldowns.md` |

## 5. Riscos e gotchas da rodada

- **Flakes ①:** `ProvisionamentoB5Tests` e `R7OBOnboardingPreferenciasIdiomaTests` (deadlock 40P01 em teardown sob RAM baixa; método diferente a cada run; receita: isolado 5× EXIT 0 = não-regressão).
- **Dapper:** POCO com setters para materialização (records com parâmetros nulos quebram a assinatura) — padrão r10b1.
- **Mudança de comportamento de API:** `AbrirAsync` passa a escrever movimentos — qualquer teste que assuma "zero movimentos" após abrir (além do fato do r10b1, já mapeado) precisa ser revisado; varredura no momento da implementação.
- **Idempotency dos movimentos:** unique `(tenant_id, idempotency_key)` — chaves devem incorporar o id da ocorrência (gerado no INSERT) + local.
- **Build lock:** servidores Api/Web parados durante builds Release.
