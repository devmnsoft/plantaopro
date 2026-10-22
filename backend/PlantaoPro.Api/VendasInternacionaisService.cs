using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

// ============================================================================
// R89 (venda para fora do país): faturamento internacional simplificado —
// cotações institucionais com moeda base BRL (USD/EUR/GBP), catálogo de
// alíquotas padrão de VAT por país de destino e nota fiscal não-tradicional
// amarrada à fatura SaaS (conversão pela cotação + VAT acrescido do destino).
// Superfície global de administração (mesmo plano dos trilhos e da cobrança).
// Regras puras em VendasInternacionaisRegras; valores vêm de parametros_legais.
// ============================================================================

public sealed record ViCotacaoDto(string Moeda, DateOnly Data, decimal Taxa, string Fonte);

public sealed record ViPaisVatDto(string Pais, string Nome, decimal Aliquota, string Fonte);

public sealed record ViFaturaDto(Guid Id, string? ClienteNome, string Status, decimal ValorBrl, DateOnly? Vencimento, DateOnly Competencia);

public sealed record ViNotaComando(Guid FaturaId, string PaisDestino, string Moeda);

public sealed record ViAnularNotaCommand(Guid Id, string? Motivo);

public sealed record ViCotacaoCommand(string Moeda, DateOnly Data, decimal Taxa, string? Fonte);

public sealed record ViNotaSimuladaDto(
    Guid FaturaId, string? ClienteNome, string Moeda, string PaisDestino, string PaisNome,
    DateOnly DataEmisso, decimal ValorBrl, decimal TaxaAplicada, DateOnly DataCotacao,
    decimal ValorMoeda, decimal Aliquota, decimal ValorVat, decimal TotalMoeda,
    string FormaDocumento, IReadOnlyList<string> Fontes);

public sealed record ViNotaDto(
    Guid Id, Guid FaturaId, string? ClienteNome, string Moeda, string PaisDestino, string PaisNome,
    DateOnly DataEmisso, decimal ValorBrl, decimal TaxaAplicada, DateOnly DataCotacao,
    decimal ValorMoeda, decimal Aliquota, decimal ValorVat, decimal TotalMoeda,
    string FormaDocumento, string Status, string? MotivoAnulacao, DateTime CriadoEm);

public sealed class VendasInternacionaisService
{
    private sealed record CotacaoLinhaDto(string Moeda, DateOnly Data, decimal Taxa, string Fonte);
    private sealed record PaisVatLinhaDto(string Pais, string Nome, decimal Aliquota, string Fonte);
    private sealed record FaturaLinhaDto(Guid Id, string? ClienteNome, string Status, decimal ValorBrl, DateOnly? Vencimento, DateOnly Competencia);
    private sealed record NotaLinhaDto(
        Guid Id, Guid FaturaId, string? ClienteNome, string Moeda, string PaisDestino, string PaisNome,
        DateOnly DataEmisso, decimal ValorBrl, DateOnly DataCotacao, decimal TaxaAplicada,
        decimal ValorMoeda, decimal Aliquota, decimal ValorVat, decimal TotalMoeda,
        string FormaDocumento, string Status, string? MotivoAnulacao, DateTime CriadoEm);
    private sealed record ParamLinhaDto(string Codigo, string? Valor);
    private sealed record FaturaBaseDto(Guid Id, string? ClienteNome, string Status, decimal Valor);
    private sealed record DestinoPaisDto(string Pais, string Nome, decimal Aliquota);
    private sealed record CotacaoBaseDto(decimal Taxa, DateOnly Data);
    private sealed record ContextoNota(FaturaBaseDto Fatura, DestinoPaisDto Destino, CotacaoBaseDto Cotacao, string Moeda);

    private static readonly string[] FontesNota =
    {
        "Moeda base do faturamento: real brasileiro (faturas_saas); a cotação guarda BRL por unidade da moeda externa",
        "VAT do país de destino acrescido sobre a base convertida (subtotal + imposto), com alíquota padrão do catálogo institucional",
        "Dispensa de ICMS na exportação: Lei 8.080/1990, art. 39, II",
        "Dispensa de IPI na exportação: Lei 4.502/1964, art. 15, III, c/c Decreto 4.404/2002, arts. 19-23"
    };

