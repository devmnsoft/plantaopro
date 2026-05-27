using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public sealed class MotivoElegibilidadeDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public bool Bloqueante { get; set; }
}

public sealed class MedicoElegibilidadeDto
{
    public bool Elegivel { get; set; }
    public bool Bloqueado { get; set; }
    public List<string> Alertas { get; set; } = new();
    public List<string> MotivosBloqueio { get; set; } = new();
    public decimal ScoreElegibilidade { get; set; }
    public string RecomendacaoTexto { get; set; } = string.Empty;
    public List<MotivoElegibilidadeDto> Motivos { get; set; } = new();
}

public sealed class ConflitoHorarioDetalheDto
{
    public Guid EscalaId { get; set; }
    public Guid PlantaoId { get; set; }
    public string Hospital { get; set; } = string.Empty;
    public string Especialidade { get; set; } = string.Empty;
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public string Status { get; set; } = string.Empty;
    public string GrauConflito { get; set; } = string.Empty;
}

public sealed class ConflitoHorarioService
{
    private readonly IConfiguration _cfg;
    public ConflitoHorarioService(IConfiguration cfg) => _cfg = cfg;
    private NpgsqlConnection Cn() => new(_cfg.GetConnectionString("Default"));

    public async Task<bool> ExisteConflitoAsync(Guid medicoId, DateTime dataInicio, DateTime dataFim, Guid? escalaIgnoradaId = null)
    {
        await using var cn = Cn();
        var conflito = await cn.ExecuteScalarAsync<int>(@"select count(1)
from plantaopro.escalas e
join plantaopro.plantoes p on p.id=e.plantao_id
where e.medico_id=@medicoId and e.reg_status='A'
  and lower(coalesce(e.status,'')) in ('solicitado','confirmado','em_andamento','realizado')
  and (@escalaIgnoradaId is null or e.id <> @escalaIgnoradaId)
  and @dataInicio < (p.data_fim + (@margem || ' minutes')::interval)
  and @dataFim > (p.data_inicio - (@margem || ' minutes')::interval)",
            new { medicoId, dataInicio, dataFim, escalaIgnoradaId, margem = 60 });
        return conflito > 0;
    }

    public async Task<IEnumerable<ConflitoHorarioDetalheDto>> ListarConflitosAsync(Guid medicoId, DateTime dataInicio, DateTime dataFim)
    {
        await using var cn = Cn();
        return await cn.QueryAsync<ConflitoHorarioDetalheDto>(@"select e.id as EscalaId,e.plantao_id as PlantaoId,
coalesce(h.nome_fantasia,'') as Hospital,coalesce(esp.nome,'') as Especialidade,
p.data_inicio as DataInicio,p.data_fim as DataFim,coalesce(e.status,'') as Status,
case when p.data_inicio < @dataFim and p.data_fim > @dataInicio then 'ALTO' else 'MEDIO' end as GrauConflito
from plantaopro.escalas e
join plantaopro.plantoes p on p.id=e.plantao_id
join plantaopro.hospitais h on h.id=p.hospital_id
join plantaopro.especialidades esp on esp.id=p.especialidade_id
where e.medico_id=@medicoId and e.reg_status='A' and lower(coalesce(e.status,'')) in ('solicitado','confirmado','em_andamento','realizado')
and @dataInicio < p.data_fim and @dataFim > p.data_inicio");
    }
}

public sealed class MedicoElegibilidadeService
{
    private readonly IConfiguration _cfg;
    private readonly ConflitoHorarioService _conflito;
    public MedicoElegibilidadeService(IConfiguration cfg, ConflitoHorarioService conflito)
    {
        _cfg = cfg;
        _conflito = conflito;
    }
    private NpgsqlConnection Cn() => new(_cfg.GetConnectionString("Default"));

