using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PlantaoPro.Application.Legislacao;

namespace PlantaoPro.Api.Data;

/// <summary>
/// R100 (notificações pautadas no calendário fiscal): alerta de uma obrigação do período
/// exibido com o nível de SLA derivado do estado do reminder (R84) e a marca de escalonamento
/// quando o atraso após o vencimento atinge o limiar operacional da fonte única. O SLA aqui é o
/// prazo fiscal em si: ATRASADA &gt; CRITICO, VENCE_HOJE &gt; ALTA, VENCE_EM_CURTO_PRAZO &gt; MEDIA;
/// NENHUM para os estados sem pendência (CONCLUIDA, NAO_SE_APLICA, AGENDADA).
/// </summary>
public sealed record AlertaObrigacaoFiscalDto(
    string Codigo,
    string Nome,
    DateOnly DataVencimento,
    int DiasRestantes,
    string Estado,
    string SlaNivel,
    bool Escalonada);

/// <summary>Resultado da pauta: alertas do período + quantas notificações novas entraram na caixa (0 em reexecuções).</summary>
public sealed record GerarAlertasFiscaisResultado(
    int MesExibicao,
    int AnoExibicao,
    int JanelaAlertaDias,
    int LimiarEscalonamentoDias,
    IReadOnlyList<AlertaObrigacaoFiscalDto> Alertas,
    int NotificacoesCriadas);

/// <summary>
/// R100: pauta as notificações da caixa do usuário contra o calendário fiscal do tenant. Os
/// destinos sao os gestores da organizacao (dono ADMINISTRADOR_CLIENTE ou administrador do
/// tenant ADMINISTRADOR) e somente quando o modulo ADM360 e efetivo no tenant — a mesma regra
/// que libera o calendario na area administrativa. Idempotente pela chave canônica da caixa
/// ((usuario_id, origem_tipo, origem_id, titulo) parcial, on conflict do nothing): a mesma
/// obrigação/período nunca duplica para o mesmo usuário, e o escalonamento usa origem e título
/// próprios para coexistir com o aviso base. O id de origem é determinístico (MD5 de
/// tenant|código|período|variante), sem novo estado persistido.
/// </summary>
public interface ICalendarioFiscalAlertasService
{
    Task<GerarAlertasFiscaisResultado> GerarAsync(Guid tenantId, int? mesExibicao, int? anoExibicao, DateOnly hoje, CancellationToken ct = default);
}

public sealed class CalendarioFiscalAlertasService : ICalendarioFiscalAlertasService
{
    /// <summary>Limiar operacional (dias de atraso) da fonte única — nenhum número vive no código.</summary>
    private const string ParametroLimiar = "OBRIGACOES_FISCAIS_ESCALONAMENTO_ATRASO_DIAS";

    private const string OrigemBase = "OBRIGACAO_FISCAL";
    private const string OrigemEscalonada = "OBRIGACAO_FISCAL_ESCALONADA";

    private readonly ICalendarioFiscalService calendario;
    private readonly IParametrosLegaisRepository parametros;
    private readonly NotificacaoService notificacoes;
    private readonly IConfiguration cfg;

    public CalendarioFiscalAlertasService(
        ICalendarioFiscalService calendario,
        IParametrosLegaisRepository parametros,
        NotificacaoService notificacoes,
        IConfiguration cfg)
    {
        this.calendario = calendario;
        this.parametros = parametros;
        this.notificacoes = notificacoes;
        this.cfg = cfg;
    }

    /// <summary>Nível de SLA do estado do reminder (R84): quanto mais perto (ou além) do prazo, mais crítico.</summary>
    internal static string NivelSla(string estado) => estado switch
    {
        "ATRASADA" => "CRITICO",
        "VENCE_HOJE" => "ALTA",
        "VENCE_EM_CURTO_PRAZO" => "MEDIA",
        _ => "NENHUM"
    };

    /// <summary>Escalonamento: obrigação ativa, sem conclusão e atrasada há pelo menos o limiar da fonte única.</summary>
    internal static bool DeveEscalar(string estado, int diasRestantes, int limiarAtrasoDias) =>
        estado == "ATRASADA" && -diasRestantes >= limiarAtrasoDias;

