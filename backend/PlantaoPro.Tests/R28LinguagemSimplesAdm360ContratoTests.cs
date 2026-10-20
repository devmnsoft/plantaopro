using System.Text;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R28 (diretriz r24a1, epic E1 — "linguagem que qualquer usuário entenda", zona Adm360 parte 1:
/// suprimentos/compras/fiscal): jargão técnico renderizado (UUID em labels/placeholders, outbox em
/// título, "por API", GUID, BOM) reescrito em linguagem de usuário. Este fato trava anti-regressão
/// dos literais antigos E presença dos novos (contra remoção acidental). Whitelist (código/comentário,
/// não renderiza): @Guid.NewGuid() em inputs hidden idempotencyKey, comentários HTML/Razor que mencionam
/// UUID (Cotacoes\Detalhes s8), URLs /api/... em href, tokens fiscais SEFAZ/DIF. SHA-256 no Importar foi
/// mantido como informação de integridade útil — só a redação mudou ("hash verificado" →
/// "integridade verificada"). Inventário file:line em
/// docs/evidencias/2026-10-18-rodada-28/r28a0-linguagem-simples-adm360-parte1-plano.md.
/// </summary>
public class R28LinguagemSimplesAdm360ContratoTests
{
    private static string ReadRelative(params string[] partes) => File.ReadAllText(
        Path.Combine(new[] { RepositoryPathResolver.RepoRoot }.Concat(partes).ToArray()), Encoding.UTF8);

    [Fact]
    public void Adm360Parte1_JargaoTecnicoRemovidoDasViews()
    {
        var orcamento = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "OrcamentoNovo.cshtml");
        Assert.DoesNotContain("UUID do Vendedor", orcamento, StringComparison.Ordinal);

        var vale = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "ValeNovo.cshtml");
        Assert.DoesNotContain("UUID da reserva", vale, StringComparison.Ordinal);
        Assert.DoesNotContain("Reserva Vinculada (UUID)", vale, StringComparison.Ordinal);

        var relacionamento = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "Cotacoes", "Relacionamento.cshtml");
        Assert.DoesNotContain("ID Interno UUID", relacionamento, StringComparison.Ordinal);

        var docXml = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "DocumentosXml", "Detalhes.cshtml");
        Assert.DoesNotContain("(UUID)", docXml, StringComparison.Ordinal);
        Assert.DoesNotContain("idempotente", docXml, StringComparison.Ordinal);

        // Regra estrutural: a palavra de arquitetura "outbox" sai de TODA a view do monitor (título + estado vazio).
        var monitor = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "Gestao", "Monitor.cshtml");
        Assert.DoesNotContain("outbox", monitor, StringComparison.OrdinalIgnoreCase);

        var titulo = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "TituloDetalhes.cshtml");
        Assert.DoesNotContain("bancária", titulo, StringComparison.Ordinal);
        Assert.DoesNotContain("por API", titulo, StringComparison.Ordinal);

        var fiscalConfigurar = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "Fiscal", "Configurar.cshtml");
        Assert.DoesNotContain("pool da API", fiscalConfigurar, StringComparison.Ordinal);

        var fiscalNova = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "Fiscal", "Nova.cshtml");
        Assert.DoesNotContain("(GUID)", fiscalNova, StringComparison.Ordinal);

        var importar = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "DocumentosXml", "Importar.cshtml");
        Assert.DoesNotContain("incluindo BOM", importar, StringComparison.Ordinal);
    }

    [Fact]
    public void Adm360Parte1_TextosNovosEmLinguagemSimples_Existem()
    {
        var orcamento = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "OrcamentoNovo.cshtml");
        Assert.Contains("Código do vendedor (opcional)", orcamento, StringComparison.Ordinal);

        var vale = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "ValeNovo.cshtml");
        // Nota R33: o placeholder "Código da reserva ativa (opcional)" deixou de existir quando o campo
        // virou select de reservas ativas (R33E6LookupsReservaEPedidoContratoTests cobre a estrutura nova).
        // R59 (D-R59-1): a varredura de th scope inseriu scope="col" no cabeçalho; o literal travado
        // acompanhou a mudança (mesmo tratamento dado pelo contrato R57 em copy tratada).
        Assert.Contains("<th scope=\"col\">Reserva vinculada</th>", vale, StringComparison.Ordinal);

        var monitor = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "Gestao", "Monitor.cshtml");
        Assert.Contains("Envios para os fornecedores (@Model.RespostasRecentes.Count)", monitor, StringComparison.Ordinal);
        Assert.Contains("Nenhum envio para os fornecedores registrado ainda.", monitor, StringComparison.Ordinal);

        var titulo = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "TituloDetalhes.cshtml");
        Assert.Contains("Sem confirmação automática com o banco.", titulo, StringComparison.Ordinal);

        var fiscalConfigurar = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "Fiscal", "Configurar.cshtml");
        // R73 (linguagem simples v2): "banco de dados" virou "registros do sistema"; o literal travado
        // acompanhou a mudança (mesmo tratamento dado pelo contrato R57 em copy tratada).
        Assert.Contains("nunca nos registros do sistema nem nas telas.", fiscalConfigurar, StringComparison.Ordinal);

        var importar = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "DocumentosXml", "Importar.cshtml");
        Assert.Contains("com integridade verificada (SHA-256)", importar, StringComparison.Ordinal);
    }
}
