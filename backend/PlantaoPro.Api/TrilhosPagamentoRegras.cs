using System.Globalization;
using System.Text;

namespace PlantaoPro.Api;

// ============================================================================
// R86 - Trilhos de pagamento: aritmetica pura e citavel, sem banco.
//   * PIX EMV (QR Code BR Code, BACEN/EMVCo): payload TLV 000201... com fechamento
//     CRC16-CCITT-FALSE (init 0xFFFF, polinomio 0x1021). GUI br.gov.bcb.pix e MCC
//     padrao 0000 vem da fonte unica (parametros_legais) no servico.
//   * Boleto (BACEN Carta-Circular 2926): codigo de barras de 44 posicoes
//     (banco 3 | moeda 1 | DV mod-11 na posicao 5 | fator 4 | valor 10 | campo
//     livre 25 = agencia 4 + carteira 2 + nosso numero 11 + conta cedente 7 +
//     zero 1) e linha digitavel de 47 (tres campos com DV mod-10 + DV geral +
//     fator + valor). DV mod-11: pesos 2..9 da direita para a esquerda
//     reiniciando em 2; resto 0, 1 ou 10 => DV 1 (regra oficial do bloqueto).
//   * Fator de vencimento: dias corridos desde a data base oficial (07/10/1997);
//     a partir de fev/2025 vale rollover: fator exibido = dias totais mod 10000
//     (a tabela oficial de correlacao se encerra em 21/02/2025 com fator 9999).
//   * CNAB 400 ARREM: layout canonico documentado (cabeçalho/registro/trailer de
//     exatamente 400 posicoes). Posicoes classicas mantidas (nosso numero D17,
//     linha numero 44, data ddMMyyyy, valor D15 em centavos); ajustes especificos
//     por banco entram com a homologacao externa.
// Os vetores travados (exemplos oficiais + fixos) vivem em R86TrilhosContratoTests.
// ============================================================================

public sealed record PixValidacaoDto(bool Valida, IReadOnlyList<string> Erros, string? Gui, string? Chave, string? Mcc,
    string? Moeda, string? Pais, string? Nome, string? Cidade, string? Valor, string? Txid);

public sealed record LinhaDigitavelValidacaoDto(bool Valida, IReadOnlyList<string> Erros, string? Banco, string? Moeda,
    int? Fator, DateOnly? VencimentoProvavel, long? ValorCentavos, string? CampoLivre);

public static class TrilhosPagamentoRegras
{
    // Limites oficiais do QR Code BR Code: campo 59 (nome) 25 e campo 60 (cidade) 15.
    public const int MaxNomeBeneficiario = 25;
    public const int MaxCidadeBeneficiario = 15;

    // ---------------------------------------------------------------- PIX EMV

