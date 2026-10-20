namespace PlantaoPro.Application.Administrativo360;

/// <summary>
/// R72 (rodada 72): anexos de arquivos binarios (PDF/Word/imagem) por entidade — pedido do usuario.
/// Antes da R72 o unico upload do sistema era a Central XML (arquivos .xml apenas); funcionarios
/// (colaboradores), empresas (parceiros) e notas fiscais (pre-emissao) nao recebiam documentos.
/// Convencao da casa: conteudo em base64 em coluna text (mesmo padrao de v115_recurso_documentos),
/// auditavel (created_by/created_at) e com exclusao macia (reg_status 'I').
/// </summary>
public sealed record AnexoArquivoCriarCommand(
    string? Categoria,
    Guid? EntidadeId,
    string? NomeArquivo,
    string? TipoMime,
    string? ConteudoBase64,
    int Tamanho,
    string? Observacao);

public sealed record AnexoArquivoResumoDto(
    Guid Id,
    Guid? EntidadeId,
    string Categoria,
    string NomeOriginal,
    string Extensao,
    string TipoMime,
    int Tamanho,
    DateTime CriadoEm,
    string? Observacao);

public sealed record AnexoArquivoByteDto(byte[] Bytes, string TipoMime, string NomeOriginal);

public interface IAnexosArquivosRepository
{
    Task<Guid> CriarAsync(Guid tenantId, Guid usuarioId, AnexoArquivoCriarCommand comando, CancellationToken ct = default);
    Task<IReadOnlyList<AnexoArquivoResumoDto>> ListarAsync(Guid tenantId, string categoria, Guid? entidadeId, CancellationToken ct = default);
    Task<AnexoArquivoByteDto?> ObterConteudoAsync(Guid tenantId, Guid anexoId, CancellationToken ct = default);
    Task<bool> RemoverAsync(Guid tenantId, Guid anexoId, CancellationToken ct = default);
}
