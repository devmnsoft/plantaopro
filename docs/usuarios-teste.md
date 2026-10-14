# Usuários de teste PlantãoPro

**Ambiente:** local/homologação.

> **Atenção:** esses usuários são apenas para ambiente local/homologação. Não usar em produção.

| Perfil | Login | Senha |
|---|---|---|
| Super Admin | `superadmin@plantaopro.local` | `Super@123456` |
| Administrador Cliente | `admin.clinica@plantaopro.local` | `Cliente@123456` |
| Médico | `medico@plantaopro.local` | `Medico@123456` |
| Recepção | `recepcao@plantaopro.local` | `Recepcao@123456` |
| Financeiro | `financeiro@plantaopro.local` | `Financeiro@123456` |
| Sistema Vertical (acesso de TESTE) | `admin.sistemavertical@plantaopro.local` | `Sv-Homolog-2026!` |

## Provisionamento

Após a instalação canônica do banco, aplique explicitamente:

```bash
psql "$ConnectionStrings__Default" -v ON_ERROR_STOP=1 -f database/seeds/development/122_usuarios_homologacao_plantaopro.sql
```

O seed é idempotente e cria a Clínica Modelo PlantãoPro, um segundo tenant sintético sem usuários para testes de isolamento, o plano de homologação, módulos, unidade, especialidade, médico e vínculos de perfil. As senhas são persistidas somente como hashes **BCrypt cost 11**, o mesmo formato verificado por `BCrypt.Net.BCrypt.Verify` na API. Ao reaplicar, o seed preserva a senha das contas já existentes.

### Tenant "Sistema Vertical" (seed 123)

Acesso de **teste** dedicado à homologação do SaaS (cliente SaaS separado dos clientes comerciais do Administrativo 360):

```bash
psql "$ConnectionStrings__Default" -v ON_ERROR_STOP=1 -f database/seeds/development/123_sistema_vertical_plantaopro.sql
```

Idempotente: cria tenant/cliente/próprio, perfil `ADMINISTRADOR` escopado ao próprio tenant e o usuário de teste, reutilizando o plano `HOMOLOGACAO_360`; resolve os contratos de módulo por `modulo_id`/`codigo_modulo` (fonte canônica `plantaopro.modulos_efetivos()`) e escopa as permissões aos módulos contratados. Reaplicar preserva a senha existente; detalhes e provas de execução em `docs/evidencias/2026-10-14-rodada-9/r4-sistema-vertical-provas.md`.

### Reset local das cinco senhas

O reset é uma operação distinta do provisionamento. Execute explicitamente:

```bash
psql "$ConnectionStrings__Default" -v ON_ERROR_STOP=1 \
  -v demo_environment=Development \
  -v confirm_demo_password_reset=RESET_DEMO_PASSWORDS \
  -f database/seeds/development/reset_usuarios_homologacao_plantaopro.sql
```

O script recusa ambientes diferentes de `Development`/`Test`, exige a
confirmação literal, exige que as cinco contas já existam, redefine os hashes
atomicamente e grava auditoria sem senha. O seed de provisionamento localiza as contas pelo e-mail normalizado,
preserva os dados de acesso existentes e não duplica registros.
Antes de atribuir o perfil canônico, o seed
inativa vínculos residuais dessas cinco identidades. Ao final, valida que cada
conta possui exatamente um perfil ativo e o tenant esperado; qualquer
divergência aborta a transação inteira. O código fica em
`database/seeds/development/122_usuarios_homologacao_plantaopro.sql`.

### Validação automatizada

```bash
dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj --filter HomologationUsersSeedContractTests
```

O serviço único de geração e verificação é
`backend/PlantaoPro.Api/Security/PasswordHashService.cs`. O salt aleatório e o
fator de trabalho ficam embutidos no próprio hash BCrypt; não existe coluna de
salt separada.

## Segurança

- Nunca habilite esse seed no instalador ou pipeline de produção.
- Troque ou remova as contas ao promover uma cópia de banco.
- Não copie as senhas desta página para configuração, código-fonte ou logs.
