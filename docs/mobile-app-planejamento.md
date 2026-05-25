# Planejamento Técnico — App Mobile PlantãoPro

## Objetivo
Entregar uma experiência mobile para médicos focada em operação diária: login, agenda, convites, escalas, pagamentos e notificações.

## Público-alvo
- Médicos plantonistas (principal)
- Coordenação (fase posterior)

## Stack recomendada
1. **Principal: React Native + Expo**
2. **Alternativa: .NET MAUI**

## Arquitetura sugerida
- Camadas: `presentation`, `application`, `infrastructure`.
- API única via `/api/mobile/*`.
- DTOs leves e respostas padronizadas `ApiResponse<T>`.

## Organização de pastas (sugestão)
- `src/app` (rotas)
- `src/features` (módulos)
- `src/shared` (componentes, tema, cliente HTTP)
- `src/state` (store global)

## Autenticação e JWT
- Login em `/api/mobile/auth/login`.
- Guardar access token seguro (SecureStore/Keychain).
- Renovação com refresh token quando o backend suportar endpoint dedicado.

## Tratamento de erro
- Mapear erros HTTP para mensagens amigáveis.
- Retry só para falhas transitórias.

## Estado global
- Sessão do usuário.
- Notificações não lidas.
- Preferências locais de UI.

## Navegação
- Stack inicial: Login -> Dashboard.
- Tabs principais: Agenda, Convites, Escalas, Pagamentos, Perfil.
