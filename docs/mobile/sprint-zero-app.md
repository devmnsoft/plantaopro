# Sprint Zero — App Mobile PlantãoPro

## Objetivo
Preparar o aplicativo mobile MVP para médicos usando os contratos atuais da API Mobile, sem criar dependências de publicação nas lojas nesta fase.

## Stack recomendada
- React Native com Expo.
- TypeScript.
- Expo Router ou React Navigation.
- SecureStore para JWT e dados sensíveis.
- TanStack Query ou camada simples de services para cache e loading.
- Axios/fetch com interceptor para `Authorization: Bearer <token>`.

## Organização de pastas sugerida
```text
app/
  (auth)/login.tsx
  (tabs)/dashboard.tsx
  (tabs)/agenda.tsx
  (tabs)/convites.tsx
  (tabs)/pagamentos.tsx
  suporte/
components/
  EmptyState.tsx
  KpiCard.tsx
  LoadingState.tsx
services/
  api.ts
  auth.ts
  mobile.ts
store/
  session.ts
utils/
  formatters.ts
```

## Fluxo de login
1. Usuário informa e-mail e senha.
2. App chama `POST /api/mobile/auth/login`.
3. App salva token em SecureStore.
4. App chama `GET /api/mobile/me` e `GET /api/mobile/dashboard`.
5. Em 401, limpar sessão e retornar para login.
6. Em 403 por plano sem mobile, exibir tela amigável orientando contato com a coordenação.

## Telas MVP
- Login.
- Dashboard do médico.
- Plantões disponíveis.
- Detalhe do plantão.
- Convites.
- Minhas escalas.
- Meus pagamentos.
- Notificações.
- Perfil.
- Disponibilidade.
- Preferências.
- Suporte/chamados.

## Estados obrigatórios
- Loading com skeleton simples.
- EmptyState com orientação de próxima ação.
- Erro 401: sessão expirada.
- Erro 403: acesso/plano bloqueado.
- Erro offline/time-out: tentar novamente.
- Toast/snackbar para solicitação, aceite, recusa, perfil e suporte.

## Roadmap de publicação
1. Sprint Zero: arquitetura, navegação, autenticação e design tokens.
2. Sprint 1: dashboard, plantões disponíveis e solicitação.
3. Sprint 2: convites, escalas e notificações.
4. Sprint 3: pagamentos, perfil, disponibilidade e suporte.
5. Pré-lojas: ícones, splash, permissões, política de privacidade e build Android/iOS.