    /// <summary>Origem determinística da dedupe da caixa: mesmo tenant/obrigação/período/variante sempre dá o mesmo id.</summary>
    private static Guid IdObrigacao(Guid tenantId, string codigo, int mesReferencia, int anoReferencia, string variante)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes($"fiscal|r100|{tenantId:N}|{codigo}|{mesReferencia:D2}/{anoReferencia:D4}|{variante}"));
        return new Guid(bytes.AsSpan(0, 16).ToArray());
    }

    public async Task<GerarAlertasFiscaisResultado> GerarAsync(Guid tenantId, int? mesExibicao, int? anoExibicao, DateOnly hoje, CancellationToken ct = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant da sessão indefinido.", nameof(tenantId));
        var mes = mesExibicao ?? hoje.Month;
        var ano = anoExibicao ?? hoje.Year;

        var linhas = await parametros.ListarAtivosAsync(null, ct);
        var limiar = ValorInt(linhas, ParametroLimiar);
        var calendarioPeriodo = await calendario.ObterCalendarioAsync(tenantId, mes, ano, hoje, ct);

        var alvos = calendarioPeriodo.Itens
            .Where(i => i.Estado is "ATRASADA" or "VENCE_HOJE" or "VENCE_EM_CURTO_PRAZO")
            .ToList();

        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.OpenAsync(ct);
        var temAdmEfetivo = await cn.ExecuteScalarAsync<bool>(
            new CommandDefinition("select exists(select 1 from plantaopro.modulos_efetivos(@t) f where upper(f.codigo)='ADM360')", new { t = tenantId }, cancellationToken: ct));
        var destinatarios = new List<Guid>();
        var donos = new List<Guid>();
        if (temAdmEfetivo)
        {
            destinatarios = (await cn.QueryAsync<Guid>(new CommandDefinition(
                @"select distinct u.id
                  from plantaopro.usuarios u
                  join plantaopro.usuarios_perfis up on up.usuario_id = u.id and up.reg_status = 'A'
                  join plantaopro.perfis p on p.id = up.perfil_id and p.reg_status = 'A'
                 where u.tenant_id = @t and u.reg_status = 'A' and u.status = 'ATIVO'
                   and p.codigo in ('ADMINISTRADOR_CLIENTE','ADMINISTRADOR')
                 order by 1", new { t = tenantId }, cancellationToken: ct))).ToList();
            donos = (await cn.QueryAsync<Guid>(new CommandDefinition(
                @"select distinct u.id
                  from plantaopro.usuarios u
                  join plantaopro.usuarios_perfis up on up.usuario_id = u.id and up.reg_status = 'A'
                  join plantaopro.perfis p on p.id = up.perfil_id and p.reg_status = 'A'
                 where u.tenant_id = @t and u.reg_status = 'A' and u.status = 'ATIVO'
                   and p.codigo = 'ADMINISTRADOR_CLIENTE'
                 order by 1", new { t = tenantId }, cancellationToken: ct))).ToList();
        }

        var criadas = 0;
        if (alvos.Count > 0 && destinatarios.Count > 0)
        {
            await using var tx = await cn.BeginTransactionAsync(ct);
            try
            {
                foreach (var item in alvos)
                {
                    var atraso = -item.DiasRestantes;
                    foreach (var uid in destinatarios)
                    {
                        if (await notificacoes.CriarNotificacaoAsync(uid, TituloBase(item.Nome), MensagemBase(item, atraso), "fiscal", tx,
                                OrigemBase, IdObrigacao(tenantId, item.Codigo, item.MesReferencia, item.AnoReferencia, "base")))
                            criadas++;
                    }
                    if (DeveEscalar(item.Estado, item.DiasRestantes, limiar))
                    {
                        foreach (var uid in donos)
                        {
                            if (await notificacoes.CriarNotificacaoAsync(uid, "ESCALONADO: " + TituloBase(item.Nome),
                                    $"Escalonado ao dono da organização: sem conclusão há {atraso} dia(s) após o vencimento de {item.DataVencimento:dd/MM/yyyy} (limiar de {limiar} dia(s)). Prazo final segue o calendário oficial do órgão.",
                                    "fiscal", tx, OrigemEscalonada, IdObrigacao(tenantId, item.Codigo, item.MesReferencia, item.AnoReferencia, "esc")))
                                criadas++;
                        }
                    }
                }
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        var alertas = alvos
            .Select(i => new AlertaObrigacaoFiscalDto(i.Codigo, i.Nome, i.DataVencimento, i.DiasRestantes, i.Estado, NivelSla(i.Estado), DeveEscalar(i.Estado, i.DiasRestantes, limiar)))
            .ToList();
        return new GerarAlertasFiscaisResultado(mes, ano, calendarioPeriodo.JanelaAlertaDias, limiar, alertas, criadas);
    }

    private static string TituloBase(string nome) => "Obrigação fiscal: " + nome;

    private static string MensagemBase(ItemObrigacaoFiscalDto item, int atraso) => item.Estado switch
    {
        "ATRASADA" => $"SLA CRITICO: atrasada há {atraso} dia(s); venceu em {item.DataVencimento:dd/MM/yyyy}. Prazo final segue o calendário oficial do órgão.",
        "VENCE_HOJE" => $"SLA ALTA: vence hoje ({item.DataVencimento:dd/MM/yyyy}). Prazo final segue o calendário oficial do órgão.",
        _ => $"SLA MEDIA: vence em {item.DiasRestantes} dia(s) ({item.DataVencimento:dd/MM/yyyy}). Prazo final segue o calendário oficial do órgão."
    };

    /// <summary>Dígio puro do parâmetro operacional (convenção da fonte única v2363).</summary>
    private static int ValorInt(List<ParametroLegalDto> linhas, string codigo)
    {
        var linha = linhas.FirstOrDefault(l => string.Equals(l.Codigo, codigo, StringComparison.OrdinalIgnoreCase));
        if (linha is null || !int.TryParse(linha.Valor.Trim(), out var valor))
            throw new InvalidOperationException($"Parâmetro legal {codigo} ausente ou malformado na fonte única.");
        return valor;
    }
}
