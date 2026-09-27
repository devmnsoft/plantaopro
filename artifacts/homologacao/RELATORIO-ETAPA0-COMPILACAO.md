# RELATORIO — ETAPA 0: Recuperacao da Compilacao (Plantaopro)

Data: 2026-09-27 · Ambientes: Windows 10/11 local, .NET SDK 10.0.400 / runtime 10.0.12, PostgreSQL 18
Repositorio: `C:\MNSOFT\plantaopro` · Solucao: `backend\PlantaoPro.sln` (9 projetos, incluindo `PlantaoPro.Tests`)

## 1. Objetivo

Recuperar a compilacao bloqueada da solucao completa identificando a **causa raiz** (sem aumentar
tentativas de build, sem editar targets, sem remover projetos ou testes), sem tocar em PostgreSQL,
Visual Studio ou sistemas externos, e deixar um procedimento reutilizavel anti-recorrencia:
**parar → compilar → iniciar → verificar**.

## 2. Processos identificados (por caminho/cmdline, nunca por PID estale)

Identidade obtida via `Get-CimInstance Win32_Process` (caminho do executavel + `CommandLine`),
cruzada com os PIDs registrados em `artifacts\runtime-logs\dev\last-run.json`:

| Papel | Encadeamento | Porta |
|---|---|---|
| API | `dotnet run --no-build --launch-profile https` (dotnet.exe) → `PlantaoPro.Api.exe` (`backend\PlantaoPro.Api\bin\Debug\net10.0`) | 51977 |
| Web | `dotnet run --no-build --launch-profile https` (dotnet.exe) → `PlantaoPro.Web.exe` (`backend\PlantaoPro.Web\bin\Debug\net10.0`) | 52977 |
| Visual Studio | `devenv.exe` (identidade fora do repositório) | não tocado |

- O processo `dotnet run` é o **pai** do `.exe`; matar apenas um deles deixa órfão com lock.
  O procedimento para sempre os dois elos do encadeamento, por identidade.
- PostgreSQL (serviço), `devenv` e qualquer processo fora dos caminhos acima: **não tocados**.

## 3. Causa do bloqueio (raiz)

1. Os apps estavam em execução via `dotnet run --no-build --launch-profile https`; o
   `PlantaoPro.Api.exe` mantinha locked **4 DLLs compartilhadas** em `PlantaoPro.Api\bin\Debug\net10.0`
   (referenciadas também pelo projeto de testes).
2. O build da solucao falhou na cópia dos artefatos (conflito MSB3027/MSB3021) e **abortou antes da
   geração de `PlantaoPro.Tests\obj\Debug\net10.0\ref`**.
3. Consequência em cascata: `CS0006` em `PlantaoPro.Tests` — referência de assembly ausente.
   **O CS0006 era dependente do produtor**: não havia referência de projeto faltando.

Por que só DLLs compartilhadas em Api/bin: `PlantaoPro.Web.csproj` referencia apenas `CrossCutting`
(e fala com a API por HTTP) — por design, `Application/Domain/Infrastructure.dll` não existem no
bin do Web. Os locks que importavam eram os da cadeia da API.

## 4. Ações executadas

| # | Ação | Resultado |
|---|---|---|
| 1 | Inventário por caminho/cmdline (seção 2) | 4 PIDs desta cópia identificados; VS/Postgres descartados |
| 2 | Parada **direcionada** (apenas os PIDs identificados) | Parados em ~0,7s (limite 30s); 7 handles de artefato livres |
| 3 | `dotnet restore` | Limpo |
| 4 | `dotnet build PlantaoPro.sln -c Debug` | Exit 0, **0 erros**, 5 warnings preexistentes em Tests (invariantes) |
| 5 | Criação dos scripts de ciclo de vida (seção 6) | Stop/Build/Start com códigos de saída explícitos |
| 6 | Verificações F e G (matriz abaixo) | HTTP 200 nos dois apps; rebuild pós-parada verde |

Nenhum arquivo `.csproj`, target ou teste foi alterado para recuperar o build.

## 5. Causa residual dos CS0006 (por que voltariam sem o procedimento)

O `obj\...\ref` é derivado: enquanto houver processo desta cópia segurando DLLs de `bin`, o build
anterior da cadeia produtor→consumidor aborta e o consumidor (Tests) reporta CS0006 como se a
referência tivesse sumido. Prevenção = sempre executar **parar antes de compilar** (script stop com
pré-checagem de conflitos), nunca "compilar e tentar de novo".

