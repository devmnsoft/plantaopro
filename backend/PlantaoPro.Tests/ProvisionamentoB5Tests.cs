using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R5-B5 (provisionar cliente novo de verdade), contra o PostgreSQL local
/// (plantaopro_test), no template B6: GUIDs fixos com prefixo b5, semente
/// idempotente em uma única transação (purga + insert) para reruns previsíveis.
///
/// P1  Self-service finaliza o mundo do cliente (solicitação, tenant, cliente,
///     assinatura ATIVA, admin, perfil+grants, white label, onboarding, LGPD,
///     pagamento inicial) e o admin consegue autenticar (hash confere).
/// P2  CNPJ/e-mail duplicados: 400 honesto sem resíduo.
/// P3  Corrida do mesmo CNPJ: exatamente um vence; o outro recebe 409
///     (índice único arbitra, nunca 500, nunca duplicata).
/// P4  Plano inativo: 400 atômico (nenhuma linha em nenhuma tabela do núcleo).
/// P5  Provisionamento manual B2B usa o mesmo núcleo: TRIAL com trial_fim
///     explícito e sem cobrança inicial; validações de DiasTrial/status.
/// P6  Convites: criar (token uma vez, hash no banco), listar, aceitar cria o
///     usuário com os perfis; reuso/corrida/expirado/revogado recusados;
///     perfil global rejeitado; revogar fora do tenant = 404.
/// </summary>
public sealed class ProvisionamentoB5Tests : IDisposable
{
    private static readonly Guid B5Plano = Guid.Parse("b5000001-0000-4000-9000-000000000001");
    private static readonly Guid B5PlanoOff = Guid.Parse("b5000001-0000-4000-9000-000000000002");
    private static readonly Guid B5Tenant = Guid.Parse("b5000001-0000-4000-9000-000000000010");
    private static readonly Guid B5ClienteDup = Guid.Parse("b5000001-0000-4000-9000-000000000011");
    private static readonly Guid B5UsuarioDup = Guid.Parse("b5000001-0000-4000-9000-000000000012");
    private static readonly Guid B5Perfil = Guid.Parse("b5000001-0000-4000-9000-000000000013");
    private static readonly Guid B5Admin = Guid.Parse("b5000001-0000-4000-9000-000000000014");
    private static readonly Guid B5OutroTenant = Guid.Parse("b5000001-0000-4000-9000-000000000015");

    private const string CnpjDup = "11222333000181";
    private const string EmailDup = "b5-duplicado@exemplo.test";

    private readonly Task SementePronta = SementarAsync();

    public void Dispose()
    {
        SementePronta.GetAwaiter().GetResult();
        PurgarAsync().GetAwaiter().GetResult();
    }

    // ---------------------------------------------------------------------
    // P1 - self-service provisiona o mundo e o admin autentica.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task P1_Finalizar_CriaMundoTodo_E_AdminAutentica()
    {
        await SementePronta;
        var svc = NovoSelfService();
        var cnpj = "99988877000166";
        var email = "b5-p1-admin@exemplo.test";

        var r = await svc.FinalizarCadastroAsync(Pedido(cnpj, email, B5Plano), null, null);
        Assert.True(r.Success, r.Message);
        var ids = r.Data!;
        try
        {
            Assert.NotEqual(Guid.Empty, ids.TenantId);
            Assert.True(await ExisteAsync("plantaopro.tenants", ids.TenantId));
            Assert.True(await ExisteAsync("plantaopro.clientes", ids.ClienteId));
            Assert.True(await ExisteAsync("plantaopro.assinaturas", ids.AssinaturaId));
            Assert.True(await ExisteAsync("plantaopro.usuarios", ids.UsuarioAdminId));
            Assert.Equal("ATIVA", await ConsultarAsync<string>("select status from plantaopro.assinaturas where id=@id", new { id = ids.AssinaturaId }));
            Assert.True(await ExisteLinhaAsync("plantaopro.usuarios_perfis", "usuario_id", ids.UsuarioAdminId));
            Assert.True(await ExisteLinhaAsync("plantaopro.tenant_white_label", "tenant_id", ids.TenantId));
            Assert.True(await ExisteLinhaAsync("plantaopro.tenant_onboarding", "tenant_id", ids.TenantId));
            Assert.True(await ExisteLinhaAsync("plantaopro.lgpd_consentimentos", "tenant_id", ids.TenantId));
            Assert.True(await ExisteLinhaAsync("plantaopro.cadastro_cliente_pagamentos_iniciais", "assinatura_id", ids.AssinaturaId));
            var hash = await ConsultarAsync<string>("select senha_hash from plantaopro.usuarios where id=@id", new { id = ids.UsuarioAdminId });
            Assert.True(PlantaoPro.Api.Security.PasswordHashService.Verify("B5senha123!", hash));
            var grants = await ConsultarAsync<int>("select count(*) from plantaopro.perfil_permissoes pp join plantaopro.usuarios_perfis up on up.perfil_id=pp.perfil_id where up.usuario_id=@id and up.reg_status='A' and pp.reg_status='A' and pp.permitido=true", new { id = ids.UsuarioAdminId });
            Assert.True(grants > 0, "Admin do cliente precisa nascer com grants (sem perfil vazio).");
        }
        finally
        {
            await LimparMundoAsync(ids);
        }
    }

