# R10-B1 — Writer da `adm360_ocorrencias` (spec A12)

- **Rodada:** 10 — fatia 1 de 4 (plano em `r10a0-auditoria-inicial-plano-sprint.md`)
- **Data:** 2026-10-16
- **Status:** IMPLEMENTADO + TESTADO (aceite por testes executados). HOMOLOGADO/LIBERADO seguem a janela viva.

## 1. O que estava quebrado / ausente

A tabela `plantaopro.adm360_ocorrencias` (migration `2026_09_v2191_administrativo360_suprimentos.sql` L18) existia **somente como DDL + seeds demo** — zero referência em código de produção antes desta fatia. A página Web `Administrativo360/Ocorrencias` (GET) apenas **sintetizava linhas virtuais** a partir de `api/administrativo360/qualidade/pendentes` (tipo fixo `QUARENTENA_RECEBIMENTO`, situação sempre `ABERTA`, prazo hoje+2): nada persistia. `BLOCO2_EXECUCAO.md` §Parcial confirmava: "os comandos completos de tratamento/liberação/encerramento não entraram neste incremento".

Spec A12 (`docs/administrativo360/PlantaoPro_Administrativo360_MVP_Requisitos_Prompts.md` L112): ciclo **Aberta → em tratamento → resolvida → encerrada**; "Encerrar exige destino dos itens e justificativa"; **"Registro de ocorrência não apaga estoque"**.

## 2. O que foi implementado

| Camada | Arquivo | Mudança |
|---|---|---|
| Domain | `backend/PlantaoPro.Domain/Administrativo360/OcorrenciasA12.cs` (novo) | Máquina de estados estrita (1 passo por vez, sem salto/retorno), validação de criação (tipo ≤ 40 chars, descrição, qtd > 0) e de encerramento (destino + justificativa obrigatórios) |
| Application | `backend/PlantaoPro.Application/Administrativo360/Contracts.cs` | `IOcorrenciasRepository` + records `CriarOcorrenciaCommand`, `VersaoOcorrenciaCommand`, `EncerrarOcorrenciaCommand`, `OcorrenciaResumo` (espelhável pelo Web via JSON camelCase) |
| Infrastructure | `backend/PlantaoPro.Infrastructure/Administrativo360/OcorrenciasRepository.cs` (novo) | `ListarAsync` (join produtos/lotes/locais, filtro por situação), `AbrirAsync` (checks de referência por tenant + INSERT), transições `TratarAsync`/`ResolverAsync`/`EncerrarAsync` com `SELECT ... FOR UPDATE` + UPDATE condicional em `versao` (409 em conflito), tudo dentro de `ExecutarComRetrySerializableAsync` (retry único 40P01/40001) |
| API | `Adm360SuprimentosControllers.cs` + `Program.cs` | `Ocorrencias360Controller` em `api/administrativo360/ocorrencias` com política **`Adm360.Qualidade`** (= `ADM360:LIBERAR_QUALIDADE`; estoquista puro não trata ocorrências — mesma segregação da BLOCO2). `GET` lista; `POST` cria; `POST {id}/tratar|resolver|encerrar` (409 com `message` fundamentado) |
| Web | `Administrativo360Controller.ComprasRecebimentos.cs` | `Ocorrencias()` passa a ler a **tabela real**; novos POSTs `CriarOcorrencia`, `TratarOcorrencia`, `ResolverOcorrencia`, `EncerrarOcorrencia` (validação no servidor Web antes do round-trip; mensagens via TempData) |
| Web view | `Views/Administrativo360/Ocorrencias.cshtml` (reescrita) | Tabela com os 4 estados + badge "Vencida" (prazo < hoje e ainda aberta/em tratamento), botões de comando por estado, formulário de abertura (produto→lote filtrado por JS, local INTERNO, tipo livre até 40, prazo opcional), link para fila de Inspeções mantido |
| i18n | `UiTexts.resx` ×4 (pt/en/es/fr) | +**45** chaves `Adm360.Occ.*`; contrato sobe **605 → 650** (`R7DF6Admin360I18nContratoTests`, método renomeado para `...650...`) + novo fact de completude `ViewsAdm360_R10b1OcorrenciasCompletudeSemLiterals` |
| Testes | `Administrativo360OcorrenciasA12Tests.cs` (novo) | 8 fatos / **17 casos**: matriz da máquina (9 transições), validações de criação/encerramento, ciclo completo feliz (ABERTA→…→ENCERRADA com destino/justificativa persistidos e versao 1→4), transição inválida sem efeito, versao desatualizada (409 no repo), encerrar sem campos rejeitado sem efeito, **estoque invariante** (contagem de `adm360_movimentos` idêntica antes/depois do ciclo), isolamento por tenant, validação de referências no abrir (produto/lote/local de outro tenant) |

