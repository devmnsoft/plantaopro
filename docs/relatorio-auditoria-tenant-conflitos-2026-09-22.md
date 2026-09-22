# Auditoria incremental — isolamento de tenant em conflitos e recomendações

## Escopo e achado

A rodada iniciou com a execução dos validadores estáticos disponíveis. Os nove validadores passaram, mas o SDK .NET não está instalado no ambiente (`dotnet: command not found`), impedindo build, testes compilados, inicialização, login, Swagger e homologação navegada.

A inspeção do motor operacional identificou dois caminhos que não fechavam o isolamento por tenant:

1. os endpoints de conflito aceitavam qualquer identificador de médico de um usuário autenticado, sem validar o vínculo do médico com o cliente da sessão;
2. a recomendação de plantões e médicos não relacionava explicitamente o `cliente_id` das duas entidades, podendo misturar candidatos entre clientes.

## Correções aplicadas

- Os endpoints de verificação e listagem por médico agora validam o acesso com `TenantGuardService` antes de consultar escalas.
- Uma tentativa cross-tenant retorna HTTP 403, usa mensagem segura e registra o bloqueio na auditoria operacional.
- A recomendação de plantões agora exige que o plantão pertença ao mesmo cliente do médico.
- A recomendação de médicos agora seleciona exclusivamente profissionais do cliente proprietário do plantão.
- Foi adicionado um contrato automatizado para impedir regressão dos filtros e do bloqueio no controller.

## Banco, UX e migrations

Nenhuma migration foi necessária. Não houve alteração visual e, por isso, não foi produzida captura de tela.

## Evidência e limitações

Os validadores Python, `git diff --check` e a inspeção estática comprovam os contratos incluídos. A ausência do SDK .NET e de uma aplicação/banco em execução impede declarar como comprovados: compilação, materialização Dapper, login, Swagger, instalação limpa/upgrade, isolamento em runtime e os fluxos ponta a ponta.

**A entrega não está declarada pronta para produção.** O próximo passo recomendado é executar o conjunto .NET e o roteiro de homologação em ambiente com SDK e PostgreSQL, priorizando um teste integrado com dois tenants e médicos distintos para confirmar os resultados e a auditoria persistida.
