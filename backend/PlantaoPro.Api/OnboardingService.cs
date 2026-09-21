using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace PlantaoPro.Api.Data
{
    // ============================================================================
    // SERVIÇO DE ONBOARDING SAAS
    // ============================================================================
    public sealed class OnboardingService
    {
        private readonly IConfiguration cfg;
        private readonly IAuditService auditService;
        private readonly ILogger<OnboardingService> logger;

        public OnboardingService(IConfiguration cfg, IAuditService auditService, ILogger<OnboardingService> logger)
        {
            this.cfg = cfg;
            this.auditService = auditService;
            this.logger = logger;
        }

        /// <summary>
        /// Cria cliente completo com assinatura, unidade e usuário em transação
        /// </summary>
        public async Task<ApiResponse<OnboardingResumoDto>> CriarClienteCompletoAsync(
            CreateClienteOnboardingRequest req,
            Guid usuarioAdminGlobalId,
            string? ip,
            string? ua)
        {
            logger.LogInformation("Iniciando onboarding de cliente: {RazaoSocial}", req.RazaoSocial);

            try
            {
                await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
                await cn.OpenAsync();

                using var tx = await cn.BeginTransactionAsync();

                try
                {
                    // 1. Validar CNPJ duplicado
                    var cnpjDuplicado = await cn.QueryFirstOrDefaultAsync<int>(
                        "SELECT COUNT(1) FROM plantaopro.clientes WHERE LOWER(cnpj) = LOWER(@cnpj) AND reg_status = 'A'",
                        new { cnpj = req.Cnpj },
                        transaction: tx);

                    if (cnpjDuplicado > 0)
                    {
                        logger.LogWarning("CNPJ duplicado: {Cnpj}", req.Cnpj);
                        return ApiResponse<OnboardingResumoDto>.Fail("CNPJ já cadastrado no sistema.", 400);
                    }

                    // 2. Validar e-mail do usuário duplicado
                    var emailDuplicado = await cn.QueryFirstOrDefaultAsync<int>(
                        "SELECT COUNT(1) FROM plantaopro.usuarios WHERE LOWER(email) = LOWER(@email) AND reg_status = 'A'",
                        new { email = req.UsuarioEmail },
                        transaction: tx);

                    if (emailDuplicado > 0)
                    {
                        logger.LogWarning("E-mail de usuário duplicado: {Email}", req.UsuarioEmail);
                        return ApiResponse<OnboardingResumoDto>.Fail("E-mail de usuário já cadastrado.", 400);
                    }

                    // 3. Criar Tenant e Cliente
                    var tenantId = Guid.NewGuid();
                    var clienteId = Guid.NewGuid();
                    var slug = System.Text.RegularExpressions.Regex.Replace(
                        (string.IsNullOrWhiteSpace(req.NomeFantasia) ? req.RazaoSocial : req.NomeFantasia).ToLowerInvariant(),
                        "[^a-z0-9]+",
                        "-").Trim('-');
                    if (string.IsNullOrWhiteSpace(slug)) slug = "tenant-" + tenantId.ToString("N")[..8];

                    await cn.ExecuteAsync(
                        @"INSERT INTO plantaopro.tenants
                          (id, tenant_id, codigo, nome, status, dados, criado_em)
                          VALUES (@tenantId, @tenantId, @slug, @Nome, 'ATIVO', '{}'::jsonb, NOW())
                          ON CONFLICT (id) DO NOTHING",
                        new
                        {
                            tenantId,
                            slug,
                            Nome = string.IsNullOrWhiteSpace(req.NomeFantasia) ? req.RazaoSocial : req.NomeFantasia
                        },
                        transaction: tx);

                    await cn.ExecuteAsync(
                        @"INSERT INTO plantaopro.clientes
                          (id, tenant_id, codigo, razao_social, nome_fantasia, cnpj, email, telefone, 
                           cidade, estado, plano_id, status, reg_status, reg_date)
                          VALUES (@id, @tenantId, @codigo, @RazaoSocial, @NomeFantasia, @Cnpj, @Email, @Telefone,
                                  @Cidade, @Estado, @PlanoId, @Status, 'A', NOW())",
                        new
                        {
                            id = clienteId,
                            tenantId,
                            codigo = slug,
                            req.RazaoSocial,
                            req.NomeFantasia,
                            req.Cnpj,
                            req.Email,
                            req.Telefone,
                            req.Cidade,
                            req.Estado,
                            req.PlanoId,
                            req.Status
                        },
                        transaction: tx);

                    logger.LogInformation("Cliente {ClienteId} e Tenant {TenantId} criados", clienteId, tenantId);

                    // 4. Criar Assinatura
                    var assinaturaId = Guid.NewGuid();
                    var planoInfo = await cn.QueryFirstOrDefaultAsync<(decimal ValorMensal, string Nome)>(
                        "SELECT valor_mensal as ValorMensal, coalesce(nome,'Plano Contratado') as Nome FROM plantaopro.planos WHERE id = @id AND reg_status = 'A'",
                        new { id = req.PlanoId },
                        transaction: tx);

                    if (planoInfo.ValorMensal == 0 && planoInfo.Nome is null)
                    {
                        logger.LogWarning("Plano não encontrado ou inativo: {PlanoId}", req.PlanoId);
                        return ApiResponse<OnboardingResumoDto>.Fail("Plano não encontrado ou inativo.", 404);
                    }

                    var dataFim = req.Status == "TESTE"
                        ? DateTime.UtcNow.AddDays(30)
                        : DateTime.UtcNow.AddMonths(1);

                    await cn.ExecuteAsync(
                        @"INSERT INTO plantaopro.assinaturas
                          (id, tenant_id, cliente_id, plano_id, data_inicio, data_fim, status,
                           valor_contratado, dia_vencimento, observacoes, reg_status, reg_date)
                          VALUES (@id, @tenantId, @ClienteId, @PlanoId, NOW(), @DataFim, @Status,
                                  @ValorContratado, @DiaVencimento, @Observacoes, 'A', NOW())",
                        new
                        {
                            id = assinaturaId,
                            tenantId,
                            ClienteId = clienteId,
                            req.PlanoId,
                            DataFim = dataFim,
                            Status = "ATIVA",
                            ValorContratado = planoInfo.ValorMensal,
                            DiaVencimento = DateTime.UtcNow.Day,
                            Observacoes = $"Assinatura criada via onboarding em {DateTime.UtcNow:dd/MM/yyyy HH:mm}"
                        },
                        transaction: tx);

                    logger.LogInformation("Assinatura criada: {AssinaturaId}", assinaturaId);

                    // 5. Contratação explícita dos módulos ESCALAS, EXECUCAO, CONFERENCIA
                    var modulosIniciais = new (string Code, string Name)[]
                    {
                        ("ESCALAS", "Gestão de Escalas"),
                        ("EXECUCAO", "Acompanhamento da Execução"),
                        ("CONFERENCIA", "Conferência Operacional e Turnos")
                    };

                    foreach (var (code, name) in modulosIniciais)
                    {
                        await cn.ExecuteAsync(
                            @"INSERT INTO plantaopro.tenant_modulos
                              (id, tenant_id, cliente_id, codigo, codigo_modulo, nome, habilitado, status, reg_status, reg_date)
                              VALUES (gen_random_uuid(), @tenantId, @clienteId, @code, @code, @name, true, 'ATIVO', 'A', NOW())",
                            new { tenantId, clienteId, code, name },
                            transaction: tx);
                    }

                    logger.LogInformation("Módulos operacionais contratados para o tenant {TenantId}", tenantId);

                    // 6. Criar Unidade e Hospital correspondente para operações
                    var unidadeId = Guid.NewGuid();
                    var unidadeNome = string.IsNullOrWhiteSpace(req.UnidadeNome) ? "Unidade Principal" : req.UnidadeNome;
                    await cn.ExecuteAsync(
                        @"INSERT INTO plantaopro.unidades
                          (id, tenant_id, cliente_id, nome, tipo, cidade, estado, responsavel, 
                           status, reg_status, reg_date)
                          VALUES (@id, @tenantId, @ClienteId, @Nome, @Tipo, @Cidade, @Estado,
                                  @Responsavel, 'ATIVA', 'A', NOW())",
                        new
                        {
                            id = unidadeId,
                            tenantId,
                            ClienteId = clienteId,
                            Nome = unidadeNome,
                            req.UnidadeTipo,
                            req.UnidadeCidade,
                            req.UnidadeEstado,
                            req.UnidadeResponsavel
                        },
                        transaction: tx);

                    await cn.ExecuteAsync(
                        @"INSERT INTO plantaopro.hospitais
                          (id, tenant_id, cliente_id, nome, nome_fantasia, razao_social, cnpj, cidade, estado, status, reg_status, reg_date)
                          VALUES (@id, @tenantId, @ClienteId, @Nome, @Nome, @RazaoSocial, @Cnpj, @Cidade, @Estado, 'ATIVO', 'A', NOW())
                          ON CONFLICT (id) DO NOTHING",
                        new
                        {
                            id = unidadeId,
                            tenantId,
                            ClienteId = clienteId,
                            Nome = unidadeNome,
                            req.RazaoSocial,
                            req.Cnpj,
                            Cidade = req.UnidadeCidade ?? req.Cidade,
                            Estado = req.UnidadeEstado ?? req.Estado
                        },
                        transaction: tx);

                    logger.LogInformation("Unidade e Hospital criados: {UnidadeId}", unidadeId);

                    // 7. Criar Usuário Admin do Cliente
                    var usuarioId = Guid.NewGuid();
                    var senhaHash = BCrypt.Net.BCrypt.HashPassword(req.UsuarioSenha);

                    await cn.ExecuteAsync(
                        @"INSERT INTO plantaopro.usuarios
                          (id, tenant_id, cliente_id, nome, email, email_normalizado, telefone, senha_hash,
                           status, reg_status, reg_date, criado_por)
                          VALUES (@id, @tenantId, @ClienteId, @Nome, @Email, LOWER(@Email), @Telefone, @SenhaHash,
                                  'ATIVO', 'A', NOW(), @CriadoPor)",
                        new
                        {
                            id = usuarioId,
                            tenantId,
                            ClienteId = clienteId,
                            Nome = req.UsuarioNome,
                            req.UsuarioEmail,
                            req.UsuarioTelefone,
                            SenhaHash = senhaHash,
                            CriadoPor = usuarioAdminGlobalId
                        },
                        transaction: tx);

                    logger.LogInformation("Usuário admin do cliente criado: {UsuarioId}", usuarioId);

                    // 8. Atribuir Perfil Canônico ADMINISTRADOR_CLIENTE e ADMINISTRADOR
                    var perfisDesejados = new[] { "ADMINISTRADOR_CLIENTE", "ADMINISTRADOR" };
                    foreach (var perfilCodigo in perfisDesejados)
                    {
                        var perfilId = await cn.QueryFirstOrDefaultAsync<Guid?>(
                            @"SELECT id FROM plantaopro.perfis 
                              WHERE (codigo = @codigo OR nome = @codigo) AND reg_status = 'A'
                              ORDER BY case when tenant_id = @tenantId then 1 when tenant_id is null then 2 else 3 end
                              LIMIT 1",
                            new { codigo = perfilCodigo, tenantId },
                            transaction: tx);

                        if (!perfilId.HasValue)
                        {
                            perfilId = Guid.NewGuid();
                            await cn.ExecuteAsync(
                                @"INSERT INTO plantaopro.perfis
                                  (id, tenant_id, cliente_id, codigo, nome, descricao, base_sistema, status, reg_status, reg_date)
                                  VALUES (@id, @tenantId, @clienteId, @codigo, @codigo, 'Perfil do cliente', true, 'ATIVO', 'A', NOW())",
                                new { id = perfilId.Value, tenantId, clienteId, codigo = perfilCodigo },
                                transaction: tx);
                        }

                        await cn.ExecuteAsync(
                            @"INSERT INTO plantaopro.usuarios_perfis
                              (id, tenant_id, cliente_id, usuario_id, perfil_id, reg_status, reg_date)
                              VALUES (gen_random_uuid(), @tenantId, @clienteId, @UsuarioId, @PerfilId, 'A', NOW())
                              ON CONFLICT DO NOTHING",
                            new
                            {
                                tenantId,
                                clienteId,
                                UsuarioId = usuarioId,
                                PerfilId = perfilId.Value
                            },
                            transaction: tx);
                    }

                    logger.LogInformation("Perfis ADMINISTRADOR_CLIENTE e ADMINISTRADOR atribuídos ao usuário {UsuarioId}", usuarioId);

                    // 9. Registrar Auditoria
                    await auditService.LogAsync(
                        usuarioAdminGlobalId,
                        "ONBOARDING_CLIENTE",
                        "clientes",
                        clienteId,
                        $"Cliente onboarded com sucesso: {req.RazaoSocial} (Tenant: {tenantId})",
                        ip: ip,
                        userAgent: ua);

                    await tx.CommitAsync();

                    logger.LogInformation("Onboarding concluído com sucesso para cliente: {ClienteId}, tenant: {TenantId}", clienteId, tenantId);

                    var resumo = new OnboardingResumoDto(
                        ClienteId: clienteId,
                        ClienteNome: req.NomeFantasia,
                        PlanoId: req.PlanoId,
                        PlanoNome: "",
                        UnidadeId: unidadeId,
                        UnidadeNome: req.UnidadeNome,
                        UsuarioId: usuarioId,
                        UsuarioNome: req.UsuarioNome,
                        UsuarioEmail: req.UsuarioEmail,
                        AssinaturaId: assinaturaId,
                        AssinaturaStatus: "ATIVA",
                        DataCriacaoAssinatura: DateTime.UtcNow
                    );

                    return ApiResponse<OnboardingResumoDto>.Ok(resumo, "Cliente criado com sucesso!");
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    logger.LogError(ex, "Erro ao criar cliente - transação revertida");
                    throw;
                }
            }
            catch (NpgsqlException ex)
            {
                logger.LogError(ex, "Erro PostgreSQL no onboarding");
                return ApiResponse<OnboardingResumoDto>.Fail("Erro ao acessar banco de dados.", 500);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro inesperado no onboarding");
                return ApiResponse<OnboardingResumoDto>.Fail("Erro ao criar cliente.", 500);
            }
        }

        /// <summary>
        /// Obtém resumo de um cliente criado
        /// </summary>
        public async Task<ApiResponse<OnboardingResumoDto>> GetResumoAsync(Guid clienteId)
        {
            try
            {
                await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));

                var cliente = await cn.QueryFirstOrDefaultAsync<(Guid Id, string NomeFantasia, Guid? PlanoId)>(
                    "SELECT id, nome_fantasia, plano_id FROM plantaopro.clientes WHERE id = @id AND reg_status = 'A'",
                    new { id = clienteId });

                if (cliente.Id == Guid.Empty)
                    return ApiResponse<OnboardingResumoDto>.Fail("Cliente não encontrado.", 404);

                var assinatura = await cn.QueryFirstOrDefaultAsync<(Guid Id, string Status, DateTime DataInicio)>(
                    "SELECT id, status, data_inicio FROM plantaopro.assinaturas WHERE cliente_id = @clienteId AND reg_status = 'A' LIMIT 1",
                    new { clienteId });

                var unidade = await cn.QueryFirstOrDefaultAsync<(Guid Id, string Nome)>(
                    "SELECT id, nome FROM plantaopro.unidades WHERE cliente_id = @clienteId AND reg_status = 'A' LIMIT 1",
                    new { clienteId });

                var usuario = await cn.QueryFirstOrDefaultAsync<(Guid Id, string Nome, string Email)>(
                    "SELECT id, nome, email FROM plantaopro.usuarios WHERE cliente_id = @clienteId AND reg_status = 'A' LIMIT 1",
                    new { clienteId });

                var plano = await cn.QueryFirstOrDefaultAsync<string>(
                    "SELECT nome FROM plantaopro.planos WHERE id = @id LIMIT 1",
                    new { id = cliente.PlanoId });

                var resumo = new OnboardingResumoDto(
                    ClienteId: clienteId,
                    ClienteNome: cliente.NomeFantasia,
                    PlanoId: cliente.PlanoId ?? Guid.Empty,
                    PlanoNome: plano ?? "",
                    UnidadeId: unidade.Id,
                    UnidadeNome: unidade.Nome,
                    UsuarioId: usuario.Id,
                    UsuarioNome: usuario.Nome,
                    UsuarioEmail: usuario.Email,
                    AssinaturaId: assinatura.Id,
                    AssinaturaStatus: assinatura.Status,
                    DataCriacaoAssinatura: assinatura.DataInicio
                );

                return ApiResponse<OnboardingResumoDto>.Ok(resumo);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao obter resumo do onboarding");
                return ApiResponse<OnboardingResumoDto>.Fail("Erro ao obter dados do cliente.", 500);
            }
        }
    }
}
