using System.Security.Cryptography;
using System.Text;
using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public sealed record PainelTvCallDto(string Senha, string NomeAbreviado, string Destino, DateTime Horario, string Status);
public sealed record PainelTvDto(string Nome, string? LogotipoUrl, string CorPrimaria, IReadOnlyList<PainelTvCallDto> Chamadas);

/// <summary>Consulta pública mínima do painel. O token em claro nunca é persistido ou registrado.</summary>
public sealed class PainelTvService
{
    private readonly IConfiguration configuration;
    private readonly IAuditService audit;

    public PainelTvService(IConfiguration configuration, IAuditService audit)
    {
        this.configuration = configuration;
        this.audit = audit;
    }

    public async Task<ApiResponse<PainelTvDto>> ObterAsync(Guid painelId, string? token, CancellationToken cancellationToken)
    {
        if (painelId == Guid.Empty || string.IsNullOrWhiteSpace(token) || token.Length < 32)
            return ApiResponse<PainelTvDto>.Fail("Painel indisponível.", 403);

        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
        await using var connection = new NpgsqlConnection(configuration.GetConnectionString("Default"));
        var command = new CommandDefinition(@"
select p.id as ""PainelId"", p.cliente_id as ""TenantId"", p.unidade_id as ""UnidadeId"",
       p.nome as ""Nome"", p.logotipo_url as ""LogotipoUrl"", p.cor_primaria as ""CorPrimaria"",
       t.id as ""TokenId""
  from plantaopro.paineis_publicos p
  join plantaopro.painel_publico_tokens t on t.painel_id=p.id and t.cliente_id=p.cliente_id
 where p.id=@painelId and p.ativo=true and p.reg_status='A'
   and t.token_hash=@tokenHash and t.revogado_em is null and t.expira_em>now() and t.reg_status='A'
 limit 1", new { painelId, tokenHash }, cancellationToken: cancellationToken);
        var painel = await connection.QueryFirstOrDefaultAsync(command);
        if (painel is null) return ApiResponse<PainelTvDto>.Fail("Painel indisponível.", 403);

        var calls = await connection.QueryAsync<PainelTvCallDto>(new CommandDefinition(@"
select coalesce(nullif(f.senha,''), right(f.id::text, 4)) as ""Senha"",
       case when coalesce(p.nome,f.paciente_nome,'')='' then ''
            when position(' ' in btrim(coalesce(p.nome,f.paciente_nome)))=0 then left(btrim(coalesce(p.nome,f.paciente_nome)),1)||'.'
            else left(split_part(btrim(coalesce(p.nome,f.paciente_nome)),' ',1),1)||'. '||left(reverse(split_part(reverse(btrim(coalesce(p.nome,f.paciente_nome))),' ',1)),1)||'.' end as ""NomeAbreviado"",
       coalesce(nullif(f.guiche,''),nullif(f.sala,''),f.setor,'') as ""Destino"", coalesce(f.chamado_em,f.updated_at,f.reg_date) as ""Horario"", f.status as ""Status""
  from plantaopro.painel_chamada_fila f
  left join plantaopro.pacientes p on p.id=f.paciente_id and p.cliente_id=f.cliente_id
  left join plantaopro.agendamentos a on a.id=f.agendamento_id and a.cliente_id=f.cliente_id
 where f.cliente_id=@TenantId and a.unidade_id=@UnidadeId and f.reg_status='A'
   and f.status in ('CHAMADO','EM_ATENDIMENTO')
 order by coalesce(f.chamado_em,f.updated_at,f.reg_date) desc limit 8", new { painel.TenantId, painel.UnidadeId }, cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition("update plantaopro.painel_publico_tokens set ultima_utilizacao_em=now() where id=@TokenId and cliente_id=@TenantId", new { painel.TokenId, painel.TenantId }, cancellationToken: cancellationToken));
        await audit.LogAsync(null, "PAINEL_TV_VISUALIZAR", "painel_publico", painelId, "Acesso público permitido sem dados clínicos.");
        return ApiResponse<PainelTvDto>.Ok(new PainelTvDto((string)painel.Nome, (string?)painel.LogotipoUrl, (string)painel.CorPrimaria, calls.ToList()), "Painel atualizado.");
    }
}
