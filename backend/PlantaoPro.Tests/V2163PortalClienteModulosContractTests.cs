namespace PlantaoPro.Tests;

public sealed class V2163PortalClienteModulosContractTests
{
    private static string Read(string relative) => File.ReadAllText(Path.Combine(FindRoot(), relative));
    private static string FindRoot() { var path = AppContext.BaseDirectory; while (path is not null && !File.Exists(Path.Combine(path, "database", "install-manifest.json"))) path = Directory.GetParent(path)?.FullName; return path ?? throw new DirectoryNotFoundException(); }

    [Fact]
    public void Api_SeparaRevisaoConfirmacaoEAprovacaoGlobal()
    {
        var controller = Read("backend/PlantaoPro.Api/Controllers/ModuleContractingController.cs");
        Assert.Contains("api/portal-cliente/modulos", controller);
        Assert.Contains("ConditionsChangedException", controller);
        Assert.Contains("Authorize(Roles = RolesConstants.AdministradorGlobal)", controller);
        Assert.Contains("cancelar", controller);
    }

    [Fact]
    public void Servico_UsaSnapshotIdempotenciaETransacaoNaAtivacao()
    {
        var service = Read("backend/PlantaoPro.Api/ModuleContractingService.cs");
        Assert.Contains("SHA256.HashData", service);
        Assert.Contains("on conflict (tenant_id,chave_idempotencia)", service);
        Assert.Contains("for update", service);
        Assert.Contains("BeginTransactionAsync", service);
        Assert.Contains("preco_contratado", service);
        Assert.DoesNotContain("cadastro_cliente_pagamentos_iniciais", service);
    }

    [Fact]
    public void Banco_PreservaCondicoesEImpedeDuplicidades()
    {
        var sql = Read("database/schema/390_v2163_portal_cliente_modulos.sql");
        Assert.Contains("versao_condicoes", sql);
        Assert.Contains("ux_solicitacoes_modulos_idempotencia", sql);
        Assert.Contains("ux_solicitacao_modulo_item", sql);
        Assert.Contains("preco numeric(14,2) null", sql);
    }
}
