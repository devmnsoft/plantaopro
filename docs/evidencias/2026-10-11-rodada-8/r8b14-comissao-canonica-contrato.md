# R8-B14 — Comissão calculada/apropriada em 4 locais: validação de canonicidade da fórmula (§29 item 6)

- **Rodada:** 8 (ciclo de conclusão integrada, master §1–§30) — fatia "Comissão calculada/apropriada em 4 locais (baixa, geração AP, relatórios, UI) — validar canonicidade da fórmula" (item 6 da fila §29, ordem quebrado→alto impacto: … → B11 → B12 → B13 → **B14**)
- **Data:** 2026-10-13
- **Status:** IMPLEMENTADO + TESTADO (aceite por testes executados). HOMOLOGADO/LIBERADO seguem a janela viva de homologação (`r6a1b` §7), como o restante da rodada.
- **Regra de push:** respeitada — commit local em `main`, push mantido pendente com o resto da rodada.

## 1. Achado (fonte canônica) — os 4 locais NÃO divergem

Mapa do varredura C6.1 resolvido por leitura direta:

| Local | Papel | Fórmula? |
| --- | --- | --- |
| **(a)** baixa — `ContasReceberRepository.cs` | **ÚNICO ESCRITOR** de `adm360_comissoes_apropriadas` | Delega a `ComissaoRegras.CalcularComissaoApropriada` (não aplica taxa × base inline) |
| **(b)** geração AP — `GerarDeComissaoAsync` (`ContasPagarRepository.cs`) | Consolida título a pagar | SOMENTE `SUM(valor_comissao)` já escrito por (a); nunca toca em taxa |
| **(c)** relatórios — `Adm360FinanceiroRelatoriosRepository.cs` | Exibição | LÊ coluna gravada (`v.comissao_prevista`) ou agrega (`SUM(com.valor_comissao)`); não recalcula |
| **(d)** UI — `TituloDetalhes.cshtml` | Texto explicativo | "apropria proporcionalmente a comissão do vendedor" (sem código) |

**Fórmula canônica (Domain):** `ComissaoRegras.CalcularComissaoApropriada` (`PlantaoPro.Domain/Administrativo360/FinanceiroVendas.cs`) — técnica de **"total acumulado − já apropriado", arredondada a 2 casas e com piso em 0**:
`comissão = max(0, round((baseAcumulada + valorBaixa) × pct/100, 2) − comissaoJaApropriada)`.
Evita drift de centavos entre parcelas (a última parcela absorve o resíduo). Já unitariada por
`BlocoD_ComissaoRegras_CalculoProporcionalEvitaDiferencaCentavos` (3× parcela a 5% somam exatamente 5,00). Há ainda `CalcularEstornoComissao` (mesmo arredondamento p/ reversão).

**Dois conceitos distintos (não duplicados entre si):**
- `comissao_prevista` (**planejada**): derivada na valorização e **COPIADA** para a venda (`VendaRepository.cs`), lida pelos relatórios. É o teto esperado.
- `valor_comissao` (**realizada/proporcional**): escrita na baixa via regra canônica; acumula até `prevista` conforme os recebimentos chegam.

**Estorno consistente:** o estorno marca a linha `situacao='ESTORNADA'` e o acúmulo usado pela próxima apropriação filtra `situacao='APROPRIADA'` → estornadas não contam; o "total acumulado" se auto-corrige.

**Conclusão da validação:** não há fórmula duplicada/divergente nos 4 locais. A preocupação do varredura ("verificar se (a) e (c) usam a mesma fórmula") resolve-se positivamente: (c) LÊ o que (a) ESCREVE através de uma única função no Domain. A canonicidade **já estava garantida**.

## 2. Decisão e mudança (sem regra inventada, sem deleção antecipada)

Como a fórmula já é single-source no Domain + testada, **nada havia a migrar** (migrar seria inventar trabalho). O rito §29 (comparar consumidores → canônico → migrar → regressões) conduz a:
- **Comparar:** feito (tabela acima). **Canônico:** `ComissaoRegras` no Domain. **Migrar:** inexistente. **Regressões/contrato:** entregue um **CONTRATO anti-drift** que fixa o invariante "um único escritor + consumidores só agregam/lêem", impedindo que no futuro alguém re-introduza uma segunda fórmula de taxa × base nesses pontos.
- **Zero mudança em produção:** esta fatia entrega apenas o teste de contrato + evidência (sem tocar `ContasReceber/Pagar`, relatórios, UI nem o Domain).

## 3. Alterações

| Arquivo | Mudança |
| --- | --- |
| `backend/PlantaoPro.Tests/R8B14ComissaoCanonicaContratoTests.cs` | **Novo**: 4 fatos — (1) escrita única repo-wide (`INSERT … adm360_comissoes_apropriadas` em exatamente 1 arquivo, que delega à regra canônica); (2) consumidores agregam/lêem sem re-aplicar taxa (método de geração AP só soma `valor_comissao`; relatórios leem `comissao_prevista`/`SUM(valor_comissao)`); (3) par de estorno (flip `ESTORNADA` + filtro `APROPRIADA`); (4) pontos conhecidos da fórmula (identidade total × taxa, piso em 0, estorno, guardas). |
| `docs/evidencias/2026-10-11-rodada-8/r8a0-auditoria-inicial-matriz-estado.md` | §29 item 6 (linha 232) → **R8-B14**; resumo "Estado" (§29) refletindo itens 1+2+3+4+5+6. |

