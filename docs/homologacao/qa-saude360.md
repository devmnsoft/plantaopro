# QA — Saúde 360 módulos clínicos

## Resultado desta rodada

- Migration SQL criada e idempotente.
- API/Web implementados com serviços e DI.
- Menus e permissões adicionados.

## Roteiro obrigatório de homologação manual

1. Criar paciente.
2. Criar agendamento.
3. Realizar check-in.
4. Chamar no painel.
5. Realizar triagem.
6. Iniciar consulta.
7. Buscar CID.
8. Vincular CID à consulta.
9. Criar prescrição.
10. Imprimir prescrição.
11. Finalizar consulta.
12. Gerar conta a receber.
13. Receber pagamento particular.
14. Fechar caixa.
15. Criar convênio.
16. Criar plano de saúde.
17. Vincular paciente ao plano.
18. Criar autorização.
19. Registrar glosa.
20. Ver relatório financeiro.
21. Ver auditoria.
22. Confirmar tenant.
23. Confirmar permissões.
24. Confirmar build verde.

## Pendências reais

- Build automatizado não executado neste container por ausência do comando `dotnet`.
- QA ponta a ponta depende de banco PostgreSQL com a migration aplicada e usuários/perfis clínicos configurados.
