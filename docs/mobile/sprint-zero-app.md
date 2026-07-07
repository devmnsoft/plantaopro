# Sprint zero — app médico PlantãoPro

## Implementado e testado por inspeção

- LoginScreen com validação amigável, sem `alert()` nativo.
- Navegação MVP autenticada em `AppNavigator` para Início, Plantões, Convites, Escalas, Pagamentos, Notificações, Perfil, Disponibilidade e Preferências.
- Consumo de `EXPO_PUBLIC_API_BASE_URL` via `services/api.ts`, com JWT no header Authorization e fallback amigável.
- Loading, empty state e error state básicos nos fluxos já conectados.

## Implementado e não testado em runtime

- `npm install` executou com sucesso após manter as dependências Expo já compatíveis do projeto. O `npm run start` ainda precisa ser validado em estação interativa/rede liberada porque o Metro tentou acesso externo e retornou `fetch failed`.

## Parcial

- Disponibilidade e Preferências têm telas navegáveis e mensagens honestas, mas a gravação depende da homologação final dos endpoints.
- ConviteDetalheScreen está navegável como MVP de detalhe/estado parcial; ações aceitar/recusar devem ser ligadas aos endpoints reais antes de piloto produtivo.

## Pendente

- Secure storage nativo persistente para JWT quando for permitido instalar dependências Expo adicionais.
- Testes automatizados mobile.

## Atualização homologação real 2026-07-07

- MVP mantém Login, Home, Plantões, Convites, Detalhe convite, Escalas, Pagamentos, Notificações, Perfil, Disponibilidade e Preferências.
- API base deve ser configurada por `EXPO_PUBLIC_API_BASE_URL`; não usar URL fixa de produção.
- Segurança parcial: token em storage em memória até homologar `expo-secure-store` em ambiente interativo.
- Pendência: executar `npm install` e `npm run start` com Expo/Metro aberto para evidência visual.
