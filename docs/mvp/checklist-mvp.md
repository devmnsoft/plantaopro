# Checklist MVP Comercial Avançado (Pré-Produção)

## Estabilidade
- Build API/Web sem erros locais de código.
- JWT/login preservados.
- Fluxos críticos com tratamento amigável (400/401/403/404/409/500).

## Produto
- CRUDs principais com paginação, filtros, empty-state, toasts e confirmação.
- API mobile com endpoints de autenticação, agenda, convites, escalas e pagamentos.
- Regras de negócio essenciais para plantões/escalas/pagamentos.

## Segurança
- Validação por perfil (ADMINISTRADOR_GLOBAL, ADMINISTRADOR, COORDENACAO, OPERADOR, FINANCEIRO, MEDICO, HOSPITAL).
- Isolamento por cliente_id e dados próprios para médico.
- Auditoria em ações críticas.

## Operação
- Observabilidade com indicadores executivos.
- Documentação de deploy/homologação e roteiro de demo comercial.
