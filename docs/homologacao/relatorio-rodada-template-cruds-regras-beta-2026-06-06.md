# Relatório da rodada PlantãoPro Beta Homologável — 2026-06-06

## Branch
- Branch de trabalho criada: `codex/plantaopro-template-cruds-regras-beta`.
- Branch de backup criada: `backup/antes-plantaopro-template-cruds-regras-beta`.
- O repositório local não possuía branch `main` disponível; a rodada partiu da branch `work`, que estava limpa no início.

## Varredura de resíduos da branch incorreta
- Comando executado: `rg -n "padrões definidos no checklist de limpeza" .`
- Resultado: nenhum resíduo encontrado em código ativo, views, scripts, configs, SQL ou documentação versionada.

## Correções e melhorias aplicadas nesta rodada
- O formulário de plantões foi padronizado para o design system do Web, com card principal, seções lógicas, labels explícitos, help text, campos obrigatórios marcados, validação server/client-side e container de erros AJAX.
- Os POSTs de criação/edição de plantões passaram a responder corretamente a requisições AJAX com JSON de sucesso, erro, redirecionamento e mensagens amigáveis, sem exibir stack trace ao usuário.
- As telas de criação e edição de plantões passaram a usar `_PageHeader` e `_ValidationScriptsPartial`, mantendo padrão visual com os demais CRUDs.
- A regra visual/server-side de valor de plantão foi alinhada ao critério de negócio `Valor >= 0`, impedindo apenas valor negativo.
- `_StatusBadge` foi reforçado para normalizar status com espaços, underline e acentos antes de gerar classes CSS, evitando classes inválidas e mantendo acessibilidade com `role="status"`.
- O CSS do design system recebeu refinamentos para seções de formulário e novos estados operacionais de badges usados em plantões, escalas, pagamentos, SaaS e chamados.

## Validações executadas
- `dotnet clean backend/PlantaoPro.Api/PlantaoPro.Api.csproj`: não executado por indisponibilidade do SDK no ambiente (`dotnet: command not found`).
- `dotnet clean backend/PlantaoPro.Web/PlantaoPro.Web.csproj`: não executado por indisponibilidade do SDK no ambiente (`dotnet: command not found`).
- `dotnet build backend/PlantaoPro.Api/PlantaoPro.Api.csproj`: não executado por indisponibilidade do SDK no ambiente (`dotnet: command not found`).
- `dotnet build backend/PlantaoPro.Web/PlantaoPro.Web.csproj`: não executado por indisponibilidade do SDK no ambiente (`dotnet: command not found`).
- Varredura Razor/C# proibidos em `backend/PlantaoPro.Web` e `backend/PlantaoPro.Api`: sem ocorrências impeditivas para os padrões pesquisados.
- Testes automatizados: não executados porque o SDK .NET não está instalado no ambiente.

## QA manual possível no ambiente
- Login, Dashboard, fluxo plantão -> escala -> pagamento, Operação Assistida, Faturamento SaaS e Mobile/Expo não puderam ser exercitados em runtime pela ausência do SDK .NET e pela impossibilidade de subir API/Web.
- A revisão estática confirmou ausência de resíduos da branch incorreta e preservação dos partials globais críticos `_Layout`, `_ConfirmModal`, `_ToastMessages`, `_PageHeader` e `_StatusBadge`.

## Pendências reais
- Executar build completo em ambiente com SDK .NET 10 instalado.
- Executar `dotnet test` em ambiente com SDK .NET 10 instalado.
- Realizar QA manual autenticado com usuários de demonstração e banco de homologação.
- Expandir a padronização aplicada ao formulário de plantões para os formulários restantes que ainda não estejam usando o mesmo nível de help text, máscaras, AJAX e seções lógicas.
