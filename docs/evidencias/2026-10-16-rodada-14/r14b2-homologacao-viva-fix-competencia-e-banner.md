# R14-B2 — Homologação viva do fix do 42P08 em faturas SaaS + achado e correção do banner que anunciava "Sucesso" como falha

Data: 2026-10-16/17 (homologação viva, servidores locais)
Slice: `r14b2` da rodada 14 — homologação viva do fix de `r14b1` contra o banco dev (`plantaopro`) com ciclo de grants reversíveis `HOMOLOG_R14`, net-zero ao final.
Estado desta slice: **FECHADO** — fix homologado ao vivo nos dois endpoints (antes sempre-500), segundo achado de segunda ordem descoberto na homologação foi corrigido e coberto por contrato, gate **1456/1456 limpo**, banco de volta ao estado exato pré-grant.

## 1. Objeto

- Homologar ao vivo o fix de `r14b1` (`81ebb7a`): cast duplo `(@competencia::date is null or f.competencia=@competencia::date)` nas duas ocorrências do filtro de competência de `plantaopro.faturas_saas` (listagem comercial `GET /api/faturamento-saas/faturas` e relatório SaaS `GET /api/relatorios/saas/faturamento`), + banners de degradação honesta nas duas views.
- Abrir o módulo `FATURAMENTO_SAAS` para o admin SV apenas durante a janela de captura (grants `HOMOLOG_R14`), com re-login obrigatório antes/depois (claims materializam no login) e rollback exato ao final (net-zero do banco).

## 2. Ambiente

| Item | Valor |
|---|---|
| Api | `http://localhost:51976` (bin Release de `81ebb7a`, e depois rebuild pós-correção b2) |
| Web | `http://localhost:52976` (mesmo bin; `ASPNETCORE_ENVIRONMENT=Development`) |
| Banco dev | `plantaopro` (dados reais: 4 faturas ativas dos fixtures B6) |
| Usuário | `admin.sistemavertical@plantaopro.local` (perfil SV `ADMINISTRADOR`, escopo TENANT) |
| Tenant SV | `8b0c8e74-a81b-4ea2-b499-94755a1ca301` |
| Perfil | `0941eb60-286e-5b6d-8e3c-47347a4a5bca` |

## 3. Grants HOMOLOG_R14 (reversíveis)

Snapshot pré-grant exato (capturado via `r14b2_pre_grant.sql/.bat`): efetivos = 18 módulos (com `RELATORIOS`, sem `FATURAMENTO_SAAS`); linhas `tenant_modulos` com `origem='HOMOLOG_R14'` = 0; `FATURAMENTO_SAAS.VER` do perfil SV = `reg_status='I'`, `permitido=true`.

Aplicados (`r14b2_grants_sv.sql/.bat`, D4): INSERT `tenant_modulos` (FATURAMENTO_SAAS, origem HOMOLOG_R14) + UPDATE `FATURAMENTO_SAAS.VER` dormente → `reg_status='A'`. Sem BI; `RELATORIOS` já contratado/efetivo no SV ⇒ 0 grants extras para `/Relatorios/*`.

Verificação pós-grant: `POS_GRANT_EFETIVOS`=19 (+`FATURAMENTO_SAAS`); `POS_GRANT_TM_HOMOLOG_R14|1`; `POS_GRANT_PERM_FATURAMENTO_SAAS_VER|A|permitido=true`. Resultado das escritas: `INSERT 0 1` / `UPDATE 1`.

## 4. Capturas pré-grant (estado inicial)

Com sessão SV nova e SEM `FATURAMENTO_SAAS` nos claims:

- `/FaturamentoSaas/Index` → `AccessDenied?module=FATURAMENTO_SAAS&reason=MODULO_NAO_CONTRATADO` ✓ (guard funcionando, como na R13).
- `/Relatorios/SaasFaturamento` → **200 com 4 linhas reais** (B6 Sonda ABERTA R$10 01/2028; B6 Smoke Clinica Online ABERTA R$333 09/2027, PAGA R$222 08/2027, ABERTA R$111 07/2027; cards "Total de linhas | 4" / "R$ 676,00"), sem banner — **prova viva do F-R14-A da R13**: o endpoint que era sempre-500 agora responde com dados reais no build com o cast duplo. (O `RelatoriosController` usa a variante paginada, que não depende do contrato de módulo FATURAMENTO_SAAS.)

## 5. Achado F-R14-C — banner "Falha ao carregar. Sucesso" (descoberto na captura pós-grant)

