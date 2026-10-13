using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R8-B17 — §29 item 9: "`MeuDiaService` facade sobre `ProductivityActionService` (compatibilidade intencional)."
///
/// RESULTADO DA VALIDAÇÃO (fonte canônica, sem regra inventada):
///
///   O Meu Dia NÃO tem fonte própria. A camada "Meu Dia" (<c>MeuDiaService</c> + <c>MeuDiaRepository</c>,
///   em <c>MeuDiaServices.cs</c>) é uma FACADE fina sobre o ÚNICO serviço canônico
///   <c>IProductivityActionService</c> (que delega a <c>IProductivityActionRepository</c>, onde vive a
///   única query derivada — CTE <c>with derived … count(*) filter(…)</c> de Active/Critical/Today/Overdue/Snoozed):
///
///     • <c>MeuDiaRepository.ReadAsync(q,ct)</c>     =&gt; <c>productivity.ListAsync(q,ct)</c>  (passthrough puro)
///     • <c>MeuDiaService.ObterResumo/Pendencias</c> =&gt; ReadAsync(Today)   [Today = DueTo UtcNow+1d, PageSize 25]
///     • <c>MeuDiaService.Agenda</c>                 =&gt; ReadAsync(Module:"CLINICO", DueTo UtcNow+1d)
///     • <c>MeuDiaService.Indicadores</c>            =&gt; <c>productivity.SummaryAsync(ct)</c>
///     • <c>MeuDiaService.AcoesRapidas</c>           =&gt; <c>productivity.QuickActions()</c>
///
///   A ÚNICA coisa que a facade agrega é um PADRÃO DE JANELA fixo ("hoje" UTC +1 dia, pág. 25; agenda CLINICO)
///   e o mapeamento de nomes PT-BR (ObterResumo/Indicadores/Pendencias/Agenda/AcoesRapidas) sobre ListAsync/
///   SummaryAsync/QuickActions. Sem segunda tabela, sem segunda query, sem conexão própria.
///
///   CONSUMIDORES: o Meu Dia vem SOMENTE pelo <c>MeuDiaController</c> (-&gt; IMeuDiaService); a "Central de
///   Ações" vem direto do <c>ProductivityActionController</c> (-&gt; IProductivityActionService). As duas
///   superfícies convergem no MESMO serviço canônico — o comentário no topo do arquivo já declara isso
///   ("Compatibility facade: Meu Dia and Central de Ações share one real, derived source").
///
///   CONCLUSÃO: canônico = <c>ProductivityActionService</c>; a facade é ADAPTER intencional documentado, não
///   duplicação de lógica de dados. Consolidação (apagar a facade) é estruturalmente possível e de baixo risco,
///   mas o rito §29 veda deleção antecipada nesta rodada (remover exigiria re-ligar o <c>MeuDiaController</c> e
///   relocar o padrão de janela "hoje"). Esta fatia ENTREGUE a validação + CONTRATO anti-drift que fixa:
///   (1) o "repositório" do Meu Dia é passthrough do serviço canônico; (2) o serviço Meu Dia só delega (sem
///   SQL/conexão/repositório próprio); (3) Meu Dia e Central de Ações rosam no mesmo serviço canônico. Zero mudança.
/// </summary>
public sealed class R8B17MeuDiaFacadeContratoTests
{
    private static string Api(string file) =>
        Path.Combine(RepositoryPathResolver.BackendRoot, "PlantaoPro.Api", file);
    private static string ApiCtl(string file) =>
        Path.Combine(RepositoryPathResolver.BackendRoot, "PlantaoPro.Api", "Controllers", file);

    [Fact]
    public void MeuDiaRepository_EhPassthroughDoUnicoSource()
    {
        var facade = File.ReadAllText(Api("MeuDiaServices.cs"));
        // O "repositório" do Meu Dia não tem query própria: encaminha para o serviço de produtividade.
        Assert.Contains("productivity.ListAsync(query, ct)", facade);
    }

    [Fact]
    public void MeuDiaService_SoDelega_E_NaoAbreFontePropria()
    {
        var facade = File.ReadAllText(Api("MeuDiaServices.cs"));
        // Cada método Meu Dia delega ao serviço canônico (Summary/QuickActions) ou ao passthrough (ReadAsync).
        Assert.Contains("productivity.SummaryAsync(ct)", facade);
        Assert.Contains("productivity.QuickActions()", facade);
        // A camada Meu Dia NÃO abre fonte própria: sem SQL inline, sem conexão, sem o repositório de produtividade direto.
        Assert.True(!facade.Contains("select", StringComparison.OrdinalIgnoreCase),
            "MeuDiaServices.cs não deveria ter SQL própria (a fonte é o ProductivityActionService).");
        Assert.DoesNotContain("NpgsqlConnection", facade);
        Assert.DoesNotContain("IProductivityActionRepository", facade);
    }

    [Fact]
    public void MeuDiaeCentralDeAcoes_RosamNoUnicoServicoCanonical()
    {
        var facade    = File.ReadAllText(Api("MeuDiaServices.cs"));
        var meuDiaCtl = File.ReadAllText(ApiCtl("MeuDiaController.cs"));
        var prodCtl   = File.ReadAllText(ApiCtl("ProductivityActionController.cs"));
        // Meu Dia (controller) depende da facade; Central de Ações depende do serviço canônico; e a facade depende do serviço.
        Assert.Contains("IMeuDiaService", meuDiaCtl);
        Assert.Contains("IProductivityActionService", prodCtl);
        Assert.Contains("IProductivityActionService", facade);
    }
}