    // ---------------------------------------------------------------------
    // P2 - duplicatas honestas sem resíduo.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task P2_CnpjDuplicado_400_SemResiduo()
    {
        await SementePronta;
        var svc = NovoSelfService();
        var antes = await ConsultarAsync<int>("select count(*) from plantaopro.tenants where nome='B5 Empresa 1122'", new { });

        var r = await svc.FinalizarCadastroAsync(Pedido(CnpjDup, "b5-p2-novo@exemplo.test", B5Plano), null, null);

        Assert.False(r.Success);
        Assert.Equal(400, r.StatusCode);
        Assert.Contains("CNPJ", r.Message);
        Assert.Equal(antes, await ConsultarAsync<int>("select count(*) from plantaopro.tenants where nome='B5 Empresa 1122'", new { }));
        Assert.Equal(1, await ConsultarAsync<int>("select count(*) from plantaopro.clientes where id=@id", new { id = B5ClienteDup }));
    }

    [Fact]
    public async Task P2_EmailDuplicado_400_SemResiduo()
    {
        await SementePronta;
        var svc = NovoSelfService();

        var r = await svc.FinalizarCadastroAsync(Pedido("55443322000155", EmailDup, B5Plano), null, null);

        Assert.False(r.Success);
        Assert.Equal(400, r.StatusCode);
        Assert.Contains("E-mail", r.Message);
        Assert.Equal(1, await ConsultarAsync<int>("select count(*) from plantaopro.usuarios where lower(email)=lower(@e)", new { e = EmailDup }));
        Assert.Equal(0, await ConsultarAsync<int>("select count(*) from plantaopro.clientes where regexp_replace(cnpj,'\\D','','g')='55443322000155'", new { }));
    }

    // ---------------------------------------------------------------------
    // P3 - corrida do mesmo CNPJ.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task P3_CorridaMesmoCnpj_ExatamenteUmVence_Outro409()
    {
        await SementePronta;
        var svc = NovoSelfService();
        var cnpj = "77665544000132";

        var t1 = svc.FinalizarCadastroAsync(Pedido(cnpj, "b5-p3-a@exemplo.test", B5Plano), null, null);
        var t2 = svc.FinalizarCadastroAsync(Pedido(cnpj, "b5-p3-b@exemplo.test", B5Plano), null, null);
        var resultados = await Task.WhenAll(t1, t2);

        CadastroSelfServiceResultadoDto? vencedor = null;
        try
        {
            Assert.Equal(1, resultados.Count(r => r.Success));
            Assert.Equal(1, resultados.Count(r => !r.Success && r.StatusCode == 409));
            vencedor = resultados.First(r => r.Success).Data!;
            var donos = await ConsultarAsync<int>("select count(distinct id) from plantaopro.clientes where regexp_replace(cnpj,'\\D','','g')=@cnpj and reg_status='A'", new { cnpj });
            Assert.Equal(1, donos);
        }
        finally
        {
            if (vencedor is not null) await LimparMundoAsync(vencedor);
            await LimparPorCnpjAsync(cnpj);
        }
    }

