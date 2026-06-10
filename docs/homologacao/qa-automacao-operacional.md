# QA — Automação operacional Fase 4

## Executado nesta rodada
- Build inicial solicitado: falhou porque `dotnet` não está instalado no ambiente.
- Build final solicitado: falhou pelo mesmo motivo.
- Varredura estática com `rg` executada.
- Testes de contrato adicionados para migration, endpoints e serviços.

## Roteiro manual para homologação
1. Login coordenação.
2. Criar plantão.
3. Consultar médicos disponíveis.
4. Gerar sugestão.
5. Convidar médicos sugeridos.
6. Login médico.
7. Configurar disponibilidade e indisponibilidade.
8. Solicitar substituição.
9. Login coordenação.
10. Aprovar, convidar substituto e confirmar.
11. Ver pendências e resolver com observação.
12. Exportar relatório CSV.
13. Validar auditoria.
14. Validar bloqueios de plano em homologação.

## Pendências reais
Executar o roteiro acima em ambiente com banco migrado, SDK .NET e usuários de todos os perfis.