    public async Task<MedicoElegibilidadeDto> VerificarElegibilidadeParaPlantaoAsync(Guid medicoId, Guid plantaoId)
    {
        await using var cn = Cn();
        var plantao = await cn.QueryFirstOrDefaultAsync<(Guid Id, Guid ClienteId, Guid EspecialidadeId, DateTime DataInicio, DateTime DataFim)>("select id,cliente_id as ClienteId,especialidade_id as EspecialidadeId,data_inicio as DataInicio,data_fim as DataFim from plantaopro.plantoes where id=@plantaoId and reg_status='A'", new { plantaoId });
        if (plantao.Id == Guid.Empty)
        {
            return new MedicoElegibilidadeDto { Elegivel = false, Bloqueado = true, MotivosBloqueio = new() { "Plantão não encontrado." }, ScoreElegibilidade = 0, RecomendacaoTexto = "Plantão inválido." };
        }

        var motivos = await ObterMotivosInelegibilidadeAsync(medicoId, plantaoId);
        var dto = new MedicoElegibilidadeDto();
        dto.Motivos = motivos.ToList();
        dto.MotivosBloqueio = dto.Motivos.Where(x => x.Bloqueante).Select(x => x.Mensagem).ToList();
        dto.Alertas = dto.Motivos.Where(x => !x.Bloqueante).Select(x => x.Mensagem).ToList();
        dto.Bloqueado = dto.MotivosBloqueio.Count > 0;
        dto.Elegivel = !dto.Bloqueado;
        dto.ScoreElegibilidade = Math.Max(0m, 100m - (dto.MotivosBloqueio.Count * 30m) - (dto.Alertas.Count * 10m));
        dto.RecomendacaoTexto = dto.Bloqueado ? "Médico inelegível para este plantão." : "Médico elegível para este plantão.";
        return dto;
    }

    public async Task<bool> VerificarConflitoHorarioAsync(Guid medicoId, DateTime dataInicio, DateTime dataFim) => await _conflito.ExisteConflitoAsync(medicoId, dataInicio, dataFim);
    public async Task<bool> VerificarLimiteHorasSemanaAsync(Guid medicoId, DateTime dataInicio, DateTime dataFim)
    {
        await using var cn = Cn();
        var horas = await cn.ExecuteScalarAsync<decimal>(@"select coalesce(sum(extract(epoch from (p.data_fim-p.data_inicio))/3600.0),0)
from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id
where e.medico_id=@medicoId and e.reg_status='A' and lower(coalesce(e.status,'')) in ('confirmado','realizado')
and date_trunc('week',p.data_inicio)=date_trunc('week',@dataInicio)", new { medicoId, dataInicio });
        var horasPlantao = (decimal)(dataFim - dataInicio).TotalHours;
        return (horas + horasPlantao) <= 60m;
    }
    public async Task<bool> VerificarEspecialidadeCompatívelAsync(Guid medicoId, Guid especialidadeId)
    {
        await using var cn = Cn();
        var count = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.medico_especialidades where medico_id=@medicoId and especialidade_id=@especialidadeId and reg_status='A'", new { medicoId, especialidadeId });
        return count > 0;
    }
    public Task<bool> VerificarDisponibilidadeAsync(Guid medicoId, DateTime dataInicio, DateTime dataFim) => Task.FromResult(true);
    public async Task<bool> VerificarStatusMedicoAsync(Guid medicoId)
    {
        await using var cn = Cn();
        return await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.medicos where id=@medicoId and reg_status='A' and coalesce(bloqueado,false)=false", new { medicoId }) > 0;
    }
    public Task<bool> VerificarDocumentacaoAsync(Guid medicoId) => Task.FromResult(true);

    public async Task<IEnumerable<MotivoElegibilidadeDto>> ObterMotivosInelegibilidadeAsync(Guid medicoId, Guid plantaoId)
    {
        var itens = new List<MotivoElegibilidadeDto>();
        await using var cn = Cn();
        var plantao = await cn.QueryFirstAsync<(Guid EspecialidadeId, DateTime DataInicio, DateTime DataFim)>("select especialidade_id as EspecialidadeId,data_inicio as DataInicio,data_fim as DataFim from plantaopro.plantoes where id=@plantaoId", new { plantaoId });
        if (!await VerificarStatusMedicoAsync(medicoId)) itens.Add(new MotivoElegibilidadeDto { Codigo = "MEDICO_INATIVO", Mensagem = "Médico inativo, bloqueado ou suspenso.", Bloqueante = true });
        if (!await VerificarEspecialidadeCompatívelAsync(medicoId, plantao.EspecialidadeId)) itens.Add(new MotivoElegibilidadeDto { Codigo = "ESPECIALIDADE", Mensagem = "Especialidade incompatível com o plantão.", Bloqueante = true });
        if (await VerificarConflitoHorarioAsync(medicoId, plantao.DataInicio, plantao.DataFim)) itens.Add(new MotivoElegibilidadeDto { Codigo = "CONFLITO", Mensagem = "Conflito de horário detectado.", Bloqueante = true });
        if (!await VerificarLimiteHorasSemanaAsync(medicoId, plantao.DataInicio, plantao.DataFim)) itens.Add(new MotivoElegibilidadeDto { Codigo = "LIMITE_HORAS", Mensagem = "Limite semanal de horas excedido.", Bloqueante = true });
        return itens;
    }
}

public sealed class MedicoRecomendacaoService
{
    private readonly IConfiguration _cfg;
    private readonly ConflitoHorarioService _conflitoHorarioService;

