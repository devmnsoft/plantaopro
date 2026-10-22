using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using PlantaoPro.Application.Legislacao;

namespace PlantaoPro.Infrastructure.Legislacao;

/// <summary>
/// R85 (bloco B4 - DF-e nacional): configuração SEFAZ por UF (código oficial cUF e documentos
/// apoiados) e layouts oficiais vigentes lidos da fonte única de parâmetros legais, com geração
/// e validação da chave de acesso de 44 dígitos. O dígito verificador segue o módulo 11 oficial:
/// pesos 2..9 aplicados da direita para a esquerda do corpo (43 dígitos), reiniciando em 2 quando
/// o peso 9 é atingido; resto 0 ou 1 resulta em DV 0, nos demais casos DV = 11 - resto. A parte
/// que varia por UF (cUF e modelos apoiados) vive na fonte única - é ela que "personaliza" o DV
/// por UF, pois o cUF faz parte do corpo sobre o qual o DV é calculado.
/// </summary>
public sealed class DfeNacionalService : IDfeNacionalService
{
    /// <summary>Modelos geridos neste módulo: 55 NF-e, 65 NFC-e, 59 CF-e SAT, 67 CF-e Serviços (DREX).</summary>
    private static readonly IReadOnlyDictionary<int, (string Documento, string Nome)> ModelosGeridos =
        new Dictionary<int, (string Documento, string Nome)>
        {
            [55] = ("NFE", "NF-e"),
            [65] = ("NFC-E", "NFC-e"),
            [59] = ("CFE_SAT", "CF-e SAT"),
            [67] = ("CFE_SERVICOS", "CF-e Serviços")
        };

    private const int TotalUfsEsperado = 27;
    private const int AnoMinimo = 2000;
    private const int AnoMaximo = 2099;

    private readonly IParametrosLegaisRepository parametros;

    public DfeNacionalService(IParametrosLegaisRepository parametros)
    {
        this.parametros = parametros ?? throw new ArgumentException("Parâmetros legais obrigatórios.", nameof(parametros));
    }

