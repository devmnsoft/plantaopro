# Rodada 19 — Relatório final consolidado (r19z)

- **Data:** 2026-10-18 · **Base de entrada:** `6fa7177` (`origin/main`, ponta da R18) · **Ponta local desta rodada:** commit deste arquivo · **Diretriz:** "continuar pelas próximas 10 sprints (R19→R28) e avançar também o design do sistema" — push ao fim de cada rodada segue autorização genérica registrada nesta linha.

## O que a rodada fechou

| Defeito | Severidade | Estado | Prova |
|---|---|---|---|
| **F7e** — 4 records ligados ao Dapper com parâmetro `DateTimeOffset` (`OcorrenciaDto`, `OcorrenciaEventoDto`, `WorkItemHistoryDto`, `ProductivityItemDto`); 500 em listagem/detalhe/histórico de ocorrências (inclusive **linha de produção viva**) e em histórico de work items | P2 | Corrigido (padrão de casa: `DateTime` + NOTE) | fatos de mecanismo versionado + 4 regressões de banco + smoke vivo r19b2 (3× 200 na linha que 500ava, BFF 200 no drawer) |
| **F7d** — `GET /api/clientes/central` 500 p/ todo superadmin, em duas etapas: (1) `unnest(plantaopro.modulos_efetivos(…))` rejeitado pelo PG18 (`função unnest(record) não existe`); (2) `sum()` de contagem é `numeric` vs `long` do record `ClienteCentralTotais` | P2 | Corrigido (cross join lateral canônico + `::bigint`) | fact e2e executando as statements exatas da fonte via `QueryMultipleAsync` contra o schema real + smoke vivo (página renderiza 20 linhas c/ 17 módulos e totais) |
| Item 15 da fila — guarda global SRF-em-WHERE | — | Instaurada | 4 guardas de fonte como fatos permanentes (∅ record-DTOFF×`Query*Async<T>`; zero `\bany(plantaopro.`; zero `unnest(plantaopro.<fn>(`; toda `modulos_efetivos(` em posição FROM) |
| Item 14 da fila — auditoria F7b | — | Concluída | censo 263 tipos bound / 189 props DTOFF classificados; mecanismo versionado (só record c/ parâmetro DTOFF falha; classe é isenta) |

Design (diretriz de avançar o design do sistema): **ADR-0001** (contrato de binding Dapper/Npgsql p/ datas — records com `DateTime` no construtor; `DateTimeOffset` restrito à fronteira JSON) e **ADR-0002** (SRFs só em posição FROM no PG18; padrões cross join lateral p/ agregação por tenant e rótulo por linha), ambos em `docs/arquitetura/adr/`, com índice expandido em `docs/arquitetura.md` (mapa de contratos + lista de ADRs). Padrão estabelecido: rodadas seguintes trazem 1–2 ADRs acoplados ao trabalho realizado.

## Gates finais

- Build `dotnet build --no-incremental -c Release`: **0 erros**; multiset de avisos **idêntico** ao pristino r18b4 (160 entradas, chave `arquivo|mensagem`, `Compare-Object` sem divergência).
- Suíte final: **1518/1518** (base 1508 + 9 fatos r19b1 + 1 fato e2e r19b1b). Flake conhecido 40P01 em `R7OBOnboardingPreferenciasIdiomaTests` (nova classe da família; isolate 5/5).
- Smoke vivo r19b2 concluído com cleanup **net-zero** (seeds `aaaa…{19,29,39,49,99}` removidos; contagens 0; linha de produção intacta) e apps parados.

## Commits da rodada

| Commit | Conteúdo |
|---|---|
| `d326b1f` | r19a0 auditoria inicial + plano de slices (achados F1–F4, decisões D1–D4) |
| `cfc6952` | r19b1 fixes F7e/F7d-etapa-1 + 9 fatos + ADRs 0001/0002 + índice `docs/arquitetura.md` |
| `86eed7c` | r19b1b F7d etapa 2 (`::bigint`) + fato e2e de statements completas + doc atualizado |
| (este) | r19b2 evidência do smoke vivo + r19z consolidado |

## Observações abertas (não bloqueiam; seguem na fila)

1. **Sessão portal ADMINISTRADOR_CLIENTE (gestor)**: login ok no servidor, mas o portal do perfil rebota p/ logo após (reproduzido fora do browser — não é ferramenta). Redirecionamento/autorização, não binding. Verificar na próxima rodada que tocar em sessão/portal.
2. **Fantasma legado**: redirect `Ocorrencias → Pendencias?module=OCORRENCIAS`, parâmetro inexistente na rota de destino (P3 — somar ao item 9, R22).

## Fila R20 (plano das 10 sprints)

1. **Item #1 — Extensão do scanner F-R15-A**: cobrir `Adm360*WebController*` (Web) + camada DTO-API × ViewModel-Web (mapeamento BFF). Entregável esperado: matriz de correspondência por controller/DTO, divergências classificadas (nome/shape/hydrate manual ausente) e fix cirúrgico do que o scanner apontar como defeito real (não cosmético).
2. **Design (acoplado)**: 1 ADR sobre o contrato de mapeamento BFF Web ↔ Api (nomenclatura DTO↔ViewModel, onde mora a tradução — anti-corrupção) antes/durante o scanner, estendendo o índice de `docs/arquitetura.md`.
3. Padrões de casa mantidos: commits `<secoes> (rodada 20 r20xx)` sem acentos, gate de casa antes de cada commit, evidência versionada em `docs/evidencias/<data>-rodada-20/`, push ao fim da rodada.