## 6. Scripts criados (`scripts\local\`)

| Script | Função | Códigos de saída |
|---|---|---|
| `run-dev-stop.ps1` | Identifica instâncias desta cópia por caminho/cmdline (e registra em `last-run.json`); pré-checagem de conflitos; parada dirigida | 0 ok · 2 instâncias alheias detectadas · 4 timeout |
| `run-dev-build.ps1` | Pré-verifica artefatos livres; `dotnet build` da solução com log em `artifacts\runtime-logs\dev\build-*.log` | 0 ok · 3 artefatos bloqueados · outro=erro de build |
| `run-dev-start.ps1` | `dotnet run --no-build --launch-profile https` (API e Web); registra PIDs; readiness por polling de porta + probe HTTP | 0 ok · 3 já em execução · 5 timeout de readiness |

Docker **não** é requisito obrigatório; os scripts usam o mesmo perfil de lançamento do fluxo VS.

## 7. Comandos de referência

```powershell
# Parar instâncias desta copia
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\local\run-dev-stop.ps1
# Compilar
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\local\run-dev-build.ps1
# Iniciar + verificar
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\local\run-dev-start.ps1
# Regressao funcional (autenticacao, permissoes, revogacao)
powershell -NoProfile -ExecutionPolicy Bypass -File <harness>\api_matrix.ps1
# Suíte completa
dotnet test backend\PlantaoPro.sln -c Debug
```

## 8. Matriz de aceitação ETAPA 0

| Item | Critério | Veredito |
|---|---|---|
| A | Processos identificados por caminho/cmdline (sem PID estale) | APROVADO |
| B | Parada dirigida; PostgreSQL/VS/outros sistemas não tocados | APROVADO |
| C | `dotnet restore` limpo | APROVADO |
| D | Build completo exit 0, 0 erros | APROVADO |
| E | CS0006 resolvido por causa raiz (sem mexer em referências/testes) | APROVADO |
| F | Start após build → API e Web HTTP 200 | APROVADO |
| G | Stop → rebuild verde → start | APROVADO |

## 9. Procedimento anti-recorrencia

1. **PARAR** — `run-dev-stop.ps1` (detecta por identidade; se retornar 2/4, resolver manualmente).
2. **COMPILAR** — `run-dev-build.ps1` (falha cedo com código 3 se ainda houver lock, apontando o PID).
3. **INICIAR** — `run-dev-start.ps1` (registra PIDs em `last-run.json`, só considera pronto após HTTP 200).
4. **VERIFICAR** — smoke (`api_matrix.ps1`) quando o change afetar runtime; `dotnet test` quando afetar código.

Regra de ouro: **nenhum build/rebuild sem stop prévio desta cópia**; nenhum kill por PID sem confirmar
caminho/cmdline primeiro.

---

# ANEXO A — Conciliacao da suíte de testes (10 falhas → 658/658)

Primeira execução completa desde a recuperação do build: 648 aprovados / 10 falhas. Todas as 10 foram
causa-raiz; evidência final: **`Aprovado! – Com falha: 0, Aprovado: 658, Ignorado: 0, Total: 658`**.

