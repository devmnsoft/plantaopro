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

## Provisionamento

Após a instalação canônica do banco, aplique explicitamente:

```bash
psql "$ConnectionStrings__Default" -v ON_ERROR_STOP=1 -f database/seeds/development/122_usuarios_homologacao_plantaopro.sql
```

O seed é idempotente e cria a Clínica Modelo PlantãoPro, o plano de homologação, módulos, unidade, especialidade, médico e vínculos de perfil. As senhas são persistidas somente como hashes **BCrypt cost 11**, o mesmo formato verificado por `BCrypt.Net.BCrypt.Verify` na API.

### Reset local das cinco senhas

Execute novamente o mesmo seed. Ele localiza as contas pelo e-mail normalizado,
substitui somente `senha_hash` pelos hashes BCrypt homologados, reativa os
vínculos e não duplica registros. O código fica em
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
