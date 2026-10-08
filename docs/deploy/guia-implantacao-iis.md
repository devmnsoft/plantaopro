# Guia de implantação IIS (Windows) — PlantãoPro

Aplicável a: rodadas de homologação e produção sobre a base `c84afc5`+ (net10.0, ASP.NET Core Module V2 **inprocess** via `web.config` gerado pelo SDK no publish — o arquivo **não existe no repositório**, procure-o na saída publicada).

> **Atualização 2026-10-08 (R5-A2):** as telas fiscais do ADM360 no Web viraram BFF fino — o site **Web não conecta mais no banco** em tela alguma; a API fiscal (`api/administrativo360/fiscal`, autorização por ação) é o único caminho de escrita fiscal. `ConnectionStrings__Default` sai do pool Web e `Fiscal__Credenciais__{referencia}` passa para o pool da API (§5). A linhagem de migrations termina em **v2322** (grants ADM360 → `ADMINISTRADOR_CLIENTE`; próxima livre: v2323) — **superada pelas rodadas B4–B6 abaixo**.

> **Atualização 2026-10-08 (R5-B4–B6):** a linhagem termina agora em **v2332** (matriz comercial: v2323–v2327; provisionamento/convites: v2328–v2331; cobrança SaaS em sandbox: **v2332**; próxima livre: v2333). `Cobranca__Credenciais__{PROVEDOR}` (ex.: `Cobranca__Credenciais__SANDBOX`) vive **só no pool da API** — o BFF Web nunca vê o segredo; a assinatura do webhook é validada na API por HMAC-SHA256 do corpo bruto (`X-Cobranca-Signature: sha256=<hex>`, tempo constante). O provider `SANDBOX` vem semeado (modo SANDBOX, ATIVO) e os purges de implantação **não devem removê-lo**; sem a credencial configurada, cobrar devolve 503 honesto (nunca "sucesso"). Para produção: linha nova em `cobranca_providers` (`modo='PRODUCAO'`, `status='ATIVO'`), a credencial homônima no pool da API e o conector do provedor externo implementado (pendência conhecida — o sandbox é o estado atual).

## 1. Arquitetura definida (2 sites, 2 pools)

| Item | Valor | Motivo |
|---|---|---|
| Site público | `plantao-web` → `C:\inetpub\plantao\Web` | MVC/Razor (BFF) que o usuário acessa |
| Binding público | `https/+:443:<host>` (+ `http/+:80:` para redirecionamento) | `Program.cs` usa `UseHttpsRedirection` |
| Site interno | `plantao-api` → `C:\inetpub\plantao\API` | Rotas do código já começam com `api/...`; montar a API como aplicação `/api` dentro do site do Web geraria `/api/api/...` sem código extra |
| Binding da API | somente `http/127.0.0.1:8197:` | API não é exposta ao mundo: o BFF Web é a única entrada. Porta interna escolhida livremente, **nunca** uma porta de dev |
| Pools | `PlantaoProWebAppPool`, `PlantaoProApiAppPool` | Identidade própria (`IIS AppPool\...`), reciclagem independente |
| CLR dos pools | `No Managed Code` (`managedRuntimeVersion:none`) | Exigência do ASP.NET Core Module V2 inprocess |

### Regras obrigatórias
- **Portas de desenvolvimento (51976/51977/52976/52977) ficam proibidas em Production** — agora validadas no startup do Web (`PlantaoProApiStartupValidator`): se `PlantaoProApi:BaseUrl` ainda apontar para elas fora de Development/Testing, o site falha rápido com mensagem clara em vez de quebrar no primeiro login.
- **Não execute DLLs/`dotnet run` em paralelo ao IIS como solução permanente.** Se um processo dev ocupa a porta do binding, o sintoma clássico é "port 5000 already in use"/binding recusa. Verifique com `netstat -ano | findstr <porta>` antes de culpar o IIS.
- **Não force `ASPNETCORE_URLS`** no IIS inprocess para "resolver" porta: o binding do site manda. `ASPNETCORE_URLS` só existe se houver motivo explícito e documentado.
- **Segredos nunca entram no repositório**: `Jwt__Key`, `ConnectionStrings__Default`, chaves de IA por variáveis de ambiente do site/aplicação (ou `appsettings.Production.json` local não versionado). Precedência real: **variáveis de ambiente > `appsettings.{Ambiente}.json` > `appsettings.json`**.
- **Precedência da BaseUrl do HttpClient do Web** (documentada e travada por teste): `ApiSettings:BaseUrl` > `PlantaoProApi:BaseUrl`; recomenda-se usar **apenas** `PlantaoProApi__BaseUrl` no IIS para não haver duas chaves disputando.

