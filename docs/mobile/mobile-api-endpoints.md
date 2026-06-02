# API Mobile MVP — Release Candidate

Base: `/api/mobile`  
Autenticação: JWT Bearer em todos os endpoints, exceto `POST /api/mobile/auth/login`.

## Regras transversais

- Todas as respostas seguem `ApiResponse<T>`.
- Listagens aceitam `page` e `pageSize`; o backend limita `pageSize` a 50.
- O usuário médico acessa somente seus próprios plantões, convites, escalas, pagamentos, notificações e chamados.
- O acesso é negado com mensagem amigável quando o plano do cliente não permite mobile.
- Logs não devem registrar senha, hash, token completo ou payload sensível.

## Endpoints homologáveis

| Método | Rota | Objetivo | Critério de aceite |
|---|---|---|---|
| POST | `/auth/login` | Autenticar app e retornar JWT. | Login válido retorna token; login inválido retorna falha amigável. |
| GET | `/me` | Identificar usuário autenticado. | Sem token retorna 401; com token retorna dados leves. |
| GET | `/dashboard` | Resumo mobile do médico. | Retorna próximos plantões, convites, pagamentos e notificações. |
| GET | `/plantoes-disponiveis` | Plantões abertos para solicitação. | Respeita cliente, médico e paginação. |
| GET | `/plantoes/{id}` | Detalhe leve do plantão. | Plantão de outro cliente retorna 403 amigável. |
| POST | `/plantoes/{id}/solicitar` | Solicitar plantão. | Valida médico autenticado, cliente, vaga e regras de escala. |
| GET | `/convites` | Convites do médico. | Não expõe token, senha ou usuário interno. |
| POST | `/convites/{id}/aceitar` | Aceitar convite. | Revalida vaga/conflito antes de criar/confirmar escala. |
| POST | `/convites/{id}/recusar` | Recusar convite. | Exige motivo e registra auditoria. |
| GET | `/minhas-escalas` | Escalas do médico. | Retorna somente escalas próprias. |
| GET | `/meus-pagamentos` | Pagamentos do médico. | Retorna somente pagamentos próprios. |
| GET | `/notificacoes` | Feed de notificações. | Paginação e dados leves. |
| PUT | `/notificacoes/{id}/lida` | Marcar notificação como lida. | Só altera notificação do próprio usuário. |
| GET | `/perfil` | Perfil mobile. | Dados mínimos sem informações sensíveis. |
| PUT | `/perfil` | Atualizar dados editáveis. | Validação de campos obrigatórios. |
| GET | `/disponibilidade` | Consultar disponibilidade. | Retorno leve para tela mobile-first. |
| PUT | `/disponibilidade` | Atualizar disponibilidade. | Registra auditoria. |
| GET | `/preferencias` | Consultar preferências. | Retorna preferências leves. |
| PUT | `/preferencias` | Atualizar preferências. | Registra auditoria. |
| GET | `/suporte/chamados` | Listar chamados do usuário. | Filtra por usuário e cliente; paginação obrigatória. |
| POST | `/suporte/chamados` | Abrir chamado pelo app. | Exige título e descrição; cria protocolo e auditoria. |

## Payloads principais

### Login

```json
{
  "email": "medico.demo@plantaopro.local",
  "senha": "Senha@123"
}
```

### Criar chamado mobile

```json
{
  "titulo": "Dúvida sobre pagamento",
  "descricao": "Pagamento do plantão realizado ainda não apareceu como confirmado.",
  "categoria": "FINANCEIRO",
  "prioridade": "NORMAL"
}
```

Resultado esperado: HTTP 201 com `id`, `protocolo` e `status=ABERTO`.

## Checklist de smoke test no Swagger

1. Executar `POST /api/mobile/auth/login` com médico demo.
2. Copiar o token e autorizar o Swagger com `Bearer <token>`.
3. Executar `/me`, `/dashboard`, `/plantoes-disponiveis` e `/notificacoes`.
4. Abrir um chamado em `/suporte/chamados`.
5. Listar `/suporte/chamados` e confirmar que apenas chamados do usuário aparecem.
6. Repetir uma chamada sem token e confirmar HTTP 401.
