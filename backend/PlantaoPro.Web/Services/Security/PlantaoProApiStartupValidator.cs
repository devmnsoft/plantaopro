using System.IO;

namespace PlantaoPro.Web.Services.Security;

/// <summary>
/// A2 (rodada 4): validação de configuração na inicialização do Web, com mensagens
/// claras em pt-BR e sem imprimir segredos. Falha rápido (fail-fast) para que um site
/// publicado no IIS não suba "aparentemente ok" e só quebre no primeiro POST de login.
///
/// Regras:
/// 1) BaseUrl da API obrigatória, URL absoluta http/https.
///    Precedência documentada (ver docs/deploy/guia-implantacao-iis.md):
///    ApiSettings:BaseUrl > PlantaoProApi:BaseUrl, e variáveis de ambiente sobrepõem
///    qualquer arquivo versionado (ex.: PlantaoProApi__BaseUrl no pool/aplicação do IIS).
/// 2) Fora de Development as portas de desenvolvimento (51976/51977/52976/52977) são
///    rejeitadas: em produção o Web aponta para a URL interna da API do próprio servidor.
/// 3) DataProtection:KeysDirectory obrigatório em Production (persistência de chaves fora
///    da memória; sem ele cookies/sessões morrem a cada reciclagem do pool ou quando há
///    mais de uma instância do Web). Em Production o diretório deve existir e ser gravável;
///    nos demais ambientes, se informado e ausente, é criado.
/// </summary>
public static partial class PlantaoProApiStartupValidator
{
    private static readonly int[] PortasDesenvolvimento = { 51976, 51977, 52976, 52977 };

    /// <summary>Resolve a BaseUrl efetiva com a precedência oficial (não usar outra ordem).</summary>
    public static string? ResolveBaseUrl(IConfiguration configuration)
        => configuration["ApiSettings:BaseUrl"] ?? configuration["PlantaoProApi:BaseUrl"];

    public static void Validate(IConfiguration configuration, IWebHostEnvironment environment)
    {
        ValidateBaseUrl(configuration, environment);
        ValidateDataProtectionKeysDirectory(configuration, environment);
    }

    public static void ValidateBaseUrl(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var raw = ResolveBaseUrl(configuration);
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException(
                "Configuração PlantaoProApi:BaseUrl não encontrada. Configure a URL interna da API " +
                "via variável de ambiente PlantaoProApi__BaseUrl (aplicação ou pool do IIS) ou em " +
                "appsettings.Production.json. Precedência: ApiSettings:BaseUrl > PlantaoProApi:BaseUrl > appsettings.json.");
        }

        Uri url;
        try
        {
            url = new Uri(raw, UriKind.Absolute);
        }
        catch (UriFormatException ex)
        {
            throw new InvalidOperationException(
                "Configuração PlantaoProApi:BaseUrl inválida: informe uma URL absoluta com esquema (ex.: http://127.0.0.1:8197/).", ex);
        }

        if (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                $"Configuração PlantaoProApi:BaseUrl deve usar http ou https. Esquema recebido: {url.Scheme}.");
        }

        // Testing segue o tratamento de Development na regra de porta: a fábrica de testes
        // (PlantaoProWebFactory) herda a BaseUrl do appsettings.json antes do override da
        // fábrica entrar em vigor, e o valor versionado aponta para a porta de dev 51977.
        var bloqueiaPortaDev = !environment.IsDevelopment() && !environment.IsEnvironment("Testing");
        if (bloqueiaPortaDev && PortasDesenvolvimento.Contains(url.Port))
        {
            throw new InvalidOperationException(
                $"PlantaoProApi:BaseUrl usa a porta de desenvolvimento {url.Port} fora do ambiente Development " +
                $"(ambiente atual: {environment.EnvironmentName}). Em produção aponte o Web para a URL interna da API " +
                "do próprio servidor (ex.: http://127.0.0.1:8197/) via variável PlantaoProApi__BaseUrl.");
        }
    }

    public static void ValidateDataProtectionKeysDirectory(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var directory = configuration["DataProtection:KeysDirectory"];

        if (string.IsNullOrWhiteSpace(directory))
        {
            if (environment.IsProduction())
            {
                throw new InvalidOperationException(
                    "DataProtection:KeysDirectory não configurado em Production. Defina um diretório persistente " +
                    "(ex.: C:\\ProgramData\\PlantaoPro\\DataProtection, com escrita para as identidades dos pools IIS) " +
                    "via variável DataProtection__KeysDirectory; sem persistência, cookies e sessões são invalidados " +
                    "a cada reciclagem do pool e não sobrevivem a múltiplas instâncias.");
            }
            return;
        }

        if (environment.IsProduction())
        {
            if (!Directory.Exists(directory))
            {
                throw new InvalidOperationException(
                    $"DataProtection:KeysDirectory '{directory}' não existe em Production. Crie o diretório antes de iniciar o site e conceda escrita à identidade do pool (IIS AppPool\\...).");
            }
            if (!IsWritable(directory))
            {
                throw new InvalidOperationException(
                    $"DataProtection:KeysDirectory '{directory}' não permite escrita para a identidade atual. Ajuste as permissões do diretório.");
            }
        }
        else
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static bool IsWritable(string path)
    {
        // Probe de escrita real: Directory não expõe API de verificação de permissão,
        // e o teste efetivo captura exatamente os erros que o IIS enfrentaria ao gravar chaves.
        try
        {
            var probe = Path.Combine(path, ".probe_" + Guid.NewGuid().ToString("N"));
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