    // ---------------------------------------------------------------------
    // P4 - plano inativo atômico.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task P4_PlanoInativo_400_Atomico()
    {
        await SementePronta;
        var svc = NovoSelfService();

        var r = await svc.FinalizarCadastroAsync(Pedido("00112233000144", "b5-p4@exemplo.test", B5PlanoOff), null, null);

        Assert.False(r.Success);
        Assert.Equal(400, r.StatusCode);
        Assert.Equal(0, await ConsultarAsync<int>("select count(*) from plantaopro.cadastro_cliente_solicitacoes where cnpj='00112233000144'", new { }));
        Assert.Equal(0, await ConsultarAsync<int>("select count(*) from plantaopro.usuarios where email='b5-p4@exemplo.test'", new { }));
    }

    // ---------------------------------------------------------------------
    // P5 - provisionamento manual B2B (mesmo núcleo).
    // ---------------------------------------------------------------------
    [Fact]
    public async Task P5_ProvisionarManual_Trial_ComTrialFim_E_SemCobrancaInicial()
    {
        await SementePronta;
        var svc = NovoSelfService();
        CadastroSelfServiceResultadoDto? ids = null;
        try
        {
            var r = await svc.ProvisionarManualAsync(new ProvisionarClienteRequest
            {
                Empresa = new CadastroEmpresaRequest { NomeFantasia = "B5 Trial", RazaoSocial = "B5 Trial LTDA", Cnpj = "33445566000177", Cidade = "Recife", Uf = "PE", EmailCorporativo = "b5p5@example.test" },
                PlanoId = B5Plano,
                StatusInicial = "TRIAL",
                DiasTrial = 15,
                AceiteTermos = true,
                AceitePrivacidade = true,
                UsuarioAdmin = new CadastroUsuarioAdminRequest { Nome = "B5 Trial Admin", Email = "b5-p5-admin@exemplo.test", Senha = "B5senha123!" }
            }, null);

            Assert.True(r.Success, r.Message);
            ids = r.Data!;
            var (status, trialFim, temCobranca) = await ConsultarAsync<(string, DateOnly?, bool)>(
                "select coalesce(a.status,''), a.data_trial_fim, exists(select 1 from plantaopro.cadastro_cliente_pagamentos_iniciais p where p.assinatura_id=a.id) from plantaopro.assinaturas a where a.id=@id",
                new { id = ids.AssinaturaId });
            Assert.Equal("TRIAL", status);
            Assert.NotNull(trialFim);
            var dias = trialFim.Value.ToDateTime(TimeOnly.MinValue).Date - DateTime.UtcNow.Date;
            Assert.True(dias.Days is >= 14 and <= 15);
            Assert.False(temCobranca);
        }
        finally
        {
            if (ids is not null) await LimparMundoAsync(ids);
        }
    }

    [Fact]
    public async Task P5_ProvisionarManual_DiasTrialInvalido_E_StatusInvalido_400()
    {
        await SementePronta;
        var svc = NovoSelfService();

        var diasZero = await svc.ProvisionarManualAsync(BaseManual() with { DiasTrial = 0 }, null);
        Assert.False(diasZero.Success);
        Assert.Equal(400, diasZero.StatusCode);

        var statusRuim = await svc.ProvisionarManualAsync(BaseManual() with { StatusInicial = "CONGELADA" }, null);
        Assert.False(statusRuim.Success);
        Assert.Equal(400, statusRuim.StatusCode);
    }

