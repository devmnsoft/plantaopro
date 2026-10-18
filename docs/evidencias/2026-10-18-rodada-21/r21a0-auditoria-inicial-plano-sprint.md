# Rodada 21 — Auditoria inicial e plano (r21a0)

- **Data:** 2026-10-18 · **Base:** `64287c3` (r20z, ponta de `origin/main` após o push da R20 — 8 commits: 5×R19 + r20a0/r20b1/r20z) · **Escopo:** fila r20z — **whitelist JS-driven das 6 ações** (item 2 herdado da fila R17/R18/R19): classificar com evidência de fonte cada entrada de `AcoesSemFormEstatico` no scanner `R16ScanFormBindingFonteTests` (JS-driven legítima × fantasma) e tornar a whitelist **auto-verificada** por fatos permanentes. Fora de escopo e seguidos na fila R22: fantasmas residuais P3 (os 4 classificados nesta rodada + fantasma legado `Pendencias?module=OCORRENCIAS`), quirk de sessão portal gestor (observação aberta 1 da R19).

## Achados

### F-R21-A — Entradas JS-driven (3): call-site real verificado na fonte; mantêm no catálogo

| Ação | Call-site (evidência) | Mecanismo |
|---|---|---|
| `AtivarCargo` | `Views/Administrativo360/Cargos.cshtml` L256 | `formImp.action = inativar ? '/Administrativo360/InativarCargo' : '/Administrativo360/AtivarCargo';` — o modal de impacto reusa o **mesmo `<form>`** (mesmos campos) do `InativarCargo`, que é estático e já pareado pelo scan principal; só a rota muda em tempo de execução |
| `AtivarDepartamento` | `Views/Administrativo360/Departamentos.cshtml` L231 | idêntico: `formImp.action = inativar ? '.../InativarDepartamento' : '.../AtivarDepartamento';` |
| `AnaliseIa` | `Views/Administrativo360/Cotacoes/Detalhes.cshtml` L572 | painel IA: `data-ai-panel data-ai-url="/Administrativo360/Cotacoes/@Model.Cotacao.Id/AnaliseIa" data-ai-method="POST"` — POST via fetch, nunca existe `<form>` (já catalogado como JS-driven em R20) |

Classe de risco = a do form estático (parâmetros trafegam pelo mesmo HTML form), mas **fora da cobertura do scanner** ⇒ exigem auto-verificação (F-R21-C).

### F-R21-B — Entradas fantasma (4): zero chamadores no Web, porém cadeia BFF→API completa (gap funcional)

| Ação | Definição Web (BFF) | Rota Api invocada | Rota Api |
|---|---|---|---|
| `OrcamentoRejeitar` | `Administrativo360Controller.Orcamentos.cs` L225 | `POST api/administrativo360/orcamentos/{id}/rejeitar` | `Adm360OrcamentosController.cs` L82 ✓ viva |
| `OrcamentoCancelar` | ibid. L248 | `POST .../orcamentos/{id}/cancelar` | ibid. L91 ✓ viva |
| `OrcamentoCancelarReserva` | ibid. L319 | `DELETE .../orcamentos/{id}/reservas/{reservaId}?motivo=` | ibid. L126 ✓ viva |
| `ValeCancelar` | `Administrativo360Controller.Vales.cs` L252 | `POST api/administrativo360/vales/{id}/cancelar` | `Adm360ValesController.cs` L143 ✓ viva |

**Prova de zero chamadores:** busca case-sensitive dos 4 nomes em todo `PlantaoPro.Web` retorna apenas (a) as próprias definições nos controllers e (b) as entradas da whitelist no teste — nenhum `<form asp-action>`, nenhum literal de URL em `.cshtml`/`.js`, nenhum dispatch genérico (`name="acao"`/`fetch(`) nas views Adm360. Keys resx das mensagens existem (`Adm360.Orcamentos.OkRejected/FallbackReject` etc.).

Classificação: **não é código morto — é operação de negócio implementada de ponta a ponta e inalcançável pela UI** (o gestor não tem botão para rejeitar orçamento, cancelar orçamento, cancelar reserva de material nem cancelar vale). Severidade P3 (nenhum erro visível; ausência de função). Superfície de ataque residual: rotas `[Authorize] + [ValidateAntiForgeryToken]` (POST direto exige token antiforgery de página da mesma sessão) — baixo risco.

### F-R21-C — A whitelist era de confiança (só comentário)

`AcoesSemFormEstatico` acumula entradas desde R16 sem evidência de fonte por entrada e sem antivacuidade em nenhum dos sentidos: (i) nada verifica que entrada JS-driven **ainda tenha** call-site dinâmico; (ii) nada detecta entrada que deixou de ser ação HttpPost com escalares implícitos (row obsoleta). Adicionar nome novo falha o teste por design, mas a **classificação** de cada nome nunca foi versionada.

## Decisões

- **D1 — Whitelist dividida em dois sets versionados + fato permanente auto-verificador** (slice r21b1, em `R16ScanFormBindingFonteTests`):
  - `AcoesJsDrivenSemFormEstatico` (3): fato exige ≥1 literal de URL dinâmico em quotes contendo `/Administrativo360/[segmentos/]\<acao\>` em alguma view (regex tolera segmentos intermediários, ex.: `Cotacoes/@Model.Cotacao.Id/AnaliseIa`); piso ≥3.
  - `AcoesFantasmaSemChamador` (4): fato exige **zero ocorrências** (case-sensitive) do nome em qualquer `.cshtml` de `Views/**`; piso ≥4. Se R22 ligar o form/botão, o fato falha e força a remoção do catálogo (o scan principal assume o pareamento então).
  - Antivacuidade compartilhada: toda entrada da whitelist (união) deve continuar sendo extraída pelo scanner principal como ação HttpPost com escalares implícitos — zero row obsoleta (o scan principal já garante unpaired∖whitelist=∅; o novo fato garante whitelist⊆extraídas).
- **D2 — Os 4 fantasmas entram na fila R22 com insumo de decisão**: a cadeia BFF→API está completa (rota viva + resx + BFF completo); o caminho mais barato é **ligar os botões** nas views de detalhe (`OrcamentoDetalhes`/`OrcamentoReserva`/`ValeDetalhes`) e recuperar as funções, não remover a cadeia. Remover as rotas só se R22 decidir que o fluxo é obsoleto.
- **D3 — Escopo fechado**: apenas as 7 entradas existentes são classificadas; nenhuma superfície nova entra ou sai do scan principal (invariante R20: 79 pareamentos / 203 parâmetros / 76 ações, pisos intactos).

## Plano de slices

| Slice | Conteúdo | Gate / evidência |
|---|---|---|
| r21a0 (este doc) | auditoria + classificação das 7 entradas (F-R21-A/B/C), decisões D1–D3 | este arquivo |
| r21b1 | D1: dois sets + fato `whitelist_acoes_sem_form_estatico_e_auto_verificada` + comentários com evidência file:line por entrada | gate build `--no-incremental` 0E/multiset idêntico ao pristino; suíte verde |
| r21z | relatório consolidado + push | gates finais; push com credencial wincred (fix R20) |
