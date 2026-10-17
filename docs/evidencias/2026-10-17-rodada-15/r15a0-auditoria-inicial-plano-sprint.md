# Rodada 15 · r15a0 — Auditoria inicial e plano de sprint

- **Data:** 2026-10-17 · **Base:** `ca9c8a5` (ponta do push da Rodada 14; árvore limpa) · **Suíte base:** 1456 aprovados (gate limpo R14)
- **Escopo:** topo da fila R15 (ver `r14z` §"Dívidas abertas → Rodada 15") — **P1b herdada (R11→R14): A12 baixa/devolução dirigida por `destino`.** Defeito: encerrar uma ocorrência com destino de descarte ainda devolve todo o saldo bloqueado a LIBERADO no resolver — não há consumo físico dirigido pelo destino.
- **Fora desta rodada:** demais itens da fila R15 (F-R14-C parsers Web P2, fantasmas atingíveis P2, drill-downs r10b3 P2, menu c/ FaturamentoSaas P2, F-R14-D/F-R14-B/fantasmas residuais/Meu Dia UTC/tile B18/especialidade P3, pg_dump P5) — seguem para a fila R16 via r15z.

## 1. Estado atual (fatos com evidência)

| # | Fato | Evidência |
|---|------|-----------|
| F1 | Spec A12: "Aberta → em tratamento → resolvida → encerrada. Bloqueio imediato autorizado pode abranger saldo interno e externo. **Encerrar exige destino dos itens e justificativa.** Registro de ocorrência não apaga estoque." A spec **não prescreve efeito físico por destino** ⇒ o item é extensão funcional própria (caveat versionado desde R11: `r11b1` §5, plano `r11a0` §2.3) e o design é decisão desta rodada. Regra global L296: "Somente movimentos geram saldos… Lote bloqueado/vencido não pode ser reservado ou expedido." | `docs/administrativo360/PlantaoPro_Administrativo360_MVP_Requisitos_Prompts.md` L112, L296 |
| F2 | Domínio: máquina de estados estrita (um passo por comando, sem retorno); `ValidarEncerramento` só exige destino/justificativa **não vazios** — destino é texto livre sem classificação de efeito | `backend/PlantaoPro.Domain/Administrativo360/OcorrenciasA12.cs` L14–17, L32–38 |
| F3 | Writer: ABRIR gera bloqueio imediato (pernas `BLOQUEIO_OCORRENCIA` LIBERADO→BLOQUEADO, parcial, ordem determinística local-da-ocorrência primeiro); **RESOLVER inverte todo o bloqueio registrado** (`LIBERACAO_OCORRENCIA`, espelhando os movimentos, nada recalculado); TRATAR e ENCERRAR **não mexem em saldo**; ENCERRAR persiste apenas destino (texto livre) + justificativa | `backend/PlantaoPro.Infrastructure/Administrativo360/OcorrenciasRepository.cs` header L8–16, `TransicionarAsync` L143–180 (efeito L175–178), `LiberarSaldoBloqueadoAsync` L248–275, `EscreverPernasMudancaCondicaoAsync` L281–313 (chave idempotência `{ocorrenciaId}:{BLQ\|DEL}:{localId}:{perna}`) |
| F4 | **O defeito, fixado pelo próprio contrato atual:** o fato que cobre tratar+encerrar usa destino `"Descarte assistido"` e zera a expectativa de pernas no encerrar — ou seja, hoje encerrar com descarte deixa 100% do saldo de volta em LIBERADO, sem consumo físico | `backend/PlantaoPro.Tests/Administrativo360OcorrenciasA12Tests.cs` L596–598 (`R11_TrataryEncerrar_NaoGeraPernasNovas`) |
| F5 | Contrato: `EncerrarOcorrenciaCommand(long Versao, string? Destino = null, string? JustificativaEncerramento = null)` — nenhum campo expressa o efeito físico pretendido | `backend/PlantaoPro.Application/Administrativo360/Contracts.cs` L11; `IOcorrenciasRepository.EncerrarAsync` L97 |
| F6 | API: `POST api/administrativo360/ocorrencias/{id}/encerrar` (policy `Adm360.Qualidade`); `Administrativo360BusinessException` → 409 com mensagem do domínio (400 apenas para `ArgumentException`) | `backend/PlantaoPro.Api/Controllers/Adm360SuprimentosControllers.cs` L44–55 |
| F7 | Web: `EncerrarOcorrencia(id, versao, destino, justificativa)` — destino é texto livre (`maxlength=200`) num collapse junto da justificativa (`maxlength=400`); payload `{Versao, Destino, JustificativaEncerramento}` sem efeito físico. Única consumidora do endpoint em produção além da suíte | `backend/PlantaoPro.Web/Controllers/Administrativo360Controller.ComprasRecebimentos.cs` L343–361; `backend/PlantaoPro.Web/Views/Administrativo360/Ocorrencias.cshtml` L127–154 |
| F8 | Consumidores de `ResolverAsync` (Adm360): apenas `Ocorrencias360Controller.Resolver` + testes — mover o efeito de saldo do resolver para o encerrar **não tem eco** em outro código de produção (os `ResolverAsync` do V115 são domínios distintos com o mesmo nome) | grep `ResolverAsync\(` em `backend/**/*.cs` |
| F9 | Vocabulário de movimentos: **não existe idiom de consumo físico** em saldo Adm360 hoje (varredura de literais `BAIXA*`/`PERDA*`/`CONSUMO*`: só `CONSUMO_VALE`/`PERDA_VALE` no domínio de vales) — a BAIXA de ocorrência será perna única `BLOQUEADO −q`, primeira exceção autorizada à soma física nula do ciclo | grep em `backend/**/*.cs` |
| F10 | i18n: resx ×4 (`UiTexts.resx` pt-BR default + `.en`/`.es`/`.fr`); bloco `Adm360.Occ.*` a partir de `UiTexts.resx` L3701+ (`ThDestino` L3707, `ErrCloseFields`, `OkResolved`…) — novas literais de UI entram via `UiText.L` nos **quatro** arquivos (contrato do repo); mensagens de domínio/writer seguem o padrão textual PT já existente dos writers Adm360 (fora de resx) | `backend/PlantaoPro.Web/Localization/*.resx` |
| F11 | Suíte atual da classe: 16 métodos (1 theory c/ 10 casos + 15 facts), helpers `CriarFixturesAsync`/`SearSaldoLiberadoAsync`/`ConsultarDisponivel`/`ConsultarFisicoPorCondicao`/`SomaFisicaTotal`/`PernasDaOcorrenciaAsync` já cobrem tudo que o novo contrato precisa | `backend/PlantaoPro.Tests/Administrativo360OcorrenciasA12Tests.cs` (classe inteira, 632 linhas) |