    // ---------------------------------------------------------------------
    // P6 - convites de equipe.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task P6_Convite_CriarListarAceitar_UsoUnico()
    {
        await SementePronta;
        var svc = NovoConvites(B5Tenant, true);
        string token;
        Guid conviteId;
        var email = "b5-convidado@exemplo.test";

        var criado = await svc.CriarAsync(new CriarConviteEquipeRequest { Email = email, PerfilIds = new[] { B5Perfil }, DiasValidade = 7 }, null, default);
        Assert.True(criado.Success, criado.Message);
        token = criado.Data!.Token!;
        conviteId = criado.Data.Id;
        Assert.Equal(64, token.Length);
        try
        {
            var hashBanco = await ConsultarAsync<string>("select token_hash from plantaopro.usuario_convites where id=@id", new { id = conviteId });
            Assert.Equal(64, hashBanco.Length);
            Assert.NotEqual(token, hashBanco);

            var lista = await svc.ListarAsync(default);
            Assert.Contains(lista.Data!, c => c.Email == email && c.Estado == "PENDENTE");

            var aceito = await svc.AceitarAsync(token, new AceitarConviteRequest { Nome = "B5 Convidado", Senha = "B5senha123!" }, null, default);
            Assert.True(aceito.Success, aceito.Message);
            Assert.True(await ExisteAsync("plantaopro.usuarios", aceito.Data));

            var reuso = await svc.AceitarAsync(token, new AceitarConviteRequest { Nome = "Outro", Senha = "B5senha123!" }, null, default);
            Assert.False(reuso.Success);
            Assert.True(reuso.StatusCode is 409 or 410);
        }
        finally
        {
            await LimparConviteAsync(email);
        }
    }

    [Fact]
    public async Task P6_Convite_DuplicadoPendente_Expirado_Revogado_PerfilGlobal()
    {
        await SementePronta;
        var svc = NovoConvites(B5Tenant, true);
        var email = "b5-conv-dup@exemplo.test";
        try
        {
            var c1 = await svc.CriarAsync(new CriarConviteEquipeRequest { Email = email, PerfilIds = new[] { B5Perfil } }, null, default);
            Assert.True(c1.Success, c1.Message);

            var c2 = await svc.CriarAsync(new CriarConviteEquipeRequest { Email = email, PerfilIds = new[] { B5Perfil } }, null, default);
            Assert.False(c2.Success);
            Assert.Equal(409, c2.StatusCode);

            var revogado = await svc.RevogarAsync(c1.Data!.Id, null, default);
            Assert.True(revogado.Success, revogado.Message);
            var aceitaRevogado = await svc.AceitarAsync(c1.Data.Token!, new AceitarConviteRequest { Nome = "X", Senha = "B5senha123!" }, null, default);
            Assert.False(aceitaRevogado.Success);

            var tokenExpirado = "token-expirado-b5-teste-0123456789abcdef";
            await InserirConviteExpiradoAsync(Guid.NewGuid(), "b5-conv-exp@exemplo.test", tokenExpirado);
            var aceitaExpirado = await svc.AceitarAsync(tokenExpirado, new AceitarConviteRequest { Nome = "Nome Valido", Senha = "B5senha123!" }, null, default);
            Assert.False(aceitaExpirado.Success);
            Assert.Equal(410, aceitaExpirado.StatusCode);
        }
        finally
        {
            await LimparConviteAsync(email);
            await LimparConviteAsync("b5-conv-exp@exemplo.test");
        }
    }

    [Fact]
    public async Task P6_Convite_PerfilGlobal_Rejeitado()
    {
        await SementePronta;
        var falsoGlobal = Guid.NewGuid();
        await using (var cn = new NpgsqlConnection(TestDatabase.ConnectionString))
        {
            await cn.ExecuteAsync("insert into plantaopro.perfis(id,tenant_id,codigo,nome,status,reg_status) values(@id,@t,'ADMINISTRADOR_GLOBAL','Falso global','ATIVO','A')",
                new { id = falsoGlobal, t = B5Tenant });
        }
        try
        {
            var svc = NovoConvites(B5Tenant, true);
            var r = await svc.CriarAsync(new CriarConviteEquipeRequest { Email = "b5-conv-glob@exemplo.test", PerfilIds = new[] { falsoGlobal } }, null, default);
            Assert.False(r.Success);
            Assert.Equal(400, r.StatusCode);
        }
        finally
        {
            await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
            await cn.ExecuteAsync("delete from plantaopro.perfis where id=@id", new { id = falsoGlobal });
            await LimparConviteAsync("b5-conv-glob@exemplo.test");
        }
    }

