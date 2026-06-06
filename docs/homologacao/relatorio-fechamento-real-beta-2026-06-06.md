# PlantãoPro — Relatório de Fechamento Real da Beta Homologável — 2026-06-06

## Branches

- Branch de trabalho criada: `codex/plantaopro-beta-fechamento-real`.
- Branch de segurança criada: `backup/antes-plantaopro-beta-fechamento-real`.
- A tentativa de atualizar a branch principal não foi aplicável neste container porque o repositório local expõe apenas a branch `work` e não possui remoto configurado.

## Escopo executado nesta rodada

Esta rodada foi tratada como fechamento incremental da Beta Homologável, sem abertura de módulo grande novo. O foco foi estabilizar pontos de risco ainda compatíveis com uma entrega de homologação:

1. Fortalecimento da desserialização Web/API no `BaseWebController`.
2. Tratamento padronizado de envelopes paginados com `success=false`.
3. Log de `Endpoint`, `Status` e `ResponseSample` também em falhas de desserialização paginada.
4. Uso do modal global, toast e loading por AJAX nas ações críticas da Operação Assistida.
5. Registro de evidências, limitações ambientais e pendências reais para PR limpo.

## Higiene e resíduos do produto incorreto

- Varredura executada antes das alterações com o padrão obrigatório informado na rodada.
- Resultado: sem ocorrências em arquivos versionáveis ativos do PlantãoPro.
- Não houve remoção de arquivos legítimos do PlantãoPro.
- Não foi gerado nem versionado patch de backup.

## Correções realizadas

### Desserialização Web/API

- O leitor paginado da Web passou a registrar amostra da resposta também em `JsonException`.
- O leitor paginado passou a respeitar envelope padronizado com `success=false` antes de tentar montar a página.
- A detecção de envelope foi flexibilizada para aceitar respostas com `success` e `data`, mesmo quando `message` vem ausente ou nula.
- O tratamento continua exibindo mensagens amigáveis para usuário final e sem stack trace na UI.

### Layout, toast, modal e AJAX

- As ações críticas de checklist, ocorrências e treinamentos da Operação Assistida usam `data-ajax-form="true"`.
- As mesmas ações mantêm `data-confirm="true"`, título, mensagem e tipo de confirmação.
- O fluxo usa o script global de loading no botão, toast de sucesso/erro e modal de confirmação.

## Endpoints prioritários revisados por contrato/rota

### Escalas

- `GET /api/escalas`
- `GET /api/escalas/{id}`
- `GET /api/medicos/me/plantoes`
- `POST /api/plantoes/{id}/aceitar`
- `POST /api/escalas/{id}/confirmar`
- `POST /api/escalas/{id}/recusar`
- `POST /api/escalas/{id}/cancelar`
- `POST /api/escalas/{id}/substituir`
- `POST /api/escalas/{id}/marcar-realizado`

### Financeiro

- `GET /api/financeiro/pagamentos`
- `GET /api/financeiro/pagamentos/{id}`
- `POST /api/financeiro/pagamentos/gerar`
- `POST /api/financeiro/pagamentos/{id}/confirmar`
- `POST /api/financeiro/pagamentos/{id}/cancelar`
- `GET /api/financeiro/meus-pagamentos`

### Notificações, dashboard e mobile

- `GET /api/notificacoes`
- `GET /api/notificacoes/nao-lidas`
- `PUT /api/notificacoes/{id}/lida`
- `PUT /api/notificacoes/lidas`
- `GET /api/dashboard`
- `GET /api/mobile/home`

### Operação Assistida

- `GET /api/operacao-assistida/clientes`
- `GET /api/operacao-assistida/clientes/{clienteId}`
- `GET /api/operacao-assistida/clientes/{clienteId}/checklist`
- `POST /api/operacao-assistida/checklist/{id}/concluir`
- `POST /api/operacao-assistida/checklist/{id}/reabrir`
- `GET /api/operacao-assistida/clientes/{clienteId}/ocorrencias`
- `POST /api/operacao-assistida/clientes/{clienteId}/ocorrencias`
- `POST /api/operacao-assistida/ocorrencias/{id}/resolver`
- `POST /api/operacao-assistida/clientes/{clienteId}/treinamentos`

## Fluxos testados por validação estática e contratos

- Login e proteção de endpoints permanecem cobertos pelos contratos existentes de autenticação e segurança.
- Fluxo plantão, escala e pagamento permanece coberto pelos contratos existentes de rotas, regras de plantão, ações financeiras e documentação de homologação.
- Operação Assistida foi reforçada nesta rodada com UX AJAX segura nas ações de concluir/reabrir checklist, registrar/resolver ocorrência e registrar treinamento.
- Mobile MVP permanece documentado e coberto por contratos de endpoints da Sprint Zero.

## Resultado do build

- Build API: não executado por limitação do container; o comando `dotnet` não está disponível no PATH.
- Build Web: não executado por limitação do container; o comando `dotnet` não está disponível no PATH.
- O impedimento ambiental foi registrado para QA final e deve ser reexecutado em ambiente com SDK .NET compatível com `net10.0`.

## Resultado dos testes

- `dotnet test`: não executado por limitação do container; o comando `dotnet` não está disponível no PATH.
- Foram adicionados testes de contrato para garantir o endurecimento da desserialização, o AJAX seguro na Operação Assistida e a presença deste relatório de fechamento real.

## QA manual possível no container

- `git status --short`: usado para conferir escopo de alterações.
- `git diff --stat`: usado para conferir o tamanho do patch.
- Varredura de resíduos do produto incorreto: executada antes das alterações e sem ocorrências.
- Varredura de padrões proibidos em API/Web: executada para orientar a rodada.

## Pendências reais

1. Reexecutar `dotnet clean` e `dotnet build` da API/Web em ambiente com SDK .NET disponível.
2. Reexecutar `dotnet test` em ambiente com SDK .NET disponível.
3. Executar o roteiro manual completo com banco PostgreSQL, usuário admin e perfis médico, coordenação e financeiro.
4. Validar Expo com dependências instaladas em ambiente Node/Expo completo.
5. Confirmar fluxo ponta a ponta com dados reais de homologação antes de marcar todos os itens do checklist do PR.

## PR limpo

Antes de abrir o PR, revalidar:

- Sem binários versionados.
- Sem secrets ou tokens.
- Sem patch de backup versionado.
- Documentação de homologação atualizada.
- Commits temáticos presentes na branch.
- Build e testes verdes em ambiente com SDK .NET.
