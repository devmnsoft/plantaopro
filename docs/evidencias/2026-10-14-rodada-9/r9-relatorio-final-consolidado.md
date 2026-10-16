# Rodada 9 — Relatório Final Consolidado (v2)

Data: 2026-10-15 (UTC) · Ambiente: local/homologação · Branch `main`, base inicial `4ab9770`, **HEAD local `159b2d8`** (commits da rodada + fechamento + fatias r9m, **sem push/merge/publish** por restrição da rodada). · Banco principal `plantaopro` + espelho `plantaopro_test` (PostgreSQL 18). · **Atualização 2026-10-16 (r9m):** fatias r9m-P2 (`a379780`) e r9m-P4 (`159b2d8`) aplicadas e testadas — pendências **P2 e P4 fechadas**; suíte efetiva **1398**; **P1 fechado** (capturas + aprovação visual declarada pelo usuário).

## 1. Diagnóstico (estado inicial)

- Gate de estabilização (Bloco A): build/suíte verde na baseline **1394 testes ×3**; banco disponível; autenticação BCrypt cost 11 validada; isolamento de tenant coberto pelo tenant sintético do seed 122.
- Auditoria inicial (`r9-auditoria-inicial.md`) classificou 11 domínios; **6 estavam fora de PRONTO**: 3.1 PARCIAL (produto/lotes sem UI explícita), 3.2 PARCIAL (sem precificação de venda), 3.3 AUSENTE (ANVISA), 3.4 PARCIAL (cotações sem ANVISA/lote por item), 3.7 QUEBRADO (contrato relatórios "Hospital" sobre `cliente_id`), 3.8 AUSENTE (DRE gerencial).
- Fila pendente herdada da R8 aberta ao fim da rodada: **completude i18n** (3 arquivos), **flake B14** (`ProvisionamentoB5Tests` não crash-idempotente) e **screenshots/aprovação visual**.
- Riscos de ambiente conhecidos: RAM baixa (deadlocks 40P01 transitórios em suíte paralela) e `pg_dump` 18 quebrado (catálogos por SQL direto).
- Nenhum CNPJ/endereço/e-mail/plano/preço/regra clínica foi inventado: dados de homologação são os dos seeds 122/123 e do fixture ADM360 documentado; credenciais só apresentadas após teste real de login (ver §5).

## 2. Execuções (fatias r9a → r9l)

| Fatia | Commit | Escopo | Evidência principal |
|---|---|---|---|
| r9a | `7a3f5ad` | Auditoria inicial + tenant SaaS Sistema Vertical (seed idempotente `123_`) com provas reais de login/abertura Adm360 | `r4-sistema-vertical-provas.md` |
| r9b | `8b83e10` | §5 tela de produtos com lotes vinculados (registro sem estoque automático) | `r5-produtos-lotes.md` |
| r9c | `d0fcbdc` | §6 cadastro regulatório ANVISA em produtos (texto c/ zeros à esquerda, tri-estável, manual, vigência regulatória) | `r6-produtos-anvisa.md` |
| r9d | `27e1d8d` | §7 precificação: `preco_venda` + margem/markup recalculados no servidor c/ estados ausente/não calculável/negativo | `r7-precificacao.md` |
| r9e | `684ab79` | §8 ANVISA em itens de cotação (seleção/linha/detalhe/comparativo) + snapshot por versão enviada | `r8-cotacoes-anvisa.md` |
| r9f | `9c347ff` | §9/§10 relatórios financeiros por CLIENTE (Hospital→Cliente corrigido) + vencimentos por parcela sem multiplicar totais | `r9-financeiro-cliente.md` |
| r9g | `6729b69` | §11 DRE gerencial por CLIENTE em competência + conciliação, origem e limitações | `r9-dre-gerencial.md` |
| r9h | `0dfda3e` | §12 design/UX integrado (ordem Filtros→Detalhamento→Origem, formulário 5 seções) + i18n pt/en/es/fr (37 chaves, contrato 283) + homologação visual ao vivo | `r9-design-i18n.md` + `r9h-sv-*.html` |
| r9i | `604437e` | §3.10 install/upgrade consolidado: install-manifest v2.21.9 (42 migrações v2312–v2353) + canonização de gap/convergência + `scrpt_completo.sql` regenerado; instalação limpa de ponta a ponta em banco scratch | `r9i-instalacao-consolidada.md` |
| r9j | `b02dca2` | i18n completude: 219 chaves novas (RelFin 108 / Produtos 99 / CotDet-ANVISA 12) ×4 idiomas; contrato 283→**502** + teste view-level | `r9j-i18n-completude.md` |
| r9k | `b302093` | B14 flake: `SementarAsync` crash-idempotente (pré-limpeza dos 7 CNPJs dos testes + varredura de solicitações órfãs); prova controlada antes/depois | `r9k-b14-flake-provisionamento.md` |
| r9l | `a4989da` | Captura visual: 3 telas verificadas ao vivo via DOM autenticado + artefatos `outerHTML`; diagnóstico de conexão (API/Npgsql 10); screenshots bloqueados por ambiente | `r9l-captura-visual-artefatos.md` + `artefatos-dom/` |
| r9m-P2 | `a379780` | B4 matriz flake: pré-limpeza por e-mail na semente (única chave natural c/ índice único parcial) + retry único 40P01/40001 em semente/purga de `SaasComercialB4MatrizTests`; classe isolada 12/12 | `r9m-p2-b4matriz-flake.md` |
| r9m-P4 | `159b2d8` | i18n completude de `Cotacoes/Detalhes.cshtml` fora do bloco ANVISA: 103 chaves novas ×4 idiomas (contrato Adm360 502→605) + fact novo de completude; suíte 1397→1398 | `r9m-p4-cotdet-i18n.md` |