Na primeira subida pós-grant (build de `81ebb7a`), `/FaturamentoSaas/Index` respondia 200 com KPIs zerados, mas **sem** o empty state legítimo e com um banner visível que um fetch direto (`no-store`, XHR síncrono) revelou: `<div class="alert alert-danger" role="alert"><strong>Falha ao carregar.</strong> Sucesso</div>`.

Cadeia de raiz:

1. O envelope de sucesso da API carrega `message:"Sucesso"`. O caminho de **sucesso** de `ReadApiResponsePayloadAsync` (`BaseWebController.cs` L302) devolve `Error = TryParseMessage(content)` — ou seja, `resumo.Error == "Sucesso"` em TODA chamada simples bem-sucedida.
2. A variante **paginada** NÃO tem esse vazamento: seu caminho de sucesso (L173) devolve `Error = null`. Por isso `/Relatorios/SaasFaturamento` estava limpo e o Index não.
3. `FaturamentoSaasController.Index` escolhia `resumo.Error ?? faturas.Error` — "primeiro não-nulo" — e o `"Sucesso"` do resumo saudável ganhava sempre, acionando o banner novo de `r14b1` e suprimindo o empty state (`comErro=true`).

Correção aplicada nesta slice (cirúrgica, sem tocar infra compartilhada — disciplina D1): o erro passa a ser atribuído pelo **status da chamada do resumo**, não pelo primeiro valor não-nulo:

```csharp
// FaturamentoSaasController.Index
var erroCarregamento = resumo.StatusCode >= System.Net.HttpStatusCode.BadRequest ? resumo.Error : faturas.Error;
ViewBag.ErrorMessage = erroCarregamento;
```

Contrato atualizado (`R14FaturasCompetenciaNullContratoTests.cs`): os stubs de resumo agora usam o envelope real (`"message":"Sucesso"`) — o fato `ListaVaziaReal` é o teste anti-regressão exato deste vazamento — e entrou um fato novo cobrindo a atribuição quando o próprio resumo falha (resumo 500 → banner com o literal de casa de instabilidade + vazio suprimido).

Observação de comportamento (não é defeito, documentada para a fila): `BuildApiErrorMessage` (L515-525) trata o 500 de chamada **simples** com literal fixo ("Ocorreu uma instabilidade interna...") ignorando a mensagem da API, enquanto o 500 da chamada **paginada** devolve a mensagem parseada do envelope — assimetria pré-existente entre os dois parsers (F-R14-C, ver §9).

## 6. Gate da slice

- Pré-check da classe (9 fatos): 9/9 (após isolar 1× o fato novo, que falhava pela assimetria do item acima — esperado do código, ajustado o assert, não o comportamento).
- **Gate full (suíte completa, Release): `Total de testes: 1456 / Aprovados: 1456` — 0 falhas** (baseline R14 1447 + 8 fatos b1 + 1 fato b2; nada removido).
- Logs: `%TEMP%\opencode\r14b1_gate.log`, pré-check `r14b1_precheck` (logs `r14b1_newtests.log` / `r14b1_iso.log`), build `r14b1_build.log`.

## 7. Capturas pós-grant (build corrigido, tudo verde)

Sessão SV nova (re-login pós-grant e pós-restart do Web):

| Rota | Esperado | Observado |
|---|---|---|
| `/FaturamentoSaas/Index` | 200, zeros honestos, sem banner, empty state legítimo | ✓ KPIs `Receita prevista=recebida=R$ 0,00`, `Abertas/Vencidas/Contestação=0`; `Falha ao carregar.` ausente; **"Nenhuma fatura encontrada"** presente (vazio verdadeiro, SV-scoped — as 4 faturas são de outros clientes) |
| `/FaturamentoSaas/Index?status=VENCIDA` | 200, filtro refletido | ✓ select = `VENCIDA`, API recebeu `&status=VENCIDA` (log), sem banner, vazio mantido |
| `/FaturamentoSaas/Index?competencia=2026-10-01` | 200 — round-trip DateOnly **não-nulo** Web→API (caminho tipado pelo handler) | ✓ input reflete `2026-10-01`, API logou `&competencia=2026-10-01` → 200, sem banner |
| `/FaturamentoSaas/Inadimplencia` | 200 | ✓ h1 "Inadimplência SaaS", sem banner |
| `/Relatorios/SaasFaturamento` | 200, mesmas 4 linhas do pré-grant | ✓ cards "Total de linhas \| 4" / "R$ 676,00"; 4 linhas idênticas; sem banner, sem vazio falso |

Toda chamada a `/api/faturamento-saas/faturas` e `/api/relatorios/saas/faturamento` respondeu **200** nos logs da Api (antes do fix: 500 permanente em competência nula).

