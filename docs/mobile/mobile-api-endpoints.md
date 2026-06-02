# API Mobile MVP — endpoints

## Padrões obrigatórios
- Base: `/api/mobile`.
- Autenticação: `Bearer JWT` em todos os endpoints, exceto `POST /api/mobile/auth/login`.
- Perfil esperado: `MEDICO` para jornadas do aplicativo; demais perfis devem receber `403` amigável quando a ação não for permitida.
- Envelope: todas as respostas usam `ApiResponse<T>` com `success`, `message`, `data`, `errors`, `statusCode` e `timestamp`.
- Listagens aceitam `page` e `pageSize`; o backend normaliza `page >= 1` e limita `pageSize` para evitar payload pesado.
- Payloads não retornam senha, hash, segredo, SQL bruto, stack trace ou dados de outro cliente/médico.

## Autenticação
| Método | Endpoint | Autenticação | Objetivo | Resultado esperado |
|---|---|---:|---|---|
| `POST` | `/api/mobile/auth/login` | Não | Autenticar médico com e-mail/senha. | `ApiResponse<MobileLoginResponseDto>` com `token`, `expiresAtUtc` e `roles`. |
| `POST` | `/api/mobile/auth/logout` | Sim | Encerrar sessão local do app. | Confirmação para o app descartar o token. |

## Identidade, perfil e dashboard
| Método | Endpoint | Objetivo | Observações |
|---|---|---|---|
| `GET` | `/api/mobile/me` | Dados mínimos do usuário autenticado. | Retorna id, nome, e-mail e perfil. |
| `GET` | `/api/mobile/dashboard` | Resumo mobile do médico. | Cards leves: plantões disponíveis, escalas, pagamentos e notificações. |
| `GET` | `/api/mobile/perfil` | Perfil do usuário. | Sem dados sensíveis. |
| `PUT` | `/api/mobile/perfil` | Atualizar nome/telefone. | Valida `nome` obrigatório e registra erro amigável. |

## Plantões, convites e escalas
| Método | Endpoint | Objetivo | Segurança/performance |
|---|---|---|---|
| `GET` | `/api/mobile/plantoes-disponiveis?page=1&pageSize=20` | Listar oportunidades disponíveis. | Apenas plantões do cliente do médico, paginado. |
| `GET` | `/api/mobile/plantoes/{id}` | Detalhar plantão. | `404` se não existir; `403` se fora do cliente. |
| `POST` | `/api/mobile/plantoes/{id}/solicitar` | Solicitar escala. | Bloqueia duplicidade e audita. |
| `GET` | `/api/mobile/convites?page=1&pageSize=20` | Listar convites/oportunidades. | MVP reaproveita a listagem disponível. |
| `GET` | `/api/mobile/convites/{id}` | Detalhar convite. | Mesmo contrato do detalhe de plantão. |
| `POST` | `/api/mobile/convites/{id}/aceitar` | Aceitar convite. | Mesmo contrato de solicitação de plantão. |
| `POST` | `/api/mobile/convites/{id}/recusar` | Recusar convite. | Retorna status recusado e registra auditoria. |
| `GET` | `/api/mobile/minhas-escalas?page=1&pageSize=20` | Escalas do médico. | Médico só vê dados próprios. |

## Financeiro e notificações
| Método | Endpoint | Objetivo | Observações |
|---|---|---|---|
| `GET` | `/api/mobile/meus-pagamentos?page=1&pageSize=20` | Pagamentos do médico. | Sem dados financeiros de outros médicos. |
| `GET` | `/api/mobile/notificacoes?page=1&pageSize=20` | Notificações do usuário. | Paginado e filtrado pelo usuário autenticado. |
| `GET` | `/api/mobile/notificacoes/contador` | Total de não lidas. | Retorna `long` no contador. |
| `PUT` | `/api/mobile/notificacoes/{id}/lida` | Marcar uma notificação como lida. | Usa plano mobile e usuário autenticado. |
| `PUT` | `/api/mobile/notificacoes/lidas` | Marcar todas como lidas. | Usa plano mobile e usuário autenticado. |

## Preferências, disponibilidade e recomendações
| Método | Endpoint | Objetivo | Observações |
|---|---|---|---|
| `GET` | `/api/mobile/disponibilidade` | Disponibilidade do médico. | MVP retorna contrato base para evolução do app. |
| `PUT` | `/api/mobile/disponibilidade` | Atualizar disponibilidade. | A implementar conforme app mobile avançar. |
| `GET` | `/api/mobile/preferencias` | Preferências mobile. | Retorna flags leves. |
| `PUT` | `/api/mobile/preferencias` | Atualizar preferências. | A implementar conforme app mobile avançar. |
| `GET` | `/api/mobile/recomendacoes?limite=10` | Plantões recomendados. | Limite normalizado para evitar respostas pesadas. |

## Checklist Swagger
- [ ] Tag `Mobile` visível.
- [ ] Login mobile documentado sem cadeado.
- [ ] Demais endpoints exigem JWT.
- [ ] `401` para chamada sem token.
- [ ] `403` para plano/perfil sem permissão.
- [ ] Listagens com `page` e `pageSize`.