## 2. Pré-requisitos
1. **ASP.NET Core Runtime 10.0.x + Hosting Bundle** (IIS Role Service com ASP.NET; módulo v2 incluído pelo bundle). Versão deve ser ≥ a runtime publicada nos `runtimeconfig.json` dos projetos (`net10.0`).
2. **PostgreSQL instalado/atualizado** no banco de destino: execute `Tools.Database install` (instalação limpa) ou upgrade representativo (ver roteiro de homologação); a API valida esquema/colunas essenciais no startup e falha rápido com mensagem humana por SQLSTATE.
3. **Certificado TLS**: para homologação, autossinado no servidor; para produção, certificado real vinculado ao host.
   ```powershell
   # Homologação (uma vez, no servidor):
   New-SelfSignedCertificate -DnsName "plantao.local" -CertStoreLocation cert:\LocalMachine\My -NotAfter (Get-Date).AddYears(1)
   ```
4. Pasta de dados: `C:\ProgramData\PlantaoPro\DataProtection` criada com escrita para as identidades dos pools (seção 7).

## 3. Publicação

Use o script versionado (publica os dois projetos em Release e remove os `appsettings` de ambiente específico da saída):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\local\publish-iis.ps1 -OutDir C:\inetpub\plantao
```

Por que remover `appsettings.Development.json`/`appsettings.Testing.json` do output? O SDK Web inclui **todo** `appsettings*.json` na publicação e ambos os projetos versionam `appsettings.Development.json`. A evidência da rodada 4 (saídas em `Desktop\plantaopro*`) mostrou credencial real de banco + `DemoSeed.Enabled=true` + `ApiSettings:BaseUrl=http://localhost:51976` embarcados: se o pool subir com `ASPNETCORE_ENVIRONMENT=Development` por engano, esses arquivos reativam DemoSeed e o endpoint de teste de login. Na publicação de produção/aceite eles são excluídos do diretório.

## 4. Sites e pools (comandos)

Com `appcmd` como Administrador (equivalente na GUI: Gerenciador do IIS → Sites/Pools):

```bat
set APPCMD=C:\Windows\System32\inetsrv\appcmd

%APPCMD% add apppool /name:"PlantaoProWebAppPool" /managedRuntimeVersion:none
%APPCMD% add apppool /name:"PlantaoProApiAppPool" /managedRuntimeVersion:none

:: API interna (apenas loopback)
%APPCMD% add site /name:"plantao-api" /id.:"" /bindings:"http/127.0.0.1:8197:"
%APPCMD% add app /site.name:"plantao-api" /path:"/" /physicalPath:"C:\inetpub\plantao\API" /queue:"PlantaoProApiAppPool"

:: Web pública (ajuste host/hash do certificado conforme o ambiente)
%APPCMD% add site /name:"plantao-web" /id.:"" /bindings:"http/+:80:,https/+:443:plantao.local:<HASH_DO_CERT>"
%APPCMD% add app /site.name:"plantao-web" /path:"/" /physicalPath:"C:\inetpub\plantao\Web" /queue:"PlantaoProWebAppPool"

%APPCMD% start apppool /name:"PlantaoProWebAppPool"
%APPCMD% start apppool /name:"PlantaoProApiAppPool"
```