## 3. Decisões registradas (sem regra inventada além da linha A12)

1. **`responsavel_id` vazio → autor da abertura.** Decisão registrada (a coluna é nula no DDL; a spec cita "responsável" entre os campos): o servidor grava o usuário que abriu. Não é regra nova de produto, é default de auditoria visível na API.
2. **`tipo` é texto livre** (≤ 40, coerente com `varchar(40)`); nenhum closed-set inventado — a seed demo usa `DOCUMENTACAO` e segue funcionando.
3. **Bloqueio imediato de saldo interno/externo** (última frase da linha A12) fica registrado como **P1 do A12**, fora desta fatia: as colunas `tipo`/`situacao` já suportam sem DDL, mas o escopo desta rodada é o writer + máquina de estados.
4. **Migração nenhuma**: DDL v2191 já cobre todos os campos usados.

## 4. Evidência executada

- **Build sln Release:** `BUILD_EXIT:0`, 0 erros / 0 avisos (Release completo re-validado na suíte full).
- **Testes da fatia:** `Administrativo360OcorrenciasA12Tests` + `R7DF6Admin360I18nContratoTests` = **24/24 aprovados** (17 A12: matriz da máquina 9 transições + validações + ciclo feliz + transição inválida + versao stale + encerrar sem campos + estoque invariante + isolamento por tenant + referências; 7 i18n: contrato 650 ×4 idiomas + valor canônico pt/en + completude sem literais da nova view).
- **Suíte full Release:** `Total de testes: 1416` (= 1398 baseline + 17 A12 + 1 completude), `Com falha: 1` — a única falha foi `ProvisionamentoB5Tests.P3_CorridaMesmoCnpj_ExatamenteUmVence_Outro409` (perdeu o assert de status 409 do perdedor sob concorrência de DB) — classe de flake **conhecida do catálogo ①** (métodos diferentes falharam em r8b13, r8b20, r9-dre, r9-financeiro; r9k-b14 já endureceu a idempotência pós-crash). **Reexecução isolada: 5× 11/11 aprovados, EXIT 0** (`r10b1_b5_isolate.bat`) — não-regressão. Nenhum teste Adm360/i18n/novo falhou em nenhuma execução.

## 5. Gotchas novos desta fatia

- **Dapper + record posicional com parâmetros nulos:** materializar `OcorrenciaResumo` (`Guid?`/`DateOnly?`/`string?`) direto via `QueryAsync<record>` falha com "matching signature required" — a assinatura exigida é derivada dos tipos das colunas (e o `created_at timestamptz` volta `DateTime`, não `DateTimeOffset`). Corrigido com o padrão da casa: POCO privada com setters (`OcorrenciaRow`, como `InspectionRow`/`EstadoRow`) + projeção para o record. Records Dapper-friendly seguem a regra `InspecaoPendente`: todos os parâmetros não-nulos e tipos exatos da coluna.
- **`Assert.DoesNotThrow` não existe neste xunit** (< 2.5) — usar chamada direta (exceção já falha o fato).
- **Fact de completude i18n vs. nome de chave:** proibição do literal `"Vencida"` esbarrava na chave `Adm360.Occ.Vencida` dentro da própria view — chaves cujo nome contém a palavra do valor PT devem ser excluídas da lista de literais proibidos.
- **Object initializer anônimo usa `=`**, não `:` (CS1003 cascata difícil de localizar quando os nomes casam com variáveis locais — o shorthand `new { a, b }` mascara o hábito errado).

## 6. Pendências desta fatia

- Navegação webview (login form + CRUD completo da tela) — se houver janela de homologação nesta rodada.