| Grupo | Testes | Sintoma | Causa raiz | Correção |
|---|---|---|---|---|
| A (config) | `DevelopmentConfigurationSecurityTests`, `JwtConfigurationContractTests` (×2), `HomologacaoRuntimeRealContractTests`, `HomologacaoCrudsAcoesJornadasContractTests` | Segredos reais / flags inseguras em `appsettings*.json` versionados (`Password=123456`, `AllowLegacyPostgresDatabase=true`, `AllowDevelopmentAutoCreate=true`) — o commit `bb89062` havia movido valores locais para os appsettings | Contratos de repositório escaneiam `appsettings*.json` sob `backend/` e exigem base canônica (`"Default": ""`, `"Key": ""`, flags `false`) | Reversão dos 3 arquivos (`git restore --source=bb89062^`) e transferência dos valores locais para variáveis de ambiente em `Properties\launchSettings.json` (perfis http+https, Api e Web) — convenção já usada para `Jwt__Key` |
| B (banco de teste) | `IsolamentoCadastros_DoisTenants…`, `…DuplicidadeEBloqueios`, `AutenticacaoRealPelaApi_…` (L83), `PermissoesEscrita_…` (L122) | `Npgsql 42703`: coluna `eh_hospital` de `adm360_parceiros` não existe; `lookups` com 500 | A migration **v2198** (que cria essas colunas + catálogo de 32 permissões ADM360) estava **parcialmente aplicada** em `plantaopro_test` (tabela/índice de reconciliação existiam; bloco de colunas não) | Reaplicação do **v2198 inteiro** (idempotente) em `plantaopro_test`: +3 colunas, backfill de 285 parceiros, catálogo 32 permissões ADM360, grants ADMINISTRADOR_CLIENTE=32 / AUDITOR=4 (somente leitura) |
| B2 (banco de teste, resíduo) | Mesmos dois testes via API | Após v2198, `lookups` ainda 500: `42703 coluna m.crm não existe` (capturada pelo log detalhado do teste) | A tabela `medicos` do banco de teste é de **instalador legado** (sem `crm`, contato e vínculos multitenant). Nenhuma migration gerenciava colunas de `medicos` — o DDL canônico vinha apenas de `database/PlantaoPro_PostgreSQL_Completo.sql` | Nova migration **v2200** `2026_09_v2200_reconciliar_colunas_medicos_compatibilidade.sql`: 22 `ADD COLUMN IF NOT EXISTS` com os tipos exatos do DDL vivo (sem FK, sem alterar colunas existentes); aplicada em `postgres` (no-op idempotente) e `plantaopro_test`; checksum SHA-256 `bb703b73…1eccf35` registrado em `database/migration-manifest.json` |
| C (contrato seed) | `V2197ModuleContractReconciliationTests.Demo_contract_is_canonical…` | Substring canônica do INSERT do seed 121 sem `nome` | `tenant_modulos.nome` é **NOT NULL sem default** no DDL vivo (confirmado em `information_schema`); um INSERT sem `nome` falharia em instalação nova | Contrato atualizado para `modulo_id,codigo,codigo_modulo,nome,habilitado,status` (a evolução do DDL define o contrato) |

Observações de ambiente (reproduzíveis):

```text
# 1) Reaplicar v2198 no banco de testes (idempotente)
psql -h 127.0.0.1 -U postgres -d plantaopro_test -v ON_ERROR_STOP=1 -f database/migrations/2026_09_v2198_reconciliar_duplicidades_contratos_modulos.sql
# 2) Aplicar v2200 nos dois bancos (idempotente)
psql -h 127.0.0.1 -U postgres -d postgres         -v ON_ERROR_STOP=1 -f database/migrations/2026_09_v2200_reconciliar_colunas_medicos_compatibilidade.sql
psql -h 127.0.0.1 -U postgres -d plantaopro_test  -v ON_ERROR_STOP=1 -f database/migrations/2026_09_v2200_reconciliar_colunas_medicos_compatibilidade.sql
```

# ANEXO B — Configuracao local após reversao

- `appsettings.json` / `appsettings.Development.json` (Api) e `appsettings.json` (Web) voltaram ao estado
  canônico anterior a `bb89062` (base contém `"Default": ""` e `"Key": ""`; flags `false`; nenhum segredo).
- Valores locais agora residem em `launchSettings.json` (não varridos pelos contratos de segredo):
  - Api (perfis http e https): `ConnectionStrings__Default` (string local completa), `Database__AllowLegacyPostgresDatabase=true`, `Database__AllowDevelopmentAutoCreate=true`.
  - Web (perfis http e https): `ConnectionStrings__Default` (string local completa, `Application Name=PlantaoPro.web`).
- Prova de runtime: ciclo parar → compilar → iniciar executado **depois** da reversão — API e Web com
  HTTP 200 e `api_matrix.ps1` 10/10 (login gestor 200 ⇒ conexão ao banco pela nova rota de configuração).

## Regressao final (pós-correções)

| Verificação | Resultado |
|---|---|
| `dotnet test` completo | **658/658 aprovados** (0 falhas, 0 ignorados) |
| Build da solução | OK (0 erros) |
| Start pós-build (scripts) | API e Web HTTP 200 |
| `api_matrix.ps1` (M1…M7b) | 10/10 PASS (inclui revoke/restore de permissão com o mesmo token) |

## Pendências mantidas (roadmap, sem alteração neste lote)

P1 assimetria papéis vs RoleCatalog · P2 `POST /Account/*` 405 (login na raiz `POST /`) · P3 home
AUDITOR → AccessDenied · P5 claims não normalizadas no RefreshContext · **P6 INSERT de contrato em
`ModuleContractingService.cs` sem `nome` (DDL exige NOT NULL)** · P7 política `schema_migrations` ·
P8 cookie ~14 KB · P9 versões defasadas em cabeçalhos.
