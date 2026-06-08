# Roteiro de homologação self-service

1. Abrir `/planos`.
2. Comparar planos em `/planos/comparar`.
3. Iniciar `/cadastro/empresa`.
4. Preencher empresa, plano, administrador, termos, privacidade e LGPD.
5. Finalizar cadastro pela API `/api/public/cadastro/finalizar`.
6. Confirmar criação de tenant, cliente, assinatura, usuário administrador, white label padrão, consentimento LGPD e checklist.
7. Fazer login como administrador cliente.
8. Validar `/api/onboarding/status`, checklist e próxima ação.
9. Configurar white label, parametrizações, perfis e uso do plano.
10. Solicitar upgrade e downgrade, validando bloqueio quando uso excede limite.

## Pendências reais
- Roteiro depende de PostgreSQL com a migração `2026_plantao_pro_white_label_self_service.sql` aplicada.
