# Ordem de migrations e seeds

1. Scripts base em `database/PlantaoPro_PostgreSQL_Completo.sql` quando aplicável.
2. Migrations SaaS core.
3. Migrations Saúde 360 base clínica.
4. Migrations módulos clínicos, CID, prescrições, financeiro, convênios e planos.
5. Migrations LGPD, jornada, observabilidade e white label.
6. Seeds idempotentes de demonstração.

Regras: não apagar dados, usar `CREATE TABLE IF NOT EXISTS`, constraints via blocos `DO $$` quando necessário e seeds após tabelas existentes.
