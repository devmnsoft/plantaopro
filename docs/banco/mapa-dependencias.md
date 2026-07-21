# Mapa de dependências do banco completo

Ordem efetiva no `scrpt_completo.sql`:

1. Extensões: `pgcrypto`, `unaccent`.
2. Schema: `plantaopro` e `search_path`.
3. Controle/SaaS base: `planos` antes de qualquer referência SaaS.
4. Clientes e tenants.
5. Usuários, roles, perfis, módulos, ações e permissões.
6. Assinaturas, uso, histórico, bloqueios, recursos, limites e preços.
7. White label, onboarding, billing, comercial, parceiros e Customer Success.
8. Hospitais, unidades, especialidades e médicos.
9. Plantões, escalas, convites, disponibilidade, indisponibilidades, substituições, recomendações, histórico, pagamentos e repasses.
10. Pacientes, responsáveis, consentimentos, agendamentos e check-in.
11. Painel de chamada, salas, setores, guichês, histórico, triagem e consultas.
12. CID, favoritos, prescrições, itens, modelos e histórico.
13. Convênios, contratos, planos de saúde, autorizações, guias, lotes, glosas e recursos.
14. Financeiro clínico: contas, recebimentos, formas de pagamento, caixas, movimentos, fechamento, estornos, regras e outbox.
15. Auditoria e observabilidade.
16. Relatórios e auditoria de exportações.
17. Índices idempotentes por tenant/status/período.
18. Dados referenciais mínimos.

Toda evolução incremental continua nas migrations. Instalações novas usam exclusivamente `database/scrpt_completo.sql`.
