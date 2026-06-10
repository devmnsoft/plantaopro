using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using System.Security.Claims;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notificacoes")]
public sealed class NotificacoesPreferenciasFase4Controller : ControllerBase
{
    private readonly IConfiguration cfg;
    private readonly IAuditService audit;
    public NotificacoesPreferenciasFase4Controller(IConfiguration cfg, IAuditService audit) { this.cfg = cfg; this.audit = audit; }
    private Guid Uid() => Guid.Parse(User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Usuário inválido"));

    [HttpGet("preferencias")]
    public async Task<IActionResult> Preferencias()
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<object>("select tipo_evento, in_app, email, push, whatsapp from plantaopro.notificacao_preferencias where usuario_id=@uid and reg_status='A' order by tipo_evento", new { uid = Uid() });
        return Ok(ApiResponse<IEnumerable<object>>.Ok(rows, "Preferências de notificação carregadas."));
    }

    [HttpPut("preferencias")]
    public async Task<IActionResult> AtualizarPreferencias([FromBody] PreferenciaNotificacaoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TipoEvento)) return BadRequest(ApiResponse<string>.Fail("Informe o tipo de evento."));
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.ExecuteAsync(@"insert into plantaopro.notificacao_preferencias(id,usuario_id,tipo_evento,in_app,email,push,whatsapp,reg_date,reg_status)
values(gen_random_uuid(),@uid,@tipo,@inApp,@email,@push,@whatsapp,now(),'A')
on conflict (usuario_id,tipo_evento) do update set in_app=@inApp,email=@email,push=@push,whatsapp=@whatsapp,reg_update=now()", new { uid = Uid(), tipo = request.TipoEvento.Trim().ToUpperInvariant(), inApp = request.InApp, email = request.Email, push = request.Push, whatsapp = request.Whatsapp });
        await audit.LogAsync(Uid(), "UPDATE", "notificacoes", null, "Preferências de notificação atualizadas");
        return Ok(ApiResponse<string>.Ok("ok", "Preferências atualizadas."));
    }

    [HttpPost("reprocessar-pendentes")]
    public async Task<IActionResult> ReprocessarPendentes()
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var total = await cn.ExecuteAsync("update plantaopro.notificacao_filas set status='PENDENTE', tentativas=0, reg_update=now() where status in ('ERRO','AGUARDANDO')");
        await audit.LogAsync(Uid(), "UPDATE", "notificacoes", null, "Fila de notificações reprocessada", valorNovo: total.ToString());
        return Ok(ApiResponse<string>.Ok("ok", total + " notificação(ões) reprocessada(s)."));
    }
}

