# ADR-0002 — Posicionamento de funções set-returning no PostgreSQL 18

- **Status:** aceita (rodada 18 r18b3 + rodada 19 r19b1)
- **Contexto:** o app tem exatamente uma função set-returning: `plantaopro.modulos_efetivos(uuid)` → `TABLE(codigo text, modulo_id uuid, pacote_codigo text)` (verificar via `pg_proc where proretset`). Duas formas de chamá-la fora de posição FROM falham no PG18:
  - `... where codigo = any(plantaopro.modulos_efetivos(@t))` → `0A000` (rejeitada em parse — R18 F7c, `GET /api/modulos` 500);
  - `select unnest(plantaopro.modulos_efetivos(t.id)) as codigo ...` → `função unnest(record) não existe` (a SRF retorna linhas, não array — R19 F7d, `GET /api/clientes/central` 500).

## Decisão

1. SRFs só em **posição FROM**: item de FROM direto (`select codigo from plantaopro.modulos_efetivos(@t)`) ou via `cross join lateral` / `left join lateral` quando correlacionada.
2. Padrões canônicos já consolidados:
   - **contagem/agregação por tenant** (catalogo global): uma única passagem com `cross join lateral ... group by` (R18 F7c — ~180 ms p/ 1142 módulos × 5196 tenants, vs ~4 min 23 s da forma EXISTS);
   - **rótulo por linha** (uma linha do resultado consulta os módulos de um cliente): subconsulta escalar com `cross join lateral` dentro dela (R19 F7d — 8 ms p/ 88 clientes).
3. `EXISTS`/subquery escalar com SRF dentro de WHERE segue proibida pelo mesmo motivo do parse 0A000.

## Consequências

- Guardas versionadas em `R19DapperBindingGuardTests`: nenhum `\bany(plantaopro.`; nenhum `unnest(plantaopro.<fn>(`; toda ocorrência de `modulos_efetivos(` precedida de `from`/`join`/`lateral` (código de produção).
- Nova função set-returning só entra com teste de fato executando a forma canônica contra o schema real (padrão r18b4) — o parser do PG não dá aviso prévio, o 500 aparece em runtime.
