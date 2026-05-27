# Roadmap Mobile MVP

## Fase 1 (MVP funcional)
- Auth mobile (`/api/mobile/auth/login`, `/api/mobile/me`).
- Dashboard resumido.
- Plantões disponíveis e solicitação.
- Convites (listar, aceitar, recusar).
- Minhas escalas.
- Meus pagamentos e contestação.

## Fase 2 (Operação assistida)
- Disponibilidade semanal.
- Preferências médicas.
- Notificações com contador e marcação em lote.
- Perfil com atualização segura.
- Indicadores de desempenho pessoal.

## Fase 3 (Escala inteligente)
- Recomendação contextual de plantões.
- Alertas de conflito preventivo.
- Telemetria de uso para CS e produto.

## Requisitos não funcionais
- JWT obrigatório em todas as rotas privadas.
- Resposta padronizada em `ApiResponse<T>`.
- Paginação em listagens.
- Logs com usuário, perfil, IP e duração.
- Retorno 403 amigável quando plano não permitir módulo mobile.
