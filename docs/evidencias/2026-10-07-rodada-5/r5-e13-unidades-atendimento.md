# R5-E13/P1 — Rota de escrita de unidades de atendimento (ONB_SD_UNIDADE → 12/12)

Data: 2026-10-09. Contexto: sequência pós-R5 executando o backlog P1 da matriz de aceite
(`r5-entrega-matriz-aceitacao.md` §3). Defeito medido em D11: `clinica_unidades_atendimento`
só tinha leitura via lookups legados; sem POST real, `ONB_SD_UNIDADE` era injogável em
self-service e travava `ONB_REVISAO` (matriz parava em 10/12 por razão honesta).

## 1. O que foi implementado (6 camadas, zero paralelo de módulos)

| Camada | Arquivo | Mudança |
|---|---|---|
| SQL/kernel | `backend/PlantaoPro.Api/Saude360ClinicalService.cs` | `Tables["unidadesAtendimento"] = clinica_unidades_atendimento`; `BuildInsert` grava **as duas chaves de escopo** (`cliente_id` pela convenção do kernel + `tenant_id` da origem SaaS), `status='ATIVO'`, auditoria `created_by`; `BuildUpdate` escopado (`nome`/`status`, tenant-safe); `ValidateCreate` exige nome |
| API | `backend/PlantaoPro.Api/Controllers/Saude360ClinicalControllers.cs` | `ClinicaUnidadesController` em `api/unidades-atendimento` (GET/lista/get-by-id/POST/PUT/inativar/reativar) com gate de contrato `[Saude360Module]` + role assistencial |
| Guard Web | `backend/PlantaoPro.Web/Services/Security/SaasRouteGuardFilter.cs` | `["ClinicaUnidades"] = "SAUDE360"` (módulo grosso já em contrato e nos grants v2334). O código fino `UNIDADES` não existe em `modulos_sistema` — mapear para ele hoje daria `CATALOGO_NAO_CONFIGURADO`; a decisão de granularidade E13 permanece aberta e o guard documenta isso |
| BFF | `backend/PlantaoPro.Web/Controllers/Saude360WebControllers.cs` | `ClinicaUnidadesController : Saude360WebControllerBase` (Index/Create/Edit; `Salvar` herdado — antiforgery + POST na API real) |
| Views | `backend/PlantaoPro.Web/Views/ClinicaUnidades/{Index,Create,Edit}.cshtml` | Wrappers finos sobre `~/Views/Saude360/Modulo.cshtml` e `Formulario.cshtml` (mesmo padrão PlanosSaude) |
| Navegação | `backend/PlantaoPro.Web/Views/Shared/_AppSidebar.cshtml` | Item "Unidades clínica" gated por `Can("SAUDE360")` — mesmos códigos do guard de rota |
| Jornada | `backend/PlantaoPro.Api/OnboardingJornadaService.cs` | `PRIMEIRA_UNIDADE_SAUDE` aceita escopo duplo `(tenant_id=@tenantId or cliente_id=@clienteId)` — mesma correção da homologação D11 para pacientes/triagem |

Contrato travado por teste: `backend/PlantaoPro.Tests/OnboardingE13UnidadesContractTests.cs`
(4 Facts: insert dual-key SQL, rota+gate da API, mapeamento guard+BFF+views, avaliador dual-key).

## 2. Suite automatizada (hosts down)

- `dotnet build` limpo (0 erros; warnings pré-existentes).
- 2 execuções completas: **1174/1175** cada, com **testes faltantes diferentes** entre si
  (`P6_Convite...` e `B5_AtivarAgendados...`) e **ambos aprovados isoladamente** (11/11 e 12/12) —
  flakiness conhecida de banco compartilhado sob paralelismo xUnit, não regressão. Os 4 testes
  novos (apenas leitura de fonte, sem banco) passaram em todas as execuções. Total: 1171 → 1175.

## 3. Loop ao vivo (hosts 51976/52976, banco `plantaopro_test`)

Rota de evidência: login gestor demo pelo BFF (claims do contrato SAUDE360 de D9) e fluxo real
de página, sem tocar banco para criar o dado:

```
WEB_LOGIN=302
UNIDADES_INDEX=200                      # /ClinicaUnidades/Index passa no guard (estado vazio honesto)
UNIDADE_SAVE=200 url=.../ClinicaUnidades/Index   # POST /ClinicaUnidades/Salvar (antiforgery) -> Index
NA_LISTA=True                           # unidade aparece na listagem paginada real
REAVALIAR=302
ONB_REVISAO     |CONCLUIDO|AUTOMATICO|Todas as etapas obrigatorias verificadas em dados reais.
ONB_SD_PACIENTE |CONCLUIDO|AUTOMATICO|Paciente cadastrado no tenant.
ONB_SD_TRIAGEM  |CONCLUIDO|AUTOMATICO|Triagem registrada no fluxo assistido.
ONB_SD_UNIDADE  |CONCLUIDO|AUTOMATICO|Unidade de atendimento cadastrada.
12|12
```

Persistência real conferida no banco (POST API isolado, mesmo caminho do BFF): linha
`Santa Casa Central - Matriz` com `tenant_id=d3f6584c-…7502` **e** `cliente_id=…7501`,
`status=ATIVO`, `created_by` do gestor — as duas chaves gravadas desde o primeiro dia.

`ONB_REVISAO` fechou sozinha como derivacao correta: antes do POST o proprio avaliador declarou
"Faltam 1 etapa(s) obrigatoria(s): Cadastrar a unidade de atendimento." (gate honesto provado no
meio do loop); depois virou CONCLUIDO/AUTOMATICO sem nenhum clique de conclusao manual.

Nota de processo: um primeiro POST manual sem os hidden fields do formulario generico
(`ApiEndpoint/Controller/Action`) devolveu 500 no caminho de re-renderizacao de erro do BFF
(comportamento pre-existente identico em todos os controllers genericos do kernel); o fluxo real
com formulario completo devolve 200. Registrado como observacao, nao alterado.

## 4. Limites conhecidos (sem maquiagem)

- Lookups `api/lookups/unidades` e `agendamentos.unidade_id` continuam apontando para a tabela
  legado `plantaopro.unidades`; unificar o FK de agendamento para `clinica_unidades_atendimento`
  depende da decisao de granularidade/escopo E13 e fica no backlog.
- Codigo fino `UNIDADES` em `modulos_sistema` (e expansao por contrato) segue aguardando a
  decisao de produto; ate la o gate correto e o contrato grosso SAUDE360.
- Matriz final da jornada demo: **12/12 CONCLUIDO, todos AUTOMATICO, zero estado sem dado**.
