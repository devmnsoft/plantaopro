# Comunicação operacional interna

A comunicação operacional interna reduz dependência de canais externos e mantém histórico auditável dentro do PlantãoPro.

## Fluxo implementado

- `GET /api/comunicacao/conversas` lista conversas do usuário autenticado com paginação, busca, tipo e status.
- `POST /api/comunicacao/conversas` cria conversa com participantes e grava a mensagem inicial quando informada.
- `GET /api/comunicacao/conversas/{id}` carrega detalhe compatível com a Web: metadados da conversa, mensagens, participantes e indicador `MinhaMensagem`.
- `POST /api/comunicacao/conversas/{id}/mensagens` adiciona mensagem à conversa após validar participação.
- `PUT /api/comunicacao/mensagens/{id}/lida` marca leitura e registra `mensagem_leituras`.
- `POST /api/comunicacao/conversas/{id}/encerrar` encerra a conversa com auditoria.
- `GET/POST /api/comunicacao/templates` mantém modelos reutilizáveis de comunicação operacional.

## Regras operacionais

- Usuários só acessam conversas em que participam.
- A criação registra o usuário autenticado como participante automaticamente.
- Mensagens vazias são rejeitadas.
- Leituras e encerramentos são auditados.
- O schema incremental consolida `conversas`, `conversa_participantes`, `mensagens` e `mensagem_leituras`, incluindo `cliente_id`, índices de usuário/conversa/status e suporte a `updated_by`.

## Pendências reais

- Entrega externa por WhatsApp/e-mail ainda não foi implementada; a fase cobre comunicação interna in-app.
- Notificação em tempo real por WebSocket/SignalR ainda não foi implementada; a atualização ocorre por navegação/reload.
