using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R8-B19 — §27 linha 206: "Zero ≠ informação indisponível".
///
/// RESULTADO DA AUDITORIA (fonte canônica: o código de Bi + MeuDia, não o snapshot da varredura):
///
///   • <c>Bi/Index</c>: KPIs hidratadas por JS com `fetch('/api/bi/resumo-executivo')`. ANTES da fatia,
///     a cadeia era `.then(r=&gt;r.json()).then(x=&gt;{…`??0`…})` SEM guarda de sucesso HTTP e SEM `.catch` —
///     qualquer falha (403 do guard, 500, rede, parse) caía em `x.data==null` e renderizava `0`/`R$ 0,00`,
///     MASCARANDO "indisponível" como zero. FIX aplicado nesta fatia: guarda `if(!r.ok) throw` (padrão
///     `V112/OperationalPage`) + `.catch` que devolve TODOS os KPIs ao placeholder `-` (o mesmo usado na
///     inicialização server-side, ou seja NÃO é zero) e revela um `alert-warning` honesto. O caminho de
///     sucesso preserva `??0` — um zero REAL continua exibindo 0.
///   • <c>MeuDia/Index</c>: snapshot desatualizado — NÃO é JS-hidratado. É RENDERIZADO NO SERVIDOR
///     (`@Model.Summary.*`) e JÁ distingue falha de dado via `ProductivityWebService.GetAsync`, que mapeia
///     CADA classe de falha (401/403/5xx/outra, corpo nulo, JSON inválido, cancelamento, timeout, transporte)
///     para um `ErrorKind`+`Error` distintos; a view oculta os KPIs e mostra a mensagem + "Tentar novamente".
///     =&gt; existente+comprovada, sem mudança.
///
///   §27 linha 208 (drill-down por KPI): proposta/pendente — depende da definição das métricas (dívida
///   "Meu Dia/CommandCenter/Admin360 definitions") registrada em B18. Fora do rito desta fatia (sem regra inventada).
///
/// Única mudança de produção: estado-de-erro do Bi/Index. Contrato anti-drift:
/// (1) Bi guarda falha de API + devolve KPIs ao placeholder (não zero) + banner honesto + preserva ??0 no sucesso;
/// (2) MeuDia segue server-rendered com estado de erro (não há hidratação JS que zere em falha);
/// (3) ProductivityWebService mapeia cada classe de falha para erro honesto distinto.
/// </summary>
public sealed class R8B19BiMeuDiaEstadoDeErroContratoTests
{
    private static string Web(params string[] segs) =>
        Path.Combine(new[] { RepositoryPathResolver.BackendRoot, "PlantaoPro.Web" }.Concat(segs).ToArray());

    [Fact]
    public void Bi_GuardaFalhaDeApi_DevolveKpisAoPlaceholder_NaoZera()
    {
        var bi = File.ReadAllText(Web("Views", "Bi", "Index.cshtml"));

        // Endpoint continua o mesmo (compat com R8-B18) e a hidratação segue client-side.
        Assert.Contains("fetch('/api/bi/resumo-executivo')", bi);

        // Guarda de sucesso HTTP (padrão V112/OperationalPage): 403/404/500 deixam de cair no json() e no ??0.
        Assert.Contains("if(!r.ok)", bi);

        // .catch presente: falha de rede/parse também cai num caminho honesto, sem exception não tratada.
        Assert.Contains(".catch(", bi);

        // No catch, todos os KPIs voltam ao placeholder '-' (o MESMO da inicialização server-side) — claramente
        // DISTINTO de '0'. Um zero real (API 200 com valor 0) segue exibindo 0 via ??0 preservado no sucesso.
        Assert.Contains("el.innerText='-'", bi);
        Assert.Contains("d.totalClientesAtivos??0", bi);

        // Banner honesto de indisponibilidade existe no markup e é revelado no catch.
        Assert.Contains("id=\"bi-status\"", bi);
        Assert.Contains("classList.remove('d-none')", bi);
    }

    [Fact]
    public void MeuDia_SegueServerRenderedComEstadoDeErro_NaoHidrataJSQueZera()
    {
        var meuDia = File.ReadAllText(Web("Views", "MeuDia", "Index.cshtml"));

        // Refuta o snapshot da varredura ("MeuDia ??0/toFixed sem catch"): os KPIs vêm do model server-side…
        Assert.Contains("@Model.Summary.Active", meuDia);
        Assert.DoesNotContain("fetch(", meuDia);

        // …e a página distingue falha de dado: mostra a mensagem de erro + ação de repetir e SUPRIME os KPIs.
        Assert.Contains("Model.Error", meuDia);
        Assert.Contains("Tentar novamente", meuDia);
    }

    [Fact]
    public void ProductivityWebService_MapeiaCadaClasseDeFalhaParaErroHonesto()
    {
        var svc = File.ReadAllText(Web("Services", "ProductivityWebService.cs"));

        // Sucesso é verificado explicitamente (não assume 200 por default)…
        Assert.Contains("IsSuccessStatusCode", svc);
        // …e cada classe de falha vira um ErrorKind+mensagem distintos (jamais um silêncio/zero).
        Assert.Contains("SEM_PERMISSAO", svc);      // 403 (módulo/perfil)
        Assert.Contains("FALHA_SERVIDOR", svc);     // 5xx
        Assert.Contains("TRANSPORTE", svc);         // rede/host fora
    }
}