## 2. Decisões de design

- **D1 — Efeito de saldo move de RESOLVER para ENCERRAR, dirigido por campo explícito.** No resolver o destino ainda é desconhecido (a spec o ancora no encerrar); liberar antes de saber o destino forçaria a BAIXA a consumir de LIBERADO depois (corrida + unidades arbitrárias em vez das específicas que estavam bloqueadas). Consequência versionada: ocorrência RESOLVIDA mantém o saldo BLOQUEADO até ser encerrada — coerente com L296 (lote bloqueado não reserva/expede) e com o espírito de A19 ("perda exige decisão individual"). Fatos R11 ancorados na semântica antiga viram alvos de adaptação (§3).
- **D2 — Campo canônico `EfeitoFisico` ∈ {`DEVOLUCAO`, `BAIXA`}, não inferência de texto livre.** `Destino` permanece texto livre (display/auditoria) e continua obrigatório; o efeito físico é dado estruturado (select de 2 opções na UI). Compatibilidade: parâmetro opcional no fim do command; vazio/nulo ⇒ `DEVOLUCAO` (payloads pré-rodada mantêm o comportamento atual — devolução). Qualquer outro valor ⇒ 409 `Administrativo360BusinessException`, ocorrência fica RESOLVIDA, zero efeitos colaterais.
- **D3 — BAIXA = consumo físico:** uma perna por chave `(produto, lote, local)` espelhando o registro do ABRIR: `BLOQUEADO −q`, `tipo = 'BAIXA_OCORRENCIA'`, motivo referencia a ocorrência (L296 — motivo consultável), chave idempotência `{ocorrenciaId}:BXA:{localId}:BAIXA_BLOQUEADO`, `created_by` = usuário do encerramento. A soma física cai exatamente pela quantidade bloqueada — o único caso em que ocorrência "apaga estoque", e somente porque a operadora decidiu isso no encerrar ("Registro de ocorrência não apaga estoque" se preserva: o registro em si nunca apaga; o consumo é a decisão explícita de destino).
- **D4 — Sem DDL, sem coluna nova, sem seed.** O efeito aplicado é inferível dos movimentos (`origem_tipo='OCORRENCIA'` + `tipo ∈ {BAIXA_OCORRENCIA, LIBERACAO_OCORRENCIA}`); a coluna `destino` permanece texto livre. `access_catalog_version` continua `v2149`. O endpoint da API não muda de forma (o JSON ganha um campo opcional).
- **D5 — Princípio do espelho preservado:** o efeito atua exatamente sobre o que foi bloqueado registrado no ABRIR (bloqueio parcial ⇒ consome/libera só a parte bloqueada; bloqueio zero ⇒ zero pernas, preservando o fato `A12_SemSaldoLiberado`). O caveat documentado em r11b1 (inventário que ajustou o BLOQUEADO entre abrir e resolver; fluxo simétrico espelha o registrado, divergência física é papel do inventário) estende-se ao BAIXA.
- **D6 — Invariantes que não mudam:** máquina de estados estrita, concorrência otimista (`FOR UPDATE` + `versao`), ABRIR/TRATAR, escopo tenant, ordens determinísticas. RESOLVER passa a ser ato administrativo (sem efeito em saldo), alinhado ao TRATAR.
- **D7 — Cadeia de contrato:** (a) domínio `OcorrenciasA12`: constantes `EfeitoDevolucao`/`EfeitoBaixa` + normalização/validação em `ValidarEncerramento(destino, justificativa, efeito)`; (b) `EncerrarOcorrenciaCommand` + `string? EfeitoFisico = null` no fim (construções posicionais antigas compilam); (c) repository: `TransicionarAsync` recebe o efeito; no ENCERRAR, após o UPDATE, ramifica `BaixarSaldoBloqueadoAsync` (nova — espelha a leitura do `LiberarSaldoBloqueadoAsync` e escreve perna única reusando o INSERT dos movimentos) ou `LiberarSaldoBloqueadoAsync` (movida para cá, sem alteração); o ramo RESOLVIDA perde a liberação; comentários de header atualizados; (d) controller Web ganha parâmetro `efetivoFisico` e o payload passa a incluí-lo (literal nenhuma acoplada — `UiText.L`); (e) view: select `name="efetivoFisico"` no collapse do encerrar (obrigatório, default `DEVOLUCAO`); (f) resx ×4: ~3 chaves novas (`Adm360.Occ.LabelEfeitoFisico` + as duas opções), com traduções en/es/fr.
- **D8 — Flakes:** regra de casa família ① (40P01 em teardown `PurgarAsync()`) ⇒ isolar 5× antes de tratar como regressão.

