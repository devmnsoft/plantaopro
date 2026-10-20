namespace PlantaoPro.Web.Localization;

/// <summary>
/// R71 — Mapa único de estados para semântica de cor: estado normalizado -&gt; (tom, icone).
/// Fonte única do pareamento entre um estado de negócio e sua semântica visual.
/// O tom -&gt; cor vive somente na camada canônica (v2151 secao 9, tokens
/// --pp-tone-&lt;tom&gt;-ink | --pp-tone-&lt;tom&gt;-soft). O componente
/// &lt;partial name="_StatusBadge"&gt; renderiza sempre icone + texto (nunca cor sozinha);
/// estados fora do mapa caem no fallback neutro (secondary / pergunta).
/// Os slugs cobrem os valores reais dos guards CHECK do banco (database/scrpt_completo.sql),
/// as classes legadas .status-* e os valores exibidos pelas telas.
/// Contrato travado em: PlantaoPro.Tests/R71SemanticaDeCoresContratoTests.
/// </summary>
public static class EstadoVisual
{
    /// <summary>Resultado da interpretacao de um estado bruto vindo do dominio.</summary>
    public readonly record struct Estado(string Nome, string Tom, string Icone, string Rotulo);

    private static readonly Dictionary<string, (string Tom, string Icone)> Mapa = Construir();

    /// <summary>Tons admitidos pelo mapa (mesmos da paleta funcional R69 / sec. 8).</summary>
    public static readonly string[] Tons = { "success", "warning", "info", "danger", "secondary" };

    /// <summary>
    /// Interpreta um valor bruto de estado: normaliza (minúsculas, espaco/traco,
    /// sem acentos), resolve tom e icone no mapa e devolve o rotulo amigavel.
    /// </summary>
    public static Estado Interpretar(string? bruto)
    {
        var rotulo = string.IsNullOrWhiteSpace(bruto) ? "N/A" : bruto.Trim();
        var nome = Normalizar(rotulo);
        if (string.IsNullOrWhiteSpace(nome)) nome = "indefinido";
        if (!Mapa.TryGetValue(nome, out var entrada)) entrada = ("secondary", "bi-question-circle");
        return new Estado(nome, entrada.Tom, entrada.Icone, rotulo);
    }

    /// <summary>Normalizacao identica ao padrao legado do _StatusBadge (slug com hifens).</summary>
    public static string Normalizar(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return string.Empty;
        return valor.Trim().ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-")
            .Replace("á", "a").Replace("à", "a").Replace("ã", "a").Replace("â", "a")
            .Replace("é", "e").Replace("ê", "e")
            .Replace("í", "i")
            .Replace("ó", "o").Replace("õ", "o").Replace("ô", "o")
            .Replace("ú", "u")
            .Replace("ç", "c");
    }

