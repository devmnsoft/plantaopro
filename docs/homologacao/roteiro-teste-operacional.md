# Roteiro de teste operacional (homologação)

1. Login `ADMINISTRADOR_GLOBAL`.
2. Abrir Central de Escala.
3. Criar plantão (`RASCUNHO`).
4. Publicar plantão.
5. Gerar recomendação de médicos.
6. Convidar médico.
7. Login `MEDICO`.
8. Ver convite.
9. Aceitar convite.
10. Ver escala confirmada.
11. Login `COORDENACAO`.
12. Marcar escala realizada.
13. Login `FINANCEIRO`.
14. Gerar pagamento.
15. Contestar pagamento (perfil médico).
16. Resolver contestação (perfil financeiro).
17. Confirmar pagamento.
18. Validar notificação.
19. Abrir comunicação contextual.
20. Abrir agenda operacional.
21. Ver alertas operacionais.
22. Abrir Swagger Mobile e validar endpoints.
23. Testar acesso negado por perfil e cliente.
24. Confirmar build verde (API e Web).
25. Confirmar `git status` limpo após validações.

## Evidências mínimas por etapa
- Captura de tela ou log do endpoint.
- Status HTTP esperado.
- Resultado funcional (sucesso/bloqueio amigável).
- Registro de auditoria para ações críticas.
