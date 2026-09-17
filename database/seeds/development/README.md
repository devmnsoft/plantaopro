# Seeds de desenvolvimento

Este diretório é deliberadamente opt-in. O instalador canônico nunca cria administrador com senha conhecida em produção.

Em Development a API agora auto-provisiona as contas se `DemoSeed:Enabled` e `DemoSeed:AutoProvisionIfEmpty` estiverem ativos.

## Contas locais (somente Development)

| Perfil | E-mail | Senha |
|---|---|---|
| Super administrador | `superadmin@mnsoft.example` | `MnSoft!Demo2026#Admin` |
| Gestor da Santa Casa Demonstração | `gestor@santacasa-demo.example` | `SantaCasa!Demo2026#Gestor` |
| Médica fictícia | `medico@santacasa-demo.example` | `Medico!Demo2026#Acesso` |

## SQL manual

```bash
psql -d plantaopro -f database/seeds/development/121_acesso_demo_local.sql
```

Se a API estiver no banco `postgres` legado, execute o mesmo arquivo nesse banco — desde que o schema `plantaopro` exista.

## Diagnóstico

```sql
SELECT email, status, reg_status, left(senha_hash,7) AS hash_prefix
FROM plantaopro.usuarios
WHERE lower(email) IN (
  'superadmin@mnsoft.example',
  'gestor@santacasa-demo.example',
  'medico@santacasa-demo.example'
);
```
