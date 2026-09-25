# Correção do login por contrato de módulos (v2.19.7)

## Causa raiz e contrato canônico

Instalações criadas por `database/schema/020_saas_tenants.sql` possuem a forma
legada de `tenant_modulos` (`codigo`, `nome`, `dados`, `criado_em`), enquanto o
login e a contratação usam `modulo_id`, `codigo_modulo`, `habilitado`, `status`,
`reg_status`, `reg_date` e `reg_update`. A primeira coluna ausente observada é
`modulo_id`, mas acrescentá-la isoladamente apenas desloca o erro para a coluna
seguinte.

O contrato canônico é: um acesso existe somente quando há uma linha de contrato
do tenant ligada por `modulo_id` a um módulo ativo de `modulos_sistema`, com
`tenant_modulos.reg_status='A'`, `habilitado=true` e `status='ATIVO'`. Códigos
legados são auxiliares de migração, não concedem acesso sem relacionamento.

## Ordem pelo pgAdmin Query Tool

Pré-requisitos: PostgreSQL 14+, backup confirmado, conexão no banco correto e
nenhuma execução concorrente do instalador.

1. Execute `database/diagnostics/tenant_modulos_readonly.sql`. Ele abre uma
   transação somente leitura e termina com `ROLLBACK`.
2. Abra `database/migrations/2026_09_v2197_reconciliar_contratos_modulos.sql`,
   envolva todo o conteúdo em `BEGIN;` e, no final da mesma execução, acrescente:

   ```sql
   insert into plantaopro.schema_migrations(id,script_path,checksum)
   values ('2026_09_v2197_reconciliar_contratos_modulos',
           'database/migrations/2026_09_v2197_reconciliar_contratos_modulos.sql',
           '71eb03eda2961fbaa29d80df37c0f6c7f846bd3472d7002684e894493aceb2a4')
   on conflict(id) do nothing;
   commit;
   ```

   Se a migration apontar duplicidade, relação órfã ou tenant ausente, faça
   `ROLLBACK`, corrija explicitamente os registros indicados e repita. Não
   registre a migration após erro. O instalador oficial faz essa mesma aplicação
   e conferência de checksum por `scripts/apply-canonical-migrations.sh upgrade`.
3. Em desenvolvimento/homologação, execute
   `database/seeds/development/121_acesso_demo_local.sql`.
4. Execute as migrations v2190–v2196 e depois
   `database/pgadmin/administrativo360_demo_completo.sql` para a massa completa.
5. Somente se a senha do gestor precisar ser restaurada, execute a operação
   restrita `database/seeds/development/121b_restaurar_senha_gestor_demo.sql`.

Depois valide que `tenant_modulos_reconciliacao` não tem ocorrências não
resolvidas para contratos que deveriam estar vigentes. Ocorrências não resolvidas
permanecem sem conceder módulo até revisão administrativa.

## Execução da aplicação

```bash
dotnet run --project backend/PlantaoPro.Api/PlantaoPro.Api.csproj
dotnet run --project backend/PlantaoPro.Web/PlantaoPro.Web.csproj
```

Use `gestor@santacasa-demo.example` / `SantaCasa!Demo2026#Gestor`. Um HTTP 500
de infraestrutura é exibido como indisponibilidade temporária com a referência
de correlação; 401 continua reservado a identificador ou senha inválidos.
