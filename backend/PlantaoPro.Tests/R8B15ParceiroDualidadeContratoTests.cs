using System.Text.RegularExpressions;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R8-B15 — §29 item 7: "`parceiros` (B2B jsonb) × `adm360_parceiros` (master c/ flags) —
/// propósitos distintos, sobreposição semântica."
///
/// RESULTADO DA VALIDAÇÃO (fonte canônica, sem regra inventada):
///   As duas tabelas são CONCEITOS DIFERENTES e NÃO se sobrepõem operacionalmente:
///
///   • `adm360_parceiros` = MESTRE OPERACIONAL do administrativo-360. Parceiro POR PAPÉIS
///     (flags `fornecedor`, `eh_hospital`, `eh_pagador`, `eh_cliente`), scoping por tenant,
///     referenciado por FK em pedidos/orçamentos/cotações/contas a pagar e receber/vendas/etc.,
///     com CRUD em <c>CadastrosRepository</c> + view <c>Parceiros.cshtml</c>. É o único parceiro
///     que o código operacional lê/escreve.
///
///   • `parceiros` + `parceiro_tenants` = schema de lançamento B2B/white-label
///     (<c>2026_plantao_pro_white_label_b2b_launch.sql</c>): parceiro SaaS/assinatura com
///     <c>plano_id</c>, <c>parceiro_id</c>, <c>metadata jsonb</c> livre, <c>tenant_id</c> NULO.
///     É DDL FORWARD-SCAFFOLDED: ZERO consumidores em código de produção (nenhum
///     <c>FROM/INTO/JOIN/UPDATE … parceiros</c>). O único "Portal do parceiro" em código
///     (<c>ParceiroPortal*</c>) é DEMO comercial com dados em memória (sem banco).
///
///   CONCLUSÃO: a "sobreposição" é apenas SEMÂNTICA (o nome "parceiro"); não há duplo-mestre,
///   duplo-write nem leitura conflitante. Nada a migrar/mergear, e — pelo rito (§29) — SEM
///   deleção antecipada da schema B2B (forward-deployed, pode ser implementada depois). Esta fatia
///   ENTREGUE a validação + um CONTRATO anti-conflusão que fixa: (1) `adm360_parceiros` é o único
///   mestre operacional amplamente usado; (2) a tabela B2B `parceiros` não é consumida por código
///   de produção; (3) as duas modelam papéis estruturalmente distintos nos DDL canônicos.
/// </summary>
public sealed class R8B15ParceiroDualidadeContratoTests
{
    // Tabela B2B "parceiros" (e companion) em contexto SQL — não casa com "adm360_parceiros"
    // nem com nomes de índice (o \b exige fronteira de palavra; "_" é caractere de palavra).
    private static readonly Regex RxParceiros = new(
        @"(?:FROM|INTO|JOIN|UPDATE)\s+(?:plantaopro\.)?parceiros\b", RegexOptions.IgnoreCase);
    private static readonly Regex RxParceiroTenants = new(
        @"(?:FROM|INTO|JOIN|UPDATE)\s+(?:plantaopro\.)?parceiro_tenants\b", RegexOptions.IgnoreCase);

    private static string Infra(string file) =>
        Path.Combine(RepositoryPathResolver.BackendRoot, "PlantaoPro.Infrastructure", "Administrativo360", file);

    private static string Mig(params string[] parts) =>
        File.ReadAllText(RepositoryPathResolver.DatabaseFile(parts));

    private static IEnumerable<string> ProducaoCs() =>
        RepositoryPathResolver
            .EnumerateRepositoryFiles(new[] { RepositoryPathResolver.BackendRoot }, "*.cs")
            .Where(p => p.Replace('\\', '/').IndexOf("/PlantaoPro.Tests/", StringComparison.OrdinalIgnoreCase) < 0);

    [Fact]
    public void MestreOperacional_AdM360Parceiros_EhUsadoPElcosOperacionais()
    {
        // O CRUD do parceiro operacional vive em CadastrosRepository (SELECT/INSERT/UPDATE).
        var cadastros = File.ReadAllText(Infra("CadastrosRepository.cs"));
        Assert.Contains("FROM plantaopro.adm360_parceiros", cadastros);
        Assert.Contains("INSERT INTO plantaopro.adm360_parceiros", cadastros);
        Assert.Contains("UPDATE plantaopro.adm360_parceiros", cadastros);

        // E é o alvo do domínio administrativo-360: muitos repositórios operacionais o JOINem/leem.
        var usos = ProducaoCs().Where(p => File.ReadAllText(p).Contains("plantaopro.adm360_parceiros")).ToArray();
        Assert.True(usos.Length >= 8,
            $"adm360_parceiros deveria ser o mestre operacional amplamente usado (encontrado em {usos.Length} arquivo(s) de produção).");
    }

    [Fact]
    public void TabelasB2BLaunch_NaoSaoConsumidasPorCodigoDeProducao()
    {
        // A schema B2B de lançamento segue dormant: nenhum repositório/serviço/controller a toca via SQL.
        var consumidores = ProducaoCs()
            .Where(p => { var t = File.ReadAllText(p); return RxParceiros.IsMatch(t) || RxParceiroTenants.IsMatch(t); })
            .Select(c => Path.GetFileName(c))
            .ToArray();

        Assert.True(consumidores.Length == 0,
            "Código de produção passou a consumir a tabela B2B 'parceiros'/'parceiro_tenants' — decisão pendente (não confundir com adm360_parceiros): "
            + string.Join(", ", consumidores));
    }

    [Fact]
    public void AsDuasTabelas_ModelamPapeisDiferentes_NosDdlCanonicos()
    {
        var v2191 = Mig("migrations", "2026_09_v2191_administrativo360_suprimentos.sql");
        var v2198 = Mig("migrations", "2026_09_v2198_reconciliar_duplicidades_contratos_modulos.sql");
        var b2b = Mig("migrations", "2026_plantao_pro_white_label_b2b_launch.sql");

        // adm360_parceiros: parceiro POR PAPEL OPERACIONAL (flags), scoping por tenant.
        Assert.Contains("fornecedor boolean", v2191);
        Assert.Contains("eh_hospital", v2198);
        Assert.Contains("eh_pagador", v2198);
        Assert.Contains("eh_cliente", v2198);

        // parceiros (B2B): parceiro SaaS/ASSINATURA (plano + metadados livres), não papéis operacionais.
        Assert.Contains("plano_id", b2b);
        Assert.Contains("parceiro_id", b2b);
        Assert.Contains("metadata jsonb", b2b);
    }
}
