# Sugestão inteligente de médicos

O motor `OperationalAutomationService` usa regras determinísticas, sem IA externa, para pontuar médicos ativos por disponibilidade, ausência de indisponibilidade e ausência de conflito de agenda.

Endpoints:
- `GET /api/central-escala/plantoes/{plantaoId}/sugestoes`
- `POST /api/central-escala/plantoes/{plantaoId}/gerar-sugestoes`
- `POST /api/central-escala/plantoes/{plantaoId}/convidar-sugeridos`
- `POST /api/central-escala/sugestoes/{id}/feedback`

A sugestão não confirma escala automaticamente; apenas registra candidatos e permite convite auditado.
