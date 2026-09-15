namespace PlantaoPro.Tests;

public sealed class V2170CoverageSubstitutionContractTests
{
    private static readonly string Root = RepositoryPathResolver.RepoRoot;
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    [Fact]
    public void PedidoPreservaOriginalEBloqueiaDuplicidadePresencaEFinanceiro()
    {
        var service = Read("backend/PlantaoPro.Api/Fase4OperationalServices.cs");
        Assert.Contains("A atribuição não pertence ao profissional ou não está confirmada", service);
        Assert.Contains("medico_checkins where escala_id=@escalaId", service);
        Assert.Contains("pagamentos where escala_id=@escalaId", service);
        Assert.Contains("Já existe uma solicitação ativa", service);
        Assert.Contains("Você permanece responsável até a cobertura ser efetivada", service);
    }

    [Fact]
    public void EfetivacaoEAtomicaVersionadaEIdempotente()
    {
        var service = Read("backend/PlantaoPro.Api/Fase4OperationalServices.cs");
        Assert.Contains("IsolationLevel.Serializable", service);
        Assert.Contains("pg_advisory_xact_lock", service);
        Assert.Contains("versao=versao+1", service);
        Assert.Contains("Cobertura já efetivada anteriormente", service);
        Assert.Contains("status='substituido'", service);
        Assert.Contains("nova_escala_id=@novaEscalaId", service);
        Assert.Contains("then 'EFETIVADO' else 'REVOGADO'", service);
    }

    [Fact]
    public void BancoGaranteUmPedidoAtivoEUmConvitePorCandidato()
    {
        var sql = Read("database/schema/420_v2170_cobertura_substituicoes.sql");
        Assert.Contains("ux_v2170_substituicao_ativa_por_escala", sql);
        Assert.Contains("ux_v2170_candidato_convite_ativo", sql);
        Assert.Contains("notificacao_status", sql);
        Assert.Contains("Sem disponibilidade confirmada para todo o período", Read("backend/PlantaoPro.Api/Fase4OperationalServices.cs"));
    }

    [Fact]
    public void EndpointsSeparamProfissionalDeGestao()
    {
        var controller = Read("backend/PlantaoPro.Api/Controllers/Fase4OperationalController.cs");
        Assert.Contains("api/medicos/me/substituicoes", controller);
        Assert.Contains("Authorize(Roles = RolesConstants.EscalasGestao)", controller);
    }
}