## 3. Plano de fatos (contrato da fatia r15b1)

**Adaptações** (semântica mudou — mesmos nomes de teste quando a essência persiste):

1. `R11_Resolver_InverteBloqueio_RestauraDisponivelECicloNetZero` → **reapontar:** RESOLVER **não** gera pernas nem mexe em saldo (BLOQUEADO persiste); ENCERRAR c/ `DEVOLUCAO` inverte (mesmas pernas espelhadas de antes), restaura disponível, ciclo net-zero e reserva pós-encerramento possível (assert de reserva portado daqui).
2. `R11_TrataryEncerrar_NaoGeraPernasNovas` → TRATAR sem pernas (mantém) / RESOLVER agora também sem pernas / ENCERRAR `DEVOLUCAO` gera as pernas de inversão (virada).
3. `A12_ValidarEncerramento_RequiresDestinoEJustificativa` → ganho de casos p/ efeito: vazio⇒DEVOLUCAO aceito, `BAIXA` aceito, valor desconhecido/`DESCARTE` rejeitado (fato puro).
4. `A12_CicloCompleto_AbreTrataResolveEncerraComDestinoEJustificativa` → constructor do command ganha `EfeitoFisico = DEVOLUCAO`; resto idêntico.
5. `A12_EncerrarSemDestinoOuJustificativa_RejeitaEMantemResolvida` → ganha um caso: efeito desconhecido rejeita, mantém RESOLVIDA na mesma versão, BLOQUEADO intacto, zero pernas novas.
6. `A12_SemSaldoLiberado_CicloCompleto_NaoGeraPernasDeBloqueio` → encerrar com `BAIXA` (o destino de exemplo já é `"Descarte assistido"`): prova que BAIXA com bloqueio zero é no-op honesto (zero pernas, ciclo completa).

**Novos:**

7. ENCERRAR `BAIXA` consome exatamente o bloqueado: perna única `BLOQUEADO −q` `tipo=BAIXA_OCORRENCIA` com motivo/origem consultáveis; soma física cai exatamente q; `BLOQUEADO → 0`; LIBERADO intacto; ocorrência ENCERRADA com destino/justificativa persistidos.
8. ENCERRAR `BAIXA` c/ bloqueio parcial (seed 3, quantidade 10): consome só a parte bloqueada (soma física cai 3); oc.Quantidade permanece integral (paridade com o fato de abertura parcial).
9. ENCERRAR `BAIXA` multi-local: consumo por local espelhando a ordem/cobertura do bloqueio (setup do `R11_Abrir_MultiLocal` reaproveitado).
10. Source-scan (strings canônicas, estilo R14): `BAIXA_OCORRENCIA` aparece apenas no writer; `EfeitoFisico` presente no command; a view renderiza o select `name="efetivoFisico"` e o controller o repassa no payload.
11. web-bff `[Collection("web-bff")]` (stub determinístico, estilo R14/R12): POST `EncerrarOcorrencia` envia `EfeitoFisico` no JSON e reage ao 409 de efeito inválido com `TempData["Error"]` (degradação honesta) — 1 fato.

