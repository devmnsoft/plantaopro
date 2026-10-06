using System;
using System.Globalization;
using System.Linq;

namespace PlantaoPro.CrossCutting.Localization
{
    /// <summary>
    /// Contrato central de leitura de valores numericos digitados por humanos (item 2 do escopo:
    /// gate financeiro). Fonte unica de regras para converter texto em decimal, aplicada ao
    /// binding de formularios/route/query da aplicacao Web e aos pontos que parseiam strings.
    ///
    /// Regras (documentadas e cobertas por ValorHumanoTests):
    ///  - Espacos, "R$" e "$" sao ignorados; sinal apenas inicial ("-5,70" / "+5").
    ///  - Dois tipos de separador presentes: o que aparece POR ULTIMO e o separador decimal e
    ///    o outro e milhar ("1.234,56" -> 1234,56; "1,234.56" -> 1234,56).
    ///  - Separador unico com fracao de 1 a 4 casas: decimal ("12,50" -> 12.50; "12.50" -> 12.50).
    ///  - Separador unico com fracao de EXATAMENTE 3 casas: AMBIGUO -> rejeitado
    ///    ("1.234" pode ser mil duzentos e trinta e quatro ou um ponto dois tres quatro).
    ///  - Separador unico repetido: agrupamento de milhar, ultimo grupo com 3 digitos
    ///    ("1.234.567" -> 1234567).
    ///  - Fracao vazia apos separador ("12,", "12.") ou separador so (".", ","): incompleto -> rejeitado.
    ///  - Mais de 4 casas decimais: rejeitado (precisao numeric(18,4)).
    ///  - |valor| limitado ao teto de numeric(18,4).
    ///
    /// Nunca ha multiplicacao/divisao por 100: o valor digitado e o valor persistido.
    /// </summary>
    public static class ValorHumano
    {
        /// <summary>Teto absoluto compativel com o schema numeric(18,4).</summary>
        public const decimal LimiteMaximo = 999_999_999_999.9999m;

        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        /// <summary>
        /// Converte texto humano em decimal. Em falha retorna false com mensagem para exibicao
        /// (sem termos tecnicos tipo "Parameter 'x'").
        /// </summary>
        public static bool TentarConverter(string texto, out decimal valor, out string erro)
        {
            valor = 0m;
            erro = string.Empty;

            if (string.IsNullOrWhiteSpace(texto))
            {
                erro = "Informe um valor numerico.";
                return false;
            }

            string limpo = texto.Trim()
                .Replace("R$", string.Empty)
                .Replace("$", string.Empty)
                .Replace("\u00A0", string.Empty);
            limpo = new string(limpo.Where(c => !char.IsWhiteSpace(c)).ToArray());
            if (limpo.Length == 0)
            {
                erro = "Informe um valor numerico.";
                return false;
            }

            bool negativo = false;
            if (limpo[0] == '-') { negativo = true; limpo = limpo.Substring(1); }
            else if (limpo[0] == '+') { limpo = limpo.Substring(1); }
            if (limpo.Length == 0)
            {
                erro = "Valor invalido: use apenas digitos e os separadores de milhar/centavos.";
                return false;
            }

            int ultimoVirgula = limpo.LastIndexOf(',');
            int ultimoPonto = limpo.LastIndexOf('.');
            string inteira;
            string fracao;

            if (ultimoVirgula >= 0 && ultimoPonto >= 0)
            {
                // Dois tipos de separador: o ultimo e o decimal, o outro e milhar.
                if (ultimoVirgula > ultimoPonto)
                {
                    inteira = limpo.Substring(0, ultimoVirgula).Replace(".", string.Empty);
                    fracao = limpo.Substring(ultimoVirgula + 1);
                }
                else
                {
                    inteira = limpo.Substring(0, ultimoPonto).Replace(",", string.Empty);
                    fracao = limpo.Substring(ultimoPonto + 1);
                }
            }
            else if (ultimoVirgula >= 0 || ultimoPonto >= 0)
            {
                char sep = ultimoVirgula >= 0 ? ',' : '.';
                int ocorrencias = 0;
                for (int i = 0; i < limpo.Length; i++)
                    if (limpo[i] == sep) ocorrencias++;

                if (ocorrencias == 1)
                {
                    int indice = limpo.IndexOf(sep);
                    inteira = limpo.Substring(0, indice);
                    fracao = limpo.Substring(indice + 1);
                    if (fracao.Length == 0)
                    {
                        erro = "Valor incompleto: finalize os centavos ou remova o separador no final.";
                        return false;
                    }
                    if (fracao.Length == 3)
                    {
                        erro = "Separador ambiguo (" + texto.Trim() + "): pode ser milhar ou centavos. "
                             + "Use duas casas decimais (ex.: 1.234,00) ou informe o valor inteiro.";
                        return false;
                    }
                }
                else
                {
                    // Separador repetido do mesmo tipo: agrupamento de milhar.
                    string[] grupos = limpo.Split(sep);
                    if (grupos.Length >= 2 && grupos[grupos.Length - 1].Length != 3)
                    {
                        erro = "Agrupamento de milhares invalido: o ultimo grupo deve ter 3 digitos (ex.: 1.234.567 ou 1.234,56).";
                        return false;
                    }
                    inteira = string.Concat(grupos);
                    fracao = string.Empty;
                }
            }
            else
            {
                inteira = limpo;
                fracao = string.Empty;
            }

            if (!TodosDigitos(inteira) || !TodosDigitos(fracao))
            {
                erro = "Valor invalido: use apenas digitos e os separadores de milhar/centavos.";
                return false;
            }
            if (fracao.Length > 4)
            {
                erro = "Maximo de 4 casas decimais (precisao do sistema).";
                return false;
            }
            if (inteira.Length == 0 && fracao.Length == 0)
            {
                erro = "Informe um valor numerico.";
                return false;
            }
            // parte inteira ausente (ex.: ",50"): interpreta como 0,50
            string canonical = (inteira.Length > 0 ? inteira : "0") + (fracao.Length > 0 ? "." + fracao : string.Empty);

            // NumberStyles.None nao permite ponto decimal; o sinal/espaco ja foram
            // tratados acima e o expoente foi rejeitado pelo TodosDigitos.
            if (!decimal.TryParse(canonical, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal bruto))
            {
                erro = "Valor invalido.";
                return false;
            }
            bruto = negativo ? -bruto : bruto;
            if (bruto < -LimiteMaximo || bruto > LimiteMaximo)
            {
                erro = "Valor excede o limite do sistema (999.999.999.999,9999).";
                return false;
            }

            valor = bruto;
            return true;
        }

        /// <summary>
        /// Contrato unico de apresentacao de valores (pt-BR, N2): altera apenas a EXIBICAO
        /// (ex.: CSV/HTML), nunca o valor persistido em banco.
        /// </summary>
        public static string Format(decimal valor) => valor.ToString("N2", PtBr);

        private static bool TodosDigitos(string s)
        {
            foreach (char c in s)
                if (c < '0' || c > '9') return false;
            return true;
        }
    }
}
