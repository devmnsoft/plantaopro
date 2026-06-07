# PlantãoPro — Relatório de fechamento técnico e funcional Beta Homologável

Data: 2026-06-05  
Branch de trabalho: `codex/plantaopro-beta-homologavel-fechamento`  
Branch de backup local: `backup/antes-plantaopro-beta-homologavel-fechamento`

## 1. Escopo desta rodada

Esta rodada consolidou evidências de fechamento para o **PlantãoPro Beta Homologável Estável**, sem abertura de módulos grandes novos. O foco foi confirmar higiene do repositório, contratos mínimos de API/Web/Mobile, documentação de homologação e pendências reais para execução assistida em ambiente com SDK .NET e PostgreSQL disponíveis.

## 2. Comandos de preparação executados

- `git branch --show-current`
- `git status --short`
- `git diff --stat`
- `git log --oneline --decorate -10`
- `git checkout -b codex/plantaopro-beta-homologavel-fechamento`
- `git branch backup/antes-plantaopro-beta-homologavel-fechamento`

Resultado: branch de trabalho criada a partir do estado local disponível. A branch `main` não estava presente no clone local, portanto não houve troca para `main` nem `git pull` nesta execução.

## 3. Sanitização do repositório

A varredura obrigatória com `rg` para resíduos do produto externo citado no briefing foi executada antes das alterações. Resultado: **sem ocorrências em arquivos ativos do repositório**.

Também foram revisados sinais de risco comuns para homologação:

- Sem alteração em `bin/`, `obj/`, `.vs/` ou dependências locais.
- Sem inclusão de secrets, tokens ou senhas reais.
- Sem backup patch versionado.
- Sem uso novo de recursos incompatíveis com C# 10.

## 4. Build e testes

Os comandos obrigatórios foram disparados, mas o ambiente desta execução não possui `dotnet` no PATH. A falha observada é de ambiente e deve ser reexecutada em uma máquina com SDK compatível com `net10.0`.

Comandos que precisam ser reexecutados no ambiente de homologação:

- `dotnet clean backend/PlantaoPro.Api/PlantaoPro.Api.csproj`
- `dotnet clean backend/PlantaoPro.Web/PlantaoPro.Web.csproj`
- `dotnet build backend/PlantaoPro.Api/PlantaoPro.Api.csproj`
- `dotnet build backend/PlantaoPro.Web/PlantaoPro.Web.csproj`
- `dotnet test`

## 5. Endpoints principais cobertos por contrato

A suíte `backend/PlantaoPro.Tests` já mantém testes de contrato para confirmar presença dos endpoints principais descritos para a homologação:

- Escalas e fluxo de plantão: listagem, detalhe, aceite, confirmação, recusa, cancelamento, substituição e marcação de realizado.
- Financeiro: listagem, detalhe, geração, confirmação, cancelamento e pagamentos do médico.
- Notificações: listagem, não lidas, marcar uma como lida e marcar todas como lidas.
- Dashboard e Mobile MVP: `/api/dashboard` e `/api/mobile/home`.

## 6. Fluxos funcionais para QA manual

A homologação manual deve seguir os roteiros versionados em `docs/homologacao` e registrar evidências para:

1. Login admin e acesso ao Dashboard.
2. Cadastro de hospital, especialidade e médico.
3. Cadastro e publicação de plantão respeitando data final maior que inicial, valor não negativo e vagas positivas.
4. Login médico e solicitação ou aceite de plantão.
5. Confirmação de escala pela coordenação, com controle de vagas.
6. Marcação de escala realizada.
7. Geração e confirmação de pagamento pelo financeiro.
8. Notificação gerada ao médico.
9. Auditoria e histórico de status para ações críticas.
10. Reflexos no Dashboard, Central de Escala, Minha Agenda e relatório financeiro.

## 7. Operação Assistida, SaaS e Mobile

Foram mantidas como fontes oficiais de homologação os documentos já versionados para:

- Operação Assistida: clientes em implantação, checklist, ocorrências, treinamentos e timeline.
- SaaS básico: clientes, planos, assinaturas, limites, faturas, inadimplência, suspensão e reativação.
- Mobile Expo MVP: login, home, dados do médico, plantões disponíveis, convites, escalas, pagamentos e notificações, sempre com fallback amigável quando um endpoint ainda depender de ambiente real.

## 8. Pendências reais antes de aprovar o Beta

- Reexecutar build e testes em ambiente com SDK .NET compatível.
- Executar fluxo médico ponta a ponta contra PostgreSQL de homologação.
- Validar permissões por perfil com massa multiempresa: admin global, admin cliente, coordenação, financeiro, médico e hospital.
- Validar Expo com API configurável em dispositivo ou emulador.
- Registrar evidências visuais apenas quando Web/API/Mobile estiverem em execução.

## 9. Critério de PR limpo

O PR pode ser revisado quando os seguintes itens forem confirmados no ambiente de homologação:

- Build API verde.
- Build Web verde.
- Testes automatizados verdes.
- Varredura de resíduos externa sem ocorrências.
- Login, logout, JWT, cookie authentication, Swagger, Dashboard e endpoint de saúde preservados.
- Fluxo plantão → escala → pagamento validado.
- Documentação de homologação e mobile atualizada.
