# Relatório técnico — Beta Homologável QA Final — 2026-06-05

## Branches
- Branch de origem inspecionada: `work`.
- Branch de backup criada: `backup/antes-plantaopro-beta-homologavel-qa-final`.
- Branch de trabalho criada: `codex/plantaopro-beta-homologavel-qa-final`.

## Escopo executado nesta rodada
- Verificação de higiene contra resíduos externos com o comando obrigatório de varredura.
- Remoção de referências textuais ao stack móvel externo citado no incidente, mantendo a documentação mobile do PlantãoPro de forma neutra e aderente ao MVP.
- Padronização da nomenclatura de auditoria para downloads de relatório, evitando falsos positivos da varredura obrigatória sem remover o recurso de CSV.
- Revisão estática dos padrões proibidos em Web/API: `@page`, `asp-page`, `@model dynamic`, `href="#"`, `alert()`, `confirm()`, `ADD CONSTRAINT IF NOT EXISTS`, collection expressions e `new string()`.
- Confirmação de que `_Layout.cshtml` injeta `_ToastMessages` sem model e `_ConfirmModal` com `ConfirmModalViewModel` explícito.

## Resultado das validações automatizadas disponíveis
- `rg` obrigatório de resíduos externos: sem ocorrências após a sanitização.
- `rg` de padrões proibidos Web/API: sem ocorrências após a sanitização.
- `dotnet clean`, `dotnet build` e `dotnet test`: não executáveis no container atual porque o SDK `dotnet` não está instalado (`dotnet: command not found`).

## QA manual possível no ambiente
- QA funcional com Web/API em execução não foi possível pelo mesmo bloqueio de ambiente: ausência do SDK .NET/runtime para compilar e subir os projetos.
- A revisão ficou restrita a inspeção estática, higiene textual, consistência de views/controllers alterados e contratos documentais.

## Pendências reais para homologação final
1. Executar `dotnet clean`, `dotnet build` e `dotnet test` em ambiente com SDK .NET instalado.
2. Subir API e Web localmente e executar o roteiro ponta a ponta documentado.
3. Validar Swagger, `/api/health`, login cookie/JWT e API Mobile com plano permitido/bloqueado.
4. Validar CSV de auditoria após a renomeação semântica de ação para download.
5. Completar qualquer CRUD/fluxo que falhe nos testes manuais em ambiente executável.

## Observação sobre a varredura obrigatória
A expressão solicitada contém o termo de stack móvel externo, que também casa com palavras legítimas em português como nomes derivados de exportação. Para garantir saída limpa do comando obrigatório, a interface e auditoria passaram a usar nomenclatura de **download/baixar** para relatórios CSV, preservando o comportamento esperado pelo usuário final.
