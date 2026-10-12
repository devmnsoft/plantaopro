# R8-B8 — Central de importações: prévia + confirmar no fluxo padrão (§24 — fatia §24)

- **Rodada:** 8 (ciclo de conclusão integrada, master §1–§30) — fatia de bug/gap "importações sem prévia" da fila pós-R7-D (ordem quebrado→alto impacto: … → B6 → B7 → **B8**)
- **Data:** 2026-10-12
- **Status:** IMPLEMENTADO + TESTADO (aceite por testes executados). HOMOLOGADO/LIBERADO seguem a janela viva de homologação (roteiro `r6a1b` §7), como o restante da rodada.
- **Regra de push:** respeitada — commit local em `main`, push mantido pendente com o resto da rodada.

## 1. Achado (fonte canônica)

Matriz `r8a0-auditoria-inicial-matriz-estado.md`, linha "Fluxo selecionar→validar→prévia→confirmar→processar→resultado" (§24):

> "XML: selecionar→validar (2MiB/quarentena)→processar→resultado consultável; **prévia e confirmar ausentes** (grava direto) | **parcial** | **etapas do prompt faltando** | fatia §24 | novo teste"

Varredura `r8a0-varredura-financas-pacientes-imports-falhas-notificacoes.md` (área C, C2): as etapas de **prévia** e **confirmar** não existem para **nenhum** importador real (busca por "previa"/"preview" em importação → nada). A linha "Importadores reais" (§24) pede explicitamente **"padronizar fluxo"** entre os importadores.

Fatos de domínio sustentando o desenho (todos já existentes em HEAD):

- **XML NF-e/NFS-e (Adm360):** `DocumentosXmlRepository.ImportarXmlAsync` valida (teto 2 MiB, quarentena por destinatário não autorizado, dedup intra-arquivo) e **grava direto** via `ImportarUnidadeAsync` (advisory lock determinístico + `FOR UPDATE` + insert `ON CONFLICT DO NOTHING` + itens). Não há modo de rodar o parsing/validação sem gravar.
- **CID (CSV):** `Saude360ClinicalService.ImportarCidCsvAsync` faz parse + upsert por linha (`on conflict (versao, upper(codigo)) where reg_status='A'`) + grava resumo em `cid_importacoes`. Também sem modo dry-run.
- As **decisões de gravação** do XML já são todas computáveis por **leitura**: existência por chave + comparação de conteúdo (hash/bytes/texto), conflito de identidade fiscal (emitente+modelo+número+série, só quando não-quarentena), e quarentena por destinatário. Ou seja: dá para **espelhar o resultado exato da gravação sem gravar** — sem inventar regra nova.

Escopo desta fatia: dar a **ambos** os importadores a etapa de **PRÉVIA** (dry-run — mesmo parsing/validação, sem gravar) e tratar o endpoint de escrita já existente como a etapa de **CONFIRMAR/processar** (back-compat). Isso adiciona as etapas faltantes (linha "Fluxo") e padroniza o fluxo entre os dois importadores (linha "Importadores reais").

## 2. Decisões (sem regras inventadas)

1. **Prévia = dry-run no MESMO caminho**, não um reprocessamento paralelo com regras novas: XML recebe uma flag `bool previa=false` em `ImportarXmlAsync` (param opcional cauda, back-compat); CID ganha um método dedicado `PreverImportacaoCidCsvAsync`. Em ambos, o parsing/validação é o existente.
2. **XML prévia espelha por leitura** (`AvaliarUnidadePrevioAsync`, read-only, sem advisory lock): (a) unidade já existe pela chave + mesmo conteúdo (hash/bytes/texto) → **duplicado idempotente**; (b) existe com conteúdo divergente → **falha**; (c) conflito de identidade fiscal (só quando não-quarentena) → **falha**; (d) senão → **importável**. Mesma árvore de decisão de `ImportarUnidadeAsync`, apenas sem gravar.
3. **CID prévia espelha por leitura o upsert**: `select exists(versao, upper(codigo), reg_status='A')` → **inserido × atualizado**; linhas sem `codigo`/`descricao` → **erro**. Sem gravar linhas nem o resumo.
4. **Confirmar/processar = endpoint de escrita já existente** (`POST …/xml/importar-manual`, `POST api/cid/importar-csv`/`importar-url`). `previa=false` é **o caminho original**, idêntico byte-a-byte em comportamento → zero regressão para quem chama hoje.
5. **Resposta sinaliza que nada foi gravado**: XML → `{ previa=true, resultado }`; CID → `{ previa=true, TotalLinhas, Inseridos, Atualizados, Erros }`. Na prévia XML o `DocumentoId` de unidade nova é **sintético** (a prévia não persiste); a prova autoritativa de "nada gravou" é a **contagem zero no banco**, não o valor do id.
6. **Sem migration, sem nova tabela, sem nova regra de domínio, sem nova chave i18n.** Web inalterado (passo de prévia na tela = dívida UI, §5).

## 3. Alterações

