# Operação Assistida — Beta Comercial Controlada

## Objetivo

A operação assistida permite que a MNSOFT acompanhe um cliente real durante implantação, homologação final, treinamento e preparação para produção controlada. O módulo centraliza progresso, checklist, ocorrências, treinamentos e evidências de aprovação.

## Perfis e segurança

- **ADMINISTRADOR_GLOBAL** visualiza todos os clientes.
- **ADMINISTRADOR** e **COORDENACAO** visualizam apenas o próprio cliente.
- Toda conclusão/reabertura de checklist, ocorrência e treinamento registra auditoria crítica.
- Ocorrência crítica registra alerta operacional para acompanhamento executivo.

## Fluxo de uso

1. Acessar **Operação Assistida**.
2. Selecionar o cliente em implantação.
3. Abrir **Checklist** e concluir os itens com observação/evidência.
4. Registrar ocorrências de tipo BUG, DUVIDA, MELHORIA, TREINAMENTO ou CONFIGURACAO.
5. Priorizar ocorrências como BAIXA, MEDIA, ALTA ou CRITICA.
6. Resolver ocorrências informando a solução aplicada.
7. Registrar treinamentos por perfil e participantes.
8. Revisar percentual, riscos e pendências antes do go-live beta.

## Checklist padrão

- Cliente cadastrado.
- Plano selecionado.
- Assinatura criada.
- Usuário administrador criado.
- Unidade criada.
- Hospital cadastrado.
- Especialidades cadastradas.
- Médicos cadastrados.
- Primeiro plantão criado.
- Primeiro plantão publicado.
- Primeiro médico convidado.
- Primeira escala confirmada.
- Primeiro pagamento confirmado.
- Usuários treinados.
- Área do médico validada.
- Relatórios validados.
- Faturamento SaaS validado.
- Homologação aprovada.

## Critérios de aprovação

- Checklist com 100% dos itens concluídos ou justificativa formal para pendências aceitas.
- Nenhuma ocorrência CRITICA ou ALTA aberta.
- Treinamentos registrados para administração, coordenação, financeiro e médicos pilotos.
- Fluxo operacional médico e fluxo SaaS básico validados.
- Auditoria e observabilidade revisadas após simulação ponta a ponta.

## Regras técnicas consolidadas nesta beta

- Ao abrir o checklist de um cliente pela primeira vez, a API materializa o checklist padrão no schema `plantaopro`, com IDs determinísticos e `ON CONFLICT` seguro. Isso evita falha operacional ao concluir um item recém-exibido na primeira homologação do cliente.
- A listagem de clientes usa paginação (`page`/`pageSize`, máximo 50) e calcula o percentual por consulta agregada no banco, evitando uma chamada adicional de checklist por cliente na tela executiva.
- Tipos aceitos para ocorrência: `BUG`, `DUVIDA`, `MELHORIA`, `TREINAMENTO`, `CONFIGURACAO`.
- Prioridades aceitas para ocorrência: `BAIXA`, `MEDIA`, `ALTA`, `CRITICA`.
- Payload vazio ou incompleto em ações críticas retorna erro amigável 400, sem stack trace para o usuário.

## Smoke test recomendado antes da demonstração

1. Abrir `/OperacaoAssistida` como `ADMINISTRADOR_GLOBAL` e confirmar cards dos clientes.
2. Entrar no detalhe de um cliente sem checklist salvo e validar que os 18 itens aparecem.
3. Concluir o item **Cliente cadastrado** e confirmar aumento do percentual.
4. Reabrir o item com justificativa e confirmar auditoria.
5. Criar ocorrência `CRITICA` do tipo `BUG` e confirmar alerta operacional.
6. Resolver a ocorrência com solução preenchida.
7. Registrar treinamento para `COORDENACAO` e validar timeline.
