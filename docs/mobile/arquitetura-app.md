# Arquitetura do App Mobile

## Princípios
- Mobile-first para médico, com payload leve e paginação.
- JWT obrigatório em todas as rotas exceto login.
- Nenhum token, senha, hash ou segredo deve ser logado.
- Dados sensíveis ficam em SecureStore; preferências visuais podem ficar em AsyncStorage.

## Camadas
- `services/api.ts`: cliente HTTP base, timeout, headers, tratamento de 401/403.
- `services/mobile.ts`: funções por endpoint Mobile.
- `store/session.ts`: sessão autenticada, usuário e perfil.
- `components`: cards, badges, EmptyState, LoadingState e feedback.
- `screens/app`: telas autenticadas.

## Navegação
- Stack pública para Login.
- Tabs autenticadas para Dashboard, Agenda, Convites, Pagamentos e Perfil.
- Stack interna para detalhe do plantão e suporte.

## Observabilidade mobile
- Registrar evento local de login, logout, erro de API e tempo de resposta, sem payload sensível.
- Enviar versão do app em header futuro `X-App-Version`.

## Segurança
- Renovação de sessão por novo login na Sprint Zero.
- Bloquear screenshots somente se política do cliente exigir em fase futura.
- Tratar deep links apenas após validação de domínio e autenticação.
