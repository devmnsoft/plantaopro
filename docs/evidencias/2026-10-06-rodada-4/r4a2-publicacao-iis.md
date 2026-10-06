# R4-A2 — Publicação/IIS: configuração de implantação, validação de startup e roteiro

Data: 2026-10-06 · Base: `c84afc5` + commit de auditoria `cdd517b` · Máquina: sem IIS instalado (validação viva fica no roteiro executável do guia).

## 1. Raízes causais confirmadas (estático, sobre o código publicado)

| Erro observado | Causa raiz | Evidência |
|---|---|---|
| Web → "connection refused" em `https://localhost:51977/` | `PlantaoProApi:BaseUrl` fixado em porta **dev** no `appsettings.json` versionado; produção não sobrescrevia por variável | `backend/PlantaoPro.Web/appsettings.json:9`; precedência real `ApiSettings:BaseUrl` > `PlantaoProApi:BaseUrl` em `Program.cs` (hoje centralizada no `PlantaoProApiStartupValidator.ResolveBaseUrl`) |
| API cai/comportamento inseguro com `Jwt:Key` ausente | Não existia fail-fast antes desta base; a base `c84afc5` já trazia `JwtConfigurationValidator` (32+ chars) e `DatabaseStartupReadinessValidator` (schema/colunas, mensagens por SQLSTATE) + `ConnectionStringStartupValidator` | `backend/PlantaoPro.Api/Program.cs:33,104`; contrato travado em `JwtConfigurationContractTests` |
| Web ocupando porta 5000 | Sintoma de binding/`ASPNETCORE_URLS` mal definido ou processo dev paralelo; `web.config` publicado usa AspNetCoreModuleV2 inprocess (binding do site manda) | `C:\Users\NCELL-DEV-020\Desktop\plantaopro\web.config` (saída publicada inspecionada na D1) |
| Segredos/créditos reais nas saídas publicadas | O SDK Web embarca **todo** `appsettings*.json` na publicação e os projetos versionam `appsettings.Development.json` (`DemoSeed.Enabled=true`, `ApiSettings:BaseUrl=http://localhost:51976`, credencial de banco em claro) — sob `ASPNETCORE_ENVIRONMENT=Development` no pool, reativam DemoSeed e TestSignin | Saídas `Desktop\plantaopro\appsettings*.json` e `Desktop\plantaopro.api\appsettings*.json` (D1) |

## 2. Implementado nesta rodada

1. **`backend/PlantaoPro.Web/Services/Security/PlantaoProApiStartupValidator.cs`** (novo): fail-fast no startup do Web com mensagens pt-BR claras, **sem segredos**:
   - BaseUrl obrigatória, URL absoluta http/https; precedência oficial `ApiSettings:BaseUrl` > `PlantaoProApi:BaseUrl` documentada e única via `ResolveBaseUrl`;
   - portas de dev (51976/51977/52976/52977) **rejeitadas fora de Development/Testing** (Testing isenta porque a fábrica de testes herda a BaseUrl versionada antes do override);
   - `DataProtection:KeysDirectory` obrigatório em Production (existente + gravável via probe de escrita); em Development cria se informado e ausente.
2. **`backend/PlantaoPro.Web/Program.cs`**: chamada de validação logo após `CreateBuilder`; HttpClient `"PlantaoProApi"` usa o resolver oficial; registro `AddDataProtection().PersistKeysToFileSystem` quando o diretório é configurado (persistência de cookies/sessões entre reciclagens/instâncias).
3. **`scripts/local/publish-iis.ps1`** (novo): publica API+Web em Release para `C:\inetpub\plantao\{Web,API}` e **remove** `appsettings.Development.json`/`appsettings.Testing.json` da saída; imprime as etapas seguintes.
4. **`docs/deploy/guia-implantacao-iis.md`** (novo): arquitetura 2 sites/2 pools (Web pública 443/80; API interna só loopback `http://127.0.0.1:8197/`), pools "No Managed Code", comandos `appcmd`, matriz de variáveis de ambiente por site (segredos fora do repositório; Web **não** conecta direto no banco), permissões de pastas, regras (sem porta de dev em produção, sem DLL/dotnet run paralelo, sem brigar binding com `ASPNETCORE_URLS`), sessão em memória × Data Protection persistido, **roteiro de validação P0–P7** (API→health→health/db→system→auth→Web→login→operação autorizada→persistência no banco→erro humano com API parada→reciclagem preservando sessão), rollback e tabela sintoma→causa.

## 3. Erro humano de API indisponível (login)

Já existia e foi reconfirmado por execução (suíte): falha de rede/timeout no login devolve **"Não foi possível conectar ao serviço de autenticação. Tente novamente em instantes."** (ou mensagem de timeout) — nunca "Identificador ou senha inválidos" para falha de conexão (`AccountController.HandleApiConnectionFailure`; contrato em `V2164CompilationAndJourneyContractTests`). Sem aumento de timeout (15s mantido). Passos P5 do roteiro cobrem a verificação viva.

## 4. Evidências por execução

- Testes novos `ImplantacaoPublicacaoConfigValidationTests`: **20/20 aprovados** (precedência, ausência/formato/esquema de URL, 4 portas de dev × Production, aceitação em Development/Testing, chaves DP em Production sem diretório/inexistente/existente, criação em Dev, contrato completo do `Validate`).
- Suíte canônica completa: **930/930 aprovados** (base anterior 910 + 20 novos) — `dotnet test backend\PlantaoPro.Tests\PlantaoPro.Tests.csproj --nologo -v q`.
- Build Release dos dois projetos executado dentro da suíte (compile limpo, apenas warnings pré-existentes de nullability em código anterior).

## 5. Status e pendências

- **Status técnico: implementado e testado por execução nesta máquina** (unidade + build + suíte canônica). **Homologação viva em IIS: NÃO EXECUTADA aqui** (IIS ausente nesta máquina) → executar roteiro P0–P7 no ambiente de homologação e anexar `docs\evidencias\<data>\<ambiente>-iis.md`.
- Backlog decorrente:
  1. `appsettings.Development.json` versionado nos dois projetos (mantido p/ dev local; mitigado no publish — avaliar mover para user-secrets em todas as máquinas dev);
  2. saídas publicadas em `Desktop\plantaopro*` ainda carregam credencial real do banco (republicar com o script e apagar cópias antigas antes da homologação);
  3. `ConnectionStringStartupValidator` imprime host/porta/banco via `Console.WriteLine` (não é segredo, mas foge do logging estruturado — alinhar quando houver oportunidade);
  4. múltiplas instâncias do Web exigem sticky sessions ou store externo de sessão (documentado no guia; decisão de hospedagem).
