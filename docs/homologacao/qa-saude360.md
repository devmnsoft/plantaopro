# QA Saúde 360 — Fase 5

## Roteiro manual obrigatório

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

## Status desta rodada

- Migration criada.
- APIs, serviços, Web MVC, menus, permissões e documentação criados.
- Build não pôde ser executado neste container porque o comando `dotnet` não está instalado.

## Pendências reais

- Executar `dotnet clean`, `dotnet build` e `dotnet test` em ambiente com SDK .NET.
- Aplicar migration em banco PostgreSQL de homologação.
- Executar roteiro manual completo com usuários de cada perfil.
