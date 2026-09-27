# Segredos locais e rotação de credenciais (desenvolvimento)

Última atualização: 2026-09-27 (BLOCO A — entrega Administrativo 360).

## 1. Mecanismo oficial para desenvolvimento local

Os valores privados de desenvolvimento vivem em **user-secrets por projeto**:

| Projeto | `UserSecretsId` | Chaves do store |
|---|---|---|
| `backend\PlantaoPro.Api` | `plantaopro-api-development` | `ConnectionStrings:Default` · `Jwt:Key` · `Jwt:Issuer` · `Jwt:Audience` · `Database:AllowLegacyPostgresDatabase` · `Database:AllowDevelopmentAutoCreate` |
| `backend\PlantaoPro.Web` | `plantaopro-web-development` | `ConnectionStrings:Default` |

Regras:

- Os `launchSettings.json` **não** versionam credenciais: apenas URLs, `launchUrl` e
  `ASPNETCORE_ENVIRONMENT`. A precedência do .NET é *variáveis de ambiente do processo*
  (incluindo as injetadas pelo launchSettings) > user-secrets > `appsettings.*.json`,
  por isso o segredo tem que sair do launchSettings para o user-secrets valer.
- As chaves do store usam **`:`** como separador de seção (`ConnectionStrings:Default`,
  `Jwt:Key`). O formato `Nome__SubItem` só é convertido em hierarquia quando a origem é
  **variável de ambiente**; no `secrets.json` (JSON plano) o `__` ficaria literal e a chave
  não seria encontrada (`GetConnectionString("Default")` → null).
- Em Windows o SDK .NET 10 grava o store em
  `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json` (SDKs anteriores usavam
  `%LOCALAPPDATA%`). O arquivo é legível em texto puro nesta máquina de dev — nunca
  versionar.
- O `setup-local-config.ps1` é idempotente: só cria chaves ausentes; `-Force` sobrescreve
  tudo e gera nova `Jwt:Key`. Ele imprime apenas NOMES, nunca valores.

```powershell
# configuração local (rodar uma vez, ou após rotação)
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\local\setup-local-config.ps1
```

## 2. Credenciais que ficaram versionadas no histórico e precisam de rotação

Estes valores aparecem em commits anteriores do Git (principalmente nos `launchSettings.json`
versionados até o commit `bb89062`). Trate-os como **comprometidos** para fins de ambiente local:

| Credencial | Onde apareceu | Estado da rotação |
|---|---|---|
| `PLANTAOPRO_LOCAL_DEV_JWT_KEY_2026_CHANGE_ME_64_CHARS` | `backend\PlantaoPro.Api\Properties\launchSettings.json` (ambos os perfis) | **Rotacionada**: removida do launchSettings; o primeiro `setup-local-config.ps1` gera uma `Jwt:Key` aleatória de 64 chars no user-secrets. |
| Senha de banco `123456` (usuário `postgres`, `127.0.0.1:5432`) | `launchSettings.json` da API e da Web; exemplos de instalação | **Pendente decisão de rotação** (ver §3). A senha segue sendo o default do `setup-local-config.ps1`; se for trocada no Postgres local, repasse `-DbPassword` ou defina `PLANTAOPRO_DEV_DB_PASSWORD`. |

> O Git não teve histórico reescrito (regra do projeto). As credenciais continuam visíveis
> no histórico de commits; a rotação do valor ativo é o que garante o isolamento.

## 3. Procedimento de rotação

### 3.1 Rotação da `Jwt:Key` (API)

```powershell
# opção A: via script (gera chave nova de 64 chars e reescreve as chaves mantendo as atuais)
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\local\setup-local-config.ps1 -Force
```

Efeitos:

- Todos os tokens JWT emitidos antes da rotação deixam de validar → usuários locais fazem
  login novamente.
- Sessões Web ativas também exigem novo login (o cookie é assinado/validado com a mesma chave).
- Nenhum dado do banco é afetado (a senha de login dos usuários é BCrypt, independente da key).

Manualmente, sem `-Force` (mantém ConnectionStrings):

```powershell
$bytes = New-Object byte[] 48
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$novaKey = [Convert]::ToBase64String($bytes)   # 64 caracteres
dotnet user-secrets set "Jwt:Key" $novaKey --project backend\PlantaoPro.Api\PlantaoPro.Api.csproj
# reinicie a API (parar -> compilar nao e necessario -> iniciar)
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\local\run-dev-stop.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\local\run-dev-start.ps1 -SkipBuildGate
```

### 3.2 Rotação da senha do Postgres local

1. Altere a senha no Postgres (ex.: `\alter user postgres PASSWORD '<nova>'`).
2. Atualize o user-secrets de **ambos** os projetos:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File scripts\local\setup-local-config.ps1 -DbPassword '<nova>' -Force
   ```

   (ou defina `PLANTAOPRO_DEV_DB_PASSWORD` no ambiente antes de rodar o script.)
3. Pare e reinicie o ciclo dev (stop → build não necessário → start).
4. Se outras ferramentas locais usam a mesma senha (psql, pgAdmin, harness de testes),
   atualize-as junto — o script de probe psql usa `PGPASSWORD`.

### 3.3 Rotação na instalação de homologação/produção local (`.local\plantaopro.env`)

O caminho oficial `scripts\database\install-plantaopro.ps1` gera uma `Jwt__Key` base64
(64 bytes) e grava `.local\plantaopro.env` (diretório fora do Git, via `.gitignore`). Para
rotacionar: apague a variável do env e refaça a instalação, ou edite o valor manualmente e
reinicie o serviço com o novo env.

## 4. Verificação rápida pós-rotação

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\local\run-dev-stop.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\local\run-dev-build.ps1   # opcional se nao houve cambio de codigo
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\local\run-dev-start.ps1
# esperado: exit 0, readiness API /api/health -> Healthy e Web /Account/Login -> form 200
```

Depois, faça login no navegador com o usuário demo e confirme que o fluxo de ADM360 carrega.