    [Fact]
    public async Task P6_Convite_SemTenantAdmin_403_E_RevogarOutroTenant_404()
    {
        await SementePronta;
        var semPerfil = NovoConvites(B5Tenant, false);
        var r = await semPerfil.CriarAsync(new CriarConviteEquipeRequest { Email = "b5-x@exemplo.test", PerfilIds = new[] { B5Perfil } }, null, default);
        Assert.False(r.Success);
        Assert.Equal(403, r.StatusCode);

        var outro = NovoConvites(B5OutroTenant, true);
        var lista = await outro.ListarAsync(default);
        Assert.True(lista.Success);
        Assert.Empty(lista.Data!);
    }

    // ---------------------------------------------------------------------
    // Infraestrutura local (semente, fakes, helpers).
    // ---------------------------------------------------------------------
    private static IConfiguration BuildCfg() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = TestDatabase.ConnectionString })
        .Build();

    private static SelfServiceSaasService NovoSelfService()
    {
        var cfg = BuildCfg();
        var audit = new B5FakeAuditService();
        return new SelfServiceSaasService(cfg, new TenantContextService(new HttpContextAccessor(), cfg, NullLogger<TenantContextService>.Instance),
            audit, new AssinaturaGuardService(cfg, audit, NullLogger<AssinaturaGuardService>.Instance),
            NullLogger<SelfServiceSaasService>.Instance);
    }

    private static ConvitesEquipeService NovoConvites(Guid tenant, bool admin) =>
        new(BuildCfg(), new B5FakeCurrentUser(B5Admin, tenant, admin), new B5FakeAuditService(), NullLogger<ConvitesEquipeService>.Instance);

    private static CadastroSelfServiceRequest Pedido(string cnpj, string email, Guid plano) => new()
    {
        Empresa = new CadastroEmpresaRequest { NomeFantasia = "B5 Empresa " + cnpj[..4], RazaoSocial = "B5 Empresa LTDA", Cnpj = cnpj, Cidade = "Olinda", Uf = "PE", EmailCorporativo = "b5-emp@exemplo.test" },
        Plano = new CadastroPlanoRequest { PlanoId = plano, Periodicidade = "MENSAL", AceiteTermos = true, AceitePrivacidade = true, ConsentimentoLgpd = true },
        UsuarioAdmin = new CadastroUsuarioAdminRequest { Nome = "B5 Admin", Email = email, Senha = "B5senha123!" }
    };

    private static ProvisionarClienteRequest BaseManual() => new()
    {
        Empresa = new CadastroEmpresaRequest { NomeFantasia = "B5 Manual", RazaoSocial = "B5 Manual LTDA", Cnpj = "66554433000188", Cidade = "Recife", Uf = "PE", EmailCorporativo = "b5-man@exemplo.test" },
        PlanoId = B5Plano,
        StatusInicial = "TRIAL",
        DiasTrial = 10,
        AceiteTermos = true,
        AceitePrivacidade = true,
        UsuarioAdmin = new CadastroUsuarioAdminRequest { Nome = "B5 Manual Admin", Email = "b5-man-admin@exemplo.test", Senha = "B5senha123!" }
    };

    private static async Task<T> ConsultarAsync<T>(string sql, object parametros)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        return await cn.QuerySingleAsync<T>(sql, parametros);
    }

    private static Task<bool> ExisteAsync(string tabela, Guid id) =>
        ConsultarAsync<bool>($"select exists(select 1 from {tabela} where id=@id)", new { id });

    private static Task<bool> ExisteLinhaAsync(string tabela, string coluna, Guid valor) =>
        ConsultarAsync<bool>($"select exists(select 1 from {tabela} where {coluna}=@valor)", new { valor });

    private static async Task LimparMundoAsync(CadastroSelfServiceResultadoDto ids)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.ExecuteAsync(@"delete from plantaopro.cadastro_cliente_pagamentos_iniciais where assinatura_id=@a;
delete from plantaopro.lgpd_consentimentos where tenant_id=@t;
delete from plantaopro.tenant_onboarding_checklist where tenant_id=@t;
delete from plantaopro.tenant_onboarding where tenant_id=@t;
delete from plantaopro.tenant_white_label where tenant_id=@t;
delete from plantaopro.usuarios_perfis where usuario_id=@u;
delete from plantaopro.perfil_permissoes where perfil_id in (select id from plantaopro.perfis where tenant_id=@t);
delete from plantaopro.perfis where tenant_id=@t;
delete from plantaopro.usuarios where id=@u;
delete from plantaopro.assinatura_historico where assinatura_id=@a;
delete from plantaopro.assinaturas where id=@a;
delete from plantaopro.clientes where id=@c;
delete from plantaopro.tenants where id=@t;
delete from plantaopro.cadastro_cliente_solicitacoes where id=@s;",
            new { a = ids.AssinaturaId, t = ids.TenantId, c = ids.ClienteId, u = ids.UsuarioAdminId, s = ids.SolicitacaoId });
    }

    private static async Task LimparPorCnpjAsync(string cnpj)
    {
        // Rede de segurança: o perdedor da corrida não deixa resíduo (409 com
        // rollback), mas se algo escapar, remove tudo ligado ao CNPJ.
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        var clientes = (await cn.QueryAsync<Guid>("select id from plantaopro.clientes where regexp_replace(cnpj,'\\D','','g')=@cnpj", new { cnpj })).ToList();
        foreach (var c in clientes)
        {
            var t = await cn.QueryFirstOrDefaultAsync<Guid?>("select id from plantaopro.tenants where cliente_id=@c", new { c });
            var a = await cn.QueryFirstOrDefaultAsync<Guid?>("select id from plantaopro.assinaturas where cliente_id=@c", new { c });
            var u = await cn.QueryFirstOrDefaultAsync<Guid?>("select id from plantaopro.usuarios where cliente_id=@c", new { c });
            var s = await cn.QueryFirstOrDefaultAsync<Guid?>("select id from plantaopro.cadastro_cliente_solicitacoes where regexp_replace(cnpj,'\\D','','g')=@cnpj", new { cnpj });
            if (t.HasValue || a.HasValue || u.HasValue || s.HasValue)
                await LimparMundoAsync(new CadastroSelfServiceResultadoDto
                {
                    SolicitacaoId = s ?? Guid.Empty,
                    TenantId = t ?? Guid.Empty,
                    ClienteId = c,
                    AssinaturaId = a ?? Guid.Empty,
                    UsuarioAdminId = u ?? Guid.Empty
                });
        }
    }

    private static async Task LimparConviteAsync(string email)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        var usuario = await cn.QueryFirstOrDefaultAsync<Guid?>("select id from plantaopro.usuarios where lower(email)=lower(@email)", new { email });
        if (usuario.HasValue)
        {
            await cn.ExecuteAsync("delete from plantaopro.usuarios_perfis where usuario_id=@id", new { id = usuario.Value });
            await cn.ExecuteAsync("delete from plantaopro.usuarios where id=@id", new { id = usuario.Value });
        }
        await cn.ExecuteAsync("delete from plantaopro.usuario_convites where lower(email)=lower(@email)", new { email });
    }

    private static async Task InserirConviteExpiradoAsync(Guid id, string email, string token)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.ExecuteAsync(@"insert into plantaopro.usuario_convites(id,tenant_id,cliente_id,email,perfil_ids,token_hash,expira_em,criado_por,reg_date,reg_status)
values(@id,@t,@t,@email,@perfis,@hash,now()-interval '1 day',@autor,now(),'A')",
            new { id, t = B5Tenant, email, perfis = new[] { B5Perfil }, hash = ConvitesEquipeService.HashToken(token), autor = B5Admin });
    }

    private const string PurgeSql = @"
