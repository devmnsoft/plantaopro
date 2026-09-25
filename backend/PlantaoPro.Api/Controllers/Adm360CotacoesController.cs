using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/administrativo360/cotacoes")]
public sealed class Adm360CotacoesController : ControllerBase
{
    private readonly ICotacoesRepository repository;
    private readonly ICurrentUserService current;
    private readonly ILogger<Adm360CotacoesController> logger;

    public Adm360CotacoesController(
        ICotacoesRepository repository,
        ICurrentUserService current,
        ILogger<Adm360CotacoesController> logger)
    {
        this.repository = repository;
        this.current = current;
        this.logger = logger;
    }

    private (Guid Tenant, Guid User) Context() =>
        (current.TenantId ?? throw new UnauthorizedAccessException("Tenant não identificado."),
         current.UserId ?? throw new UnauthorizedAccessException("Usuário não autenticado."));

    [HttpGet]
    [Authorize(Policy = "Adm360.CotacaoConsultar")]
    public async Task<IActionResult> Listar(
        [FromQuery] string? status,
        [FromQuery] string? provedor,
        CancellationToken ct)
    {
        var (tenant, _) = Context();
        var lista = await repository.ListarCotacoesAsync(tenant, status, provedor, ct);
        return Ok(lista);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Adm360.CotacaoConsultar")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken ct)
    {
        var (tenant, _) = Context();
        var cotacao = await repository.ObterCotacaoPorIdAsync(tenant, id, ct);
        if (cotacao is null) return NotFound("Cotação não encontrada.");
        return Ok(cotacao);
    }

    [HttpPost("capturar")]
    [Authorize(Policy = "Adm360.CotacaoConsultar")]
    public async Task<IActionResult> Capturar([FromBody] CapturarCotacaoCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        var id = await repository.CapturarCotacaoAsync(tenant, user, command, ct);
        logger.LogInformation("Cotação {CotacaoId} capturada/atualizada pelo usuário {UsuarioId} no tenant {TenantId}.", id, user, tenant);
        return Ok(new { id });
    }

    [HttpGet("{id:guid}/anexos/{anexoId:guid}")]
    [Authorize(Policy = "Adm360.ConsultarAnexos")]
    public async Task<IActionResult> ObterAnexo(Guid id, Guid anexoId, CancellationToken ct)
    {
        var (tenant, _) = Context();
        var anexo = await repository.ObterAnexoAsync(tenant, anexoId, ct);
        if (anexo is null || anexo.Value.Bytes is null) return NotFound("Anexo não encontrado ou sem conteúdo.");
        return File(anexo.Value.Bytes, anexo.Value.ContentType, anexo.Value.Nome);
    }

    [HttpGet("mapeamentos")]
    [Authorize(Policy = "Adm360.MapearCadastros")]
    public async Task<IActionResult> ListarMapeamentos([FromQuery] string? provedor, [FromQuery] string? tipoEntidade, CancellationToken ct)
    {
        var (tenant, _) = Context();
        var lista = await repository.ListarMapeamentosAsync(tenant, provedor, tipoEntidade, ct);
        return Ok(lista);
    }

    [HttpPost("mapeamentos")]
    [Authorize(Policy = "Adm360.MapearCadastros")]
    public async Task<IActionResult> SalvarMapeamento([FromBody] SalvarMapeamentoDeParaCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        var id = await repository.SalvarMapeamentoAsync(tenant, user, command, ct);
        logger.LogInformation("Mapeamento {MapeamentoId} salvo pelo usuário {UsuarioId}.", id, user);
        return Ok(new { id });
    }

    [HttpPost("relacionar-item")]
    [Authorize(Policy = "Adm360.MapearProdutos")]
    public async Task<IActionResult> RelacionarItem([FromBody] RelacionarItemCotacaoCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        await repository.RelacionarItemAsync(tenant, user, command, ct);
        logger.LogInformation("Item {ItemId} relacionado pelo usuário {UsuarioId}.", command.CotacaoItemId, user);
        return Ok(new { sucesso = true });
    }

    [HttpPost("{id:guid}/gerar-orcamento")]
    [Authorize(Policy = "Adm360.ElaborarOrcamento")]
    public async Task<IActionResult> GerarOrcamento(Guid id, CancellationToken ct)
    {
        var (tenant, user) = Context();
        var orcamentoId = await repository.GerarOrcamentoCirurgicoAsync(tenant, user, new GerarOrcamentoDaCotacaoCommand(id), ct);
        logger.LogInformation("Orçamento {OrcamentoId} gerado para cotação {CotacaoId}.", orcamentoId, id);
        return Ok(new { orcamentoId });
    }

    [HttpPost("{id:guid}/aprovar-resposta")]
    [Authorize(Policy = "Adm360.AprovarResposta")]
    public async Task<IActionResult> AprovarResposta(Guid id, CancellationToken ct)
    {
        var (tenant, user) = Context();
        var respostaId = await repository.AprovarRespostaAsync(tenant, user, new AprovarRespostaCotacaoCommand(id), ct);
        logger.LogInformation("Resposta {RespostaId} da cotação {CotacaoId} aprovada pelo usuário {UsuarioId}.", respostaId, id, user);
        return Ok(new { respostaId });
    }

    [HttpGet("respostas")]
    [Authorize(Policy = "Adm360.CotacaoConsultar")]
    public async Task<IActionResult> ListarRespostas([FromQuery] string? status, CancellationToken ct)
    {
        var (tenant, _) = Context();
        var respostas = await repository.ListarRespostasAsync(tenant, status, ct);
        return Ok(respostas);
    }

    [HttpGet("respostas/{respostaId:guid}")]
    [Authorize(Policy = "Adm360.CotacaoConsultar")]
    public async Task<IActionResult> ObterRespostaPorId(Guid respostaId, CancellationToken ct)
    {
        var (tenant, _) = Context();
        var resposta = await repository.ObterRespostaPorIdAsync(tenant, respostaId, ct);
        if (resposta is null) return NotFound("Resposta não encontrada.");
        return Ok(resposta);
    }

    [HttpPost("respostas/{respostaId:guid}/transmitir")]
    [Authorize(Policy = "Adm360.TransmitirResposta")]
    public async Task<IActionResult> TransmitirResposta(Guid respostaId, CancellationToken ct)
    {
        var (tenant, user) = Context();
        await repository.TransmitirRespostaAsync(tenant, user, new TransmitirRespostaCommand(respostaId), ct);
        return Ok(new { sucesso = true });
    }

    [HttpGet("contas-portal")]
    [Authorize(Policy = "Adm360.ConfigurarIntegracao")]
    public async Task<IActionResult> ListarContasPortal(CancellationToken ct)
    {
        var (tenant, _) = Context();
        var contas = await repository.ListarContasPortalAsync(tenant, ct);
        return Ok(contas);
    }

    [HttpPost("contas-portal")]
    [Authorize(Policy = "Adm360.ConfigurarIntegracao")]
    public async Task<IActionResult> ConfigurarContaPortal([FromBody] ConfigurarPortalContaCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        var id = await repository.ConfigurarContaPortalAsync(tenant, user, command, ct);
        return Ok(new { id });
    }

    [HttpGet("estabelecimentos")]
    [Authorize(Policy = "Adm360.ConfigurarIntegracao")]
    public async Task<IActionResult> ListarEstabelecimentos(CancellationToken ct)
    {
        var (tenant, _) = Context();
        var lista = await repository.ListarEstabelecimentosAsync(tenant, ct);
        return Ok(lista);
    }

    [HttpPost("estabelecimentos")]
    [Authorize(Policy = "Adm360.ConfigurarIntegracao")]
    public async Task<IActionResult> CriarEstabelecimento([FromBody] CriarEstabelecimentoCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        var id = await repository.CriarEstabelecimentoAsync(tenant, user, command, ct);
        return Ok(new { id });
    }

    [HttpGet("capacidades")]
    [Authorize(Policy = "Adm360.ConfigurarIntegracao")]
    public async Task<IActionResult> ListarCapacidades(CancellationToken ct)
    {
        var (tenant, _) = Context();
        var lista = await repository.ListarCapacidadesAsync(tenant, ct);
        return Ok(lista);
    }

    [HttpPost("capacidades")]
    [Authorize(Policy = "Adm360.ConfigurarIntegracao")]
    public async Task<IActionResult> HabilitarCapacidade([FromBody] HabilitarCapacidadeCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        await repository.HabilitarCapacidadeAsync(tenant, user, command, ct);
        return Ok(new { sucesso = true });
    }
}