    private readonly string _conexao;

    public VendasInternacionaisService(string connectionString) => _conexao = connectionString;

    private static CommandDefinition Cmd(string sql, object? param = null, NpgsqlTransaction? tx = null, CancellationToken ct = default) =>
        new(sql, param, tx, cancellationToken: ct);

    private sealed record Parametros(IReadOnlyList<string> Moedas, string MoedaBase, string FormaDocumento)
    {
        public static Parametros Padrao() => new(
            VendasInternacionaisRegras.MoedasFallback.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            VendasInternacionaisRegras.MoedaBase, VendasInternacionaisRegras.FormaDocumento);
    }

    private static async Task<Parametros> LerParametrosAsync(NpgsqlConnection cn, CancellationToken ct)
    {
        var linhas = await cn.QueryAsync<ParamLinhaDto>(Cmd(@"
select codigo Codigo, valor Valor from plantaopro.parametros_legais
where codigo in ('COTACAO_MOEDAS','COTACAO_MOEDA_BASE','NOTA_INTERNACIONAL_FORMA') and reg_status='A'
order by ano desc", null, null, ct));
        var porCodigo = linhas.GroupBy(l => l.Codigo).ToDictionary(g => g.Key, g => g.First().Valor, StringComparer.OrdinalIgnoreCase);
        var p = Parametros.Padrao();
        if (!string.IsNullOrWhiteSpace(porCodigo.GetValueOrDefault("COTACAO_MOEDAS")))
            p = p with { Moedas = porCodigo["COTACAO_MOEDAS"]!.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) };
        if (!string.IsNullOrWhiteSpace(porCodigo.GetValueOrDefault("COTACAO_MOEDA_BASE")))
            p = p with { MoedaBase = porCodigo["COTACAO_MOEDA_BASE"]!.Trim().ToUpperInvariant() };
        if (!string.IsNullOrWhiteSpace(porCodigo.GetValueOrDefault("NOTA_INTERNACIONAL_FORMA")))
            p = p with { FormaDocumento = porCodigo["NOTA_INTERNACIONAL_FORMA"]!.Trim() };
        return p;
    }

    // ---------------------------------------------------------------------
    // Cotações institucionais (moeda base BRL)
    // ---------------------------------------------------------------------

    /// <summary>Histórico de cotações registradas (filtro opcional por moeda e intervalo), mais recentes primeiro.</summary>
    public async Task<ApiResponse<IReadOnlyList<ViCotacaoDto>>> ListarCotacoesAsync(string? moeda, DateOnly? de, DateOnly? ate, CancellationToken ct)
    {
        if (de.HasValue && ate.HasValue && de.Value > ate.Value)
            return ApiResponse<IReadOnlyList<ViCotacaoDto>>.Fail("O intervalo é inválido: a data inicial está após a final.", 400);
        await using var cn = new NpgsqlConnection(_conexao);
        await cn.OpenAsync(ct);
        var m = string.IsNullOrWhiteSpace(moeda) ? null : moeda.Trim().ToUpperInvariant();
        var lista = (await cn.QueryAsync<CotacaoLinhaDto>(Cmd(@"
select moeda Moeda, data::date Data, taxa Taxa, fonte Fonte
from plantaopro.cotacoes_moedas
where reg_status='A'
  and (@m is null or moeda=@m)
  and (@de::date is null or data >= @de::date)
  and (@ate::date is null or data <= @ate::date)
order by data desc, moeda asc
limit 200", new { m, de, ate }, null, ct))).ToList();
        return ApiResponse<IReadOnlyList<ViCotacaoDto>>.Ok(lista.Select(c => new ViCotacaoDto(c.Moeda, c.Data, c.Taxa, c.Fonte)).ToList());
    }

    /// <summary>Registra/atualiza a cotação de uma moeda em uma data (upsert por moeda+data; o histórico é preservado).</summary>
    public async Task<ApiResponse<ViCotacaoDto>> RegistrarCotacaoAsync(ViCotacaoCommand cmd, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_conexao);
        await cn.OpenAsync(ct);
        var par = await LerParametrosAsync(cn, ct);
        var moeda = string.IsNullOrWhiteSpace(cmd.Moeda) ? null : cmd.Moeda.Trim().ToUpperInvariant();
        if (!VendasInternacionaisRegras.MoedaAceita(moeda, string.Join("|", par.Moedas)))
            return ApiResponse<ViCotacaoDto>.Fail("Selecione uma moeda aceita no faturamento internacional (USD, EUR ou GBP).", 400);
        if (cmd.Data < VendasInternacionaisRegras.DataMinimaCotacao)
            return ApiResponse<ViCotacaoDto>.Fail("Informe a data da cotação (a partir de 2020-01-01).", 400);
        if (cmd.Data > DateOnly.FromDateTime(DateTime.UtcNow))
            return ApiResponse<ViCotacaoDto>.Fail("A data da cotação não pode ser futura.", 400);
        if (cmd.Taxa <= 0m)
            return ApiResponse<ViCotacaoDto>.Fail("Informe a cotação em reais positivos (ex.: 5.027700 por unidade de dólar).", 400);
        if (cmd.Taxa > VendasInternacionaisRegras.TaxaMaxSanidade)
            return ApiResponse<ViCotacaoDto>.Fail("A cotação ultrapassa o limite de sanidade desta etapa.", 400);
        var fonte = string.IsNullOrWhiteSpace(cmd.Fonte) ? "Registrado manualmente na área de vendas internacionais" : cmd.Fonte.Trim();
        if (fonte.Length > 300) return ApiResponse<ViCotacaoDto>.Fail("A fonte pode ter no máximo 300 caracteres.", 400);

        var linha = await cn.QuerySingleAsync<CotacaoLinhaDto>(Cmd(@"
insert into plantaopro.cotacoes_moedas (moeda, data, taxa, fonte)
values (@m, @d, @t, @f)
on conflict (moeda, data) do update set
    taxa=excluded.taxa,
    fonte=excluded.fonte,
    reg_update=now()
returning moeda Moeda, data::date Data, taxa Taxa, fonte Fonte",
            new { m = moeda, d = cmd.Data, t = cmd.Taxa, f = fonte }, null, ct));
        return ApiResponse<ViCotacaoDto>.Ok(new ViCotacaoDto(linha.Moeda, linha.Data, linha.Taxa, linha.Fonte), "Cotação registrada.");
    }

    // ---------------------------------------------------------------------
    // Catálogo de destinos (alíquotas padrão de VAT)
    // ---------------------------------------------------------------------

    /// <summary>Países de destino catalogados com a alíquota padrão de VAT aplicada pela nota internacional.</summary>
    public async Task<ApiResponse<IReadOnlyList<ViPaisVatDto>>> ListarPaisesVatAsync(CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_conexao);
        await cn.OpenAsync(ct);
        var lista = (await cn.QueryAsync<PaisVatLinhaDto>(Cmd(@"
select pais Pais, nome Nome, aliquota Aliquota, fonte Fonte
from plantaopro.paises_vat
where reg_status='A'
order by nome", null, null, ct))).ToList();
        return ApiResponse<IReadOnlyList<ViPaisVatDto>>.Ok(lista.Select(x => new ViPaisVatDto(x.Pais, x.Nome, x.Aliquota, x.Fonte)).ToList());
    }

    // ---------------------------------------------------------------------
    // Faturas candidatas e notas internacionais
    // ---------------------------------------------------------------------

    /// <summary>Faturas abertas/vencidas com valor positivo, candidatas à emissão de nota internacional.</summary>
    public async Task<ApiResponse<IReadOnlyList<ViFaturaDto>>> FaturasDisponiveisAsync(int limite, CancellationToken ct)
    {
        var qtd = Math.Clamp(limite <= 0 ? 100 : limite, 1, 500);
        await using var cn = new NpgsqlConnection(_conexao);
        await cn.OpenAsync(ct);
        var lista = (await cn.QueryAsync<FaturaLinhaDto>(Cmd(@"
select f.id Id, coalesce(c.razao_social, c.nome, c.codigo, 'Cliente não identificado') ClienteNome,
       f.status Status, f.valor ValorBrl, f.vencimento::date Vencimento, f.competencia::date Competencia
from plantaopro.faturas_saas f
left join plantaopro.clientes c on c.id=f.cliente_id
where f.reg_status='A' and f.status in ('ABERTA','VENCIDA') and f.valor > 0
order by coalesce(f.vencimento, '9999-12-31'::date) asc, f.competencia desc
limit @limite", new { limite = qtd }, null, ct))).ToList();
        return ApiResponse<IReadOnlyList<ViFaturaDto>>.Ok(lista.Select(x => new ViFaturaDto(x.Id, x.ClienteNome, x.Status, x.ValorBrl, x.Vencimento, x.Competencia)).ToList());
    }

    private static async Task<(ContextoNota? Contexto, int Status, string? Erro)> CarregarContextoNotaAsync(
        NpgsqlConnection cn, NpgsqlTransaction? tx, ViNotaComando cmd, Parametros par, CancellationToken ct)
    {
        var moeda = string.IsNullOrWhiteSpace(cmd.Moeda) ? null : cmd.Moeda.Trim().ToUpperInvariant();
        var pais = string.IsNullOrWhiteSpace(cmd.PaisDestino) ? null : cmd.PaisDestino.Trim().ToUpperInvariant();
        if (cmd.FaturaId == Guid.Empty) return (null, 400, "Selecione a fatura.");
        if (!VendasInternacionaisRegras.FormatoIso(pais, 2)) return (null, 400, "Selecione o país de destino (código ISO de dois caracteres).");
        if (!VendasInternacionaisRegras.MoedaAceita(moeda, string.Join("|", par.Moedas)))
            return (null, 400, "Selecione uma moeda aceita no faturamento internacional (USD, EUR ou GBP).");

        var fatura = await cn.QueryFirstOrDefaultAsync<FaturaBaseDto>(Cmd(@"
select f.id Id, coalesce(c.razao_social, c.nome, c.codigo, 'Cliente não identificado') ClienteNome,
       f.status Status, f.valor Valor
from plantaopro.faturas_saas f
left join plantaopro.clientes c on c.id=f.cliente_id
where f.id=@id and f.reg_status='A'", new { id = cmd.FaturaId }, tx, ct));
        if (fatura is null) return (null, 404, "Fatura não encontrada.");
        if (fatura.Status != "ABERTA" && fatura.Status != "VENCIDA")
            return (null, 400, "A fatura está em status " + fatura.Status + "; apenas faturas ABERTA ou VENCIDA aceitam nota internacional.");
        if (fatura.Valor <= 0m) return (null, 400, "A fatura não possui valor positivo para conversão.");

        var destino = await cn.QueryFirstOrDefaultAsync<DestinoPaisDto>(Cmd(
            "select pais Pais, nome Nome, aliquota Aliquota from plantaopro.paises_vat where pais=@p and reg_status='A'",
            new { p = pais }, tx, ct));
        if (destino is null) return (null, 400, "País de destino não presente no catálogo institucional de alíquotas de VAT.");

        var cotacao = await cn.QueryFirstOrDefaultAsync<CotacaoBaseDto>(Cmd(@"
select taxa Taxa, data::date Data from plantaopro.cotacoes_moedas
where moeda=@m and data <= current_date and reg_status='A'
order by data desc limit 1", new { m = moeda }, tx, ct));
        if (cotacao is null) return (null, 400, "Sem cotação registrada até hoje para esta moeda; registre a cotação antes de emitir.");

        return (new ContextoNota(fatura, destino, cotacao, moeda!), 0, null);
    }

    /// <summary>Simulação da nota internacional (conversão + VAT) sem persistir nada.</summary>
    public async Task<ApiResponse<ViNotaSimuladaDto>> SimularNotaAsync(ViNotaComando cmd, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_conexao);
        await cn.OpenAsync(ct);
        var par = await LerParametrosAsync(cn, ct);
        var (ctx, status, erro) = await CarregarContextoNotaAsync(cn, null, cmd, par, ct);
        if (ctx is null) return ApiResponse<ViNotaSimuladaDto>.Fail(erro!, status);
        var r = VendasInternacionaisRegras.CalcularNota(ctx.Fatura.Valor, ctx.Cotacao.Taxa, ctx.Destino.Aliquota);
        return ApiResponse<ViNotaSimuladaDto>.Ok(new ViNotaSimuladaDto(
            ctx.Fatura.Id, ctx.Fatura.ClienteNome, ctx.Moeda, ctx.Destino.Pais, ctx.Destino.Nome,
            DateOnly.FromDateTime(DateTime.UtcNow), r.ValorBrl, r.TaxaAplicada, ctx.Cotacao.Data,
            r.ValorMoeda, r.AliquotaPct, r.ValorVat, r.TotalMoeda, par.FormaDocumento, FontesNota));
    }

    /// <summary>
    /// Emite a nota fiscal não-tradicional da fatura: converte o valor em BRL pela
    /// cotação mais recente registrada (data &lt;= hoje) e aplica o VAT padrão do
    /// destino sobre a base convertida. Exatamente uma nota EMITIDA por fatura.
    /// </summary>
    public async Task<ApiResponse<ViNotaDto>> EmitirNotaAsync(Guid? userId, ViNotaComando cmd, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_conexao);
        await cn.OpenAsync(ct);
        var par = await LerParametrosAsync(cn, ct);
        var (ctx, status, erro) = await CarregarContextoNotaAsync(cn, null, cmd, par, ct);
        if (ctx is null) return ApiResponse<ViNotaDto>.Fail(erro!, status);
        var r = VendasInternacionaisRegras.CalcularNota(ctx.Fatura.Valor, ctx.Cotacao.Taxa, ctx.Destino.Aliquota);

        await using var tx = (await cn.BeginTransactionAsync(ct))!;
        try
        {
            var jaEmitida = await cn.ExecuteScalarAsync<bool>(Cmd(
                "select exists(select 1 from plantaopro.notas_internacionais where fatura_id=@f and status='EMITIDA' and reg_status='A')",
                new { f = ctx.Fatura.Id }, tx, ct));
            if (jaEmitida) return ApiResponse<ViNotaDto>.Fail("Esta fatura já possui uma nota internacional emitida; anule a existente para reemitir.", 409);

            var linha = await cn.QuerySingleAsync<NotaLinhaDto>(Cmd(@"
with novo as (
    insert into plantaopro.notas_internacionais (fatura_id, pais_destino, moeda, data_emissao, valor_brl, data_cotacao, taxa_aplicada, valor_moeda, aliquota_aplicada, valor_vat, total_moeda, forma_documento, criado_por)
    values (@f, @p, @m, current_date, @vb, @dc, @ta, @vm, @aa, @vv, @tm, @forma, @u)
    returning *)
select n.id Id, n.fatura_id FaturaId, coalesce(c.razao_social, c.nome, c.codigo, 'Cliente não identificado') ClienteNome,
       n.moeda Moeda, n.pais_destino PaisDestino, pv.nome PaisNome,
       n.data_emissao::date DataEmisso, n.valor_brl ValorBrl, n.data_cotacao::date DataCotacao,
       n.taxa_aplicada TaxaAplicada, n.valor_moeda ValorMoeda, n.aliquota_aplicada Aliquota,
       n.valor_vat ValorVat, n.total_moeda TotalMoeda, n.forma_documento FormaDocumento,
       n.status Status, n.motivo_anulacao MotivoAnulacao, coalesce(n.reg_date, now()) CriadoEm
from novo n
join plantaopro.paises_vat pv on pv.pais=n.pais_destino
left join plantaopro.faturas_saas fs on fs.id=n.fatura_id
left join plantaopro.clientes c on c.id=fs.cliente_id",
                new { f = ctx.Fatura.Id, p = ctx.Destino.Pais, m = ctx.Moeda, vb = r.ValorBrl, dc = ctx.Cotacao.Data,
                      ta = r.TaxaAplicada, vm = r.ValorMoeda, aa = r.AliquotaPct, vv = r.ValorVat, tm = r.TotalMoeda,
                      forma = par.FormaDocumento, u = userId }, tx, ct));
            await tx.CommitAsync(ct);
            return ApiResponse<ViNotaDto>.Ok(MapNota(linha), "Nota internacional emitida.");
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            await tx.RollbackAsync(ct);
            return ApiResponse<ViNotaDto>.Fail("Esta fatura já possui uma nota internacional emitida; anule a existente para reemitir.", 409);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    /// <summary>Notas internacionais emitidas/anuladas (filtro opcional por fatura), mais recentes primeiro.</summary>
    public async Task<ApiResponse<IReadOnlyList<ViNotaDto>>> ListarNotasAsync(Guid? faturaId, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_conexao);
        await cn.OpenAsync(ct);
        var lista = (await cn.QueryAsync<NotaLinhaDto>(Cmd(@"
select n.id Id, n.fatura_id FaturaId, coalesce(c.razao_social, c.nome, c.codigo, 'Cliente não identificado') ClienteNome,
       n.moeda Moeda, n.pais_destino PaisDestino, pv.nome PaisNome,
       n.data_emissao::date DataEmisso, n.valor_brl ValorBrl, n.data_cotacao::date DataCotacao,
       n.taxa_aplicada TaxaAplicada, n.valor_moeda ValorMoeda, n.aliquota_aplicada Aliquota,
       n.valor_vat ValorVat, n.total_moeda TotalMoeda, n.forma_documento FormaDocumento,
       n.status Status, n.motivo_anulacao MotivoAnulacao, coalesce(n.reg_date, now()) CriadoEm
from plantaopro.notas_internacionais n
join plantaopro.paises_vat pv on pv.pais=n.pais_destino
left join plantaopro.faturas_saas fs on fs.id=n.fatura_id
left join plantaopro.clientes c on c.id=fs.cliente_id
where n.reg_status='A' and (@fid is null or n.fatura_id=@fid)
order by n.reg_date desc, n.id
limit 200", new { fid = faturaId }, null, ct))).ToList();
        return ApiResponse<IReadOnlyList<ViNotaDto>>.Ok(lista.Select(MapNota).ToList());
    }

    private static ViNotaDto MapNota(NotaLinhaDto l) => new(
        l.Id, l.FaturaId, l.ClienteNome, l.Moeda, l.PaisDestino, l.PaisNome,
        l.DataEmisso, l.ValorBrl, l.TaxaAplicada, l.DataCotacao,
        l.ValorMoeda, l.Aliquota, l.ValorVat, l.TotalMoeda,
        l.FormaDocumento, l.Status, l.MotivoAnulacao, l.CriadoEm);

    /// <summary>Anula uma nota internacional (motivo obrigatório); libera a fatura para nova emissão.</summary>
    public async Task<ApiResponse<object>> AnularNotaAsync(ViAnularNotaCommand cmd, CancellationToken ct)
    {
        var motivo = string.IsNullOrWhiteSpace(cmd.Motivo) ? null : cmd.Motivo.Trim();
        if (motivo is null) return ApiResponse<object>.Fail("Informe o motivo da anulação.", 400);
        if (motivo.Length > 400) return ApiResponse<object>.Fail("O motivo pode ter no máximo 400 caracteres.", 400);
        await using var cn = new NpgsqlConnection(_conexao);
        await cn.OpenAsync(ct);
        var afetos = await cn.ExecuteAsync(Cmd(@"
update plantaopro.notas_internacionais
set status='ANULADA', motivo_anulacao=@m, reg_update=now()
where id=@id and status='EMITIDA' and reg_status='A'",
            new { m = motivo, id = cmd.Id }, null, ct));
        if (afetos == 0) return ApiResponse<object>.Fail("Nota não encontrada ou já anulada.", 404);
        return ApiResponse<object>.Ok(new object(), "Nota anulada; a fatura voltou a estar disponível para emissão.");
    }
}
