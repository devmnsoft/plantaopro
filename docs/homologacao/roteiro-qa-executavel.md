# Roteiro QA executável — PlantãoPro Saúde 360

Status geral: **Release Candidate parcial** até execução completa em ambiente com .NET 10, Docker, PostgreSQL, psql e Expo interativo.

## Pré-condições

1. `docker compose up -d`
2. `scripts/database/apply-local-postgres.sh`
3. `dotnet run --project backend/PlantaoPro.Api/PlantaoPro.Api.csproj`
4. validar `GET /`, `GET /api/health` e `GET /api/health/db`.

## Fluxo 1 — Login e perfis

Validar admin global, admin cliente, coordenação, médico, recepção, triagem, financeiro, faturamento convênio e auditor. Para cada perfil: login, redirecionamento, menu, acesso negado e ausência de 404 em item visível.

## Fluxo 2 — Plantões

Criar, editar, publicar, recomendar médico, convidar médico, aceite médico, confirmação da coordenação, realizado, gerar pagamento, confirmar pagamento, auditoria e notificação.

## Fluxo 3 — Saúde 360

Criar paciente, agendar consulta, confirmar, check-in, painel de chamada, triagem, consulta, CID, prescrição, financeiro clínica, convênio e plano de saúde.

## Fluxo 4 — Permissões

Médico não acessa dados de outro médico; recepção não acessa evolução/prescrição; financeiro não vê conteúdo clínico sensível; tenant não acessa outro tenant; módulo bloqueado exibe bloqueio amigável.