    private static Dictionary<string, (string Tom, string Icone)> Construir()
    {
        var mapa = new Dictionary<string, (string, string)>(StringComparer.Ordinal);
        void M(string estado, string tom, string icone) => mapa[estado] = (tom, icone);

        // ---- success: concluido, confirmado, ativo, positivo ----------------
        M("pago", "success", "bi-check-circle-fill");
        M("paga", "success", "bi-check-circle-fill");
        M("quitado", "success", "bi-check-circle-fill");
        M("liquidado", "success", "bi-check-circle-fill");
        M("conferido", "success", "bi-check-circle-fill");
        M("vinculado", "success", "bi-check-circle-fill");
        M("aprovado", "success", "bi-check-circle-fill");
        M("aprovada", "success", "bi-check-circle-fill");
        M("concluido", "success", "bi-check-circle-fill");
        M("concluida", "success", "bi-check-circle-fill");
        M("confirmado", "success", "bi-check-circle-fill");
        M("confirmada", "success", "bi-check-circle-fill");
        M("realizado", "success", "bi-check-circle-fill");
        M("realizada", "success", "bi-check-circle-fill");
        M("ativo", "success", "bi-check-circle-fill");
        M("ativa", "success", "bi-check-circle-fill");
        M("resolvido", "success", "bi-check-circle-fill");
        M("resolvida", "success", "bi-check-circle-fill");
        M("liberado", "success", "bi-check-circle-fill");
        M("preenchido", "success", "bi-check-circle-fill");
        M("vigente", "success", "bi-check-circle-fill");
        M("sucesso", "success", "bi-check-circle-fill");
        M("sem-bloqueio", "success", "bi-check-circle-fill");
        M("aceito", "success", "bi-check-circle-fill");
        M("aceite", "success", "bi-check-circle-fill");
        M("aceita-pelo-portal", "success", "bi-check-circle-fill");
        M("assinado", "success", "bi-check-circle-fill");
        M("convertida", "success", "bi-check-circle-fill");
        M("exportada-manualmente", "success", "bi-check-circle-fill");
        M("configurado", "success", "bi-check-circle-fill");
        M("configurada", "success", "bi-check-circle-fill");
        M("pronto", "success", "bi-check-circle-fill");
        M("ciencia-da-operacao", "success", "bi-check-circle-fill");
        M("confirmacao-da-operacao", "success", "bi-check-circle-fill");
        M("vencedora", "success", "bi-check-circle-fill");
        M("resultado-disponivel", "success", "bi-check-circle-fill");
        M("autorizado", "success", "bi-check-circle-fill");
        M("respondida", "success", "bi-check-circle-fill");
        M("em-uso", "success", "bi-check-circle-fill");
        M("fechado", "success", "bi-check-circle-fill");

        // ---- warning / relogio: aguardando acao humana ou sistema ------------
        M("pendente", "warning", "bi-clock-fill");
        M("aguardando", "warning", "bi-clock-fill");
        M("aguardando-aprovacao", "warning", "bi-clock-fill");
        M("aguardando-agendamento", "warning", "bi-clock-fill");
        M("aguardando-decisao", "warning", "bi-clock-fill");
        M("aguardando-intervalo", "warning", "bi-clock-fill");
        M("autorizacao-pendente", "warning", "bi-clock-fill");
        M("em-analise", "warning", "bi-clock-fill");
        M("aberto", "warning", "bi-clock-fill");
        M("aberta", "warning", "bi-clock-fill");
        M("rascunho", "warning", "bi-clock-fill");
        M("solicitado", "warning", "bi-clock-fill");
        M("solicitada", "warning", "bi-clock-fill");
        M("suspenso", "warning", "bi-clock-fill");
        M("suspensa", "warning", "bi-clock-fill");
        M("parado", "warning", "bi-clock-fill");
        M("apurado", "warning", "bi-clock-fill");
        M("nao-assinado", "warning", "bi-clock-fill");
        M("nao-configurada", "warning", "bi-clock-fill");
        M("configuracao-pendente", "warning", "bi-clock-fill");
        M("pendente-de-configuracao", "warning", "bi-clock-fill");
        M("pronta-para-envio", "warning", "bi-clock-fill");
        M("reaberto", "warning", "bi-clock-fill");
        M("afastado", "warning", "bi-clock-fill");
        M("sem-manifestacao", "warning", "bi-clock-fill");

        // ---- warning / triangulo: exige atencao, pode virar problema --------
        M("atrasado", "warning", "bi-exclamation-triangle-fill");
        M("vencido", "warning", "bi-exclamation-triangle-fill");
        M("vencida", "warning", "bi-exclamation-triangle-fill");
        M("contestado", "warning", "bi-exclamation-triangle-fill");
        M("em-contestacao", "warning", "bi-exclamation-triangle-fill");
        M("divergente", "warning", "bi-exclamation-triangle-fill");
        M("com-divergencia", "warning", "bi-exclamation-triangle-fill");
        M("risco", "warning", "bi-exclamation-triangle-fill");
        M("bloqueio-aberto", "warning", "bi-exclamation-triangle-fill");
        M("devolvido", "warning", "bi-exclamation-triangle-fill");
        M("parcialmente-resultado", "warning", "bi-exclamation-triangle-fill");
        M("desconhecimento", "warning", "bi-exclamation-triangle-fill");
        M("registro-incompleto", "warning", "bi-exclamation-triangle-fill");
        M("correcao-pendente", "warning", "bi-exclamation-triangle-fill");
        M("ajuste-pos-apuracao", "warning", "bi-exclamation-triangle-fill");
        M("retorno-solicitado", "warning", "bi-exclamation-triangle-fill");
        M("nao-atendido", "warning", "bi-exclamation-triangle-fill");
        M("alta", "warning", "bi-exclamation-triangle-fill");
        M("media", "warning", "bi-exclamation-triangle-fill");
        M("moderada", "warning", "bi-exclamation-triangle-fill");

        // ---- info / setas: processo em movimento ----------------------------
        M("em-andamento", "info", "bi-arrow-repeat");
        M("processando", "info", "bi-arrow-repeat");
        M("sincronizando", "info", "bi-arrow-repeat");
        M("carregando", "info", "bi-arrow-repeat");
        M("em-conferencia", "info", "bi-arrow-repeat");
        M("em-atendimento", "info", "bi-arrow-repeat");
        M("em-preenchimento", "info", "bi-arrow-repeat");
        M("enviando", "info", "bi-arrow-repeat");
        M("financeiro-gerado", "info", "bi-arrow-repeat");
        M("gerado", "info", "bi-arrow-repeat");
        M("iniciada", "info", "bi-arrow-repeat");
        M("em-relacionamento", "info", "bi-arrow-repeat");
        M("em-orcamento", "info", "bi-arrow-repeat");
        M("substituida", "info", "bi-arrow-repeat");

        // ---- info / comunicacao ---------------------------------------------
        M("nao-lida", "info", "bi-envelope-fill");
        M("enviado", "info", "bi-envelope-fill");
        M("enviada", "info", "bi-envelope-fill");
        M("recebida", "info", "bi-envelope-fill");

        // ---- info / marcacao: agendado, periodo de teste, novidade, visivel --
        M("agendado", "info", "bi-calendar-check");
        M("trial", "info", "bi-hourglass-split");
        M("criado", "info", "bi-plus-circle-fill");
        M("publicado", "info", "bi-globe2");
        M("disponivel", "info", "bi-globe2");
        M("online", "info", "bi-globe2");
        M("relacionado", "info", "bi-globe2");
        M("teste", "info", "bi-globe2");

        // ---- danger / cruz: rejeitado, cancelado, perdido --------------------
        M("rejeitado", "danger", "bi-x-circle-fill");
        M("rejeitada", "danger", "bi-x-circle-fill");
        M("rejeitada-pelo-portal", "danger", "bi-x-circle-fill");
        M("cancelado", "danger", "bi-x-circle-fill");
        M("cancelada", "danger", "bi-x-circle-fill");
        M("cancelada-portal", "danger", "bi-x-circle-fill");
        M("recusado", "danger", "bi-x-circle-fill");
        M("recusada", "danger", "bi-x-circle-fill");
        M("expirado", "danger", "bi-x-circle-fill");
        M("expirada", "danger", "bi-x-circle-fill");
        M("estornado", "danger", "bi-x-circle-fill");
        M("estornada", "danger", "bi-x-circle-fill");
        M("negado", "danger", "bi-x-circle-fill");
        M("operacao-nao-realizada", "danger", "bi-x-circle-fill");

        M("conflito", "danger", "bi-exclamation-octagon-fill");

        // ---- danger / oitavangulo: critico, falha, bloqueio total ------------
        M("critico", "danger", "bi-exclamation-octagon-fill");
        M("critica", "danger", "bi-exclamation-octagon-fill");
        M("urgente", "danger", "bi-exclamation-octagon-fill");
        M("emergencia", "danger", "bi-exclamation-octagon-fill");
        M("erro", "danger", "bi-exclamation-octagon-fill");
        M("falha", "danger", "bi-exclamation-octagon-fill");
        M("falhou", "danger", "bi-exclamation-octagon-fill");
        M("bloqueado", "danger", "bi-exclamation-octagon-fill");
        M("bloqueada", "danger", "bi-exclamation-octagon-fill");
        M("erro-autenticacao", "danger", "bi-exclamation-octagon-fill");

        // ---- secondary / risca: inativo, encerrado, descartado --------------
        M("inativo", "secondary", "bi-circle-slash");
        M("inativa", "secondary", "bi-circle-slash");
        M("desativado", "secondary", "bi-circle-slash");
        M("desativada", "secondary", "bi-circle-slash");
        M("desligado", "secondary", "bi-circle-slash");
        M("finalizado", "secondary", "bi-circle-slash");
        M("finalizada", "secondary", "bi-circle-slash");
        M("encerrado", "secondary", "bi-circle-slash");
        M("encerrada", "secondary", "bi-circle-slash");
        M("ignorado", "secondary", "bi-circle-slash");
        M("perdida", "secondary", "bi-circle-slash");

        // ---- secondary / arquivo e consulta ---------------------------------
        M("arquivado", "secondary", "bi-archive-fill");
        M("arquivada", "secondary", "bi-archive-fill");
        M("lida", "secondary", "bi-check-circle-fill");
        M("indefinido", "secondary", "bi-question-circle");
        M("neutro", "secondary", "bi-question-circle");
        M("outro", "secondary", "bi-question-circle");
        M("nao-informado", "secondary", "bi-question-circle");
        M("resultado-desconhecido", "secondary", "bi-question-circle");

        return mapa;
    }
}

/// <summary>
/// Par (estado, rotulo) para badges canonicos com texto amigavel proprio
/// (_StatusBadgeComRotulo): o tom e o icone sao derivados do estado; o rotulo
/// exibido e o fornecido pela view (linguagem simples para leigos).
/// </summary>
public readonly record struct StatusRotulo(string Status, string Rotulo);
