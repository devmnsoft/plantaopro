using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using PlantaoPro.Api;
using PlantaoPro.Api.Controllers;
using PlantaoPro.Api.Contracts.Notifications;
using PlantaoPro.Api.Models;
using PlantaoPro.Api.Operation360.Notifications;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R5-A3 (defeito visual #1): o badge de notificações do cabeçalho quebrava com 500
/// para sessões da área global (ADMINISTRADOR_GLOBAL sem tenant ativo) porque
/// OperationNotificationService é escopado por tenant e lançava
/// UnauthorizedAccessException na leitura. Correção pela causa no controller:
/// leitura honesta (lista vazia 200) sem consultar o serviço operacional.
/// O guard só vale para global SEM tenant — admin global com tenant ativo e
/// usuário de tenant continuam consultando a caixa normalmente.
/// </summary>
public sealed class NotificacoesA3GlobalAdminTests
{
    private static readonly Guid Tenant = Guid.Parse("a3a3a3a3-0000-4000-9000-0000000000f1");
    private static readonly Guid Usuario = Guid.Parse("a3a3a3a3-0000-4000-9000-0000000000f2");

    [Fact]
    public async Task Unread_GlobalAdminSemTenant_RetornaListaVazia_SemConsultarServico()
    {
        var servico = new ServicoDeContagem();
        var sut = Build(servico, globalAdmin: true, tenant: null);

        var resultado = await sut.Unread(CancellationToken.None);

        AssertVazioHonesto(resultado);
        Assert.Equal(0, servico.ListCalls);
    }

    [Fact]
    public async Task List_GlobalAdminSemTenant_RetornaListaVazia_SemConsultarServico()
    {
        var servico = new ServicoDeContagem();
        var sut = Build(servico, globalAdmin: true, tenant: null);

        var resultado = await sut.List(null, null, null, null, null, null, 100, CancellationToken.None);

        AssertVazioHonesto(resultado);
        Assert.Equal(0, servico.ListCalls);
    }

    [Fact]
    public async Task Unread_GlobalAdminComTenantAtivo_ConsultaCaixaOperacional()
    {
        var servico = new ServicoDeContagem();
        var sut = Build(servico, globalAdmin: true, tenant: Tenant);

        var resultado = await sut.Unread(CancellationToken.None);

        Assert.NotNull(resultado.Result);
        Assert.Equal(1, servico.ListCalls);
    }

    [Fact]
    public async Task Unread_UsuarioDeTenant_ConsultaCaixaOperacional()
    {
        var servico = new ServicoDeContagem();
        var sut = Build(servico, globalAdmin: false, tenant: Tenant);

        var resultado = await sut.Unread(CancellationToken.None);

        Assert.NotNull(resultado.Result);
        Assert.Equal(1, servico.ListCalls);
    }

    private static void AssertVazioHonesto(ActionResult<ApiResponse<IReadOnlyList<NotificationDto>>> resultado)
    {
        var ok = Assert.IsType<OkObjectResult>(resultado.Result);
        var envelope = Assert.IsAssignableFrom<ApiResponse<IReadOnlyList<NotificationDto>>>(ok.Value);
        Assert.True(envelope.Success);
        Assert.NotNull(envelope.Data);
        Assert.Empty(envelope.Data);
    }

    private static NotificacoesController Build(IOperationNotificationService servico, bool globalAdmin, Guid? tenant)
        => new(servico, new FalsoUsuarioAtual(globalAdmin, tenant), NullLogger<NotificacoesController>.Instance);

    private sealed class FalsoUsuarioAtual : ICurrentUserService
    {
        public Guid? UserId => Usuario;
        public Guid? TenantId { get; }
        public Guid? ClienteId => TenantId;
        public Guid? SessionId => null;
        public IReadOnlyCollection<string> Roles { get; }

        public FalsoUsuarioAtual(bool globalAdmin, Guid? tenant)
        {
            TenantId = tenant;
            Roles = globalAdmin ? new[] { "ADMINISTRADOR_GLOBAL" } : Array.Empty<string>();
        }

        public bool IsAuthenticated() => true;
        public bool IsGlobalAdmin() => Roles.Contains("ADMINISTRADOR_GLOBAL");
        public bool IsTenantAdmin() => false;
        public bool IsPartner() => false;
        public bool IsDoctor() => false;
        public bool HasRole(string role) => Roles.Contains(role);
    }

    /// <summary>Serviço que conta as leituras — prova que o guard não chega ao caixa operacional.</summary>
    private sealed class ServicoDeContagem : IOperationNotificationService
    {
        public int ListCalls;

        public Task<IReadOnlyList<NotificationDto>> ListAsync(NotificationFilter filter, CancellationToken ct)
        {
            ListCalls++;
            return Task.FromResult<IReadOnlyList<NotificationDto>>(Array.Empty<NotificationDto>());
        }

        public Task<NotificationReadResult?> ReadAsync(Guid id, CancellationToken ct) => throw new NotImplementedException();
        public Task<int> ReadAllAsync(CancellationToken ct) => throw new NotImplementedException();
        public Task<bool> SetStatusAsync(Guid id, string status, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<NotificationPreferenceDto>> PreferencesAsync(CancellationToken ct) => throw new NotImplementedException();
        public Task SavePreferencesAsync(NotificationPreferencesRequest request, CancellationToken ct) => throw new NotImplementedException();
    }
}
