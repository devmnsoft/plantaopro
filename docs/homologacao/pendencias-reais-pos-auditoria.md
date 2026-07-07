# Pendências reais pós-auditoria — RC julho/2026

## Bloqueado por ambiente

- Instalar SDK .NET 10 no runner/local para executar `dotnet restore`, `dotnet build` e `dotnet test`.
- Executar PostgreSQL real e aplicar `database/PlantaoPro_PostgreSQL_Completo.sql`, migrations e seeds.
- Liberar registry npm para validar `npm install` e `npm run start` do app Expo.

## Parcial

- Homologação ponta a ponta Saúde 360 em Web/API com perfis RECEPCAO, TRIAGEM, MEDICO, FINANCEIRO_CLINICA, COORDENADOR_CLINICO, ADMINISTRADOR_CLINICA e AUDITOR_CLINICO.
- Homologação de plantões/escalas/financeiro médico com validação de conflitos, duplicidade de pagamento, justificativas, auditoria e notificações.
- Secure storage nativo no mobile para persistência criptografada de JWT.

## Pendente antes de piloto produtivo

- Evidência de Swagger, `/api/health`, login Web e menus sem 404 em ambiente executável.
- Relatório SQL com aplicação real das migrations/seeds e correção de eventuais 42P01/42703.

## Pendências reais pós-homologação 2026-07-07

- Validar `dotnet restore`, `dotnet build` e `dotnet test` no GitHub Actions com SDK 10 preview.
- Executar PostgreSQL local via Docker e aplicar `scripts/database/apply-local-postgres.*` com `psql`.
- Executar roteiro QA completo por perfil e registrar evidências.
- Homologar Expo/Metro em ambiente interativo e substituir storage em memória por secure storage quando possível.
