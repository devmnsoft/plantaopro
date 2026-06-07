# Roteiro de Homologação — Operação Assistida

## Usuários de teste

- ADMINISTRADOR_GLOBAL: valida todos os clientes e visão executiva.
- ADMINISTRADOR: valida apenas o cliente associado.
- COORDENACAO: registra pendências operacionais e acompanha checklist.

## Passo a passo

1. Fazer login como ADMINISTRADOR_GLOBAL.
2. Acessar **Operação Assistida**.
3. Selecionar um cliente em implantação.
4. Conferir cards de progresso, ocorrências abertas, ocorrências críticas e treinamentos.
5. Abrir o checklist e concluir um item com observação.
6. Reabrir o mesmo item informando justificativa.
7. Registrar ocorrência CRITICA de homologação.
8. Confirmar geração de alerta operacional e auditoria.
9. Resolver a ocorrência informando solução aplicada.
10. Registrar treinamento de coordenação e treinamento de médicos.
11. Validar que ADMINISTRADOR do cliente não acessa clientes de terceiros.

## Resultado esperado

- O percentual do cliente muda conforme checklist concluído.
- Ocorrência crítica fica destacada e rastreável.
- Resolver ocorrência exige solução.
- Reabrir checklist exige justificativa.
- Ações POST exibem toast de sucesso/erro no Web.
- A API retorna `ApiResponse<T>` e códigos 400/403/404 amigáveis.

## Critérios de aprovação

- Nenhum stack trace aparece para o usuário.
- Auditoria registra conclusão, reabertura, criação/resolução de ocorrência e treinamento.
- Filtros por status/prioridade funcionam.
- Cliente comum não visualiza outra empresa.

## Validação técnica adicional

- Em um cliente sem checklist prévio, abrir a aba **Checklist** antes de concluir itens; a API deve persistir o checklist padrão automaticamente.
- Tentar criar ocorrência com tipo inválido e confirmar retorno amigável 400.
- Tentar criar ocorrência com prioridade inválida e confirmar retorno amigável 400.
- Confirmar que a listagem de clientes aceita `page` e `pageSize` e que `pageSize` acima de 50 é limitado pelo backend.
