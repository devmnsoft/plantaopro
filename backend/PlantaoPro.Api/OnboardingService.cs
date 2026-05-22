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

                    // 3. Criar Cliente
                    var clienteId = Guid.NewGuid();
                    await cn.ExecuteAsync(
                        @"INSERT INTO plantaopro.clientes
                          (id, razao_social, nome_fantasia, cnpj, email, telefone, 
                           cidade, estado, plano_id, status, reg_status, reg_date)
                          VALUES (@id, @RazaoSocial, @NomeFantasia, @Cnpj, @Email, @Telefone,
                                  @Cidade, @Estado, @PlanoId, @Status, 'A', NOW())",
                        new
                        {
                            id = clienteId,
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

                    logger.LogInformation("Cliente criado: {ClienteId}", clienteId);

                    // 4. Criar Assinatura
                    var assinaturaId = Guid.NewGuid();
                    var planoDetails = await cn.QueryFirstOrDefaultAsync<(decimal Valor,)>(
                        "SELECT valor_mensal FROM plantaopro.planos WHERE id = @id",
                        new { id = req.PlanoId },
                        transaction: tx);

                    var dataFim = req.Status == "TESTE"
                        ? DateTime.UtcNow.AddDays(30)
                        : DateTime.UtcNow.AddMonths(1);

                    await cn.ExecuteAsync(
                        @"INSERT INTO plantaopro.assinaturas
                          (id, cliente_id, plano_id, data_inicio, data_fim, status,
                           valor_contratado, dia_vencimento, observacoes, reg_status, reg_date)
                          VALUES (@id, @ClienteId, @PlanoId, NOW(), @DataFim, @Status,
                                  @ValorContratado, @DiaVencimento, @Observacoes, 'A', NOW())",
                        new
                        {
                            id = assinaturaId,
                            ClienteId = clienteId,
                            req.PlanoId,
                            DataFim = dataFim,
                            Status = "ATIVA",
                            ValorContratado = planoDetails.Valor,
                            DiaVencimento = DateTime.UtcNow.Day,
                            Observacoes = $"Assinatura criada via onboarding em {DateTime.UtcNow:dd/MM/yyyy HH:mm}"
                        },
                        transaction: tx);

                    logger.LogInformation("Assinatura criada: {AssinaturaId}", assinaturaId);

                    // 5. Criar Unidade Inicial
                    var unidadeId = Guid.NewGuid();
                    await cn.ExecuteAsync(
                        @"INSERT INTO plantaopro.unidades
                          (id, cliente_id, nome, tipo, cidade, estado, responsavel, 
                           status, reg_status, reg_date)
                          VALUES (@id, @ClienteId, @Nome, @Tipo, @Cidade, @Estado,
                                  @Responsavel, 'ATIVA', 'A', NOW())",
                        new
                        {
                            id = unidadeId,
                            ClienteId = clienteId,
                            Nome = req.UnidadeNome,
                            req.UnidadeTipo,
                            req.UnidadeCidade,
                            req.UnidadeEstado,
                            req.UnidadeResponsavel
                        },
                        transaction: tx);

                    logger.LogInformation("Unidade criada: {UnidadeId}", unidadeId);

                    // 6. Criar Usuário Admin
                    var usuarioId = Guid.NewGuid();
                    var senhaHash = BCrypt.Net.BCrypt.HashPassword(req.UsuarioSenha);

                    await cn.ExecuteAsync(
                        @"INSERT INTO plantaopro.usuarios
                          (id, nome, email, telefone, senha_hash, cliente_id,
                           status, reg_status, reg_date, criado_por)
                          VALUES (@id, @Nome, @Email, @Telefone, @SenhaHash, @ClienteId,
                                  'ATIVO', 'A', NOW(), @CriadoPor)",
                        new
                        {
                            id = usuarioId,
                            Nome = req.UsuarioNome,
                            req.UsuarioEmail,
                            req.UsuarioTelefone,
                            SenhaHash = senhaHash,
                            ClienteId = clienteId,
                            CriadoPor = usuarioAdminGlobalId
                        },
                        transaction: tx);

                    logger.LogInformation("Usuário criado: {UsuarioId}", usuarioId);

                    // 7. Atribuir Perfil ADMINISTRADOR
                    var perfilAdminId = await cn.QueryFirstOrDefaultAsync<Guid?>(
                        "SELECT id FROM plantaopro.perfis WHERE nome = 'ADMINISTRADOR' AND reg_status = 'A' LIMIT 1",
                        transaction: tx);

                    if (perfilAdminId.HasValue)
                    {
                        await cn.ExecuteAsync(
                            @"INSERT INTO plantaopro.usuarios_perfis
                              (id, usuario_id, perfil_id, reg_status, reg_date)
                              VALUES (gen_random_uuid(), @UsuarioId, @PerfilId, 'A', NOW())",
                            new
                            {
                                UsuarioId = usuarioId,
                                PerfilId = perfilAdminId.Value
                            },
                            transaction: tx);

                        logger.LogInformation("Perfil ADMINISTRADOR atribuído ao usuário");
                    }

                    // 8. Registrar Auditoria
                    await auditService.LogAsync(
                        usuarioAdminGlobalId,
                        "ONBOARDING_CLIENTE",
                        "clientes",
                        clienteId,
                        $"Cliente onboarded: {req.RazaoSocial}",
                        ip: ip,
                        userAgent: ua);

                    await tx.CommitAsync();

                    logger.LogInformation("Onboarding concluído com sucesso para cliente: {ClienteId}", clienteId);

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
