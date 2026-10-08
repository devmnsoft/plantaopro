# R5-A2 — Fiscal honesto: BFF fino + ENVIANDO só com conector real

Data: 2026-10-08. Escopo: eliminar o sucesso fictício do `Emitir` (PRONTA→ENVIANDO
sem transmissão) e tirar o banco fiscal do Web.

## O que mudou

- **API fiscal nova** (`api/administrativo360/fiscal`): `Administrativo360FiscalService`
  + `Administrativo360FiscalController` — único caminho de escrita das pré-notas e
  parâmetros. Políticas novas por ação: `Adm360.Configurar/Criar/Editar/Reabrir/
  Confirmar/Cancelar` (auditor passa só nas leituras `Adm360.Ver`).
- **Semântica honesta do Emitir**: bloqueio de parâmetros → 200
  `{Avancou=false, Mensagem}` (sem mudar estado); parâmetros ok mas sem conector
  → 400 honesto via `NotaPreEmitidaRegras.ValidarTransmissorDisponivel`
  ("conector do provedor X ainda não está integrado (escopo P1)"); catálogo
  `FiscalTransmissorCatalogo` vazio até o P1 (singleton DI). Falha técnica do
  conector desfaz ENVIANDO→PRONTA (sem estado órfão) e propaga o erro.
- **Web virou BFF fino**: `Adm360FiscalWebController` só chama a API via
  `CreateApiClient`/`AddBearerToken` + tuplas `SendApiAsync/ReadApiResponseAsync`
  (mensagem real da API chega à UI via `TempData`). Rotas/roles/nomes de ação
  preservados. Credencial A33 agora é lida **pela API**
  (`Fiscal:Credenciais:{ref}` saiu do appsettings do Web para o da API).
- **Guard por ação**: `SaasRouteGuardFilter.PermissionActionOverrides`
  (`"Controller/Action"`, antes do switch genérico que mandava `Cancelar→EXCLUIR`).
  Fiscal: GETs→VER, SalvarConfiguracao→CONFIGURAR, SalvarNota→CRIAR,
  MarcarPronta→EDITAR, Emitir→CONFIRMAR, Reabrir→REABRIR, CancelarNota→CANCELAR.
  XML real mapeado junto (IMPORTAR_XML/CONFERIR/MANIFESTAR_DFE/
  VINCULAR_DOCUMENTOS/CRIAR/CONFIRMAR).
- **Banco**: migration `v2322` (grants dos 13 códigos ADM360 aos perfis ativos
  `ADMINISTRADOR_CLIENTE`, dedupe determinístico ponto/dois-pontos + not-exists;
  aplicada em `plantaopro_test`: 13 códigos × 2 perfis, sem duplicatas) + seed
  `GarantirPerfilAdminClienteAsync` estendido + entrada no `migration-manifest.json`.
- **Views**: banner P1 em Detalhes ("pré-nota preparada — emissão autorizada
  INDISPONÍVEL: conector X não integrado") + badges de credencial/conector em
  Configurar; texto de ajuda da credencial aponta para o pool da API.
- **Guia IIS**: Web sem `ConnectionStrings__Default` e sem `Fiscal:*` (remover do
  pool Web); `Fiscal__Credenciais__{ref}` passa ao pool da API; linhagem v2322.

## Verificação

- `dotnet test`: **1115/1115** (1106 base + 9 novos em
  `Administrativo360R5A2FiscalEmissaoTests`).
- Novos testes: unidade (`ValidarTransmissorDisponivel` ×3 + catálogo ×3, sem DB,
  coleção `A360Transmissao`) + web-bff ×4 (AUDITOR POST Emitir → 302 AccessDenied
  com 0 chamadas à API; AUDITOR GET Notas → 200 via API; ADMIN Emitir sem conector
  → motivo honesto + banner P1, sem "transmitida"; ADMIN Cancelar → motivo chega
  à API + toast de confirmação).
- Smoke dev (API 51976 + Web 52977, `plantaopro_test`, gestor Santa Casa):
  health ok, login real, `GET parametros/notas` ok; Emitir em nota PRONTA devolve
  400 honesto sem mudar a situação (quando há nota PRONTA; sem escrita de teste).

## Fica para depois (fora da máquina)

- P1: primeiro `IFiscalTransmissor` real (Sefaz/provedor) + credenciais reais (P2
  comercial). Com o catálogo vazio, emissão autorizada segue impossível — por
  desenho, não por defeito.
- Backlog R4 herdado (flakes Aceite17/Aceite12, IDENTITY_SCHEMA_INCOMPLETE, DateOnly
  Npgsql 10 etc. — ver `r4j-matriz-homologacao.md` §5).
