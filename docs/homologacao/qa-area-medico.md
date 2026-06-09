# Homologação — fase 2

## Cenário coberto

- Acessar a área do perfil autenticado.
- Validar KPIs do dashboard.
- Validar EmptyState quando não houver itens.
- Validar botões de navegação para ações reais.
- Validar modal em ação crítica.
- Validar mensagem de bloqueio por plano quando aplicável.
- Validar que o escopo exibido corresponde ao tenant/perfil autenticado.

## Resultado esperado

- Usuário visualiza somente dados e ações do seu perfil.
- Ações críticas não são executadas sem confirmação e auditoria.
- Limitações de plano aparecem com CTA de upgrade.
- Falhas técnicas não aparecem como stack trace para o usuário.

## Pendências reais

- Executar com SDK .NET e PostgreSQL disponíveis.
- Registrar evidência visual de cada perfil durante homologação manual.