Diagnóstico inicial: habilite `stdoutlogEnabled` nas duas aplicações (GUI: Aplicação → Configurações avançadas → Habilitar log de saída; ou `web.config` em `<aspNetCore processPath... stdoutLogFile=".\logs\stdout">`). Desabilite após a estabilização.

## 5. Variáveis de ambiente (por site/aplicação)

GUI: IIS → Aplicação → Configuração → Variáveis de ambiente. Ou:

```bat
%APPCMD% set config "plantao-api" -section:applicationSettings -+[name="ASPNETCORE_ENVIRONMENT"].value:"Production"
:: ... idem para cada chave abaixo
```

**Site `plantao-api` (pool API):**

| Variável | Valor | Observação |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Obrigatório; com ela as validações fail-fast do startup (DB, JWT, schema) ficam ativas |
| `ConnectionStrings__Default` | string real do PostgreSQL | Segredo; nunca versionar. A API valida presença + abertura real + tabelas/colunas essenciais no startup |
| `Jwt__Key` | 32+ caracteres | Validado no startup (`JwtConfigurationValidator`); não usar o placeholder de dev |
| `Jwt__Issuer` | ex.: `PlantaoPro` | Validado no startup |
| `Jwt__Audience` | ex.: `PlantaoPro` | Validado no startup |
| `Ai__EncryptionKey` | opcional; **exatamente 64 caracteres hex** (32 bytes; validado em `AiSecretProtector` — hex malformado/curto é tratado como ausente) | Cifra (AES-GCM) as chaves de provider salvas por tenant. Sem ele a IA funciona com providers configurados no servidor, mas **salvar chave por cliente** falha com mensagem clara; não quebra o startup |
| `Ai__Providers__Groq__ApiKey`, `Ai__Providers__Gemini__ApiKey`, `Ai__Providers__DeepSeek__ApiKey` | opcionais | Sem chave de um provider, aquele provider fica indisponível; a homologação de inferência real externa só é declarada com chave real |
| `Fiscal__Credenciais__{referencia}` | valor segredo (ex.: `Fiscal__Credenciais__nfe-santacasa-demo`) | **R5-A2:** o segredo fiscal agora é lido **pela API** (único caminho de escrita fiscal; regra A33: o banco guarda apenas o **nome** da referência, nunca o valor). Sem a chave, a emissão segue bloqueada com motivo real (nunca "sucesso") |
| `Cobranca__Credenciais__{PROVEDOR}` | valor segredo (ex.: `Cobranca__Credenciais__SANDBOX`) | **R5-B6:** chave HMAC do webhook do provedor de cobrança, lida **só pela API** (mesma regra A33: o banco guarda só o código do provider). Sem ela, gerar cobrança/webhook responde **503** honesto ("credencial não configurada"); em produção trocar para o provedor real mantendo segredo por código |

**Site `plantao-web` (pool Web):**

| Variável | Valor | Observação |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Idem |
| `PlantaoProApi__BaseUrl` | `http://127.0.0.1:8197/` | URL interna da API no loopback (HTTP local entre apps do próprio servidor). Porta de dev → startup falha |
| `DataProtection__KeysDirectory` | `C:\ProgramData\PlantaoPro\DataProtection` | Obrigatório em Production: sem persistir as chaves, cookies/sessões morrem a cada reciclagem e não funcionam entre instâncias |
| `ConnectionStrings__Default` | (remover) | **R5-A2:** o Web voltou a não acessar banco em tela alguma (BFF fino — a exceção fiscal do R4-F2 foi removida); a variável deve ser **removida** do pool Web. Sem ela, telas fiscais com 500/"ConnectionStrings" indicam deploy anterior ao R5-A2 |

A partir do R5-A2 o Web voltou a não usar banco em tela alguma (BFF fino); a exceção fiscal do R4-F2 foi removida e o segredo fiscal passou para o pool da API (linhas acima).