    /// <summary>CRC16-CCITT-FALSE (init 0xFFFF, polinomio 0x1021), em hex maiúsculo de 4 dígitos.</summary>
    public static string Crc16Ccitt(string s)
    {
        ushort crc = 0xFFFF;
        foreach (var b in Encoding.ASCII.GetBytes(s))
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++)
                crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ 0x1021 : crc << 1);
        }
        return crc.ToString("X4");
    }

    private static string Tlv(string tag, string valor) => tag + valor.Length.ToString("D2") + valor;

    /// <summary>
    /// Gera o payload copia-e-cola. Estático (sem valor/txid) ou dinâmico (valor
    /// "0.00" invariant + txid de até 25 caracteres alfanuméricos no campo 62>05).
    /// </summary>
    public static string GerarPixEmv(string gui, string chave, string mcc, string nome, string cidade, string? valor, string? txid)
    {
        var contaMercado = Tlv("00", gui) + Tlv("01", chave);
        var corpo = Tlv("00", "26") + Tlv("26", contaMercado) + Tlv("52", mcc) + Tlv("53", "986")
            + (valor is null ? string.Empty : Tlv("54", valor))
            + Tlv("58", "BR") + Tlv("59", nome) + Tlv("60", cidade)
            + (txid is null ? string.Empty : Tlv("62", Tlv("05", txid)));
        // O CRC16 é calculado sobre todo o dado precedente, incluindo o par 63|04 (EMVCo QRCPS / BACEN).
        return corpo + "6304" + Crc16Ccitt(corpo + "6304");
    }

    /// <summary>Valida um payload PIX EMV: estrutura TLV, campos obrigatórios e CRC16.</summary>
    public static PixValidacaoDto ValidarPixEmv(string payload)
    {
        var erros = new List<string>();
        var campos = new Dictionary<string, string>(StringComparer.Ordinal);
        string? gui = null, chave = null, txid = null;

        if (string.IsNullOrWhiteSpace(payload))
        {
            erros.Add("Informe o payload copia-e-cola para validar.");
            return new PixValidacaoDto(false, erros, null, null, null, null, null, null, null, null, null);
        }
        if (!payload.All(c => c >= 32 && c <= 126))
            erros.Add("O payload contem caracteres fora do ASCII imprimível (use letras sem acento).");
        else
        {
            var i = 0;
            var primeiroPar = true;
            var ultimoTag = "";
            try
            {
                while (i < payload.Length)
                {
                    if (payload.Length - i < 4) throw new FormatException();
                    var tag = payload.Substring(i, 2);
                    if (!tag.All(char.IsDigit)) throw new FormatException();
                    var tam = int.Parse(payload.Substring(i + 2, 2), CultureInfo.InvariantCulture);
                    if (tam < 1 || i + 4 + tam > payload.Length) throw new FormatException();
                    var valor = payload.Substring(i + 4, tam);
                    if (primeiroPar)
                    {
                        if (tag != "00" || valor != "26") erros.Add("O payload deve iniciar com o par 00|02|26 (formato BR Code).");
                        primeiroPar = false;
                    }
                    ultimoTag = tag;
                    campos[tag] = valor;
                    i += 4 + tam;
                }
            }
            catch (FormatException)
            {
                erros.Add("Estrutura TLV malformada: tag ou comprimento inválido.");
            }

            if (erros.Count == 0 || ultimoTag.Length == 2)
            {
                if (campos.TryGetValue("26", out var ma))
                {
                    var sub = new Dictionary<string, string>(StringComparer.Ordinal);
                    try
                    {
                        var j = 0;
                        while (j < ma.Length)
                        {
                            if (ma.Length - j < 4) throw new FormatException();
                            var st = ma.Substring(j, 2);
                            if (!st.All(char.IsDigit)) throw new FormatException();
                            var stam = int.Parse(ma.Substring(j + 2, 2), CultureInfo.InvariantCulture);
                            if (stam < 1 || j + 4 + stam > ma.Length) throw new FormatException();
                            sub[st] = ma.Substring(j + 4, stam);
                            j += 4 + stam;
                        }
                    }
                    catch (FormatException)
                    {
                        erros.Add("Bloco 26 (informações da conta do mercador) malformado.");
                    }
                    if (sub.TryGetValue("00", out var g)) { gui = g; if (g != "br.gov.bcb.pix") erros.Add("A GUI deve ser br.gov.bcb.pix (registro oficial Pix)."); }
                    else erros.Add("Faltou a GUI (bloco 26>00) do payload.");
                    if (sub.TryGetValue("01", out var k)) chave = k;
                    else erros.Add("Faltou a chave Pix (bloco 26>01) do payload.");
                }
                else erros.Add("Faltou o bloco 26 (informações da conta do mercador).");

                if (!campos.TryGetValue("52", out var mcc) || !mcc.All(char.IsDigit) || mcc.Length != 4)
                    erros.Add("O campo 52 (MCC) deve ter exatamente 4 dígitos.");
                if (!campos.TryGetValue("53", out var moeda) || moeda != "986")
                    erros.Add("O campo 53 (moeda) deve ser 986 (real).");
                if (!campos.TryGetValue("58", out var pais) || pais != "BR")
                    erros.Add("O campo 58 (país) deve ser BR.");
                if (!campos.TryGetValue("59", out var nome) || nome.Length < 1 || nome.Length > MaxNomeBeneficiario)
                    erros.Add($"O campo 59 (nome do beneficiário) deve ter de 1 a {MaxNomeBeneficiario} caracteres.");
                if (!campos.TryGetValue("60", out var cidade) || cidade.Length < 1 || cidade.Length > MaxCidadeBeneficiario)
                    erros.Add($"O campo 60 (cidade) deve ter de 1 a {MaxCidadeBeneficiario} caracteres.");
                if (campos.TryGetValue("54", out var v))
                {
                    var okValor = v.Length <= 13 && v.All(c => char.IsDigit(c) || c == '.')
                        && v.Split('.').Length <= 2
                        && decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out var num) && num > 0;
                    if (!okValor) erros.Add("O campo 54 (valor) deve ser numérico positivo com ponto decimal, em reais.");
                }
                if (campos.TryGetValue("62", out var extra))
                {
                    try
                    {
                        var j = 0;
                        while (j < extra.Length)
                        {
                            var st = extra.Substring(j, 2);
                            var stam = int.Parse(extra.Substring(j + 2, 2), CultureInfo.InvariantCulture);
                            if (st == "05")
                            {
                                txid = extra.Substring(j + 4, stam);
                                if (txid.Length < 1 || txid.Length > 25 || !txid.All(c => char.IsLetterOrDigit(c) || c == '*'))
                                    erros.Add("O identificador da transação (campo 62>05) deve ter de 1 a 25 caracteres alfanuméricos.");
                            }
                            j += 4 + stam;
                        }
                    }
                    catch (Exception)
                    {
                        erros.Add("Bloco 62 (dados adicionais) malformado.");
                    }
                }
            }

            // O campo 63 fecha o payload: CRC16 calculado sobre tudo antes dele.
            if (ultimoTag == "63" && campos.TryGetValue("63", out var crc) && crc.Length == 4)
            {
                var corpo = payload[..^4];
                if (!string.Equals(Crc16Ccitt(corpo), crc, StringComparison.OrdinalIgnoreCase))
                    erros.Add("CRC16 (campo 63) não confere com o payload: o copia-e-cola pode estar truncado ou alterado.");
            }
            else erros.Add("O payload deve terminar com o campo 63 (CRC16).");
        }

        return new PixValidacaoDto(erros.Count == 0, erros, gui, chave,
            campos.TryGetValue("52", out var m2) ? m2 : null,
            campos.TryGetValue("53", out var m3) ? m3 : null,
            campos.TryGetValue("58", out var m58) ? m58 : null,
            campos.TryGetValue("59", out var m59) ? m59 : null,
            campos.TryGetValue("60", out var m60) ? m60 : null,
            campos.TryGetValue("54", out var m54) ? m54 : null,
            txid);
    }

    // ---------------------------------------------------------------- modulo 10 / 11

    /// <summary>DV módulo 11 do boleto: pesos 2..9 da direita para a esquerda; resto 0, 1 ou 10 => DV 1.</summary>
    public static (int X, int R, int DV) Modulo11Boleto(string digits)
    {
        var soma = 0;
        var peso = 2;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            soma += (digits[i] - '0') * peso;
            peso = peso == 9 ? 2 : peso + 1;
        }
        var resto = soma % 11;
        var dv = resto == 0 || resto == 1 || resto == 10 ? 1 : 11 - resto;
        return (soma, resto, dv);
    }

    /// <summary>DV módulo 10 dos três primeiros campos da linha digitável: pesos 2,1,2,1 da direita.</summary>
    public static (int X, int R, int DV) Modulo10Campo(string digits)
    {
        var soma = 0;
        var peso = 2;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var produto = (digits[i] - '0') * peso;
            soma += produto >= 10 ? produto / 10 + produto % 10 : produto;
            peso = peso == 2 ? 1 : 2;
        }
        var resto = soma % 10;
        return (soma, resto, (10 - resto) % 10);
    }

    // ---------------------------------------------------------------- boleto

    /// <summary>Fator de vencimento: dias corridos desde a data base oficial, com rollover mod 10000.</summary>
    public static (long DiasTotais, int FatorExibido, long Rollover) FatorVencimento(DateOnly vencimento, DateOnly dataBase)
    {
        var dias = (vencimento.ToDateTime(TimeOnly.MinValue) - dataBase.ToDateTime(TimeOnly.MinValue)).Days;
        if (dias <= 0)
            throw new InvalidOperationException("O vencimento precisa ser posterior à data base do fator de vencimento.");
        var exibido = (int)(((dias % 10000) + 10000) % 10000);
        if (exibido == 0)
            throw new InvalidOperationException("O fator de vencimento resultante seria 0000, ambíguo no código de barras (regra da CC 2926).");
        return (dias, exibido, dias / 10000);
    }

    /// <summary>Campo livre do código de barras (25): agência 4 + carteira 2 + nosso número 11 + conta cedente 7 + zero 1.</summary>
    public static string CampoLivreBoleto(string agencia4, string carteira2, long nossoNumero, string contaCedente7) =>
        agencia4 + carteira2 + nossoNumero.ToString(CultureInfo.InvariantCulture).PadLeft(11, '0') + contaCedente7 + "0";

    /// <summary>Código de barras (linha número) de 44 posições com o DV na posição 5.</summary>
    public static string LinhaNumeroBoleto(string banco3, char moeda, int fatorExibido, long valorCentavos, string campoLivre25)
    {
        var corpo = banco3 + moeda + fatorExibido.ToString("D4", CultureInfo.InvariantCulture)
            + valorCentavos.ToString(CultureInfo.InvariantCulture).PadLeft(10, '0') + campoLivre25;
        return corpo.Insert(4, Modulo11Boleto(corpo).DV.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>Linha digitável de 47 posições (raw e exibição pontuada) a partir do código de barras de 44.</summary>
    public static (string Raw47, string Exibicao) LinhaDigitavelBoleto(string linhaNumero44)
    {
        if (linhaNumero44.Length != 44)
            throw new InvalidOperationException("O código de barras do boleto tem exatamente 44 posições.");
        var campoLivre = linhaNumero44.Substring(19, 25);
        var b1 = linhaNumero44.Substring(0, 4) + campoLivre.Substring(0, 5);
        var b2 = campoLivre.Substring(5, 10);
        var b3 = campoLivre.Substring(15, 10);
        var dv1 = Modulo10Campo(b1).DV.ToString(CultureInfo.InvariantCulture);
        var dv2 = Modulo10Campo(b2).DV.ToString(CultureInfo.InvariantCulture);
        var dv3 = Modulo10Campo(b3).DV.ToString(CultureInfo.InvariantCulture);
        var fator = linhaNumero44.Substring(5, 4);
        var valor = linhaNumero44.Substring(9, 10);
        var raw = b1 + dv1 + b2 + dv2 + b3 + dv3 + linhaNumero44.Substring(4, 1) + fator + valor;
        var exibicao = b1.Substring(0, 5) + "." + b1.Substring(5) + dv1 + " "
                     + b2.Substring(0, 5) + "." + b2.Substring(5) + dv2 + " "
                     + b3.Substring(0, 5) + "." + b3.Substring(5) + dv3 + " "
                     + linhaNumero44.Substring(4, 1) + " " + fator + valor;
        return (raw, exibicao);
    }

    /// <summary>Valida uma linha digitável de 47: confere os três DV mod-10, o DV geral mod-11 e extrai os dados.</summary>
    public static LinhaDigitavelValidacaoDto ValidarLinhaDigitavel(string linha, DateOnly dataBase, DateOnly hoje)
    {
        var erros = new List<string>();
        var digitos = linha.Replace(" ", string.Empty).Replace("-", string.Empty).Replace(".", string.Empty);
        if (digitos.Length != 47 || !digitos.All(char.IsDigit))
        {
            erros.Add("A linha digitável do boleto tem exatamente 47 dígitos numéricos.");
            return new LinhaDigitavelValidacaoDto(false, erros, null, null, null, null, null, null);
        }

        var banco = digitos.Substring(0, 3);
        var moeda = digitos[3].ToString();
        if (moeda != "9") erros.Add($"A moeda (posição 4) deve ser 9 (real); veio '{moeda}'.");

        var b1 = digitos.Substring(0, 9);
        var b2 = digitos.Substring(10, 10);
        var b3 = digitos.Substring(21, 10);
        if (Modulo10Campo(b1).DV.ToString(CultureInfo.InvariantCulture) != digitos[9].ToString())
            erros.Add("Dígito verificador do 1º campo (módulo 10) não confere.");
        if (Modulo10Campo(b2).DV.ToString(CultureInfo.InvariantCulture) != digitos[20].ToString())
            erros.Add("Dígito verificador do 2º campo (módulo 10) não confere.");
        if (Modulo10Campo(b3).DV.ToString(CultureInfo.InvariantCulture) != digitos[31].ToString())
            erros.Add("Dígito verificador do 3º campo (módulo 10) não confere.");

        var fatorStr = digitos.Substring(33, 4);
        var valorStr = digitos.Substring(37, 10);
        var campoLivre = digitos.Substring(4, 5) + b2 + b3;
        var corpo = banco + moeda + fatorStr + valorStr + campoLivre;
        if (Modulo11Boleto(corpo).DV.ToString(CultureInfo.InvariantCulture) != digitos[32].ToString())
            erros.Add("Dígito verificador geral (posição 33, módulo 11) não confere com o código de barras reconstituído.");

        var fator = int.Parse(fatorStr, CultureInfo.InvariantCulture);
        var valor = long.Parse(valorStr, CultureInfo.InvariantCulture);

        // Janela de rollover: o mesmo fator exibido ocorre a cada 10000 dias; a provável é a primeira >= hoje.
        var hojeDt = hoje.ToDateTime(TimeOnly.MinValue);
        DateOnly? vencimento = null;
        for (var k = 0; k < 4; k++)
        {
            var candidato = dataBase.ToDateTime(TimeOnly.MinValue).AddDays(fator + k * 10000L).Date;
            vencimento = DateOnly.FromDateTime(candidato);
            if (candidato >= hojeDt) break;
        }

        return new LinhaDigitavelValidacaoDto(erros.Count == 0, erros, banco, moeda, fator, vencimento, valor, campoLivre);
    }

    // ---------------------------------------------------------------- CNAB 400 ARREM (canon documentado)

    private static string Preencher(char c, int n) => new(c, n);
    private static string AlEsq(string s, int n) => s.Length >= n ? s[..n] : s.PadRight(n, ' ');
    private static string Zq(string s, int n) => s.Length >= n ? s[..n] : s.PadLeft(n, '0');

    /// <summary>Cabeçalho da remessa (400 posições): tipo 01, RECE/REMISSAO, banco, agência, conta, nome, data, sequências.</summary>
    public static string CnabCabecalhoRemessa(string banco3, string agencia4, string contaCedente7, string beneficiarioNome, DateOnly dataExecucao, int sequenciaArquivo = 1)
    {
        var sb = new StringBuilder();
        sb.Append('0').Append('1').Append("RECE").Append("REMISSAO")
          .Append(Zq(banco3, 3)).Append(agencia4).Append(' ').Append(AlEsq(contaCedente7, 11))
          .Append(AlEsq(beneficiarioNome, 16)).Append(dataExecucao.ToString("ddMMyyyy", CultureInfo.InvariantCulture)).Append('0')
          .Append(sequenciaArquivo.ToString("D4", CultureInfo.InvariantCulture)).Append("0000").Append("000").Append("0000").Append("0000")
          .Append(Preencher('*', 323));
        return sb.ToString();
    }

    /// <summary>Registro de cobrança (400 posições): nosso número D17, linha número 44, vencimento ddMMyyyy, valor D15 centavos.</summary>
    public static string CnabRegistroRemessa(string agencia4, string contaCedente7, string beneficiarioNome, long nossoNumero, string linhaNumero44, DateOnly vencimento, long valorCentavos)
    {
        var sb = new StringBuilder();
        sb.Append('1').Append('0').Append(Preencher('*', 8))
          .Append("000").Append(agencia4).Append(' ').Append(AlEsq(contaCedente7, 11))
          .Append(AlEsq(beneficiarioNome, 16)).Append(Preencher(' ', 4))
          .Append('1').Append('0').Append('0').Append(Zq(nossoNumero.ToString(CultureInfo.InvariantCulture), 17))
          .Append(Preencher(' ', 6)).Append(linhaNumero44).Append(Preencher(' ', 4)).Append(' ')
          .Append(vencimento.ToString("ddMMyyyy", CultureInfo.InvariantCulture)).Append(Zq(valorCentavos.ToString(CultureInfo.InvariantCulture), 15))
          .Append("000").Append(Preencher('0', 15)).Append("99").Append(Preencher('0', 15)).Append(' ')
          .Append("000").Append(Preencher('0', 15)).Append(Preencher(' ', 6)).Append(Preencher(' ', 2))
          .Append('1').Append('0').Append("00000000").Append(Preencher('0', 44))
          .Append(Preencher(' ', 20)).Append(Preencher(' ', 20)).Append(Preencher(' ', 2)).Append(Preencher(' ', 4))
          .Append('0').Append('0').Append(Preencher(' ', 89));
        return sb.ToString();
    }

    /// <summary>Trailer da remessa (400 posições): total de registros e total em centavos.</summary>
    public static string CnabTrailerRemessa(string agencia4, string contaCedente7, string beneficiarioNome, int totalRegistros, long totalCentavos)
    {
        var sb = new StringBuilder();
        sb.Append('9').Append(' ').Append(Preencher('*', 8)).Append("000")
          .Append(agencia4).Append(' ').Append(AlEsq(contaCedente7, 11))
          .Append(AlEsq(beneficiarioNome, 16)).Append(Preencher(' ', 4))
          .Append('0').Append('0').Append('0').Append(Preencher('0', 17)).Append(Preencher(' ', 4)).Append("0000")
          .Append(totalRegistros.ToString("D3", CultureInfo.InvariantCulture)).Append("0000").Append("0000")
          .Append(Preencher('0', 17)).Append("00000").Append(Preencher('0', 14)).Append("0000").Append("0000")
          .Append(Zq(totalCentavos.ToString(CultureInfo.InvariantCulture), 15)).Append(Preencher(' ', 253));
        return sb.ToString();
    }

    /// <summary>Monta o arquivo com quebras CRLF entre as linhas e fim de arquivo CRLF (padrão CNAB).</summary>
    public static string MontarArquivoRemessa(IReadOnlyList<string> linhas) => string.Join("\r\n", linhas) + "\r\n";
}
