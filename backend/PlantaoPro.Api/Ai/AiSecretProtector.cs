using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace PlantaoPro.Api.Ai;

/// <summary>
/// Cifra as chaves de provedor guardadas por tenant (AES-GCM, 256 bits).
/// A chave mestra vem SOMENTE da configuração do servidor (Ai__EncryptionKey,
/// 64 caracteres hex = 32 bytes) — nunca é gravada em banco nem devolvida à UI.
///
/// Formato do blob persistido: nonce(12) || tag(16) || ciphertext.
/// O valor puro da chave nunca é logado; a exibição usa <see cref="Mask"/>.
/// </summary>
public sealed class AiSecretProtector
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[]? _masterKey;

    public AiSecretProtector(IConfiguration configuration)
    {
        var raw = configuration["Ai:EncryptionKey"];
        if (string.IsNullOrWhiteSpace(raw)) return;
        try
        {
            var key = Convert.FromHexString(raw.Trim());
            if (key.Length == 32) _masterKey = key;
            // Comprimento diferente de 32 bytes: trata-se como ausente
            // (mensagem clara ao salvar; nada falha silenciosamente em runtime).
        }
        catch (FormatException)
        {
            // Hex malformado: chave mestra considerada ausente.
        }
    }

    public bool HasMasterKey => _masterKey is not null;

    public byte[] Protect(string plaintext)
    {
        if (_masterKey is null)
            throw new InvalidOperationException("Chave mestra de cifragem ausente (Ai:EncryptionKey).");
        using var aes = new AesGcm(_masterKey, TagSize);
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[plainBytes.Length];
        var tag = new byte[TagSize];
        aes.Encrypt(nonce, plainBytes, cipher, tag);
        var blob = new byte[NonceSize + TagSize + cipher.Length];
        Buffer.BlockCopy(nonce, 0, blob, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, blob, NonceSize, TagSize);
        Buffer.BlockCopy(cipher, 0, blob, NonceSize + TagSize, cipher.Length);
        return blob;
    }

    public string Unprotect(byte[] blob)
    {
        if (_masterKey is null)
            throw new InvalidOperationException("Chave mestra de cifragem ausente (Ai:EncryptionKey).");
        if (blob is null || blob.Length < NonceSize + TagSize)
            throw new FormatException("Chave cifrada armazenada em formato inválido.");
        using var aes = new AesGcm(_masterKey, TagSize);
        var plain = new byte[blob.Length - NonceSize - TagSize];
        aes.Decrypt(blob.AsSpan(0, NonceSize), blob.AsSpan(NonceSize + TagSize), blob.AsSpan(NonceSize, TagSize), plain);
        return Encoding.UTF8.GetString(plain);
    }

    /// <summary>Máscara para exibição na interface: apenas os 4 últimos caracteres.</summary>
    public static string? Mask(string? key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (key.Length <= 4) return "****";
        return "…" + key.Substring(key.Length - 4);
    }
}