## 6. Sessões e Data Protection
- **Chaves de Data Protection**: persistidas em `DataProtection__KeysDirectory` quando configurado; em Production o diretório é obrigatório e o validador exige que exista e seja gravável pela identidade do pool (mensagem clara caso contrário). O mesmo diretório deve ser usado por **todas as instâncias** do Web.
- **Store de sessão é em memória**: em **uma única instância** do Web isso é suficiente (sobrevive à reciclagem por causa das chaves persistidas). Para múltiplas instâncias: sticky sessions no load balancer ou externalizar o store de sessão antes de escalar horizontalmente — o guia não resolve por mágica o que a hospedagem exige.
- Cookie de autenticação: `PlantaoPro.Auth`, HttpOnly, SameSite=Lax, expiração deslizante de 8h (configurados no código).

## 7. Permissões de pastas

Para `IIS AppPool\PlantaoProWebAppPool` e `IIS AppPool\PlantaoProApiAppPool`:

| Pasta | Permissão |
|---|---|
| `C:\inetpub\plantao\Web`, `C:\inetpub\plantao\API` | Leitura (execução do binário inprocess não precisa de escrita) |
| `C:\inetpub\plantao\*\logs` (stdoutLog/app) | Escrita |
| `C:\ProgramData\PlantaoPro\DataProtection` | **Escrita** (ambos os pools) |
| `C:\inetpub\logs\LogFiles\plantao-web`, `...\plantao-api` | Escrita (geridas pelo IIS) |

Exemplo PowerShell:
```powershell
foreach ($p in @("C:\inetpub\plantao","C:\ProgramData\PlantaoPro\DataProtection")) {
  icacls $p /grant "IIS AppPool\PlantaoProWebAppPool:(OI)(CI)M" /grant "IIS AppPool\PlantaoProApiAppPool:(OI)(CI)M" | Out-Null
}
```

## 8. Checklist de regras (revisão rápida)
- [ ] Hosting Bundle/runtime 10.0.x instalado; pools em "No Managed Code".
- [ ] API só no loopback (binding `127.0.0.1:8197`), Web em 443/80.
- [ ] Nenhuma porta de dev em uso em Production (validada no startup).
- [ ] `ASPNETCORE_ENVIRONMENT=Production` nos dois sites; nenhum `appsettings.Development.json` no diretório publicado.
- [ ] Segredos por variável de ambiente; nada impresso nos logs de startup além de host/porta/banco (validadores não mostram valores).
- [ ] Data Protection persistido em diretório compartilhado com escrita.
- [ ] Sem `dotnet run`/DLL manual ocupando porta em paralelo (confirme com `netstat`).
- [ ] stdout log ligado durante a implantação, desligado depois.
- [ ] Fiscal (R5-A2): upgrade terminou em **v2322**; `Fiscal__Credenciais__{referencia}` só no pool da API; sem `ConnectionStrings__Default` no pool Web.
- [ ] Cobrança SaaS (R5-B6): upgrade terminou em **v2332**; `Cobranca__Credenciais__SANDBOX` (e do provedor futuro) só no pool da API; provider `SANDBOX` ATIVO presente em `cobranca_providers` (seed v2332 — não remover); expor o webhook `POST /api/cobranca/webhooks/{provedor}` ao provedor externo com a assinatura `X-Cobranca-Signature` (sem cartão/segredo em log).

## 9. Roteiro de publicação e validação (execução ponta a ponta)

Execute em ordem; cada passo tem o resultado esperado. Qualquer passo que falhe **bloqueia** o próximo.

**P0 — Pré-condições (no servidor)**
1. `netstat -ano | findstr :8197` e `:443` → nenhuma surpresa de processo dev (PIDs de `dotnet.exe` em execução manual devem estar ausentes).
2. Banco atualizado: `Tools.Database` install/upgrade executado contra o banco de destino; `psql` confirma o último migration aplicado — na linhagem atual (R5-B6) o upgrade deve terminar em **v2332** (`2026_10_v2332_b6_cobranca_saas.sql`; próxima versão livre: v2333).
3. `powershell -File scripts\local\publish-iis.ps1 -OutDir C:\inetpub\plantao` → saída termina com "Etapas seguintes" e sem erro.

