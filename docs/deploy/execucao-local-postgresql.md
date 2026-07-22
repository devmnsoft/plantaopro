# PostgreSQL local

Banco padrão: `plantaopro`.

Connection string exemplo sem segredo real:

```text
Host=localhost;Port=5432;Database=plantaopro;Username=<usuario>;Password=<senha-local>;Search Path=plantaopro
```

Não use `Database=postgres` para desenvolvimento normal. O app falha rapidamente se essa base legada for usada em Development sem a opção explícita `Database:AllowLegacyPostgresDatabase=true`.