## 4. Evidência (executada)

- **Build sln completa:** EXIT 0 (0 erros; apenas avisos pré-existentes).
- **Alvo:** `R8B14ComissaoCanonicaContratoTests` = **4/4 aprovados** (`--filter FullyQualifiedName~R8B14`).
- **Gate phaseA v2 (pipeline):** **52/52, EXIT=0**.
- **Full suite v2 (pipeline final, RAM livre 1,22 GB no início):** **full1/full2/full3 = EXIT 0, `Total de testes: 1355`** (= 1351 baseline B13 + 4 novos), **0 falhas em cada run** — run limpo 1355/1355 três vezes.
- **Investigação de falhas pontuais (rodada anterior sob RAM 0,55 GB):** `SaasComercialB4MatrizTests` (×3 métodos) e `R7E1I18nPersistenciaRealTests.ApiReal_SalvaColunaV2344_E_RefleteNoRefreshENoNovoLogin` — **todos aprovados 5/5 em isolamento** (flakes de banco sob pressão de memória; método variável por run).
- **Bloqueio de suíte raiz-causado e resolvido:** `ProvisionamentoB5Tests.P1_Finalizar_CriaMundoTodo_E_AdminAutentica` falhava em **todas** as isolamento ("CNPJ já cadastrado") — causa: **resíduo de tenant criado por uma run anterior matada por OOM (`EXIT=-1`)** que pulou o `finally { LimparMundoAsync }` e o `Dispose`; a semente idempotente do teste só purga as fixtures de GUID fixo, não o tenant por-CNPJ de cada run. **Limpeza por psql** (ordem FK idêntica a `LimparMundoAsync`) → `P1` **re-provado green (EXIT 0)**. Não é regressão desta fatia (sidebar + teste de leitura não tocam provisioning/CNPJ) — ver §5.
- **Verificação byte-level:** `git status` = matriz + teste novo (+ doc selado); **nenhum arquivo de produção alterado**.

## 5. Dívida registrada (fora do escopo desta fatia)

1. **`ProvisionamentoB5Tests` não é 100% crash-idempotente:** a semente (`SementarAsync`) promete "purga + insert para reruns previsíveis" mas só cobre as fixtures de GUID fixo. Após uma run OOM-matada no meio de P1, o tenant por-CNPJ (`99988877000166`) fica órfão → P1 passa a falhar com "CNPJ já cadastrado" até limpeza manual. **Correção recomendada:** em `SementarAsync`, antes do seed, invocar o helper já existente `LimparPorCnpjAsync` para os CNPJs dos testes (`99988877000166`, `55443322000155`, `CnpjDup`). Correção de test-infra (não bloqueante p/ aceite; suíte verde com a limpeza aplicada).
2. **§29 item 7 (próximo na fila):** `parceiros` (B2B jsonb) × `adm360_parceiros` (master c/ flags) — propósitos distintos, sobreposição semântica.
3. **Main DB v2339 / homologação viva:** invariado — janela segue em `r6a1b` §7 (+ lote v2340–v2351 + IIS).

## 6. Fila seguinte (R8)

Com §29 item 6 entregue (validação + contrato anti-drift), o próximo item da fila §29 = **item 7: `parceiros` (B2B jsonb) × `adm360_parceiros` (master c/ flags) — propósitos distintos, sobreposição semântica.** Alternativas: janela viva de homologação (`r6a1b` §7), ou o push da rodada (pendente).

## 7. Gotchas aprendidos (reutilizáveis)

- **CTE no PostgreSQL tem escopo de UMA instrução:** `WITH x AS (…) DELETE …; DELETE …` — o CTE `x` vale só para o PRIMEIRO `DELETE`; instruções seguintes não o veem ("relação x não existe"). Para múltiplos deletes, use subqueries autocontidas ou repita o CTE por instrução.
- **`Search Path=PlantaoPro,public` → schema `plantaopro`:** identifiers sem aspas são dobrados p/ minúsculas; o schema efetivo é `plantaopro` (não "plantaoro"). Confirme com `information_schema.tables` antes de limpar estado no DB de teste.
- **Flakes de banco sob pressão de RAM vs. estado residual:** com < ~1 GB livre o full suite causa `EXIT=-1` (OOM) e classes flakies falham em métodos variados a cada run (isolá-las prova). UM teste que falha em **todas** as isolamento geralmente é **estado residual** (ex.: "já cadastrado"), não flake — investigar o erro real e limpar o estado.
- **`Assert.Equal(int, int, "msg")`** não existe no xUnit deste projeto → usar `Assert.True(cond, "msg")` (CS1503 caso contrário).
