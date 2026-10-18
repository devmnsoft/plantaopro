using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R48 (diretriz r24a1, epic E9 — alertas de inadimplência): quando a fatura do cliente vence
/// (por sweep preguiçoso ou transição explícita), o admin do cliente (ADMINISTRADOR_CLIENTE)
/// recebe uma notificação em linguagem simples, com dedupe por fatura via convenção R8-B2; o
/// lado MNSOFT passa a ser avisado também pelo sweep (que antes virava VENCIDA em silêncio) e
/// existe dono único dos efeitos colaterais do vencimento. Plano:
/// docs/evidencias/2026-10-18-rodada-48/r48a0-e9-alertas-inadimplencia-plano.md.
/// </summary>
public sealed class R48AlertasInadimplenciaContratoTests
{
    private static string Ler(string relativo) => System.IO.File.ReadAllText(System.IO.Path.Combine(RepositoryPathResolver.RepoRoot, relativo), System.Text.Encoding.UTF8);

    private static int Contar(string texto, string alvo)
    {
        var n = 0;
        var i = texto.IndexOf(alvo, StringComparison.Ordinal);
        while (i >= 0) { n++; i = texto.IndexOf(alvo, i + 1, StringComparison.Ordinal); }
        return n;
    }

    [Fact]
    public void Api_SweepDeVencidas_IdentificaAntesEDelegaAoDonoUnico()
    {
        var apiController = Ler("backend/PlantaoPro.Api/Controllers/SaasCommercialController.cs");
        var inicio = apiController.IndexOf("private async Task AtualizarVencidasAsync", StringComparison.Ordinal);
        Assert.True(inicio >= 0, "sweep AtualizarVencidasAsync ausente");
        var regiao = apiController.Substring(inicio, 1200);
        // Ponto cego fechado: as faturas que vão p/ VENCIDA são identificadas ANTES do flip
        // e os efeitos colaterais passam pelo dono único dentro de transação.
        Assert.Contains("select id as Id, cliente_id as ClienteId from ", regiao);
        Assert.Contains("where status='ABERTA' and vencimento < current_date and reg_status='A'", regiao);
        Assert.Contains("await cn.BeginTransactionAsync()", regiao);
        Assert.Contains("foreach (var f in vencendo) await RegistrarVencimentoFaturaAsync(cn, tx, f.Id, f.ClienteId);", regiao);
    }

    [Fact]
    public void Api_TransicaoExplicitaParaVencida_DelegaAoDonoUnico()
    {
        var apiController = Ler("backend/PlantaoPro.Api/Controllers/SaasCommercialController.cs");
        var inicio = apiController.IndexOf("else if (string.Equals(status, \"VENCIDA\"", StringComparison.Ordinal);
        Assert.True(inicio >= 0, "transição explícita para VENCIDA ausente");
        var regiao = apiController.Substring(inicio, Math.Min(250, apiController.Length - inicio));
        // A branch delega ao dono único — não registra evento/alerta por conta própria.
        Assert.Contains("await RegistrarVencimentoFaturaAsync(cn, tx, id, fatura.ClienteId);", regiao);
        Assert.DoesNotContain("RegistrarEventoCobrancaAsync", regiao);

        // Dono único: exatamente UM lugar no arquivo registra o evento FATURA_VENCIDA
        // (o helper). A menção em comentário de regra é tolerada, mas a chamada efetiva
        // deve existir exatamente uma vez.
        Assert.Equal(1, Contar(apiController, "faturaId, \"FATURA_VENCIDA\""));
    }

    [Fact]
    public void NotificacaoAoCliente_AlvoEstritoLinguagemSimplesEDedupePorFatura()
    {
        var apiController = Ler("backend/PlantaoPro.Api/Controllers/SaasCommercialController.cs");
        var inicio = apiController.IndexOf("private async Task RegistrarVencimentoFaturaAsync", StringComparison.Ordinal);
        Assert.True(inicio >= 0, "dono único RegistrarVencimentoFaturaAsync ausente");
        var regiao = apiController.Substring(inicio, 1800);

        // Alvo estrito (D-R48-4): usuários ATIVOS do cliente com o perfil de admin da conta —
        // pattern de casa (usuarios x usuarios_perfis x perfis), sem fallback para qualquer usuário.
        Assert.Contains("join ", regiao);
        Assert.Contains("pf.codigo='ADMINISTRADOR_CLIENTE'", regiao);
        Assert.Contains("u.cliente_id=@clienteId", regiao);

        // Linguagem simples (D-R48-3): título/mensagem humanos, valor e data formatados pt-BR.
        Assert.Contains("\"Fatura vencida\"", regiao);
        Assert.Contains("Sua fatura de R$ {fatura.Valor.Value.ToString(\"N2\", ptBr)} venceu em {fatura.Vencimento.Value.ToString(\"dd/MM/yyyy\")}. Para regularizar, acesse Minha Assinatura.", regiao);

        // Dedupe por fatura (convenção R8-B2): origem declarada fatura_saas + id da fatura,
        // tipo de família financeiro.
        Assert.Contains("_notificacao.CriarNotificacaoAsync(uid, \"Fatura vencida\", mensagem, \"financeiro\", tx, \"fatura_saas\", faturaId)", regiao);

        // O alerta do lado MNSOFT continua registrado (INADIMPLENCIA/ALTA) — agora também pelo sweep.
        Assert.Contains("\"INADIMPLENCIA\"", regiao);
        Assert.Contains("\"ALTA\"", regiao);
    }
}
