# QA manual — Saúde 360 Base Clínica

## Roteiro obrigatório

1. Login admin global.
2. Confirmar menus clínicos.
3. Login admin cliente.
4. Confirmar módulos clínicos do plano.
5. Criar, editar e buscar paciente.
6. Criar, confirmar, cancelar, reagendar e marcar falta em agendamento.
7. Fazer check-in e validar fila de painel/triagem.
8. Chamar, rechamar, marcar ausente e finalizar no painel.
9. Iniciar, preencher sinais vitais, classificar e finalizar triagem.
10. Ver histórico do paciente e dashboard clínico.
11. Testar usuários recepção, triagem, médico e bloqueios indevidos.
12. Confirmar auditoria e logs sem dados sensíveis.

## Resultado nesta rodada

QA manual completo depende de ambiente executável com .NET SDK e banco PostgreSQL. No container desta execução, `dotnet` não estava disponível; a validação realizada foi estática por arquivos, rotas, SQL e varreduras.