## 3. Migrations

- Novos DDL da rodada: `2026_10_v2352_r9c_cadastro_anvisa_produtos.sql` e `2026_10_v2353_r9d_precificacao_preco_venda.sql` (incrementais, anexadas ao manifesto de upgrade; checksums conferem).
- r9e/r9f/r9g: **sem novo DDL** — snapshot ANVISA persistido nos dados da versão da cotação; relatórios/DRE reconstroem sobre tabelas existentes (compatibilidade com registros históricos vinculados a hospital preservada).
- Fechamento de instalação (r9i): upgrade manifesto verificado completo até **v2353**; `install-manifest.json` elevado a **v2.21.9** cobrindo as 42 migrações v2312–v2353 + 2 arquivos de canonização (`database/schema/450_v2219_tabelas_legado_gap_instalacao.sql`, `460_v2219_colunas_convergencia_instalacao.sql`); `scrpt_completo.sql` regenerado e validado (cobertura 100%).
- Caminhos validados de ponta a ponta: **instalação limpa** em banco scratch (128 fontes, `IDENTITY_SCHEMA_READY`) e **upgrade** sobre banco existente (manifesto completo).

## 4. Evidências

Todas em `docs/evidencias/2026-10-14-rodada-9/`: provas por slice (coluna acima), artefatos HTML/CSV/JSON das jornadas autenticadas do Sistema Vertical, seed de demo (`r9-seed-demo.sql`, `r9-seed-dre-demo.sql`), o registro de fechamentos da fila pendente (auditoria §6) e os artefatos DOM de r9l (`artefatos-dom/relfin.html`, `produtos.html`, `cotdet.html`).

## 5. Checks — passou / falhou / bloqueada / não executada

**PASSOU**
- Build Release `PlantaoPro.sln`: 0 erros em todas as fatias (warnings pré-existentes).
- Suíte completa Release: baseline 1394×3 no gate; **final 1397/1397 efetivos** em r9j e r9k (1 flake ambiental 40P01 em cada execução, método diferente a cada vez, retry isolado 12/12 nas duas — ver nota abaixo).
- **Suíte completa Release pós-r9m: 1398/1398 efetivos sem flake** (nova base = 1397 + 1 fact novo do r9m-P4).
- Contrato i18n: 502 chaves ×4 idiomas com paridade + âncoras de valor pt/en/es/fr + teste de ausência de literais nas views de r9j (elevado a **605 chaves** no r9m-P4, com fact novo de completude do detalhe da cotação).
- B14 crash-idempotência: experimento controlado (órfãos plantados → HEAD falha exatamente com `"CNPJ já cadastrado."`; correção passa e zera os órfãos); classe isolada 11/11.
- Instalação limpa scratch + upgrade de manifesto (r9i); `scrpt_completo.sql` cobertura 100%.
- Credencial do admin SV testada ao vivo: `BCrypt.Verify("Sv-Homolog-2026!", hash)=True` (DLL do próprio build) e `POST /api/auth/login` → success, tenant `8b0c8e74-a81b-4ea2-b499-94755a1ca301`.
- Verificação funcional das 3 telas de r9j autenticado no tenant SV (títulos/âncoras pt-BR, coluna ANVISA e valores ao vivo, dados reais, sem overflow horizontal em 1000×700) — r9l.

