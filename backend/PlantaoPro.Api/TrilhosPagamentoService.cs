using System.Globalization;
using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

// ============================================================================
// R86 - Trilhos de pagamento amarrados ao motor de cobranca SaaS (R5-B6).
// A configuracao e GLOBAL (plano SaaS, como cobranca_providers): banco/conta/chave
// PIX da operacao que emite as faturas. As faturas abertas (ABERTA/VENCIDA) do
// motor geram: PIX copia-e-cola EMV por fatura (valor + txid derivado do id da
// fatura), codigo de barras/linha digitavel por titulo e arquivo CNAB 400 de
// remessa (canon documentado, 400 posicoes por linha). Constantes institucionais
// (GUI Pix, MCC, moeda do boleto, data base do fator, layout, documentos FEBEN)
// leem da fonte unica parametros_legais. Nao existe integracao bancaria real:
// a homologacao externa (CNAB 240, XML FEBEN, SPI/DCI) e o proximo marco.
// ============================================================================

public sealed record TrilhosConfigCommand(string BancoCodigo, string BancoNome, string Agencia, string Carteira, string ContaCedente, long NossoNumeroBase, string ChavePix, string BeneficiarioNome, string BeneficiarioCidade);
public sealed record TrilhosConfigDto(string BancoCodigo, string BancoNome, string Agencia, string Carteira, string ContaCedente, long NossoNumeroBase, string ChavePix, string BeneficiarioNome, string BeneficiarioCidade, bool Ativo, DateTime AtualizadoEm);
public sealed record FaturaAbertaDto(Guid FaturaId, Guid ClienteId, string ClienteNome, DateOnly Competencia, DateOnly Vencimento, decimal Valor, string Status);
public sealed record BoletoItemDto(Guid FaturaId, string ClienteNome, DateOnly Vencimento, decimal Valor, long NossoNumero, int FatorVencimento, long RolloverFator, string LinhaNumero, string LinhaDigitavel, string LinhaDigitavelExibicao);
public sealed record RemessaBoletosDto(string ConteudoArquivo, int TotalItens, decimal TotalValor, IReadOnlyList<BoletoItemDto> Itens, int Layout, string BancoCodigo, string Agencia, string ContaCedente);
public sealed record PixEmvDto(string CopiaECola, string? Txid, decimal? Valor);
public sealed record ValidarPixRequest(string Payload);
public sealed record ValidarLinhaDigitavelRequest(string Linha);
public sealed record PixFaturaRequest(Guid FaturaId);
public sealed record GerarRemessaCommand(DateOnly? Ate, int Limite = 200);

public sealed class TrilhosPagamentoService
{
    private const int LimiteMaximoFaturas = 500;
    private const int LayoutRemessaGerido = 400;
    private readonly string _conexao;

    public TrilhosPagamentoService(string connectionString) => _conexao = connectionString;

    private async Task<string?> ParametroAsync(NpgsqlConnection cn, string codigo, CancellationToken ct) =>
        await cn.ExecuteScalarAsync<string>(new CommandDefinition(
            "select valor from plantaopro.parametros_legais where codigo=@codigo and reg_status='A' order by ano desc limit 1",
            new { codigo }, cancellationToken: ct));

    private static bool SoDigitos(string? s, int comprimento) => !string.IsNullOrEmpty(s) && s!.Length == comprimento && s.All(char.IsDigit);
    private static bool SomenteAsciiImprimivel(string s) => s.All(c => c >= 32 && c <= 126);

    // ---------------------------------------------------------------- configuração