[ApiController]
[Authorize]
[Route("api/comunicacao")]
public sealed class ComunicacaoFase4Controller : ControllerBase
{
    private readonly IConfiguration cfg;
    private readonly IAuditService audit;
    public ComunicacaoFase4Controller(IConfiguration cfg, IAuditService audit) { this.cfg = cfg; this.audit = audit; }
    private Guid Uid() => Guid.Parse(User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Usuário inválido"));

    [HttpGet("conversas/{id:guid}")]
    public async Task<IActionResult> Conversa(Guid id)
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var participa = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.conversa_participantes where conversa_id=@id and usuario_id=@uid and reg_status='A'", new { id, uid = Uid() });
        if (participa == 0) return Forbid();
        var conversa = await cn.QueryFirstOrDefaultAsync<ComunicacaoConversaDetalheDto>(@"select c.id as ""Id"", coalesce(c.titulo,'') as ""Titulo"", coalesce(c.tipo,'') as ""Tipo"", coalesce(c.status,'') as ""Status"",
       coalesce(c.entidade,'') as ""Entidade"", c.entidade_id as ""EntidadeId"", c.reg_date as ""RegDate""
from plantaopro.conversas c
where c.id=@id and c.reg_status='A'", new { id });
        if (conversa is null) return NotFound(ApiResponse<string>.Fail("Conversa não encontrada.", 404));

        var mensagens = await cn.QueryAsync<object>(@"select m.id as ""Id"", m.conversa_id as ""ConversaId"", m.remetente_usuario_id as ""RemetenteUsuarioId"",
       coalesce(u.nome,u.email,'') as ""RemetenteNome"", coalesce(m.mensagem,'') as ""Mensagem"", coalesce(m.tipo,'TEXTO') as ""Tipo"",
       m.anexo_url as ""AnexoUrl"", coalesce(m.lida,false) as ""Lida"", m.reg_date as ""RegDate"",
       case when m.remetente_usuario_id=@uid then true else false end as ""MinhaMensagem""
from plantaopro.mensagens m
left join plantaopro.usuarios u on u.id=m.remetente_usuario_id
where m.conversa_id=@id and m.reg_status='A'
order by m.reg_date", new { id, uid = Uid() });
        var participantes = await cn.QueryAsync<object>(@"select cp.usuario_id as ""UsuarioId"", coalesce(u.nome,u.email,'') as ""Nome"", coalesce(u.email,'') as ""Email"",
       coalesce(u.perfil,cp.papel,'') as ""Perfil"", false as ""Online""
from plantaopro.conversa_participantes cp
left join plantaopro.usuarios u on u.id=cp.usuario_id
where cp.conversa_id=@id and cp.reg_status='A'
order by coalesce(u.nome,u.email,'')", new { id });
        conversa.Mensagens = mensagens;
        conversa.Participantes = participantes;
        return Ok(ApiResponse<ComunicacaoConversaDetalheDto>.Ok(conversa, "Conversa carregada."));
    }

    [HttpPost("conversas/{id:guid}/encerrar")]
    public async Task<IActionResult> Encerrar(Guid id)
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var participa = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.conversa_participantes where conversa_id=@id and usuario_id=@uid and reg_status='A'", new { id, uid = Uid() });
        if (participa == 0) return Forbid();
        await cn.ExecuteAsync("update plantaopro.conversas set status='ENCERRADA', updated_by=@uid, reg_update=now() where id=@id", new { id, uid = Uid() });
        await audit.LogAsync(Uid(), "UPDATE", "conversas", id, "Conversa encerrada");
        return Ok(ApiResponse<string>.Ok("ok", "Conversa encerrada."));
    }

    [HttpGet("templates")]
    public async Task<IActionResult> Templates()
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<object>("select id, nome, tipo, canal, assunto, conteudo, status from plantaopro.comunicacao_templates where reg_status='A' order by nome limit 100");
        return Ok(ApiResponse<IEnumerable<object>>.Ok(rows, "Templates carregados."));
    }

    [HttpPost("templates")]
    public async Task<IActionResult> CriarTemplate([FromBody] ComunicacaoTemplateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome) || string.IsNullOrWhiteSpace(request.Conteudo)) return BadRequest(ApiResponse<string>.Fail("Informe nome e conteúdo do template."));
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var id = Guid.NewGuid();
        await cn.ExecuteAsync("insert into plantaopro.comunicacao_templates(id,nome,tipo,canal,assunto,conteudo,status,created_by,reg_date,reg_status) values(@id,@nome,@tipo,'INTERNO',@assunto,@conteudo,'ATIVO',@uid,now(),'A')", new { id, nome = request.Nome.Trim(), tipo = request.Tipo ?? "OPERACIONAL", assunto = request.Assunto, conteudo = request.Conteudo, uid = Uid() });
        await audit.LogAsync(Uid(), "CREATE", "comunicacao_templates", id, "Template de comunicação criado");
        return Ok(ApiResponse<Guid>.Ok(id, "Template criado."));
    }
}

public record ComunicacaoTemplateRequest(string Nome, string? Tipo, string? Assunto, string Conteudo);


public sealed class ComunicacaoConversaDetalheDto
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Entidade { get; set; } = string.Empty;
    public Guid? EntidadeId { get; set; }
    public IEnumerable<object> Mensagens { get; set; } = Array.Empty<object>();
    public IEnumerable<object> Participantes { get; set; } = Array.Empty<object>();
}
