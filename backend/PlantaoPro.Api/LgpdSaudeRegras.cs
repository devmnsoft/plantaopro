using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PlantaoPro.Api;

/// <summary>
/// R87 - Regras puras de LGPD + saúde (sem IO). Vetores travados em contrato por
/// R87LgpdSaudeContratoTests. Fontes citáveis:
///   * Lei 13.709/2018 (LGPD) art. 7, I (consentimento); art. 11, I (dados sensíveis:
///     consentimento específico e destacado) e art. 11, II, "a" (assistência exclusiva
///     dispensa prévio consentimento: obrigação legal); art. 18 (direitos do titular);
///   * Lei 13.787/2018 art. 6 (prontuário: mínimo de 20 anos a partir do último registro);
///   * Resolução CFM nº 1.821/2007 arts. 7-8 (guarda permanente eletrônica / 20 anos papel).
/// O "último registro" é resolvido na camada de serviço (consultas + documentos clínicos).
/// </summary>
public static class LgpdSaudeRegras
{
    /// <summary>Escopos de consentimento de dados de saúde gerenciados pelo produto (espelho do catálogo lgpd_escopos_consentimento).</summary>
    public static readonly string[] Escopos = { "PRONTUARIO", "COAM" };

    /// <summary>Canais de captura com trilha auditável (espelho do parâmetro LGPD_SA_CANAIS).</summary>
    public static readonly string[] Canais = { "RECEPCAO", "TELEFONE", "DIGITAL", "PRONTUARIO" };

    /// <summary>Direitos do titular exercíveis (espelho do parâmetro LGPD_SA_DIREITOS).</summary>
    public static readonly string[] Direitos = { "LISTAR", "EXPORTAR", "APAGAR" };

    /// <summary>Base do tratamento de dado sensível além da assistência (LGPD art. 11, I).</summary>
    public const string BaseSensivel = "CONSENTIMENTO_EXPLICITO";

    /// <summary>Retenção mínima legal do prontuário em meses — 20 anos (Lei 13.787/2018, art. 6).</summary>
    public const int PrazoProntuarioMeses = 240;

    /// <summary>Motivo padronizado quando a eliminação está bloqueada pela retenção mínima legal.</summary>
    public const string MotivoRetencaoMinima = "RETENCAO_MINIMA";

    public const string FonteRetencaoProntuario = "Lei Federal 13.787/2018, art. 6; Resolução CFM n. 1.821/2007, arts. 7 e 8";

    /// <summary>Códigos de trilha gravados em lgpd_eventos_privacidade para cada exercício.</summary>
    public static string EventoTrilha(string direto) => direto switch
    {
        "LISTAR" => "LGPD_LISTAR_PACIENTE",
        "EXPORTAR" => "LGPD_EXPORTAR_PACIENTE",
        "APAGAR" => "LGPD_APAGAR_PACIENTE",
        _ => "LGPD_EVENTO"
    };

    public const string EventoTrilhaConsentimento = "LGPD_CONSENTIMENTO_REGISTRADO";

    public static string? ValidarEscopo(string? escopo) =>
        Escopos.Contains(escopo ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            ? null
            : "Escopo deve ser PRONTUARIO ou COAM.";

    public static string? ValidarDireto(string? direto) =>
        Direitos.Contains(direto ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            ? null
            : "Direito deve ser LISTAR, EXPORTAR ou APAGAR.";

    public static string? ValidarCanal(string? canal) =>
        Canais.Contains(canal ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            ? null
            : "Canal deve ser RECEPCAO, TELEFONE, DIGITAL ou PRONTUARIO.";

    /// <summary>
    /// Validação completa do registro de consentimento de saúde (todos os erros, não só o primeiro):
    /// escopo controlado, canal rastreável e versão do termo obrigatória quando concedido
    /// (dado sensível exige consentimento específico e destacado — LGPD art. 11, I).
    /// </summary>
    public static IReadOnlyList<string> ValidarConsentimento(string? escopo, bool concedido, string? canal, string? versaoTermo)
    {
        var erros = new List<string>();
        var e1 = ValidarEscopo(escopo);
        if (e1 != null) erros.Add(e1);
        var e2 = ValidarCanal(canal);
        if (e2 != null) erros.Add(e2);
        if (concedido && string.IsNullOrWhiteSpace(versaoTermo))
            erros.Add("Versão do termo obrigatória quando o consentimento é concedido.");
        return erros;
    }

    /// <summary>
    /// Soma meses completos a uma data, truncando o dia ao último dia do mês resultante
    /// (determinístico; 2004-01-31 + 1 mês = 2004-02-29).
    /// </summary>
    public static DateOnly AddMeses(DateOnly data, int meses)
    {
        var total = data.Year * 12L + (data.Month - 1) + meses;
        var ano = (int)(total / 12);
        var mes = (int)(total % 12) + 1;
        if (ano < 1) throw new ArgumentOutOfRangeException(nameof(meses), "Resultado anterior a 0001-01-01.");
        return new DateOnly(ano, mes, Math.Min(data.Day, DateTime.DaysInMonth(ano, mes)));
    }

    /// <summary>Data a partir da qual o prontuário pode ser eliminado: último registro + retenção mínima (Lei 13.787/2018, art. 6).</summary>
    public static DateOnly AptoDesdeUltimoRegistro(DateOnly ultimoRegistro, int prazoMeses = PrazoProntuarioMeses) =>
        AddMeses(ultimoRegistro, prazoMeses);

    /// <summary>A eliminação é permitida apenas a partir da data apta (dia igual já cumpre os 20 anos).</summary>
    public static bool PodeEliminarEm(DateOnly hoje, DateOnly aptoDesde) => hoje >= aptoDesde;

    /// <summary>Rótulo determinístico do paciente após eliminação/anonimização (LGPD art. 18, IV).</summary>
    public static string RotuloAnonimo(string semente) =>
        "ANONIMIZADO-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(semente))).ToLowerInvariant()[..12];
}
