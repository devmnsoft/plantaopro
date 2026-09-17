# Acesso local estabelecido

## Por que o login anterior falhava
- O instalador não cria usuário.
- `DevelopmentSeed` exigia comando `--provision-demo` + senhas em config; `appsettings.Development.json` não tinha `DemoSeed`.
- A connection string padrão usa `Database=postgres`. O seed `120` recusava esse banco.
- Sem linha em `plantaopro.usuarios`, a API devolve 401.

## O que passou a valer
- `appsettings.Development.json` da API habilita `DemoSeed` com as três senhas locais.
- Startup Development com `AutoProvisionIfEmpty` cria/resetas as contas usando o mesmo BCrypt da API.
- Banco `postgres` legado é aceito se o schema existir.
- Seed SQL `121_acesso_demo_local.sql` cobre o mesmo conjunto, inclusive médico fictício.

## Contas
- `superadmin@mnsoft.example` / `MnSoft!Demo2026#Admin`
- `gestor@santacasa-demo.example` / `SantaCasa!Demo2026#Gestor`
- `medico@santacasa-demo.example` / `Medico!Demo2026#Acesso`
