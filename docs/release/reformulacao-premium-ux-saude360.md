# Reformulação Premium UX Saúde 360

## Diagnóstico inicial
A base já possuía controllers Web, `Saude360WebControllerBase`, views genéricas e integração API via serviço Web. A rodada reduz a aparência genérica com design system, ajuda contextual, tour, manual e formulários guiados sem remover o fallback genérico.

## Entregas
- Design system premium Saúde 360.
- Layout global com link para manual do perfil.
- Ajuda contextual reutilizável.
- Pop-up de boas-vindas e tour leve.
- Manual por perfil em Web e Markdown.
- EmptyState premium nas listagens genéricas.
- Formulários com estado de loading e orientação de validação.

## Pendências reais
- Validar build em ambiente com SDK .NET instalado.
- Evoluir selects de texto/Guid para autocomplete visual completo quando o pacote front-end final for definido.
