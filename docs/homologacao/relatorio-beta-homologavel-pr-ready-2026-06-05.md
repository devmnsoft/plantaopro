# Relatório técnico — Beta Homologável PR Ready — 2026-06-05

## Branches e estratégia Git
- Branch inicial inspecionada: `work`.
- Branch de trabalho criada nesta rodada: `codex/plantaopro-beta-homologavel-pr-ready`.
- Branch de backup criada nesta rodada: `backup/antes-plantaopro-beta-homologavel-pr-ready`.
- Observação operacional: o clone local não possui branch `main` nem remoto configurado visível no ambiente; por isso a branch de trabalho foi criada a partir do HEAD disponível, sem `pull`.

## Higiene do repositório
- A varredura obrigatória contra resíduos do produto externo foi executada.
- As únicas ocorrências encontradas antes da correção eram menções legítimas ao stack do app móvel na documentação.
- A documentação foi ajustada para nomenclatura neutra de `app móvel`, mantendo o contrato técnico do PlantãoPro e evitando falso positivo na varredura de higiene.
- A segunda varredura de higiene ficou sem ocorrências.

## Estabilização estática Web/API
- A varredura de padrões proibidos em Web/API foi executada para `@page`, `asp-page`, `@model dynamic`, `href="#"`, `alert()`, `confirm()`, `ADD CONSTRAINT IF NOT EXISTS`, collection expressions e `new string()`.
- Não houve ocorrência nos projetos `backend/PlantaoPro.Web` e `backend/PlantaoPro.Api` após a sanitização documental.
- A revisão estática confirmou que o layout global injeta `_ToastMessages` sem depender do model da página e `_ConfirmModal` com `ConfirmModalViewModel` explícito.

## Validações de documentação e contratos existentes
- A documentação obrigatória de homologação, deploy, SaaS e mobile já está presente no repositório.
- Os testes de contrato existentes cobrem higiene do repositório, documentos obrigatórios, endpoints MVP mobile, endpoints de operação assistida, regras de plantão e contratos de segurança essenciais.
- A documentação de endpoints mobile mantém fallback amigável para endpoint indisponível, paginação e normalização de DTOs para as telas.

## Build e testes no ambiente atual
- `dotnet clean`, `dotnet build` e `dotnet test` não puderam ser executados de forma efetiva porque o SDK .NET não está instalado no container atual (`dotnet: command not found`).
- O QA funcional com API/Web em execução também ficou bloqueado pelo mesmo motivo.
- A validação possível nesta rodada foi composta por inspeção estática, varreduras com `rg`, revisão documental e revisão de contratos de teste já versionados.

## Pendências reais para homologação assistida
1. Executar `dotnet clean backend/PlantaoPro.Api/PlantaoPro.Api.csproj` em ambiente com SDK .NET 10 instalado.
2. Executar `dotnet clean backend/PlantaoPro.Web/PlantaoPro.Web.csproj` em ambiente com SDK .NET 10 instalado.
3. Executar `dotnet build backend/PlantaoPro.Api/PlantaoPro.Api.csproj`.
4. Executar `dotnet build backend/PlantaoPro.Web/PlantaoPro.Web.csproj`.
5. Executar `dotnet test`.
6. Subir API e Web e validar login admin, Dashboard, Swagger, health, fluxo plantão → escala → pagamento, Operação Assistida, faturamento SaaS e app móvel.

## Conclusão PR Ready
- O repositório ficou limpo para a varredura obrigatória de resíduos externos.
- A documentação de homologação foi consolidada com o relatório desta rodada.
- O PR deve ser revisado com atenção especial ao bloqueio ambiental de build/testes e às pendências funcionais que exigem runtime .NET disponível.