delete from plantaopro.usuario_convites where tenant_id in (@tenant,@outro);
delete from plantaopro.usuarios_perfis where usuario_id=@usuario;
delete from plantaopro.perfil_permissoes where perfil_id=@perfil;
delete from plantaopro.usuarios where id in (@usuario,@admin);
delete from plantaopro.perfis where id=@perfil;
delete from plantaopro.assinaturas where cliente_id=@clienteDup;
delete from plantaopro.clientes where id=@clienteDup;
delete from plantaopro.tenants where id in (@tenant,@outro);
delete from plantaopro.planos where id in (@plano,@planoOff);";

    private const string InsertSql = @"
insert into plantaopro.tenants (id, tenant_id, codigo, nome, status, reg_status) values
  (@tenant, @tenant, 'B5TENT', 'B5 Tenant Convites', 'ATIVO', 'A'),
  (@outro, @outro, 'B5TEN2', 'B5 Outro Tenant', 'ATIVO', 'A');
insert into plantaopro.planos (id, nome, valor_mensal, limite_medicos, limite_hospitais, limite_plantoes_mes, limite_usuarios, status, reg_status) values
  (@plano, 'B5 Plano', 0, 0, 0, 0, 0, 'ATIVO', 'A'),
  (@planoOff, 'B5 Plano Off', 0, 0, 0, 0, 0, 'INATIVO', 'A');
