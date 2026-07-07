# Ordem oficial de banco, migrations e seeds

Status: **preparado para homologação local**.

A aplicação de banco local deve parar no primeiro erro e seguir exatamente esta ordem:

1. `database/PlantaoPro_PostgreSQL_Completo.sql`
2. `database/migrations/*.sql` em ordem alfabética
3. `database/seeds.sql`
4. `database/seeds/*.sql` em ordem alfabética

Use `scripts/database/apply-local-postgres.sh` ou `scripts/database/apply-local-postgres.ps1`. Ambos aceitam host, porta, banco, usuário e senha e exigem `psql` instalado.

Checklist mínimo de validação após aplicar:

- [ ] tabelas base no schema `plantaopro`;
- [ ] SaaS comercial;
- [ ] Saúde 360;
- [ ] plantões/escalas;
- [ ] financeiro;
- [ ] convênios e planos de saúde;
- [ ] auditoria e LGPD;
- [ ] seeds demo idempotentes.
