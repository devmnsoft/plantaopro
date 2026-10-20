using System.Security.Cryptography;
using Dapper;
using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

public sealed class AnexosArquivosRepository : Adm360Repository, IAnexosArquivosRepository
{
    public const int TamanhoMaximoBytes = 8 * 1024 * 1024; // 8 MiB por arquivo

    public static readonly IReadOnlySet<string> ExtensoesPermitidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { "pdf", "doc", "docx", "jpg", "jpeg", "png", "webp", "gif", "xml" };

    public static readonly IReadOnlySet<string> CategoriasPermitidas = new HashSet<string>(StringComparer.Ordinal)
    { "FUNCIONARIO", "EMPRESA", "NOTA_FISCAL", "OUTRO" };

    public AnexosArquivosRepository(string connectionString) : base(connectionString) { }

    /// <summary>Valida nome/tamanho antes de ler o conteudo. Retorna a mensagem de erro ou null quando valido.</summary>
    public static string? ValidarArquivo(string? nomeArquivo, long tamanho)
    {
        if (string.IsNullOrWhiteSpace(nomeArquivo)) return "Selecione um arquivo para enviar.";
        var ext = Path.GetExtension(nomeArquivo ?? "").TrimStart('.').ToLowerInvariant();
        if (!ExtensoesPermitidas.Contains(ext))
            return "Formato nao aceito. Envie PDF, Word (.doc/.docx) ou imagem (JPG/PNG/WebP/GIF).";
        if (tamanho <= 0) return "O arquivo enviado esta vazio.";
        if (tamanho > TamanhoMaximoBytes) return "O arquivo excede o limite de 8 MB.";
        return null;
    }

    public async Task<Guid> CriarAsync(Guid tenantId, Guid usuarioId, AnexoArquivoCriarCommand comando, CancellationToken ct = default)
    {
        await using var cn = Connection();
        var bytes = Convert.FromBase64String(comando.ConteudoBase64 ?? "");
        var sha = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var ext = Path.GetExtension(comando.NomeArquivo ?? "").TrimStart('.').ToLowerInvariant();
        return await cn.ExecuteScalarAsync<Guid>(new CommandDefinition(
            @"insert into plantaopro.anexos_arquivos
                 (tenant_id, categoria, entidade_id, nome_original, extensao, tipo_mime, conteudo, sha256, tamanho, observacao, created_by, reg_status)
               values (@tenant, @cat, @ent, @nome, @ext, @mime, @conteudo, @sha, @tam, @obs, @user, 'A')
               returning id",
            new
            {
                tenant = tenantId,
                cat = comando.Categoria?.ToUpperInvariant() ?? "",
                ent = comando.EntidadeId,
                nome = comando.NomeArquivo ?? "",
                ext,
                mime = comando.TipoMime ?? "",
                conteudo = comando.ConteudoBase64,
                sha,
                tam = bytes.Length,
                obs = comando.Observacao,
                user = usuarioId
            },
            cancellationToken: ct));
    }

    private sealed record AnexoLinha(
        Guid Id, Guid? EntidadeId, string Categoria, string NomeOriginal, string Extensao,
        string TipoMime, int Tamanho, DateTime CriadoEm, string? Observacao);

    public async Task<IReadOnlyList<AnexoArquivoResumoDto>> ListarAsync(Guid tenantId, string categoria, Guid? entidadeId, CancellationToken ct = default)
    {
        await using var cn = Connection();
        var linhas = await cn.QueryAsync<AnexoLinha>(new CommandDefinition(
            @"select id, entidade_id as EntidadeId, categoria, nome_original as NomeOriginal,
                     extensao as Extensao, tipo_mime as TipoMime, tamanho as Tamanho,
                     created_at::timestamp as CriadoEm, observacao as Observacao
                from plantaopro.anexos_arquivos
               where tenant_id = @tenant
                 and upper(categoria) = @cat
                 and (@ent::uuid is null or entidade_id = @ent::uuid)
                 and reg_status = 'A'
               order by created_at desc, id desc",
            new { tenant = tenantId, cat = categoria?.ToUpperInvariant() ?? "", ent = entidadeId },
            cancellationToken: ct));
        return linhas.Select(l => new AnexoArquivoResumoDto(l.Id, l.EntidadeId, l.Categoria, l.NomeOriginal, l.Extensao, l.TipoMime, l.Tamanho, l.CriadoEm, l.Observacao)).ToList();
    }

    private sealed record ConteudoLinha(string Conteudo, string TipoMime, string NomeOriginal);

    public async Task<AnexoArquivoByteDto?> ObterConteudoAsync(Guid tenantId, Guid anexoId, CancellationToken ct = default)
    {
        await using var cn = Connection();
        var linha = await cn.QueryFirstOrDefaultAsync<ConteudoLinha>(new CommandDefinition(
            @"select conteudo as Conteudo, tipo_mime as TipoMime, nome_original as NomeOriginal
                from plantaopro.anexos_arquivos
               where id = @id and tenant_id = @tenant and reg_status = 'A'",
            new { id = anexoId, tenant = tenantId },
            cancellationToken: ct));
        if (linha is null || string.IsNullOrEmpty(linha.Conteudo)) return null;
        return new AnexoArquivoByteDto(Convert.FromBase64String(linha.Conteudo), linha.TipoMime, linha.NomeOriginal);
    }

    public async Task<bool> RemoverAsync(Guid tenantId, Guid anexoId, CancellationToken ct = default)
    {
        await using var cn = Connection();
        var afetadas = await cn.ExecuteAsync(new CommandDefinition(
            @"update plantaopro.anexos_arquivos
                  set reg_status = 'I'
                where id = @id and tenant_id = @tenant and reg_status = 'A'",
            new { id = anexoId, tenant = tenantId },
            cancellationToken: ct));
        return afetadas > 0;
    }
}