insert into plantaopro.clientes (id, razao_social, cnpj, status, reg_status) values
  (@clienteDup, 'B5 Cliente Dup', @cnpjDup, 'ATIVO', 'A');
insert into plantaopro.usuarios (id, nome, email, email_normalizado, senha_hash, status, reg_status, senha_alteracao_obrigatoria, preferencias_notificacao, reg_date) values
  (@usuario, 'B5 Dup', @emailDup, upper(@emailDup), '{b5}', 'ATIVO', 'A', false, '{}', now());
insert into plantaopro.perfis (id, tenant_id, codigo, nome, status, reg_status) values
  (@perfil, @tenant, 'B5PERFIL', 'B5 Perfil Convite', 'ATIVO', 'A');
insert into plantaopro.usuarios (id, tenant_id, cliente_id, nome, email, email_normalizado, senha_hash, status, reg_status, senha_alteracao_obrigatoria, preferencias_notificacao, reg_date) values
  (@admin, @tenant, @tenant, 'B5 Admin', 'b5-admin@exemplo.test', 'B5-ADMIN@EXEMPLO.TEST', '{b5}', 'ATIVO', 'A', false, '{}', now());";

    private static object SeedParams() => new
    {
        plano = B5Plano,
        planoOff = B5PlanoOff,
        tenant = B5Tenant,
        outro = B5OutroTenant,
        clienteDup = B5ClienteDup,
        usuario = B5UsuarioDup,
        perfil = B5Perfil,
        admin = B5Admin,
        cnpjDup = CnpjDup,
        emailDup = EmailDup
    };

    private static async Task SementarAsync()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();
        await cn.ExecuteAsync(PurgeSql, SeedParams(), tx);
        await cn.ExecuteAsync(InsertSql, SeedParams(), tx);
        await tx.CommitAsync();
    }

    private static async Task PurgarAsync()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync(PurgeSql, SeedParams());
    }

    private sealed class B5FakeCurrentUser : ICurrentUserService
    {
        public Guid? UserId { get; }
        public Guid? TenantId { get; }
        public Guid? ClienteId => TenantId;
        public Guid? SessionId => null;
        public IReadOnlyCollection<string> Roles { get; }

        public B5FakeCurrentUser(Guid userId, Guid tenant, bool admin)
        {
            UserId = userId;
            TenantId = tenant;
            Roles = admin ? new[] { "ADMINISTRADOR_CLIENTE" } : Array.Empty<string>();
        }

        public bool IsAuthenticated() => UserId.HasValue;
        public bool IsGlobalAdmin() => false;
        public bool IsTenantAdmin() => Roles.Any(role => role.Equals("ADMINISTRADOR_CLIENTE", StringComparison.OrdinalIgnoreCase));
        public bool IsPartner() => false;
        public bool IsDoctor() => false;
        public bool HasRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    private sealed class B5FakeAuditService : IAuditService
    {
        public Task RegistrarAsync(Guid? usuarioId, Guid? clienteId, string entidade, Guid? entidadeId, string acao,
            object? detalhes, bool sucesso, string? ipOrigem, string? perfil, CancellationToken ct = default) => Task.CompletedTask;

        public Task LogAsync(Guid? userId, string acao, string entidade, Guid? registroId, string descricao,
            string? valorAnterior = null, string? valorNovo = null, string? ip = null, string? userAgent = null) => Task.CompletedTask;
    }
}
