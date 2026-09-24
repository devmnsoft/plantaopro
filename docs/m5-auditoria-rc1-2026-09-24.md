# M5 — auditoria de SaaS e preparação da RC1

Data da execução: 2026-09-24. Baseline: branch `work`, commit inicial
`305fd54`. Este documento registra evidência técnica, não homologação e não
autoriza publicação em produção.

## Resultado executivo

M5 **não está concluído**. A revisão corrigiu duas fronteiras críticas da
administração SaaS: alterações no catálogo de planos agora são exclusivas do
Administrador Global e registram o ator real; a aprovação de módulos volta a
validar dependências e recusa catálogos cíclicos. A compilação, os testes de
integração com PostgreSQL, a restauração isolada e a validação visual não foram
executados porque a imagem desta rodada não contém `dotnet`, `docker` nem
`psql`.

Consequentemente, **A25 (gerar a RC1) não está liberada**. M6 permanece como a
etapa posterior de homologação formal, aceite e piloto.

## Pré-requisitos e baseline

| Dependência | Estado | Evidência e limite |
|---|---|---|
| M1 — banco, login e execução integrada | Não verificado | Há scripts e artefatos anteriores, mas não são prova desta execução; PostgreSQL e runtime indisponíveis. |
| M2 — responsabilidades, contexto e autorização | Parcial | Inspeção confirmou serviços de contexto e testes; foi corrigida escrita de plano que ainda admitia Administrador de tenant. |
| M3 — plantão até pagamento | Não verificado | Componentes e testes existem; regressão integrada não executada nesta rodada. |
| M4 — paciente até recebimento | Não verificado | Componentes e testes existem; regressão integrada não executada nesta rodada. |
| Build e testes .NET | Bloqueado | `dotnet test PlantaoPro.sln --no-restore --verbosity minimal`: `dotnet` ausente. |
| Persistência e concorrência reais | Bloqueado | `docker`, `psql` e servidor PostgreSQL não estão disponíveis. |

O repositório estava limpo no início. Nenhuma migration aplicada ou dado
existente foi alterado nesta rodada.

## Componentes canônicos confirmados

Não foram criados serviços, tabelas ou telas concorrentes. Os pontos canônicos
encontrados são:

| Capacidade | Implementação canônica |
|---|---|
| Núcleo SaaS, planos, clientes e módulos | `backend/PlantaoPro.Api/SaasCoreServices.cs` |
| Autoatendimento, onboarding e white label | `backend/PlantaoPro.Api/SelfServiceServices.cs` |
| Solicitação/aprovação de módulos | `backend/PlantaoPro.Api/ModuleContractingService.cs` e `Controllers/ModuleContractingController.cs` |
| Operação comercial B2B | `backend/PlantaoPro.Api/B2BCommercialOpsServices.cs` |
| Planos e administração comercial | `backend/PlantaoPro.Api/Controllers/SaasCommercialController.cs` |
| BI e integrações | `backend/PlantaoPro.Api/Controllers/Fase6BiIntegracoesController.cs` |
| Menu por autorização | `backend/PlantaoPro.Web/Services/Security/MenuBuilderService.cs` |
| Catálogo de features da Web | `backend/PlantaoPro.Web/Services/FeatureCatalogService.cs` |
| Composição visual SaaS | `backend/PlantaoPro.Web/Views/Shared/SaasComercialPage.cshtml` |
| Relatórios | `ReportServices.cs`, `OperationalReportService.cs` e controllers `Relatorios*` |
| Instalação e upgrade | `database/instalar_plantaopro.psql`, manifests e `scripts/apply-canonical-migrations.sh` |

### Fluxos alterados

1. Administração de plano: tela/consumidor → `/api/planos` → autorização global
   específica na ação → `PlanosController` → SQL parametrizado → auditoria com
   `UserId` original → resposta → nova consulta.
2. Contratação de módulo: portal → revisão → contexto autenticado do tenant →
   snapshot comercial → solicitação transacional/idempotente → fila global →
   aprovação com lock e revalidação de dependências → contrato → releitura.

Os contratos operacionais permanecem nos serviços de escalas/plantões e os de
convênio nos serviços clínico-financeiros; nenhum DTO universal foi introduzido.