**Estimativa de gate:** 16 métodos → ~21–22 métodos; suíte 1456 → ≈1461–1462 (contagem exata registrada na fatia).

## 4. Plano de fatias

| Fatia | Conteúdo | Artefatos |
|-------|----------|-----------|
| **r15a0** (esta) | Auditoria + plano + baseline na base `ca9c8a5` | este doc; logs `r15a0_build.log`/`r15a0_suite.log` |
| **r15b1** | Implementação D1–D7 + fatos §3 (adaptações + novos) + gate full suite + commit | build Release EXIT 0; suíte full limpa; commit `<secoes> (rodada 15 r15b1): …` |
| **r15b2** | Homologação viva opcional (sem ciclo de grants — SV admin já tem ADM360 efetivo): abrir/tratar/resolver/encerrar BAIXA numa ocorrência real no banco dev via browser, verificar pernas/saldos por psql e net-zero dos fixtures criados; se o orçamento da rodada não couber, desce para a fila R16 como "smoke vivo A12" | evidência verbatim + capturas |
| **r15z** | Relatório final consolidado + fila R16 (restantes da fila R15 + novos achados, se houver) + push sob autorização | doc r15z; commit |

## 5. Arquivos previstos (r15b1)

| Arquivo | Mudança |
|---------|---------|
| `backend/PlantaoPro.Domain/Administrativo360/OcorrenciasA12.cs` | constantes de efeito + `ValidarEncerramento(+efeito)` |
| `backend/PlantaoPro.Application/Administrativo360/Contracts.cs` | `EncerrarOcorrenciaCommand` + `EfeitoFisico` (opcional, fim) |
| `backend/PlantaoPro.Infrastructure/Administrativo360/OcorrenciasRepository.cs` | liberação sai do RESOLVIDA e vai para o ENCERRADADO (ramo por efeito); `BaixarSaldoBloqueadoAsync`; comentários de header |
| `backend/PlantaoPro.Web/Controllers/Administrativo360Controller.ComprasRecebimentos.cs` | parâmetro `efetivoFisico` + payload |
| `backend/PlantaoPro.Web/Views/Administrativo360/Ocorrencias.cshtml` | select no collapse do encerrar |
| `backend/PlantaoPro.Web/Localization/UiTexts{,.en,.es,.fr}.resx` | ~3 chaves ×4 idiomas |
| `backend/PlantaoPro.Tests/Administrativo360OcorrenciasA12Tests.cs` | adaptações + fatos novos |
| Api (`Adm360SuprimentosControllers.cs`) | **sem mudança** (binding do campo opcional no JSON) |

## 6. Baseline (base `ca9c8a5`)

- **Execução:** build Release sln completa + suíte full (`r15a0_baseline.bat`; logs `r15a0_build.log`/`r15a0_suite.log`); isolamento em `r15a0_iso.bat` (`r15a0_iso_1..5.log`).
- **Build Release:** EXIT 0 (`r15a0_build.log`).
- **Suíte full:** 1456 total, **1455 aprovados, 1 falha** — `SaasGovernancaB6Rodada4Tests.G5_RevogacaoAdministrativa_SessaoAbertaViraInvalidaComMotivo`: `Npgsql.PostgresException 40P01: impasse detectado` no **teardown** (`PurgarAsync()`, `SaasGovernancaB6Rodada4Tests.cs:481`, chamado de `Dispose()`) — o corpo do teste aprovou; o impasse veio da purga correndo em paralelo com outra coleção. Assinatura idêntica ao **flake família ① conhecido** (40P01 em `PurgarAsync()`), só que surfando nesta classe.
- **Isolamento (regra de casa):** teste isolado **5/5 green** (`r15a0_iso_1..5.log`, todos EXIT 0) ⇒ classificado como flake conhecido, **não regressão**. **Baseline efetivo: 1456 aprovados.**

## 7. Fora de escopo / riscos

- **Janela BLOQUEADO alongada:** ocorrência RESOLVIDA sem encerrar continua travando saldo até o encerramento — aceitável e versionado: a spec exige encerrar com destino; a página lista situação e sinaliza vencidas. Risco operacional menor que a imprecisão física de consumir de LIBERADO.
- Sem DDL/seed/grants (v2149 intocado), sem mobile, sem mudança de forma no endpoint da API, sem literal acoplada em controller.
- Restantes da fila R15 seguem para R16 (ver §Escopo).