**P1 — API sobe**
4. Inicie/reicle o pool `PlantaoProApiAppPool`.
5. `Invoke-RestMethod http://127.0.0.1:8197/api/health` → `success=true`, `data.status="Healthy"`, `message="PlantaoPro.Api online"`.
   - Falha de startup aparece como 5xx + mensagem clara (ex.: "Configuração Jwt:Key não encontrada...", "ConnectionStrings:Default não configurada.", erro de schema por SQLSTATE). Leia `logs\stdout` se o site não responder.

**P2 — API → banco**
6. `Invoke-RestMethod http://127.0.0.1:8197/api/health/db` → `data.database="Connected"`.
7. `Invoke-RestMethod http://127.0.0.1:8197/api/health/system` → `data.status` em `SAUDÁVEL`/`DEGRADADO` (Storage não configurado degrada de forma declarada, não é indisponibilidade) — componente **Banco: DISPONÍVEL**.
8. `Invoke-RestMethod http://127.0.0.1:8197/api/health/auth` → 200 (schema ok, JWT ok, admin global presente).

**P3 — Web sobe e alcança a API**
9. `Invoke-WebRequest https://<host>/Account/Login` → 200 (HTML do formulário de login).
10. Login real no navegador com credencial de gestão do ambiente (`<email-gestor>`): sucesso → painel autenticado; verifique nos logs do pool Web a linha `HttpClient PlantaoProApi configurado com BaseUrl: http://127.0.0.1:8197/`.

**P4 — Operação autorizada + persistência**
11. Com sessão autenticada, execute uma operação de escrita autorizada no ADM360 (ex.: criar/editar cadastro simples no módulo contratado do tenant de teste).
12. Confirme no banco (`psql`) a linha nova com `tenant_id` correto:
    ```sql
    SELECT max(created_at) FROM plantaopro.adm360_produtos WHERE tenant_id='<tenant-de-teste>';
    ```
    (tabela conforme a operação executada; o importante é registro novo com o tenant certo).

**P5 — Erro humano de API indisponível**
13. `appcmd stop site /name:"plantao-api"`; tente novamente um **login novo** no navegador.
    - Esperado: mensagem **"Não foi possível conectar ao serviço de autenticação. Tente novamente em instantes."** — nunca "Identificador ou senha inválidos" quando a falha é de rede.
14. `appcmd start site /name:"plantao-api"`; login volta a funcionar.

**P6 — Reciclagem preserva a sessão (Data Protection)**
15. Mantenha a aba autenticada aberta; `recycle` o pool `PlantaoProWebAppPool` (`appcmd recycle apppool ...` ou GUI).
    - Esperado: a sessão continua válida após a reciclagem (cookie `PlantaoPro.Auth` permanece íntegro porque as chaves estão em `C:\ProgramData\PlantaoPro\DataProtection`).
16. Repita com o pool da API: sessão do usuário não é afetada (JWT é revalidado no próximo BFF call; o login não cai).

**P7 — Telas fiscais do ADM360 (opcional; só com o tenant de teste com módulo ADM360 contratado)**
17. Login no navegador com credencial de gestão do tenant (ex.: `gestor@santacasa-demo.example` em homologação local) → dentro do ADM360 as telas de fiscal **aparecem** (gate `ADM360`).
18. "Configurar" e "Notas" abrem sem erro 5xx; parâmetros do tenant demonstram estado real (ex.: CONFIGURADO em homologação).
19. "Nova pré-emissão": criar um pré-documento interno → conferência de referências roda e persiste; a lista mostra o registro novo.
20. Tentar **emissão**: resposta com motivo real (`PENDENTE_DE_CONFIGURACAO` enquanto faltar operação/UF/provedor; ou credencial ausente se `Fiscal__Credenciais__{referencia}` não estiver no pool da **API**; ou 400 honesto "conector não integrado (P1)" quando parâmetros/credencial estão ok mas não há transmissor — sem mudar a situação). Nunca "sucesso" falso (A29/L33/H19) — emissão autorizada é escopo P1 pós-credenciais reais.

