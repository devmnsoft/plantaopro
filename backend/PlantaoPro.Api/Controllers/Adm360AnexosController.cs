using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Infrastructure.Administrativo360;

namespace PlantaoPro.Api.Controllers;

/// <summary>
/// R72 (rodada 72): anexos de arquivos binarios (PDF/Word/imagem) por entidade — pedido do usuario.
/// Politica Adm360.Ver em todos os endpoints (mesmo nivel que as acoes de pre-emissao fiscal):
/// granularidade propria de permissao de anexos e follow-up documentado na evidencia da rodada.
/// </summary>
[ApiController]
[Route("api/administrativo360/anexos")]
[Authorize(Policy = "Adm360.Ver")]
public sealed class Adm360AnexosController : ControllerBase
{
    private readonly IAnexosArquivosRepository repository;
    private readonly ICurrentUserService current;
    private readonly ILogger<Adm360AnexosController> logger;

    public Adm360AnexosController(
        IAnexosArquivosRepository repository,
        ICurrentUserService current,
        ILogger<Adm360AnexosController> logger)
    {
        this.repository = repository;
        this.current = current;
        this.logger = logger;
    }

    private (Guid Tenant, Guid User) Context() =>
        (current.TenantId ?? throw new UnauthorizedAccessException("Tenant não identificado."),
         current.UserId ?? throw new UnauthorizedAccessException("Usuário não autenticado."));

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string categoria, [FromQuery] Guid? entidadeId, CancellationToken ct)
    {
        if (!AnexosArquivosRepository.CategoriasPermitidas.Contains(categoria?.ToUpperInvariant() ?? ""))
            return BadRequest(new { erro = "Categoria inválida." });
        var (tenant, _) = Context();
        return Ok(await repository.ListarAsync(tenant, categoria!.ToUpperInvariant(), entidadeId, ct));
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] AnexoArquivoCriarCommand comando, CancellationToken ct)
    {
        var (tenant, user) = Context();
        var erro = AnexosArquivosRepository.ValidarArquivo(comando.NomeArquivo, comando.Tamanho);
        if (erro is not null) return BadRequest(new { erro });
        if (!AnexosArquivosRepository.CategoriasPermitidas.Contains(comando.Categoria?.ToUpperInvariant() ?? ""))
            return BadRequest(new { erro = "Categoria inválida." });
        if (string.IsNullOrWhiteSpace(comando.ConteudoBase64))
            return BadRequest(new { erro = "O conteúdo do arquivo não chegou até o servidor." });
        try
        {
            var id = await repository.CriarAsync(tenant, user, comando with { Categoria = comando.Categoria?.ToUpperInvariant() ?? "" }, ct);
            logger.LogInformation("Anexo {AnexoId} ({Categoria}) gravado pelo usuário {UsuarioId} no tenant {TenantId}.", id, comando.Categoria, user, tenant);
            return Ok(new { id });
        }
        catch (FormatException)
        {
            return BadRequest(new { erro = "O arquivo chegou corrompido; tente enviar de novo." });
        }
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Baixar(Guid id, CancellationToken ct)
    {
        var (tenant, _) = Context();
        var anexo = await repository.ObterConteudoAsync(tenant, id, ct);
        if (anexo is null) return NotFound(new { erro = "Arquivo não encontrado." });
        return File(anexo.Bytes, string.IsNullOrWhiteSpace(anexo.TipoMime) ? "application/octet-stream" : anexo.TipoMime, anexo.NomeOriginal);
    }

    [HttpPost("{id:guid}/remover")]
    public async Task<IActionResult> Remover(Guid id, CancellationToken ct)
    {
        var (tenant, _) = Context();
        var ok = await repository.RemoverAsync(tenant, id, ct);
        return ok ? Ok(new { sucesso = true }) : NotFound(new { erro = "Arquivo não encontrado." });
    }
}
