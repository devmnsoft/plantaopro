# Mobile API Endpoints

## Base e autenticação
- Base: `/api/mobile`.
- Autenticação: JWT Bearer em todos os endpoints exceto `POST /auth/login`.
- Resposta: `ApiResponse<T>` ou envelope equivalente com mensagem amigável.
- Paginação: usar `page` e `pageSize`; limite recomendado de `pageSize` até 50.
- Segurança: DTOs mobile não devem retornar senha, hash, token persistido, segredo ou dados de outro médico.

## Endpoints MVP
| Método | Rota | Uso no app | Regras principais |
| --- | --- | --- | --- |
| POST | `/auth/login` | Login | Retorna JWT e dados leves do usuário. Não logar senha/token completo. |
| GET | `/me` | Bootstrap da sessão | Requer JWT e retorna identificação/perfil. |
| GET | `/dashboard` | Home do médico | Resumo de próximo plantão, convites, escalas, pagamentos e notificações. |
| GET | `/plantoes-disponiveis` | Lista de oportunidades | Médico vê somente plantões elegíveis do cliente, com paginação. |
| GET | `/plantoes/{id}` | Detalhe do plantão | Validar elegibilidade e não expor dados sensíveis. |
| POST | `/plantoes/{id}/solicitar` | Solicitar vaga | Valida duplicidade, conflito crítico, médico ativo, especialidade e vaga. |
| GET | `/convites` | Convites pendentes/histórico | Apenas convites do médico autenticado. |
| POST | `/convites/{id}/aceitar` | Aceitar convite | Revalida vaga e conflito. |
| POST | `/convites/{id}/recusar` | Recusar convite | Pode exigir motivo; registra auditoria/notificação. |
| GET | `/minhas-escalas` | Agenda escalada | Apenas escalas do médico autenticado, paginado. |
| GET | `/meus-pagamentos` | Pagamentos | Apenas pagamentos do médico autenticado, com paginação. |
| GET | `/meus-pagamentos/{id}` | Detalhe do pagamento | Revalida titularidade do médico antes de retornar dados financeiros. |
| GET | `/notificacoes` | Feed | Paginação e somente notificações do usuário. |
| PUT | `/notificacoes/{id}/lida` | Marcar lida | Notificação deve pertencer ao usuário. |
| GET | `/perfil` | Perfil | Dados leves do médico/usuário. |
| PUT | `/perfil` | Atualizar perfil | Validar campos obrigatórios. |
| GET | `/disponibilidade` | Disponibilidade | Retorna disponibilidade do médico. |
| PUT | `/disponibilidade` | Salvar disponibilidade | Valida payload leve e registra ação. |
| GET | `/preferencias` | Preferências | Retorna preferências operacionais/comunicação. |
| PUT | `/preferencias` | Salvar preferências | Valida payload e registra ação. |
| GET | `/suporte/chamados` | Chamados | Médico vê apenas seus chamados, paginado. |
| POST | `/suporte/chamados` | Criar chamado | Exige título/descrição; chamado crítico gera alerta. |

## Endpoints complementares já úteis ao MVP
- `POST /auth/logout`: encerra sessão no cliente mobile.
- `PUT /notificacoes/lidas`: marca todas as notificações do usuário como lidas.
- `GET /notificacoes/contador`: contador leve para badge.
- `GET /convites/{id}`: detalhe do convite.
- `GET /recomendacoes`: recomendações/lembretes para o médico.
- `GET /suporte/chamados/{id}`: detalhe do chamado.
- `GET /meus-pagamentos/{id}`: detalhe financeiro do médico autenticado com validação de titularidade.

## Como testar API mobile
1. Autenticar em `POST /api/mobile/auth/login`.
2. Copiar o JWT para `Authorization: Bearer <token>` no Swagger ou cliente HTTP.
3. Executar `GET /api/mobile/me` e `GET /api/mobile/dashboard`.
4. Chamar `GET /api/mobile/plantoes-disponiveis?page=1&pageSize=20`.
5. Solicitar plantão elegível com `POST /api/mobile/plantoes/{id}/solicitar`.
6. Validar convite com aceitar/recusar.
7. Validar pagamentos, notificações e suporte.
8. Remover token e confirmar 401 amigável.
9. Usar cliente com plano sem mobile e confirmar 403 amigável.

## Critérios de aprovação
- Todos os endpoints MVP aparecem no Swagger com tag `Mobile`.
- Endpoints protegidos retornam 401 sem token.
- Plano sem mobile retorna 403 amigável.
- Médico não acessa dados de outro médico.
- Listagens têm paginação/limite.
- Logs não exibem senha, hash, token completo ou payload sensível.

## Integração Expo consolidada nesta revisão
- O app consome a base `EXPO_PUBLIC_API_BASE_URL` quando configurada; caso contrário usa `http://localhost:5000/api` para desenvolvimento local.
- Todas as chamadas passam por `request<T>` em `src/services/api.ts`, que normaliza propriedades PascalCase/camelCase antes de devolver `ApiResponse<T>` ao app.
- Serviços mobile normalizam DTOs específicos da API (`plantaoId`, `escalaId`, `pagamentoId`, `valorPrevisto`) para campos estáveis usados pelas telas (`id`, `valor`, `vagas`).
- Listagens usam `getPaged<T>` com `page` e `pageSize`, sempre retornando `items: []` quando a API falhar ou o endpoint ainda não existir.
- Endpoints ausentes retornam fallback amigável: `Endpoint mobile não disponível nesta versão.`, sem quebrar a tela.
- O JWT é armazenado pelo helper `storage` e reaplicado automaticamente no header `Authorization: Bearer <token>`.
- Serviços separados (`authService`, `plantaoService`, `financeiroService`, `notificationService`, `medicoService`) mapeiam os endpoints MVP para reduzir duplicidade nas telas.