| Arquivo | Mudança |
| --- | --- |
| `backend/PlantaoPro.Application/Administrativo360/CotacoesXmlContracts.cs` | `IDocumentosXmlRepository.ImportarXmlAsync` ganha `bool previa = false` (cauda opcional — todos os chamadores posicionais atuais seguem compilando/funcionando). |
| `backend/PlantaoPro.Infrastructure/Administrativo360/DocumentosXmlRepository.cs` | `ImportarXmlAsync` sinaliza `previa`; no loop: `r = previa ? AvaliarUnidadePrevioAsync(…) : ImportarUnidadeAsync(…)`; novo helper privado `AvaliarUnidadePrevioAsync` (read-only: existência por chave + conteúdo, divergente, identidade fiscal) reutilizando a conexão já aberta do método. |
| `backend/PlantaoPro.Api/Controllers/Adm360DocumentosXmlController.cs` | Novo `POST api/administrativo360/xml/importar-preview` (policy `Adm360.ImportarXml`), com a MESMA validação de teto (2M chars / 2 MiB) do `importar-manual`, retorna `{ previa=true, resultado }`. `importar-manual` intacto (etapa confirmar). |
| `backend/PlantaoPro.Api/Saude360ClinicalService.cs` | Novo `PreverImportacaoCidCsvAsync` (parse idêntico a `ImportarCidCsvAsync` + tally read-only inseridos/atualizados/erros). `ImportarCidCsvAsync` intacto. |
| `backend/PlantaoPro.Api/Controllers/Saude360ClinicalControllers.cs` | Novo `POST api/cid/prever-csv` (role `Saude360CidGestao`). `importar-csv`/`importar-url` intactos. |
| `backend/PlantaoPro.Tests/R8B8ImportsPrevioConfirmarTests.cs` | Nova: 7 fatos — XML (prévia não grava mas mostra importação; prévia em quarentena não grava; **jornada prévia→confirmar grava exatamente 1**; prévia após confirmar identifica duplicado cross-request sem regravar); CID (prévia não grava e conta inseridos/atualizados/erros; **jornada prévia→confirmar mesmos contadores e só o confirmar grava**); contrato rotas×serviços. |
| `docs/evidencias/2026-10-11-rodada-8/r8a0-auditoria-inicial-matriz-estado.md` | Linha "Fluxo selecionar→validar→prévia→…" (§24) → **corrigido** (residual = UI Web sem botão de prévia, dívida registrada). |

## 4. Evidência (executada)

- **Build sln completa:** `BUILD_R8B8_EXIT:0` (0 erros) — a mudança de assinatura do contrato (`previa` opcional) recompilou todos os projetos/chamadores sem quebrar.
- **Alvo:** `R8B8*` = **7/7 aprovados** (`RUN_R8B8_EXIT:0`).
- **Gate phaseA v2 (pipeline):** contratos Login/R7E1/R7A1/R7OB — **EXIT=0, 52/52**.
- **Full ×3 (pipeline v2):** total agora **1335** (1328 + 7 novos) — full2 **1335/1335 EXIT=0**; full3 **1335/1335 EXIT=0**; full1 com **2 falhas transitórias** em classes **já do catálogo de flake ①** (`SaasComercialB4MatrizTests.B4_Downgrade_UsoDentroDoLimite_Aprova` e `R7OBOnboardingPreferenciasIdiomaTests.O5_CliqueSemDado_NaoConcluiEtapaComCriterio_409`) — ambas **limpas em full2/full3 e 5/5 em execução isolada** (regra ①), alheias ao escopo de importação desta fatia. **Sem flake novo — catálogo inalterado.**
- **Higiene do repositório:** `RepositoryHygieneContractTests` aprovada (nenhum termo banido novo introduzido).
- **Verificação byte-level:** tokens presentes nos arquivos novos/mudados (`importar-preview`, `prever-csv`, `AvaliarUnidadePrevioAsync`, `PreverImportacaoCidCsvAsync`, `bool previa`, `select exists(select 1 from plantaopro.cid_tabela`); rotas novas confirmadas nos controllers.
- **Nota de scoping (executada):** na primeira iteração, o fixture de quarentena usava `<dest>` ausente (`incluirDest:false`), o que o parser classifica como **`DOCUMENTO_INCOMPLETO`** (seção ausente), **não** `DESTINATARIO_NAO_AUTORIZADO`. A quarentena por destinatário exige `<dest>` **presente** com CNPJ fora dos autorizados — mesma convenção do `Administrativo360EventosImutabilidadeTests` (dest CNPJ `99999999000199`). Fixture ajustado para passar `destCnpj` explícito (nulo ⇒ autorizado).

## 5. Dívida registrada (fora do escopo desta fatia)

1. **UI Web de prévia (passo prévia na tela):** as páginas de importação (`Views/Administrativo360/DocumentosXml/Importar.cshtml` e `Views/Cid/Importar.cshtml`) ainda chamam o confirmar direto; a API já expõe `POST …/xml/importar-preview` e `POST api/cid/prever-csv`, mas não há botão de "Prévia" que mostra o resultado antes de confirmar. Dívida UI (mesma categoria da B7), sem novas chaves i18n por enquanto.
2. Base principal em v2339: o lote de deploy segue incluindo **v2340–v2350** (invariado nesta fatia — não houve migration).

## 6. Fila seguinte (R8)

Pela ordem quebrado→alto impacto de `r8a0` §4, com §24 (importações) entregue: próximos itens = 5 calculadores de KPI sobrepostos → `cid_tabela` duplicada em 6 lugares. Alternativas: janela viva de homologação (`r6a1b` §7) ou a dívida UI de prévia (forma completa do passo na tela).