## 8. Rollback e net-zero

`r14b2_rollback_sv.sql/.bat`: `DELETE 1` (`tenant_modulos` onde `origem='HOMOLOG_R14'`) + `UPDATE 1` (`FATURAMENTO_SAAS.VER` → `reg_status='I'`), num único bloco `begin/commit`. Verificação:

- `ROLLBACK_TM_HOMOLOG_R14_RESIDUO|0` ✓
- `ROLLBACK_PERM_FATURAMENTO_SAAS_VER|I|permitido=true` ✓ (idêntico ao snapshot pré-grant)
- `ROLLBACK_EFETIVOS|ADM360,AGENDAMENTOS,CID,CLINICA_DASHBOARD,CLINICA_FINANCEIRO,CONSULTAS,CONVENIOS,FINANCEIRO,PACIENTES,PAINEL_CHAMADA,PENDENCIAS_CLINICAS,PLANOS_SAUDE,PRESCRICOES,RELATORIOS,SAUDE360,TRIAGEM,UNIDADES,USUARIOS` — 18 módulos, sem FATURAMENTO_SAAS ✓
- Sem DDL; `access_catalog_version` permanece `v2149`; nenhum outro grant tocado.

Re-login #2 (claims materializam no login) e `/FaturamentoSaas/Index` → `AccessDenied?module=FATURAMENTO_SAAS&reason=MODULO_NAO_CONTRATADO` ✓ — estado de negação pré-grant restaurado de ponta a ponta (também registrado nos logs do Web: `warn SaasRouteGuardFilter — Acesso negado ... Modulo:FATURAMENTO_SAAS Acao:VER`).

Servidores Api/Web encerrados ao final; Postgres segue de pé, dev DB intocado além do ciclo reversível de grants.

## 9. Achados adicionados à fila (R15)

- **F-R14-C (P2, sistêmico)**: o caminho de sucesso de `ReadApiResponsePayloadAsync` expõe `message` do envelope como `Error` (L302), enquanto a variante paginada devolve `null` — qualquer tela que leia `.Error` sobre resposta 2xx pode anunciar "Sucesso" como falha. Fix local aplicado em `FaturamentoSaasController.Index` (atribuição por status); a correção de casa (uniformizar os dois parsers) requer varredura dos consumidores de `.Error` em 2xx antes — blast radius, fora de scope desta slice.
- **F-R14-D (P3, documentação/comportamento)**: assimetria nos 500 entre chamada simples (literal fixo de instabilidade, mensagem da API descartada) e chamada paginada (mensagem do envelope repassada) em `BuildApiErrorMessage`/parsers — vale alinhar intenção antes de mudar.
- **F-R14-B (P3, herdado)**: `ExecutarRelatorioAsync` L142 `total = items.LongCount()` — total de paginação truncado ao pageSize; `RelatoriosSaasController.Faturamento` sem scoping de tenant (design pré-existente).

## 10. Artefatos

- Scripts (em `%TEMP%\opencode\`): `r14b2_pre_grant.sql/.bat`, `r14b2_grants_sv.sql/.bat`, `r14b2_rollback_sv.sql/.bat`, `r14b2_health.ps1`, `r14b1_precheck.bat`, `r14b1_gate.bat`, `r12_api.cmd`, `r12_web.cmd`.
- Logs: `r14b1_newtests.log` (artefato de raiz: `PostgresException 42P08` reproduzido no precheck), `r14b1_iso.log` (isolate do fato novo + dump DBG do banner), `r14b1_gate.log` (gate 1456), `r14b1_build.log`.
- Code versionado desta slice: `backend/PlantaoPro.Web/Controllers/FaturamentoSaasController.cs` (atribuição de erro por status + comentário do achado), `backend/PlantaoPro.Tests/R14FaturasCompetenciaNullContratoTests.cs` (9 fatos; stubs com envelope real; fato novo de atribuição em resumo falho).

## 11. Conclusão

O fix do 42P08 de `r14b1` está **homologado ao vivo**: os dois endpoints que davam 500 permanente respondem 200 (lista escopada-SV vazia verdadeira + relatório global com as 4 linhas reais), filtros `status` e `competencia` (inclusive DateOnly não-nulo) funcionam de ponta a ponta, e a degradação honesta só aparece quando há erro real — com a devida atribuição. O banco dev voltou ao estado exato pré-janela (net-zero verificado), e o ciclo completo (negar → liberar → validar → reverter → negar de novo) ficou versionado aqui. Pendências para `r14z`: relatório consolidado da rodada + fila R15 completa (F-R14-B/C/D + heranças de R13).
