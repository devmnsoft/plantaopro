# Evidência funcional v2.04.0 — plantões, escalas e seletores

## Escopo entregue

- **Substituição de escala:** o GUID digitável do substituto foi removido. A tela carrega profissionais reais da API, mantém somente registros ativos, da especialidade do plantão e diferentes do profissional atual, e apresenta nome e CRM em um `select`.
- **Validação em profundidade:** o POST remonta as opções autorizadas (não confia nas opções enviadas pelo navegador), confirma a existência da escala, restringe a operação ao estado `confirmado` e recusa profissional que não pertença ao conjunto filtrado. A API continua responsável pela validação transacional de tenant, conflito e elegibilidade.
- **Motivos operacionais:** substituição e recusa passaram a usar catálogos legíveis. “Outro” exige detalhamento no ViewModel de substituição.
- **Escalas:** o filtro textual livre de status foi substituído por uma lista de estados operacionais compreensíveis.
- **Plantões:** preservados os seletores existentes de hospital e especialidade e as validações de GUID não vazio, período, tipo e vagas.

## Campos de ID substituídos

| Tela | Antes | Depois |
|---|---|---|
| `Escalas/Substituir` | input de texto “Identificador do novo médico” | dropdown de profissionais ativos e elegíveis, exibindo nome e CRM |
| `Escalas/Index` | status textual livre | dropdown de status com rótulos operacionais |

IDs de contexto continuam em inputs `hidden`, rotas e DTOs; não são digitados pelo usuário.

## Validações adicionadas

- `Id` da escala e `NovoMedicoId` não podem ser GUID vazio.
- motivo pertence a um catálogo fechado validado por `RegularExpression` no cliente e servidor.
- detalhamento possui limite de 500 caracteres e é obrigatório para “Outro”.
- escala deve existir e estar confirmada.
- substituto deve reaparecer no catálogo remontado no POST, estar ativo, ser da especialidade e não ser o profissional substituído.
- erros são exibidos no resumo e junto ao campo correspondente.

## Testes criados

`V2040FluxosPlantaoEscalaFormulariosTests` cobre entidade e período inválidos no plantão, ausência do profissional/motivo na substituição, motivo fora do catálogo e detalhamento obrigatório para “Outro”.

## Auditoria e limitações

A varredura obrigatória ainda lista inputs `hidden` legítimos e seletores com propriedades terminadas em `Id`. Também identificou débitos preexistentes fora do fluxo fechado nesta entrega (Convites, Agendamentos, Onboarding e Assinaturas) com inputs visíveis de identificadores; eles não foram declarados como resolvidos. A varredura de segurança reporta fixtures locais de PostgreSQL e exemplos/documentação, enquanto `repository-security-check.py` passou. Não houve alteração de banco ou migrations.

O SDK `dotnet` não está instalado nesta imagem (`dotnet: command not found`), portanto restore/build/test não puderam ser executados aqui. A validação C# 10 do repositório passou.

## Comandos executados

- `python3 scripts/repository-security-check.py` — passou.
- `python3 scripts/check-csharp10-compatibility.py` — passou.
- `python3 scripts/validate-scrpt-completo.py` — passou (100%).
- `dotnet restore backend/PlantaoPro.sln` — indisponível: SDK ausente.
- `dotnet build backend/PlantaoPro.sln -c Release --no-restore` — indisponível: SDK ausente.
- `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release --no-build` — indisponível: SDK ausente.
- `git diff --check` — passou.
- `rg -n "Digite.*Id|Digite.*ID|ID da|ID do|asp-for=.*Id|name=\".*Id\"|placeholder=.*Id|placeholder=.*ID" backend/PlantaoPro.Web backend/PlantaoPro.Api backend/PlantaoPro.Tests` — executado; achados revisados acima.
- `rg -n "href=\"#\"|alert\\(|confirm\\(|Password=123456|Username=postgres;Password=|CHANGE_ME_WITH_32|Host=.*Password=|Server=.*Password=" backend/PlantaoPro.Api backend/PlantaoPro.Web backend/PlantaoPro.Tests scripts docs README.md .env.example` — executado; achados preexistentes em fixtures, testes, scripts e documentação.
