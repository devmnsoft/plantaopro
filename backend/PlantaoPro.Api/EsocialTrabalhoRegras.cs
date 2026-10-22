using System;
using System.Collections.Generic;
using System.Linq;

namespace PlantaoPro.Api;

/// <summary>
/// R88 (trabalhista / eSocial): regras puras da prévia trabalhista — aviso prévio
/// proporcional, semana de referência e análise de jornada/horas extras. Sem IO.
/// Constantes espelham a fonte única parametros_legais (códigos AVISO_PREVIO_*,
/// JORNADA_SEMANAL_MAX_HORAS, HORA_EXTRA_ACRESIMO_MIN_PCT, SALARIO_DIVISOR_MENSAL_HORAS);
/// o serviço lê os parâmetros e passa os valores reais aqui como argumentos.
/// Fontes: Lei 12.506/2011 (aviso prévio 30 + 3/ano até 90 dias), Nota Técnica MTE nº
/// 184/2012 (acréscimo a partir do tempo superior a 1 ano na mesma empresa), CLT
/// (Decreto-Lei 5.452/1943) arts. 59 §1º e 74 §2º, CF/88 art. 7, XIII/XVI.
/// </summary>
public static class EsocialTrabalhoRegras
{
    public const int AvisoPrevioBaseDias = 30;
    public const int AvisoPrevioAcrescimoPorAnoDias = 3;
    public const int AvisoPrevioTetoTotalDias = 90;
    public const decimal JornadaSemanalMaxHoras = 44m;
    public const decimal HoraExtraAcrescimoMinPct = 50m;
    public const decimal SalarioDivisorMensalHoras = 220m;
    public const decimal HorasSemanaisMaxSanidade = 120m;

    /// <summary>Semana de referência eSocial/CLT: segunda-feira da semana contendo a data informada.</summary>
    public static DateOnly SemanaInicio(DateOnly qualquer)
    {
        // PG/C#: domingo = 0; segunda-feira = 1. Volta à segunda anterior quando necessário.
        var dias = ((int)qualquer.DayOfWeek + 6) % 7;
        return qualquer.AddDays(-dias);
    }

    /// <summary>Anos completos de serviço entre duas datas (0 quando inexistente).</summary>
    public static int AnosCompletos(DateOnly inicio, DateOnly fim)
    {
        if (fim < inicio) return 0;
        var anos = fim.Year - inicio.Year;
        if (fim.Month < inicio.Month || (fim.Month == inicio.Month && fim.Day < inicio.Day)) anos--;
        return Math.Max(0, anos);
    }

    /// <summary>
    /// Aviso prévio proporcional ao tempo de serviço (Lei 12.506/2011): base de
    /// 30 dias; acrescido de 3 dias por ano COMPLETO de serviço na mesma empresa,
    /// limitado o total a 90 dias. O acréscimo só incide com pelo menos 1 ano
    /// completo (Nota Técnica MTE nº 184/2012): tempo de até 1 ano mantém 30 dias.
    /// </summary>
    public static int DiasAvisoPrevio(DateOnly admissao, DateOnly demissao, int baseDias = AvisoPrevioBaseDias, int porAno = AvisoPrevioAcrescimoPorAnoDias, int tetoTotal = AvisoPrevioTetoTotalDias)
    {
        var anos = AnosCompletos(admissao, demissao);
        var acrescimo = anos > 0 ? Math.Min(porAno * anos, Math.Max(0, tetoTotal - baseDias)) : 0;
        return Math.Min(baseDias + acrescimo, tetoTotal);
    }

    /// <summary>Situações de uma semana declarada (estáveis para a interface e os testes).</summary>
    public static class SituacoesJornada
    {
        public const string NaoDeclarada = "NAO_DECLARADA";
        public const string Conforme = "CONFORME";
        public const string ExcedeContrato = "EXCEDE_CONTRATO";
        public const string ExcedeLimiteLegal = "EXCEDE_LIMITE_LEGAL";
    }

    public sealed record ResumoSemanaJornada(
        decimal Normais,
        decimal Extras,
        decimal Total,
        bool ExcedeLimiteLegal,
        bool? ExcedeContrato,
        decimal HorasExcedentes,
        string Situacao)
    {
        /// <param name="normais">horas normais declaradas na semana;</param>
        /// <param name="extras">horas extras declaradas na semana;</param>
        /// <param name="contratadaSemanal">carga horária semanal do contrato vigente (null = sem contrato vigente);</param>
        /// <param name="maxSemanal">teto legal semanal (CF/88 art. 7, XIII: 44h);</param>
        /// <returns>resumo com total, flags e horas excedentes (referência: contratual, ou o teto legal sem contrato).</returns>
        public static ResumoSemanaJornada Calcular(decimal normais, decimal extras, decimal? contratadaSemanal, decimal maxSemanal = JornadaSemanalMaxHoras)
        {
            var normaisBase = Math.Max(0m, normais);
            var excedentesBase = Math.Max(0m, extras);
            var total = normaisBase + excedentesBase;
            var excedeContrato = (bool?)(contratadaSemanal.HasValue ? total > contratadaSemanal.Value : null);
            var excedeLimite = total > maxSemanal;
            var referencia = contratadaSemanal ?? maxSemanal;
            var excedentes = total > referencia ? total - referencia : 0m;
            var situacao = total == 0m
                ? SituacoesJornada.NaoDeclarada
                : excedeLimite
                    ? SituacoesJornada.ExcedeLimiteLegal
                    : excedeContrato == true
                        ? SituacoesJornada.ExcedeContrato
                        : SituacoesJornada.Conforme;
            return new ResumoSemanaJornada(normais, excedentesBase, total, excedeLimite, excedeContrato, excedentes, situacao);
        }
    }

    /// <summary>Valor-hora extra estimado (referência): salário/divisor mensal com acréscimo mínimo, arredondado em 4 casas.</summary>
    public static decimal? ValorHoraExtra(decimal? salarioMensal, decimal? divisorMensal = SalarioDivisorMensalHoras, decimal acrescimoPct = HoraExtraAcrescimoMinPct)
    {
        if (!salarioMensal.HasValue || salarioMensal.Value <= 0m) return null;
        var divisor = divisorMensal ?? SalarioDivisorMensalHoras;
        if (divisor <= 0m) return null;
        return Math.Round(salarioMensal.Value / divisor * (1m + acrescimoPct / 100m), 4, MidpointRounding.AwayFromZero);
    }

    /// <summary>Estimativa do valor das horas excedentes (2 casas). Null quando não há valor-hora de referência.</summary>
    public static decimal? ValorHorasExtras(decimal? valorHoraExtra, decimal horas) =>
        valorHoraExtra is null || horas <= 0m ? null : Math.Round(valorHoraExtra.Value * horas, 2, MidpointRounding.AwayFromZero);

    /// <summary>Validação compartilhada das datas do aviso prévio; devolve todos os erros encontrados.</summary>
    public static IReadOnlyList<string> ValidarAvisoPrevio(DateOnly? admissao, DateOnly? demissao)
    {
        var erros = new List<string>();
        if (admissao is null || admissao < new DateOnly(1950, 1, 1)) erros.Add("Informe uma data de admissão válida (a partir de 1950-01-01).");
        if (demissao is null) erros.Add("Informe a data de demissão.");
        if (admissao is not null && demissao is not null && demissao < admissao) erros.Add("A data de demissão deve ser posterior (ou igual) à data de admissão.");
        return erros;
    }
}