## Correções de comportamento

- As ações de criar, editar, ativar/inativar e alterar recursos de plano têm
  autorização adicional exclusiva de Administrador Global. As consultas
  continuam compatíveis com os consumidores existentes.
- A auditoria dessas escritas recebe o usuário autenticado, em vez de gravar
  ator nulo.
- Antes de aprovar uma solicitação, o serviço consulta novamente o catálogo na
  mesma transação. Pré-requisito externo precisa de contrato ativo e habilitado;
  um item da própria solicitação também satisfaz a composição.
- A revisão rejeita ciclos alcançáveis no grafo de dependências. Não há ativação
  automática durante a solicitação e preços, carências, SLAs ou rateios não
  foram inventados.

## Estado de A20–A24

| Atividade | Estado | Resultado desta rodada / pendência |
|---|---|---|
| A20 — contratos | Parcial | Fronteiras canônicas mapeadas e preservadas; falta integração real completa dos três domínios. |
| A21 — administração e portal | Parcial | Escrita de planos e aprovação endurecidas; jornadas completas ainda exigem PostgreSQL e navegação. |
| A22 — interface e navegação | Não verificado | Nenhuma tela foi alterada; smoke responsivo/teclado permanece obrigatório. |
| A23 — indicadores, relatórios e exportações | Não verificado | Origens localizadas, mas fixture e equivalência tela/arquivo não foram executadas. |
| A24 — operação e recuperação | Bloqueado | Procedimento canônico abaixo; restauração real requer ferramentas ausentes. |

## Matriz de aceite da execução

`Comprovado` exige execução; contrato estático isolado não promove um caso a
aprovado.

| Testes | Estado | Observação |
|---|---|---|
| T05, T06 | Parcial | Regras de solicitação separada, lock, idempotência e revalidação estão implementadas; concorrência real não executada. |
| T03, T13 | Parcial | Escrita de planos é global e ator é auditado; suíte integrada não executada. |
| T01–T04, T07–T12, T14–T20, T22–T23 | Não verificado | Dependem de build/API/PostgreSQL reais nesta revisão. |
| T21 | Bloqueado | Sem `docker`/`psql`; nenhuma restauração foi alegada. |
| T24 | Não verificado | Não houve mudança visual nem servidor executável para navegação. |

## Procedimento obrigatório de recuperação (ambiente isolado)

1. Registrar commit, versão do PostgreSQL, horário inicial e checksums do dump e
   dos arquivos persistentes. Nunca usar o banco de produção como destino.
2. Criar uma instância PostgreSQL 16 vazia e credenciais temporárias por variável
   de ambiente; não registrar os valores no relatório.
3. Restaurar o dump com `pg_restore --exit-on-error --clean --if-exists` (formato
   custom) ou `psql -v ON_ERROR_STOP=1` (SQL), conforme o formato efetivo.
4. Aplicar somente migrations canônicas posteriores via
   `scripts/apply-canonical-migrations.sh`; validar manifests e checksums.
5. Restaurar os arquivos persistentes necessários em diretório isolado, com
   proprietário e permissões equivalentes, e apontar a aplicação de homologação.
6. Iniciar API e Web do mesmo commit; validar health de processo e prontidão do
   banco separadamente, sem expor conexão ou tokens.
7. Executar login, leitura do tenant, solicitação/aprovação de módulo, consulta de
   fatura, uma operação de plantão e uma consulta clínica autorizada.
8. Comparar contagens e totais sanitizados antes/depois, registrar início/fim,
   dependências, erros e dados recuperados; destruir o ambiente temporário.

Tempo observado e dados recuperados: **não medidos**, pois a restauração ficou
bloqueada. Não se declara RPO, RTO ou SLA.

## Gate para A25

A25 somente poderá ser liberada após: build limpo; suíte automatizada aprovada;
T01–T24 executados no que lhes couber com PostgreSQL real; instalação limpa,
upgrade e reaplicação validados; backup/restore isolado comprovado; smoke Web em
desktop/celular/teclado; e revisão dos indicadores/exportações com fixture
conhecida. Até lá, o resultado é candidato técnico incompleto, não RC1 e não
produto pronto para produção.
