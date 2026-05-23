using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using System.Security.Claims;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/comunicacao")]
[Authorize]
public class ComunicacaoController : ControllerBase
{
    private readonly IConfiguration _cfg;
    private readonly IAuditService _audit;
    private readonly ILogger<ComunicacaoController> _logger;

    public ComunicacaoController(IConfiguration cfg, IAuditService audit, ILogger<ComunicacaoController> logger)
    {
        _cfg = cfg;
        _audit = audit;
        _logger = logger;
    }

    [HttpGet("conversas")]
    public async Task<IActionResult> ListarConversas()
    {
        var userId = GetUserId();
        _logger.LogInformation("Listando conversas para usuário {UserId}", userId);
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var data = await cn.QueryAsync<ConversaListDto>(@"select c.id as ""Id"",coalesce(c.titulo,'') as ""Titulo"",coalesce(c.tipo,'') as ""Tipo"",coalesce(c.status,'') as ""Status"",coalesce(max(m.reg_date), c.reg_date) as ""UltimaAtualizacao"",
                count(ml.id) filter (where coalesce(ml.lida,false)=false and ml.remetente_usuario_id<>@userId) as ""NaoLidas""
                from plantaopro.conversas c
                join plantaopro.conversa_participantes cp on cp.conversa_id=c.id and cp.reg_status='A'
                left join plantaopro.mensagens m on m.conversa_id=c.id and m.reg_status='A'
                left join plantaopro.mensagens ml on ml.conversa_id=c.id and ml.reg_status='A'
                where c.reg_status='A' and cp.usuario_id=@userId
                group by c.id,c.titulo,c.tipo,c.status,c.reg_date
                order by coalesce(max(m.reg_date), c.reg_date) desc", new { userId });
            return Ok(ApiResponse<IEnumerable<ConversaListDto>>.Ok(data, "Conversas listadas."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar conversas");
            return StatusCode(500, ApiResponse<string>.Fail("Erro ao listar conversas.", 500));
        }
    }

    [HttpPost("conversas")]
    public async Task<IActionResult> CriarConversa([FromBody] CriarConversaRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(request.Titulo) || request.Participantes is null || request.Participantes.Length == 0)
            return BadRequest(ApiResponse<string>.Fail("Informe título e participantes."));

        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var id = Guid.NewGuid();
            await cn.ExecuteAsync("insert into plantaopro.conversas(id,titulo,tipo,entidade,entidade_id,status,reg_status,reg_date) values(@id,@titulo,@tipo,@entidade,@entidadeId,'ABERTA','A',now())",
                new { id, titulo = request.Titulo.Trim(), tipo = request.Tipo ?? "SUPORTE", entidade = request.Entidade, entidadeId = request.EntidadeId });
            var participantes = request.Participantes.Distinct().Append(userId).Distinct().ToArray();
            foreach (var p in participantes)
            {
                await cn.ExecuteAsync("insert into plantaopro.conversa_participantes(id,conversa_id,usuario_id,papel,reg_status,reg_date) values(gen_random_uuid(),@conversa,@usuario,'PARTICIPANTE','A',now())",
                    new { conversa = id, usuario = p });
            }

            await _audit.LogAsync(userId, "CREATE", "conversas", id, "Conversa criada");
            return Ok(ApiResponse<Guid>.Ok(id, "Conversa criada com sucesso."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar conversa");
            return StatusCode(500, ApiResponse<string>.Fail("Erro ao criar conversa.", 500));
        }
    }

    [HttpPost("conversas/{id:guid}/mensagens")]
    public async Task<IActionResult> EnviarMensagem(Guid id, [FromBody] EnviarMensagemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Mensagem))
            return BadRequest(ApiResponse<string>.Fail("Mensagem não pode ser vazia."));
        var userId = GetUserId();

        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var participa = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.conversa_participantes where conversa_id=@id and usuario_id=@userId and reg_status='A'", new { id, userId });
            if (participa == 0)
                return Forbid();

            await cn.ExecuteAsync("insert into plantaopro.mensagens(id,conversa_id,remetente_usuario_id,mensagem,tipo,lida,reg_status,reg_date) values(gen_random_uuid(),@conversa,@remetente,@mensagem,'TEXTO',false,'A',now())",
                new { conversa = id, remetente = userId, mensagem = request.Mensagem.Trim() });
            await _audit.LogAsync(userId, "CREATE", "mensagens", id, "Mensagem enviada");
            return Ok(ApiResponse<string>.Ok("ok", "Mensagem enviada com sucesso."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar mensagem");
            return StatusCode(500, ApiResponse<string>.Fail("Erro ao enviar mensagem.", 500));
        }
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Usuário inválido"));
}