**P8 — Limpeza pós-validação**
21. Desligar stdout log; conferir permissões e que nenhum arquivo `*.log` cresça sem limite; registrar resultados (passo → esperado → obtido) em `docs\evidencias\<data>\<ambiente>-iis.md`.

## 10. Rollback
- Antes de sobrescrever: `xcopy C:\inetpub\plantao Web previous-<timestamp>\Web /E /I` (idem API).
- Rollback: reverter o `physicalPath` dos sites para a snapshot anterior e reciclar os pools. O banco **não** volta com rollback de código: migrations aplicados permanecem (desejável: versões são aditivas).

## 11. Solução de problemas (sintoma → causa provável → ação)
| Sintoma | Causa provável | Ação |
|---|---|---|
| Startup falha: "porta de desenvolvimento" | `PlantaoProApi:BaseUrl` ainda apontando p/ 5197x/5297x | Definir `PlantaoProApi__BaseUrl=http://127.0.0.1:8197/` no pool Web |
| Startup API falha: "Jwt:Key não encontrada..." | Chave ausente/curta no pool API | `Jwt__Key` (32+ chars), `Jwt__Issuer`, `Jwt__Audience` |
| Startup API falha com SQLSTATE | Banco/schema atrasado | Rodar `Tools.Database` upgrade; reler mensagem (ela nomeia o objeto) |
| "port 5000 already in use" no binding | Processo dev ocupando porta ou binding mal definido | `netstat -ano`; encerrar `dotnet.exe` residual; revisar bindings do site |
| Login sempre diz "conectar ao serviço" e API responde direto | Binding da API não está em 127.0.0.1:8197 ou pool parado | Testar `curl http://127.0.0.1:8197/api/health` no próprio servidor |
| Sessão cai a cada reciclagem | `DataProtection__KeysDirectory` ausente/sem escrita | Corrigir variável + permissão (seção 7) |
| Telas fiscais do ADM360 dão 500/"ConnectionStrings:Default..." | Deploy anterior ao R5-A2 no Web (a exceção fiscal com banco no Web foi removida) ou API sem banco/grants | Republicar o Web no R5-A2+ (sem `ConnectionStrings__Default` no pool Web); na API, conferir `ConnectionStrings__Default`, upgrade até **v2322** e `Fiscal__Credenciais__{referencia}` no pool da API se a falha for de credencial |
| Cobrar fatura SaaS dá **503** ("credencial não configurada") | Falta `Cobranca__Credenciais__{PROVEDOR}` no pool da **API**, ou o provider está ausente/INATIVO em `cobranca_providers` (instalação nova semeia `SANDBOX` ATIVO na v2332) | Configurar o segredo por código no pool da API; conferir `select codigo, modo, status from plantaopro.cobranca_providers`. Obs.: repostar o mesmo evento de webhook responde `DUPLICADO` 200 por design (índice único é o árbitro do dedupe) — não é falha |
| "Identificador ou senha inválidos" com senha correta | API está de fato recusando (não é rede) | Ver `api_error_logs`/logs da API; validar conta/perfil ativo no banco |

## 12. Relações
- JWT local/CI/IIS: `docs/configuracao-jwt-local-ci-iis.md`
- Banco local: `docs/deploy/execucao-local-postgresql.md`
- Roteiros de homologação/produção existentes: `docs/deploy/deploy-homologacao.md`, `docs/deploy/deploy-producao-controlada.md`
- IA pronta para chaves reais (providers, fallbacks, probe): `docs/ia/pronto-para-chaves.md`; doc fiscal F1+F2: `docs/evidencias/2026-10-06-rodada-4/r4i-fiscal.md`
- Validação de configuração implementada nesta rodada: `backend/PlantaoPro.Web/Services/Security/PlantaoProApiStartupValidator.cs` + `DatabaseStartupReadinessValidator`/`ConnectionStringStartupValidator`/`JwtConfigurationValidator` (API)
