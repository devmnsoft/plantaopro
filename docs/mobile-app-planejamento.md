# Planejamento inicial do app PlantãoPro

## Objetivo
Entregar um aplicativo mobile focado em produtividade do médico plantonista e acompanhamento operacional em tempo real.

## Público
- Médicos (MVP)
- Coordenação (fase 2)
- Financeiro (fase 3)

## Stack recomendada
- React Native com Expo (rápido para MVP)
- Alternativa corporativa: .NET MAUI

## MVP de telas
Login, Dashboard, Plantões disponíveis, Detalhes do plantão, Solicitar plantão, Convites, Minhas escalas, Meus pagamentos, Notificações, Perfil, Disponibilidade e Preferências.

## JWT e segurança
- Login em `/api/mobile/auth/login`
- Access token curto + renovação controlada
- Storage seguro (Keychain/Keystore)

## Navegação sugerida
- Tabs: Dashboard, Agenda, Convites, Pagamentos, Perfil
- Stack interno para detalhes e edições

## Próximos passos
1. Consolidar contratos de DTO mobile
2. Finalizar paginação/erros padronizados
3. Prototipar UI mobile no design system atual
