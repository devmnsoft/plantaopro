# Fluxos Mobile

## 1) Login
1. Usuário informa e-mail e senha.
2. App chama `POST /api/mobile/auth/login`.
3. App persiste token e navega para Dashboard.

## 2) Dashboard médico
1. App chama `GET /api/mobile/dashboard`.
2. Exibe resumo de convites, escalas e pendências.

## 3) Plantões disponíveis
1. App chama `GET /api/mobile/plantoes-disponiveis` paginado.
2. Usuário abre detalhes e executa solicitação (próxima etapa de API dedicada).

## 4) Convites / Escalas / Pagamentos / Notificações
Fluxos seguem o mesmo padrão:
- lista paginada,
- detalhe,
- ação com confirmação.

## 5) Disponibilidade e preferências
- fluxo de edição por formulário simples com salvar/cancelar.
