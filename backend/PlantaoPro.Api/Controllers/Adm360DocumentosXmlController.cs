using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/administrativo360/xml")]
public sealed class Adm360DocumentosXmlController : ControllerBase
{
    private readonly IDocumentosXmlRepository repository;
    private readonly ICurrentUserService current;
    private readonly ILogger<Adm360DocumentosXmlController> logger;

    public Adm360DocumentosXmlController(
        IDocumentosXmlRepository repository,
        ICurrentUserService current,
        ILogger<Adm360DocumentosXmlController> logger)
    {
        this.repository = repository;
        this.current = current;
        this.logger = logger;
    }

    private (Guid Tenant, Guid User) Context() =>
        (current.TenantId ?? throw new UnauthorizedAccessException("Tenant não identificado."),
         current.UserId ?? throw new UnauthorizedAccessException("Usuário não autenticado."));

    [HttpGet]
    [Authorize(Policy = "Adm360.ImportarXml")]
    public async Task<IActionResult> Listar(
        [FromQuery] string? status,
        [FromQuery] bool? quarentena,
        CancellationToken ct)
    {
        var (tenant, _) = Context();
        var lista = await repository.ListarDocumentosAsync(tenant, status, quarentena, ct);
        return Ok(lista);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Adm360.ImportarXml")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken ct)
    {
        var (tenant, _) = Context();
        var doc = await repository.ObterDocumentoPorIdAsync(tenant, id, ct);
        if (doc is null) return NotFound("Documento XML não encontrado.");
        return Ok(doc);
    }

    [HttpGet("{id:guid}/download")]
    [Authorize(Policy = "Adm360.ImportarXml")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var (tenant, _) = Context();

        // B1: download dos bytes originais preservados (BOM/encoding exatos).
        var doc = await repository.ObterXmlBytesAsync(tenant, id, ct);
        if (doc is null) return NotFound("Documento XML não encontrado.");

        // P4: download idempotente — ETag = SHA-256 dos bytes armazenados; If-None-Match → 304 sem reenviar o corpo.
        var etag = EtagHttp.Para(doc.Value.Hash);
        Response.Headers.ETag = etag;
        if (EtagHttp.RevalidacaoSatisfeita(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);

        return File(doc.Value.Bytes, "application/xml", $"{doc.Value.ChaveAcesso}.xml");
    }

    [HttpPost("importar-manual")]
    [Authorize(Policy = "Adm360.ImportarXml")]
    public async Task<IActionResult> ImportarManual([FromBody] ImportarXmlManualCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        // P0 (homologação): valida tamanho antes de ler/parsear integralmente o conteúdo
        // (memória previsível; mesmo teto aplicado pelo front web antes do upload).
        if (string.IsNullOrWhiteSpace(command.XmlConteudo)) return BadRequest("Forneça o conteúdo XML do documento fiscal.");
        if (command.XmlConteudo.Length > 2_000_000) return BadRequest("O conteúdo XML excede o limite de 2.000.000 caracteres.");
        if (command.XmlBytes is { Length: > (2 * 1024 * 1024) }) return BadRequest("O arquivo XML excede o limite de 2 MiB.");
        var id = await repository.ImportarXmlAsync(tenant, user, command, ct);
        logger.LogInformation("XML {DocId} importado manualmente pelo usuário {UsuarioId} no tenant {TenantId}.", id, user, tenant);
        return Ok(new { id });
    }

    [HttpPost("manifestar")]
    [Authorize(Policy = "Adm360.ManifestarDfe")]
    public async Task<IActionResult> Manifestar([FromBody] ManifestarDocumentoCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        await repository.ManifestarDocumentoAsync(tenant, user, command, ct);
        logger.LogInformation("Manifestação enviada para documento {DocId} pelo usuário {UsuarioId}.", command.DocumentoId, user);
        return Ok(new { sucesso = true });
    }

    [HttpPost("vincular-recebimento")]
    [Authorize(Policy = "Adm360.VincularDocumentos")]
    public async Task<IActionResult> VincularRecebimento([FromBody] VincularDocumentoRecebimentoCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        await repository.VincularRecebimentoAsync(tenant, user, command, ct);
        logger.LogInformation("Documento {DocId} vinculado ao pedido {PedidoId} pelo usuário {UsuarioId}.", command.DocumentoId, command.PedidoId, user);
        return Ok(new { sucesso = true });
    }

    [HttpGet("sincronizacoes")]
    [Authorize(Policy = "Adm360.ConfigurarIntegracao")]
    public async Task<IActionResult> ListarSincronizacoes(CancellationToken ct)
    {
        var (tenant, _) = Context();
        var lista = await repository.ListarSincronizacoesAsync(tenant, ct);
        return Ok(lista);
    }

    [HttpPost("sincronizar-dfe")]
    [Authorize(Policy = "Adm360.ManifestarDfe")]
    public async Task<IActionResult> SincronizarDfe([FromBody] ExecutarSincronizacaoDfeCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        await repository.ExecutarSincronizacaoDfeAsync(tenant, user, command, ct);
        return Ok(new { sucesso = true });
    }
}
