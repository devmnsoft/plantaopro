# Saúde 360 Fase 6 — BI, Integrações e Mobile

Esta entrega adiciona base funcional para BI executivo, relatórios avançados, API pública por tenant, API Keys com hash, webhooks assinados, registros mobile e auditoria/LGPD.

## Implementado
- Migration idempotente em `database/migrations/2026_saude360_bi_integracoes_mobile.sql`.
- Endpoints de BI por perfil, widgets, séries, ranking e alertas.
- Endpoints de relatórios, execução e exportação CSV UTF-8 auditável.
- Gestão de API Keys com exibição única e armazenamento somente do hash.
- Webhooks por tenant com secret hash e contrato de assinatura HMAC SHA256.
- API pública v1 com respostas `ApiResponse<T>` e escopo por API Key como requisito operacional.
- Registro/remoção de dispositivos mobile e preferências de notificação.
- Views MVC para integrações, webhooks e logs.

## Segurança e LGPD
- Logs e payloads operacionais devem registrar apenas metadados seguros.
- Dados clínicos sensíveis não são exportados/enviados por padrão.
- Exportações exigem finalidade e devem ser auditadas.

## Pendências reais
- Enforcer completo de API Key/rate limit em middleware dedicado.
- Worker assíncrono para entrega/retry real de webhooks.
- Geração PDF profissional após escolha de biblioteca homologada.
- Integração push real com Expo/Firebase mediante configuração segura.
- Homologação manual ponta a ponta em ambiente com banco populado.