**FALHOU (e foi corrigido na própria rodada)**
- Contrato Hospital→Cliente nos relatórios financeiros (3.7 QUEBRADO na auditoria) → corrigido em r9f.
- Cascata "CNPJ já cadastrado" pós-derrubada em `ProvisionamentoB5Tests` (R8-B14) → reproduzida em controle (git stash) e eliminada em r9k.

**BLOQUEADA (ambiente)**
- (histórico) `browser.screenshot` dependia da janela desktop do OpenCode estar visível (~10 tentativas frustradas) — **superado em 2026-10-16 (r9m)**: 3 viewport PNGs capturados em `imagens/` com a janela visível fornecida pelo usuário; lições de protocolo documentadas em `r9l-captura-visual-artefatos.md` §4.

**NÃO EXECUTADA / POR CONSTRUÇÃO**
- Zoom 200% empírico e breakpoints 360/768/1440: a webview do harness não expõe zoom de navegador (medido inalterado) nem device-metrics; verificados por construção (layout fluido, sem largura fixa no conteúdo).
- **Aprovação visual**: não declarada (requer pixels + aceite do usuário).

Nota de ambiente: deadlocks 40P01 transitórios sob RAM baixa (suíte paralela) ocorreram 2× nesta rodada em `SaasComercialB4MatrizTests` (métodos diferentes; 12/12 no retry isolado em ambos os casos). Fator ambiental documentado, não bloqueador; o candidato registrado em r9k §6 foi **fechado em r9m-P2** (pré-limpeza por e-mail + retry único 40P01/40001), e a suíte de fechamento executou sem flake.

## 6. Pendências priorizadas

1. **P1 — FECHADO em 2026-10-16 (r9m)** — 3 viewport PNGs em `imagens/` (CotDet com o título dinâmico do P4 no pixel, RelFin, Produtos), autenticado no tenant SV, e **aprovação visual declarada pelo usuário** ("Aprovado — fechar"). Evidência: `r9l-captura-visual-artefatos.md` (§4 capturas + lições de protocolo; §6 aprovação).
2. **P2 — FECHADO em r9m-P2 (2026-10-16, `a379780`)** — receita aplicada a `SaasComercialB4MatrizTests`: pré-limpeza por e-mail (única chave natural com índice único parcial entre os fixtures) + retry único em 40P01/40001; classe isolada 12/12; suíte de fechamento 1398/1398 sem flake. Demais classes SaaS com fixtures por chave natural: mesma receita sob demanda. Evidência: `r9m-p2-b4matriz-flake.md`.
3. **P3** — Dívidas herdadas R8: B16/B17/B18; drill-down do §27-L208; writer da tabela `adm360_ocorrencias`.
4. **P4 — FECHADO em r9m-P4 (2026-10-16, `159b2d8`)** — 103 chaves novas ×4 idiomas (contrato Adm360 502→605); 102 substituições exatas na view (guia/status/itens/modal/anexos/outbox/estorno/IA; bloco ANVISA inalterado); fact novo de completude; residuais com acento = apenas comentários não renderizados. Evidência: `r9m-p4-cotdet-i18n.md`.
5. **P5** — `pg_dump` 18 quebrado neste host: manter catálogos por SQL direto até resolver (afeta apenas tooling de snapshot).

## 7. Estado de retomada

- Git: `main` local em **`159b2d8`** (r9m-P4) + commit de documentação (r9m) + commit de evidências das capturas (r9m-P1) + commit deste fechamento (aprovação), à frente de `origin/main` em **18 commits** (restrição da rodada impede push/merge). Árvore de trabalho limpa após estes commits.
- Servidores: **parados** após a suíte de fechamento (DLLs desbloqueadas para build). Para o retry visual, relançar via `r9l_api.cmd` / `r9l_web.cmd` em `%TEMP%\opencode` (`ConnectionStrings__Default` explícito KEY=VALUE para `plantaopro` — Npgsql 10 rejeita URI); o restart do Web invalida a sessão (Data Protection key efêmera) → re-login via form em qualquer aba (admin SV, `docs/usuarios-teste.md`); o primeiro submit logo após o boot da API pode falhar em silêncio — retry resolve.
- Baseline de suíte para a próxima rodada: **1398** verdes (Release) = 1397 + 1 fact novo do r9m-P4; contrato i18n Adm360 em **605 chaves ×4**.
- Próximos passos ordenados: (1) push dos 18 commits locais quando autorizado; (2) próxima rodada: dívidas herdadas R8 (P3) — B16/B17/B18, drill-down do §27-L208, writer da `adm360_ocorrencias`.
