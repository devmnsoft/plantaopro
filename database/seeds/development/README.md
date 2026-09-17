# Seeds de desenvolvimento

Este diretório é deliberadamente opt-in. O instalador canônico nunca cria administrador com senha conhecida, pacientes, prontuários ou dados clínicos fictícios.

## Por que o login falha após instalar o banco

`database/scrpt_completo.sql` e o instalador oficial criam só o schema (tabelas `usuarios`, `perfis`, índices). A tabela `plantaopro.usuarios` nasce vazia. O `AuthService` consulta PostgreSQL por `lower(email)` e valida `senha_hash` com BCrypt — sem linha prévia, qualquer tentativa devolve 401.

Há duas formas oficiais de criar as contas locais:

1. CLI da API (Development + `DemoSeed__Enabled=true`):

```bash
ASPNETCORE_ENVIRONMENT=Development \
DemoSeed__Enabled=true \
DemoSeed__DevelopmentDatabase=plantaopro \
DemoSeed__SuperAdminPassword='MnSoft!Demo2026#Admin' \
DemoSeed__ManagerPassword='SantaCasa!Demo2026#Gestor' \
ConnectionStrings__Default='Host=127.0.0.1;Port=5432;Database=plantaopro;Username=postgres;Password=<senha-local>' \
  dotnet run --project backend/PlantaoPro.Api -- --provision-demo
```

2. Script SQL opt-in (pgAdmin / psql), no banco da aplicação:

```bash
psql -d plantaopro -f database/seeds/development/120_acesso_demo_local.sql
```

## Contas de demonstração (somente desenvolvimento)

| Perfil | E-mail | Senha |
|---|---|---|
| Super administrador | `superadmin@mnsoft.example` | `MnSoft!Demo2026#Admin` |
| Administrador do cliente (Santa Casa Demonstração) | `gestor@santacasa-demo.example` | `SantaCasa!Demo2026#Gestor` |

Essas senhas são fixtures de laboratório. Não use em produção. Troque depois do primeiro acesso.

## Diagnóstico rápido no banco

```sql
SELECT email, status, reg_status, left(senha_hash,7) AS hash_prefix, bloqueado_ate
FROM plantaopro.usuarios
WHERE lower(email) IN ('superadmin@mnsoft.example','gestor@santacasa-demo.example');
```

`hash_prefix` precisa começar com `$2a$` ou `$2y$`. Se a consulta não retornar linhas, o seed não rodou.
