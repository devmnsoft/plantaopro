# Diagnóstico: por que o PlantãoPro não autentica

## Causa-raiz

O login é real e vai ao PostgreSQL. Não existe usuário hardcoded na Web/API.

Fluxo:

1. `Login.cshtml` faz `POST /Account/Login` com antiforgery.
2. `AccountController` chama `POST /api/auth/login`.
3. `AuthService.LoginAsync` busca em `plantaopro.usuarios` por `lower(email)` e valida `senha_hash` com BCrypt.
4. Só depois grava sessão e emite o cookie `PlantaoPro.Auth`.

`database/scrpt_completo.sql` e o instalador canônico **não inserem usuário**. A tabela nasce vazia. Sem linha prévia, a API devolve 401 ("Identificador ou senha inválidos").

O `DevelopmentSeed.cs` só roda com comando explícito `--provision-demo` em `Development` + `DemoSeed:Enabled=true` + senhas por variável de ambiente. Startup normal não cria contas.

## Outras causas frequentes

- Senha tentada (`123456`, `admin`, etc.) não confere com o hash BCrypt.
- Usuário inativo (`status <> ATIVO` ou `reg_status <> A`).
- Bloqueio por tentativas (`login_tentativas.bloqueado_ate`).
- JWT ausente (`Jwt__Key` com menos de 32 caracteres).
- Connection string apontando para outro banco.
- Query de login exige `clientes.reg_status`, `nome_fantasia`, `razao_social` e `medicos.cpf/usuario_id`. Schema canônico mínimo de `clientes` não tinha essas colunas; o seed de desenvolvimento agora as cria.
- Formulário Web: PRs #475, #476, #477 e #494 já corrigiram submit concorrente, `returnUrl` e contexto sem tenant.

## Correção aplicada

Script opt-in: `database/seeds/development/120_acesso_demo_local.sql`.

Contas locais:

- `superadmin@mnsoft.example` / `MnSoft!Demo2026#Admin` — `ADMINISTRADOR_GLOBAL`
- `gestor@santacasa-demo.example` / `SantaCasa!Demo2026#Gestor` — `ADMINISTRADOR_CLIENTE` na Santa Casa Demonstração

## Como validar

```sql
SELECT email, status, reg_status, left(senha_hash,7)
FROM plantaopro.usuarios
WHERE lower(email) LIKE '%@mnsoft.example'
   OR lower(email) LIKE '%@santacasa-demo.example';
```

Em seguida, Web em `/Account/Login` ou `POST /api/auth/login`.