    public async Task<IReadOnlyList<DfeUfInfo>> ListarUfsAsync(CancellationToken ct)
    {
        var linhas = await parametros.ListarAtivosAsync(null, ct);
        var ufs = linhas
            .Where(p => p.Codigo.StartsWith("SEFAZ_UF_", StringComparison.Ordinal))
            .Select(p =>
            {
                var partes = p.Valor.Split('|');
                if (partes.Length != 2 || !int.TryParse(partes[0], out var cuf))
                    throw new InvalidOperationException($"Configuração SEFAZ inválida para o código {p.Codigo}: esperado \"cUF|documentos\".");
                var documentos = partes[1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                return new DfeUfInfo(p.Codigo.Substring("SEFAZ_UF_".Length), cuf, p.Descricao, documentos, p.FonteOficial);
            })
            .OrderBy(u => u.Cuf)
            .ToList();

        if (ufs.Count != TotalUfsEsperado)
            throw new InvalidOperationException($"Catálogo de UFs do DF-e incompleto na fonte única (esperado {TotalUfsEsperado}, encontrado {ufs.Count}).");
        return ufs;
    }

    public async Task<IReadOnlyList<DfeLayoutInfo>> ListarLayoutsAsync(CancellationToken ct)
    {
        var linhas = await parametros.ListarAtivosAsync(null, ct);
        var ordem = new[] { "DFE_LAYOUT_NFE", "DFE_LAYOUT_NFCE", "DFE_LAYOUT_CFE_SAT", "DFE_LAYOUT_CFE_SERVICOS" };
        var layouts = new List<DfeLayoutInfo>();

        foreach (var codigo in ordem)
        {
            var linha = linhas.FirstOrDefault(p => p.Codigo == codigo);
            if (linha is null)
                throw new InvalidOperationException($"Layout oficial ausente na fonte única: {codigo}.");
            if (string.IsNullOrWhiteSpace(linha.Valor))
                throw new InvalidOperationException($"Versão do layout {codigo} vazia na fonte única.");
            layouts.Add(new DfeLayoutInfo(codigo.Substring("DFE_LAYOUT_".Length), NomeDoDocumento(codigo), linha.Valor, linha.FonteOficial));
        }
        return layouts;
    }

    public async Task<DfeChaveAcessoResultado> GerarChaveAsync(GerarChaveCommand comando, CancellationToken ct)
    {
        if (comando is null) throw new ArgumentException("Comando de geração é obrigatório.", nameof(comando));

        var ufs = await ListarUfsAsync(ct);
        var uf = ufs.FirstOrDefault(u => u.Uf == (comando.Uf ?? string.Empty).Trim().ToUpperInvariant())
            ?? throw new ArgumentException($"UF '{comando.Uf}' não encontrada no catálogo SEFAZ (consulte os 27 códigos oficiais cUF).");

        var erros = new List<string>();
        var registro = Regex.Replace(comando.RegistroCnpjCpf ?? string.Empty, @"\D", string.Empty);
        if (registro.Length is not (11 or 14))
            erros.Add("A inscrição do emitente deve ter 11 dígitos (CPF) ou 14 dígitos (CNPJ).");
        if (comando.DataEmissao.Year is < AnoMinimo or > AnoMaximo)
            erros.Add($"O ano de emissão deve estar entre {AnoMinimo} e {AnoMaximo}.");
        if (comando.Modulo < 1 || comando.Modulo > 99)
            erros.Add("O modelo fiscal deve ser um número de 1 a 99 (dois dígitos na chave).");
        if (comando.Serie < 1 || comando.Serie > 999)
            erros.Add("A série deve estar entre 1 e 999.");
        if (comando.NumeroNota < 1 || comando.NumeroNota > 999_999_999)
            erros.Add("O número da nota deve estar entre 1 e 999999999.");
        if (comando.TipoEmissao < 1 || comando.TipoEmissao > 9)
            erros.Add("O tipo de emissão deve estar entre 1 (normal) e 9 (contingência).");
        if (comando.CodigoNumerico < 0 || comando.CodigoNumerico > 99_999_999)
            erros.Add("O código numérico aleatório (CNF) deve estar entre 0 e 99999999 (oito dígitos).");
        if (!ModelosGeridos.TryGetValue(comando.Modulo, out var modelo))
            erros.Add("Modelo fiscal fora dos layouts geridos por esta configuração: 55 (NF-e), 65 (NFC-e), 59 (CF-e SAT) ou 67 (CF-e Serviços).");
        else if (!uf.DocumentosApoiados.Contains(modelo.Documento, StringComparer.Ordinal))
            erros.Add($"O documento {modelo.Nome} não é apoiado na UF {uf.Uf} (apoiados: {string.Join(", ", uf.DocumentosApoiados)}).");

        if (erros.Count > 0)
            return new DfeChaveAcessoResultado(false, null, erros, string.Empty);

        var (corpo, detalhe) = MontarCorpoEComputar(uf, comando, registro);
        var chave = corpo + DetalheDoDv(detalhe);
        return new DfeChaveAcessoResultado(true, Decompor(chave, uf, detalhe), Array.Empty<string>(), DetalheTexto(detalhe));
    }

    public async Task<DfeChaveAcessoResultado> ValidarChaveAsync(string chave, CancellationToken ct)
    {
        var ufs = await ListarUfsAsync(ct);
        var limpa = (chave ?? string.Empty).Trim();
        var erros = new List<string>();

        if (limpa.Length != 44 || limpa.Any(c => !char.IsDigit(c)))
        {
            erros.Add($"A chave de acesso deve conter exatamente 44 dígitos numéricos (recebidos {limpa.Length}).");
            return new DfeChaveAcessoResultado(false, null, erros, string.Empty);
        }

        var cuf = int.Parse(limpa.Substring(0, 2));
        var uf = ufs.FirstOrDefault(u => u.Cuf == cuf);
        if (uf is null)
            erros.Add($"O cUF {cuf:D2} não corresponde a nenhuma UF autorizada no catálogo SEFAZ (verifique os 27 códigos oficiais).");

        var detalhe = Modulo11(limpa.Substring(0, 43));
        var informado = limpa[43] - '0';
        if (detalhe.Dv != informado)
            erros.Add($"Dígito verificador inválido: calculado {detalhe.Dv} pelo módulo 11, informado {informado}.");

        var mes = int.Parse(limpa.Substring(4, 2));
        if (mes < 1 || mes > 12)
            erros.Add($"Mês inválido no campo AAMM da chave: {mes:D2}.");

        var modulo = int.Parse(limpa.Substring(20, 2));
        if (ModelosGeridos.TryGetValue(modulo, out var modelo))
        {
            if (uf is not null && !uf.DocumentosApoiados.Contains(modelo.Documento, StringComparer.Ordinal))
                erros.Add($"O documento {modelo.Nome} não é apoiado na UF {uf.Uf} desta chave (apoiados: {string.Join(", ", uf.DocumentosApoiados)}).");
        }
        else
        {
            erros.Add($"O modelo fiscal {modulo:D2} desta chave está fora dos layouts geridos por esta configuração (55, 65, 59, 67).");
        }

        var serie = int.Parse(limpa.Substring(22, 3));
        if (serie < 1 || serie > 999)
            erros.Add($"Série fora da faixa na chave: {serie:D3}.");

        var numeroNota = int.Parse(limpa.Substring(25, 9));
        if (numeroNota < 1 || numeroNota > 999_999_999)
            erros.Add($"Número da nota fora da faixa na chave: {numeroNota}.");

        var tipoEmissao = limpa[34] - '0';
        if (tipoEmissao < 1 || tipoEmissao > 9)
            erros.Add($"Tipo de emissão fora da faixa na chave: {tipoEmissao}.");

        if (erros.Count > 0)
            return new DfeChaveAcessoResultado(false, null, erros, DetalheTexto(detalhe));
        return new DfeChaveAcessoResultado(true, Decompor(limpa, uf!, detalhe), Array.Empty<string>(), DetalheTexto(detalhe));
    }

    // Corpo da chave: cUF(2) + AAMM(4) + CNPJ/CPF zerado a esquerda(14) + MOD(2) + SERIE(3) +
    // NNF(9) + tpEmis(1) + CNF(8) = 43 digitos; o DV (mod 11) completa os 44.
    private static (string Corpo, (int Soma, int Resto, int Dv) Dv) MontarCorpoEComputar(DfeUfInfo uf, GerarChaveCommand comando, string registro)
    {
        var corpo = $"{uf.Cuf:D2}{comando.DataEmissao.Year % 100:D2}{comando.DataEmissao.Month:D2}{registro.PadLeft(14, '0')}" +
                    $"{comando.Modulo:D2}{comando.Serie:D3}{comando.NumeroNota:D9}{comando.TipoEmissao:D1}{comando.CodigoNumerico:D8}";
        if (corpo.Length != 43) throw new InvalidOperationException("Corpo da chave montado com tamanho inesperado (esperado 43 dígitos).");
        return (corpo, Modulo11(corpo));
    }

    /// <summary>Módulo 11 oficial do DF-e: pesos 2..9 da direita para a esquerda (recomeça em 2 após 9); resto 0 ou 1 resulta em DV 0.</summary>
    public static (int Soma, int Resto, int Dv) Modulo11(string corpo43)
    {
        if (corpo43.Length != 43) throw new ArgumentException("O corpo da chave tem que ter 43 dígitos.", nameof(corpo43));
        var soma = 0;
        var peso = 2;
        for (var i = corpo43.Length - 1; i >= 0; i--)
        {
            soma += (corpo43[i] - '0') * peso;
            peso = peso >= 9 ? 2 : peso + 1;
        }
        var resto = soma % 11;
        var dv = resto is 0 or 1 ? 0 : 11 - resto;
        return (soma, resto, dv);
    }

    private static string DetalheDoDv((int Soma, int Resto, int Dv) detalhe) => detalhe.Dv.ToString();

    private static string DetalheTexto((int Soma, int Resto, int Dv) detalhe)
    {
        var regra = detalhe.Resto is 0 or 1
            ? "DV 0 (regra oficial: resto 0 ou 1 resulta em dígito verificador 0)"
            : $"DV 11-{detalhe.Resto}={detalhe.Dv}";
        return $"Soma ponderada {detalhe.Soma}; resto módulo 11 {detalhe.Resto}; {regra}. Pesos 2..9 da direita para a esquerda, reiniciando em 2.";
    }

    private static string NomeDoDocumento(string codigo) => codigo switch
    {
        "DFE_LAYOUT_NFE" => "NF-e",
        "DFE_LAYOUT_NFCE" => "NFC-e",
        "DFE_LAYOUT_CFE_SAT" => "CF-e SAT",
        "DFE_LAYOUT_CFE_SERVICOS" => "CF-e Serviços",
        _ => codigo
    };

    private static ChaveAcessoDfe Decompor(string chave44, DfeUfInfo uf, (int Soma, int Resto, int Dv) detalhe)
    {
        var ano = AnoMinimo + int.Parse(chave44.Substring(2, 2));
        var mes = int.Parse(chave44.Substring(4, 2));
        var registro = chave44.Substring(6, 14);
        var modulo = int.Parse(chave44.Substring(20, 2));
        var serie = int.Parse(chave44.Substring(22, 3));
        var numeroNota = int.Parse(chave44.Substring(25, 9));
        var tipoEmissao = chave44[34] - '0';
        var codigoNumerico = long.Parse(chave44.Substring(35, 8));
        var dv = chave44[43] - '0';

        var segmentos = new List<ChaveSegmento>
        {
            new("UF de emissão (cUF)", $"{chave44.Substring(0, 2)} ({uf.Uf})"),
            new("Ano e mês de emissão (AAMM)", chave44.Substring(2, 4)),
            new("Emitente (CNPJ/CPF)", registro),
            new("Modelo fiscal (MOD)", chave44.Substring(20, 2)),
            new("Série (SERIE)", chave44.Substring(22, 3)),
            new("Número da nota (NNF)", chave44.Substring(25, 9)),
            new("Tipo de emissão (tpEmis)", tipoEmissao.ToString()),
            new("Código numérico (CNF)", chave44.Substring(35, 8)),
            new("Dígito verificador (DV módulo 11)", dv.ToString())
        };

        return new ChaveAcessoDfe(uf.Cuf, uf.Uf, ano, mes, registro, modulo, serie, numeroNota, tipoEmissao, codigoNumerico, dv, chave44, segmentos);
    }
}