    public MedicoRecomendacaoService(IConfiguration cfg, ConflitoHorarioService conflitoHorarioService)
    {
        _cfg = cfg;
        _conflitoHorarioService = conflitoHorarioService;
    }

    private NpgsqlConnection Cn() => new(_cfg.GetConnectionString("Default"));

    public async Task<IEnumerable<MedicoPlantaoRecomendacaoDto>> RecomendarPlantoesAsync(Guid medicoId, int limite = 10)
    {
        await using var cn = Cn();
        var candidatos = (await cn.QueryAsync<(Guid PlantaoId, string HospitalNome, string EspecialidadeNome, DateTime DataInicio, DateTime DataFim, decimal Valor, bool TemEspecialidade, bool JaSolicitado)>(@"
select p.id as PlantaoId,
       coalesce(h.nome_fantasia,'') as HospitalNome,
       coalesce(e.nome,'') as EspecialidadeNome,
       p.data_inicio as DataInicio,
       p.data_fim as DataFim,
       p.valor as Valor,
       exists(select 1 from plantaopro.medico_especialidades me where me.medico_id=@medicoId and me.especialidade_id=p.especialidade_id and me.reg_status='A') as TemEspecialidade,
       exists(select 1 from plantaopro.escalas esc where esc.plantao_id=p.id and esc.medico_id=@medicoId and esc.reg_status='A') as JaSolicitado
from plantaopro.plantoes p
join plantaopro.hospitais h on h.id=p.hospital_id
join plantaopro.especialidades e on e.id=p.especialidade_id
where p.reg_status='A' and lower(coalesce(p.status,'')) in ('aberto','em_escala') and p.data_inicio >= now()
order by p.data_inicio asc
limit @buscaLimite", new { medicoId, buscaLimite = Math.Max(limite * 3, 20) })).ToList();

        var recomendacoes = new List<MedicoPlantaoRecomendacaoDto>();
        foreach (var candidato in candidatos)
        {
            if (candidato.JaSolicitado)
            {
                continue;
            }

            var score = 100m;
            var motivos = new List<string>();
            if (!candidato.TemEspecialidade)
            {
                score -= 70m;
                motivos.Add("especialidade divergente");
            }

            var temConflito = await _conflitoHorarioService.ExisteConflitoAsync(medicoId, candidato.DataInicio, candidato.DataFim);
            if (temConflito)
            {
                score -= 50m;
                motivos.Add("conflito de horário");
            }

            var horas = (decimal)(candidato.DataFim - candidato.DataInicio).TotalHours;
            score += Math.Min(20m, horas);
            if (candidato.Valor >= 2000m)
            {
                score += 10m;
            }

            var textoMotivo = motivos.Count == 0 ? "Alta aderência ao seu perfil, sem conflitos de horário." : "Atenção: " + string.Join("; ", motivos) + ".";
            recomendacoes.Add(new MedicoPlantaoRecomendacaoDto(candidato.PlantaoId, candidato.HospitalNome, candidato.EspecialidadeNome, candidato.DataInicio, candidato.DataFim, candidato.Valor, Math.Max(0m, score), textoMotivo));
        }

        return recomendacoes.OrderByDescending(x => x.Score).ThenBy(x => x.DataInicio).Take(Math.Max(limite, 1)).ToArray();
    }
}
