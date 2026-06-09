# Padrões de telas

Toda tela profissionalizada deve conter:

1. `PageHeader` com descrição objetiva.
2. Breadcrumb via layout.
3. KPIs ou resumo quando houver decisão operacional.
4. Filtros quando houver listas.
5. Tabela com ações reais ou empty state.
6. Bloqueio comercial amigável quando recurso exigir plano/módulo.
7. Toast e modal para ações sensíveis.
8. Respeito a perfil, tenant, plano e módulo no menu e controller.

Telas sem backend completo devem exibir empty state ou bloqueio profissional; não devem renderizar botão sem rota existente.
