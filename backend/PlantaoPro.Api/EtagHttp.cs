using System.Security.Cryptography;

namespace PlantaoPro.Api;

/// <summary>
/// P4: download idempotente — ETag derivado do SHA-256 do arquivo e revalidação
/// via If-None-Match (HTTP 304 sem reenviar o corpo).
/// </summary>
public static class EtagHttp
{
    /// <summary>ETag em forma cotada a partir de um hash SHA-256 hex.</summary>
    public static string Para(string sha256Hex) => "\"" + sha256Hex.ToLowerInvariant() + "\"";

    /// <summary>ETag calculado diretamente dos bytes do arquivo.</summary>
    public static string ParaBytes(byte[] bytes) => Para(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());

    /// <summary>Verifica se o If-None-Match do pedido revalida contra o ETag dado ("*" revalida qualquer versão).</summary>
    public static bool RevalidacaoSatisfeita(HttpRequest request, string etag)
    {
        var header = request.Headers.IfNoneMatch.ToString();
        if (string.IsNullOrWhiteSpace(header)) return false;
        if (header.Contains('*', StringComparison.Ordinal)) return true;
        var alvo = etag.Trim('"').ToLowerInvariant();
        foreach (var parte in header.Split(','))
        {
            var valor = parte.Trim().Trim('"').ToLowerInvariant();
            if (!string.IsNullOrEmpty(valor) && valor == alvo) return true;
        }
        return false;
    }
}