    public async Task<ApiResponse<TrilhosConfigDto>> ObterConfiguracaoAsync(CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_conexao);
        var linha = await cn.QueryFirstOrDefaultAsync<ConfigLinha>(new CommandDefinition(
            @"select banco_codigo as BancoCodigo, banco_nome as BancoNome, agencia as Agencia, carteira as Carteira,
                     conta_cedente as ContaCedente, nosso_numero_base as NossoNumeroBase, chave_pix as ChavePix,
                     beneficiario_nome as BeneficiarioNome, beneficiario_cidade as BeneficiarioCidade,
                     ativo as Ativo, atualizado_em as AtualizadoEm
                from plantaopro.cobranca_trilhos_config where reg_status='A' order by atualizado_em desc limit 1",
            cancellationToken: ct));
        if (linha is null)
            return ApiResponse<TrilhosConfigDto>.Fail("Configure os trilhos de pagamento antes de gerar arquivos ou payloads.", 404);
        return ApiResponse<TrilhosConfigDto>.Ok(Mapa(linha));
    }

    public async Task<ApiResponse<TrilhosConfigDto>> SalvarConfiguracaoAsync(TrilhosConfigCommand c, CancellationToken ct)
    {
        var erros = new List<string>();
        var nome = (c.BancoNome ?? string.Empty).Trim();
        var chave = (c.ChavePix ?? string.Empty).Trim();
        var nomeB = (c.BeneficiarioNome ?? string.Empty).Trim();
        var cidade = (c.BeneficiarioCidade ?? string.Empty).Trim();
        if (!SoDigitos(c.BancoCodigo, 3)) erros.Add("O código do banco tem exatamente 3 dígitos.");
        if (nome.Length < 1 || nome.Length > 60) erros.Add("Informe o nome do banco (até 60 caracteres).");
        if (!SoDigitos(c.Agencia, 4)) erros.Add("O número da agência tem exatamente 4 dígitos.");
        if (!SoDigitos(c.Carteira, 2)) erros.Add("O código da carteira tem exatamente 2 dígitos.");
        if (!SoDigitos(c.ContaCedente, 7)) erros.Add("A conta do cedente tem exatamente 7 dígitos (posições 37-43 do campo livre).");
        if (c.NossoNumeroBase < 1 || c.NossoNumeroBase > 99999999999) erros.Add("O nosso número base fica entre 1 e 99999999999 (cabe nos campos de 11 e 17 dígitos).");
        if (chave.Length < 1 || chave.Length > 64) erros.Add("Informe a chave PIX (até 64 caracteres).");
        if (nomeB.Length < 1 || nomeB.Length > TrilhosPagamentoRegras.MaxNomeBeneficiario)
            erros.Add($"O nome do beneficiário tem de 1 a {TrilhosPagamentoRegras.MaxNomeBeneficiario} caracteres (campo 59 do BR Code).");
        else if (!SomenteAsciiImprimivel(nomeB))
            erros.Add("Use apenas letras sem acento no nome do beneficiário (o payload PIX é ASCII).");
        if (cidade.Length < 1 || cidade.Length > TrilhosPagamentoRegras.MaxCidadeBeneficiario)
            erros.Add($"A cidade do beneficiário tem de 1 a {TrilhosPagamentoRegras.MaxCidadeBeneficiario} caracteres (campo 60 do BR Code).");
        else if (!SomenteAsciiImprimivel(cidade))
            erros.Add("Use apenas letras sem acento na cidade do beneficiário (o payload PIX é ASCII).");
        if (erros.Count > 0)
            return ApiResponse<TrilhosConfigDto>.Fail("Revise os campos destacados da configuração.", 400, erros);

        try
        {
            await using var cn = new NpgsqlConnection(_conexao);
            await cn.OpenAsync();
            await using var tx = await cn.BeginTransactionAsync();
            try
            {
                var existente = await cn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                    "select id from plantaopro.cobranca_trilhos_config where reg_status='A' order by atualizado_em desc limit 1",
                    transaction: tx, cancellationToken: ct));
                Guid id;
                if (existente is null)
                {
                    await cn.ExecuteAsync(new CommandDefinition(
                        @"insert into plantaopro.cobranca_trilhos_config
                            (banco_codigo, banco_nome, agencia, carteira, conta_cedente, nosso_numero_base,
                             chave_pix, beneficiario_nome, beneficiario_cidade, ativo)
                         values (@banco, @bancoNome, @agencia, @carteira, @conta, @nosso, @chave, @nomeB, @cidade, true)",
                        new { banco = c.BancoCodigo.Trim(), bancoNome = nome, agencia = c.Agencia!.Trim(), carteira = c.Carteira!.Trim(),
                              conta = c.ContaCedente!.Trim(), nosso = c.NossoNumeroBase, chave, nomeB, cidade },
                        transaction: tx, cancellationToken: ct));
                    id = (await cn.ExecuteScalarAsync<Guid>(new CommandDefinition(
                        "select id from plantaopro.cobranca_trilhos_config where reg_status='A' limit 1",
                        transaction: tx, cancellationToken: ct)))!;
                }
                else
                {
                    id = existente.Value;
                    await cn.ExecuteAsync(new CommandDefinition(
                        @"update plantaopro.cobranca_trilhos_config
                              set banco_codigo=@banco, banco_nome=@bancoNome, agencia=@agencia, carteira=@carteira,
                                  conta_cedente=@conta, nosso_numero_base=@nosso, chave_pix=@chave,
                                  beneficiario_nome=@nomeB, beneficiario_cidade=@cidade, ativo=true, atualizado_em=now()
                            where id=@id",
                        new { id, banco = c.BancoCodigo.Trim(), bancoNome = nome, agencia = c.Agencia!.Trim(), carteira = c.Carteira!.Trim(),
                              conta = c.ContaCedente!.Trim(), nosso = c.NossoNumeroBase, chave, nomeB, cidade },
                        transaction: tx, cancellationToken: ct));
                }
                await tx.CommitAsync();
                var salvo = await ObterConfiguracaoAsync(ct);
                return salvo.Success ? salvo : ApiResponse<TrilhosConfigDto>.Fail("A configuração não pôde ser confirmada.", 500);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        catch (PostgresException ex) when (ex.SqlState is "23512" or "23513" or "23514" or "23505")
        {
            return ApiResponse<TrilhosConfigDto>.Fail("Um campo da configuração viola o formato esperado; revise os dígitos e os limites.", 400);
        }
    }

    // ---------------------------------------------------------------- PIX EMV

    public async Task<ApiResponse<PixEmvDto>> GerarPixEstaticoAsync(CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_conexao);
        var (linha, erro) = await ConfigLinhaAsync(cn, ct);
        if (linha is null) return ApiResponse<PixEmvDto>.Fail(erro!, 404);
        var gui = await ParametroAsync(cn, "TRILHO_PIX_GUI", ct);
        var mcc = await ParametroAsync(cn, "TRILHO_PIX_MCC_PADRAO", ct);
        if (string.IsNullOrEmpty(gui) || string.IsNullOrEmpty(mcc))
            return ApiResponse<PixEmvDto>.Fail("As constantes do PIX (GUI/MCC) não estão na fonte única de parâmetros.", 500);
        var payload = TrilhosPagamentoRegras.GerarPixEmv(gui!, linha.ChavePix, mcc!, linha.BeneficiarioNome, linha.BeneficiarioCidade, null, null);
        return ApiResponse<PixEmvDto>.Ok(new PixEmvDto(payload, null, null));
    }

    /// <summary>Pix dinâmico de uma fatura: valor em reais e txid derivado do id da fatura (determinístico).</summary>
    public async Task<ApiResponse<PixEmvDto>> GerarPixFaturaAsync(Guid faturaId, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_conexao);
        var fatura = await cn.QueryFirstOrDefaultAsync<FaturaChaveLinha>(new CommandDefinition(
            "select id as Id, valor as Valor, status as Status from plantaopro.faturas_saas where id=@id and reg_status='A'",
            new { id = faturaId }, cancellationToken: ct));
        if (fatura is null)
            return ApiResponse<PixEmvDto>.Fail("Fatura não encontrada.", 404);
        if (fatura.Status != "ABERTA" && fatura.Status != "VENCIDA")
            return ApiResponse<PixEmvDto>.Fail("Só fatura aberta ou vencida aceita trilho de pagamento.", 409);
        if (fatura.Valor <= 0)
            return ApiResponse<PixEmvDto>.Fail("A fatura precisa ter valor maior que zero para gerar o payload PIX.", 409);

        var (linha, erro) = await ConfigLinhaAsync(cn, ct);
        if (linha is null) return ApiResponse<PixEmvDto>.Fail(erro!, 404);
        var gui = await ParametroAsync(cn, "TRILHO_PIX_GUI", ct);
        var mcc = await ParametroAsync(cn, "TRILHO_PIX_MCC_PADRAO", ct);
        if (string.IsNullOrEmpty(gui) || string.IsNullOrEmpty(mcc))
            return ApiResponse<PixEmvDto>.Fail("As constantes do PIX (GUI/MCC) não estão na fonte única de parâmetros.", 500);

        var txid = faturaId.ToString("N").ToUpperInvariant()[..25];
        var valor = fatura.Valor.ToString("0.00", CultureInfo.InvariantCulture);
        var payload = TrilhosPagamentoRegras.GerarPixEmv(gui!, linha.ChavePix, mcc!, linha.BeneficiarioNome, linha.BeneficiarioCidade, valor, txid);
        return ApiResponse<PixEmvDto>.Ok(new PixEmvDto(payload, txid, fatura.Valor));
    }

    /// <summary>Relatório de validação: sempre devolve dados com a lista de erros (vazia quando válido).</summary>
    public ApiResponse<PixValidacaoDto> ValidarPixEmvAsync(string payload, CancellationToken ct) =>
        ApiResponse<PixValidacaoDto>.Ok(TrilhosPagamentoRegras.ValidarPixEmv(payload ?? string.Empty));

    // ---------------------------------------------------------------- boleto / linha digitável

    /// <summary>Valida uma linha digitável recebida, confere os DVs e estima o vencimento pela janela de rollover.</summary>
    public async Task<ApiResponse<LinhaDigitavelValidacaoDto>> ValidarLinhaDigitavelAsync(string linha, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(linha))
            return ApiResponse<LinhaDigitavelValidacaoDto>.Fail("Cole a linha digitável para conferir.", 400);
        await using var cn = new NpgsqlConnection(_conexao);
        var baseFatorTxt = await ParametroAsync(cn, "TRILHO_BOLETO_BASE_FATOR_DIA", ct);
        if (baseFatorTxt is null || !DateOnly.TryParse(baseFatorTxt, CultureInfo.InvariantCulture, out var baseFator))
            return ApiResponse<LinhaDigitavelValidacaoDto>.Fail("A data base do fator de vencimento não está na fonte única de parâmetros.", 500);
        var hoje = DateOnly.FromDateTime(DateTime.Now.Date);
        return ApiResponse<LinhaDigitavelValidacaoDto>.Ok(TrilhosPagamentoRegras.ValidarLinhaDigitavel(linha, baseFator, hoje));
    }

    // ---------------------------------------------------------------- CNAB 400 remessa

    public async Task<ApiResponse<IReadOnlyList<FaturaAbertaDto>>> ListarFaturasAbertasAsync(int limite, CancellationToken ct)
    {
        if (limite < 1 || limite > LimiteMaximoFaturas) limite = 100;
        await using var cn = new NpgsqlConnection(_conexao);
        var linhas = (await cn.QueryAsync<FaturaAbertaLinha>(new CommandDefinition(
            @"select f.id as FaturaId, f.cliente_id as ClienteId, coalesce(c.razao_social, '') as ClienteNome,
                     f.competencia as Competencia, f.vencimento as Vencimento, f.valor as Valor, f.status as Status
                from plantaopro.faturas_saas f
                left join plantaopro.clientes c on c.id = f.cliente_id
               where f.reg_status='A' and f.status in ('ABERTA','VENCIDA') and f.valor > 0
               order by f.vencimento, f.id
               limit @limite",
            new { limite }, cancellationToken: ct))).ToList();
        return ApiResponse<IReadOnlyList<FaturaAbertaDto>>.Ok(linhas.Select(MapaFatura).ToList());
    }

    /// <summary>Gera a remessa CNAB 400 das faturas abertas (canon documentado, 400 posições por linha).</summary>
    public async Task<ApiResponse<RemessaBoletosDto>> GerarRemessaBoletosAsync(GerarRemessaCommand cmd, CancellationToken ct)
    {
        var limite = cmd.Limite;
        if (limite < 1 || limite > LimiteMaximoFaturas) limite = 200;

        await using var cn = new NpgsqlConnection(_conexao);
        var (config, erroConfig) = await ConfigLinhaAsync(cn, ct);
        if (config is null) return ApiResponse<RemessaBoletosDto>.Fail(erroConfig!, 404);

        var moedaTxt = await ParametroAsync(cn, "TRILHO_BOLETO_MOEDA", ct);
        var baseFatorTxt = await ParametroAsync(cn, "TRILHO_BOLETO_BASE_FATOR_DIA", ct);
        var layoutTxt = await ParametroAsync(cn, "TRILHO_CNAB_REMESSA_LAYOUT", ct);
        if (moedaTxt is null || baseFatorTxt is null || layoutTxt is null)
            return ApiResponse<RemessaBoletosDto>.Fail("As constantes do boleto não estão na fonte única de parâmetros.", 500);
        if (!int.TryParse(layoutTxt, out var layout) || layout != LayoutRemessaGerido)
            return ApiResponse<RemessaBoletosDto>.Fail($"O layout de remessa gerido pelo produto é {LayoutRemessaGerido}; a fonte única informa '{layoutTxt}'.", 500);
        if (!DateOnly.TryParse(baseFatorTxt, CultureInfo.InvariantCulture, out var baseFator))
            return ApiResponse<RemessaBoletosDto>.Fail("A data base do fator de vencimento está inválida na fonte única.", 500);

        var faturas = (await cn.QueryAsync<FaturaAbertaLinha>(new CommandDefinition(
            @"select f.id as FaturaId, f.cliente_id as ClienteId, coalesce(c.razao_social, '') as ClienteNome,
                     f.competencia as Competencia, f.vencimento as Vencimento, f.valor as Valor, f.status as Status
                from plantaopro.faturas_saas f
                left join plantaopro.clientes c on c.id = f.cliente_id
               where f.reg_status='A' and f.status in ('ABERTA','VENCIDA') and f.valor > 0
                 and (@ate::date is null or f.vencimento <= @ate)
               order by f.vencimento, f.id
               limit @limite",
            new { ate = cmd.Ate, limite }, cancellationToken: ct))).ToList();
        if (faturas.Count == 0)
            return ApiResponse<RemessaBoletosDto>.Fail("Nenhuma fatura aberta para compor a remessa.", 404);

        var itens = new List<BoletoItemDto>(faturas.Count);
        var registros = new List<string>(faturas.Count);
        long totalCentavos = 0;
        for (var i = 0; i < faturas.Count; i++)
        {
            var f = faturas[i];
            var nosso = config.NossoNumeroBase + i + 1;
            try
            {
                var fator = TrilhosPagamentoRegras.FatorVencimento(f.Vencimento, baseFator);
                var centavos = (long)(f.Valor * 100m);
                totalCentavos += centavos;
                var campoLivre = TrilhosPagamentoRegras.CampoLivreBoleto(config.Agencia, config.Carteira, nosso, config.ContaCedente);
                var linhaNumero = TrilhosPagamentoRegras.LinhaNumeroBoleto(config.BancoCodigo, (char)moedaTxt[0], fator.FatorExibido, centavos, campoLivre);
                var ld = TrilhosPagamentoRegras.LinhaDigitavelBoleto(linhaNumero);
                itens.Add(new BoletoItemDto(f.FaturaId, f.ClienteNome, f.Vencimento, f.Valor, nosso, fator.FatorExibido, fator.Rollover, linhaNumero, ld.Raw47, ld.Exibicao));
                registros.Add(TrilhosPagamentoRegras.CnabRegistroRemessa(config.Agencia, config.ContaCedente, config.BeneficiarioNome, nosso, linhaNumero, f.Vencimento, centavos));
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse<RemessaBoletosDto>.Fail($"Fatura de vencimento {f.Vencimento.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}: {ex.Message}", 400);
            }
        }

        var hoje = DateOnly.FromDateTime(DateTime.Now.Date);
        var cabecalho = TrilhosPagamentoRegras.CnabCabecalhoRemessa(config.BancoCodigo, config.Agencia, config.ContaCedente, config.BeneficiarioNome, hoje);
        var trailer = TrilhosPagamentoRegras.CnabTrailerRemessa(config.Agencia, config.ContaCedente, config.BeneficiarioNome, itens.Count, totalCentavos);
        var linhasArquivo = new List<string> { cabecalho };
        linhasArquivo.AddRange(registros);
        linhasArquivo.Add(trailer);
        var conteudo = TrilhosPagamentoRegras.MontarArquivoRemessa(linhasArquivo);
        return ApiResponse<RemessaBoletosDto>.Ok(new RemessaBoletosDto(conteudo, itens.Count, totalCentavos / 100m, itens, layout, config.BancoCodigo, config.Agencia, config.ContaCedente));
    }

    // ---------------------------------------------------------------- helpers

    private static TrilhosConfigDto Mapa(ConfigLinha l) =>
        new(l.BancoCodigo.Trim(), l.BancoNome.Trim(), l.Agencia.Trim(), l.Carteira.Trim(), l.ContaCedente.Trim(), l.NossoNumeroBase, l.ChavePix.Trim(), l.BeneficiarioNome.Trim(), l.BeneficiarioCidade.Trim(), l.Ativo, l.AtualizadoEm);

    private static FaturaAbertaDto MapaFatura(FaturaAbertaLinha l) =>
        new(l.FaturaId, l.ClienteId, l.ClienteNome, l.Competencia, l.Vencimento, l.Valor, l.Status);

    private static async Task<(ConfigLinha?, string?)> ConfigLinhaAsync(NpgsqlConnection cn, CancellationToken ct)
    {
        var linha = await cn.QueryFirstOrDefaultAsync<ConfigLinha>(new CommandDefinition(
            @"select banco_codigo as BancoCodigo, banco_nome as BancoNome, agencia as Agencia, carteira as Carteira,
                     conta_cedente as ContaCedente, nosso_numero_base as NossoNumeroBase, chave_pix as ChavePix,
                     beneficiario_nome as BeneficiarioNome, beneficiario_cidade as BeneficiarioCidade,
                     ativo as Ativo, atualizado_em as AtualizadoEm
                from plantaopro.cobranca_trilhos_config where reg_status='A' order by atualizado_em desc limit 1",
            cancellationToken: ct));
        return (linha, linha is null ? "Configure os trilhos de pagamento antes de gerar arquivos ou payloads." : null);
    }

    private sealed record ConfigLinha(string BancoCodigo, string BancoNome, string Agencia, string Carteira, string ContaCedente, long NossoNumeroBase, string ChavePix, string BeneficiarioNome, string BeneficiarioCidade, bool Ativo, DateTime AtualizadoEm);
    private sealed record FaturaAbertaLinha(Guid FaturaId, Guid ClienteId, string ClienteNome, DateOnly Competencia, DateOnly Vencimento, decimal Valor, string Status);
    private sealed record FaturaChaveLinha(Guid Id, decimal Valor, string Status);
}
