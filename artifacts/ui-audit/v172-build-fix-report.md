# Relatório de estabilização do build v1.72.0

## Causa raiz

O namespace `PlantaoPro.Web.Controllers` possuía duas declarações de `MinhaAssinaturaController`: uma implementação parcial em `PublicSelfServiceWebControllers.cs`, baseada em `Controller`, e a implementação dedicada baseada em `BaseWebController`. A colisão produzia CS0263, CS0101 e CS0111 no Visual Studio.

## Correção consolidada

- Controller mantido: `Controllers/MinhaAssinaturaController.cs`.
- Classe removida: declaração duplicada de `MinhaAssinaturaController` em `Controllers/PublicSelfServiceWebControllers.cs`.
- Rota preservada: `[Route("MinhaAssinatura")]`, com `GET /MinhaAssinatura` e `GET /MinhaAssinatura/Index` direcionados à única action `Index`.
- Integração preservada: a action usa `BaseWebController`, propaga o bearer token e consulta `GET api/minha-assinatura` para preencher `MinhaAssinaturaViewModel`.
- Estado sem dados preservado: uma resposta sem payload cria apenas o view model vazio; nenhum plano, preço, limite, consumo ou histórico é sintetizado.

O gate `scripts/check-saas-ui.py` agora falha se a classe voltar a existir fora do arquivo dedicado, se houver mais de uma declaração ou se os contratos de rota, base, endpoint e view model forem removidos.

## Integridade do agrupador público

`PublicSelfServiceWebControllers.cs` continua responsável por `PlanosPublicosController`, `CadastroController`, `WhiteLabelController`, `PerfisController` e `ParametrizacoesController`. A remoção ficou limitada à classe de assinatura duplicada, sem eliminar rotas públicas ou administrativas vizinhas.

## Status da validação

- Validação estrutural: disponível nos gates Python v1.72 e no scanner de nomes de controllers.
- Restore/build/testes .NET: **não executados neste ambiente**, pois o comando `dotnet` não está instalado. Portanto, este relatório não declara build ou testes como aprovados.
- Runtime e screenshots autenticados: **não executados**, porque dependem do runtime .NET e de um estado de autenticação real. O runner não cria dados ou usuários fictícios.

Para homologar em uma máquina com o SDK, execute `dotnet restore backend/PlantaoPro.sln`, `dotnet build backend/PlantaoPro.sln -c Release` e `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release`. Depois inicie a aplicação e rode `scripts/ui/run-visual-smoke.sh` com `PLANTAOPRO_BASE_URL` e `PLANTAOPRO_STORAGE_STATE` válidos.
