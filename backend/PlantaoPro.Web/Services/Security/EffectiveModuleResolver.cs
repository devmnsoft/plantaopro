using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Services.Security;

/// <summary>
/// R6-BlocoA item 1: resolução LIVE do conjunto de módulos efetivamente contratados
/// pelo tenant autenticado, obtido da função canônica via <c>api/auth/effective-modules</c>
/// (predicado B6 + herança do pacote + override per-capacidade da v2340). O guard Web
/// usa este conjunto (authoritative quando disponível) para refletir suspensão/expiração/
/// revogação SEM depender apenas dos claims emitidos no login. O resultado é cacheado por
/// requisição (uma chamada por página). Global admin curto-circuita em <c>["*"]</c>.
/// Falha/indisponibilidade devolve <c>null</c>; o consumidor degrada aos claims do login
/// (mesma fonte canônica emitida no login) com janela documentada.
///
/// O check pode ser desativado por configuração (<c>Access:LiveEffectiveModuleCheck=false</c>)
/// — a fábrica de testes Web o desliga para manter os contratos determinísticos de guarda,
/// que não modelam o endpoint effective-modules; em produção o padrão é ATIVADO.
/// </summary>
public interface IEffectiveModuleResolver
{
    /// <summary>Devolve o conjunto efetivo (contém "*" p/ global admin), ou <c>null</c> se a
    /// verificação foi desabilitada ou não pôde ser concluída (degrada aos claims).</summary>
    Task<IReadOnlySet<string>?> GetModulesAsync();
}

public sealed class EffectiveModuleResolver : IEffectiveModuleResolver
{
    public const string CacheKey = "__EffectiveModules__";

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _http;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<EffectiveModuleResolver> _logger;
    private readonly bool _enabled;

    public EffectiveModuleResolver(IHttpClientFactory http, IHttpContextAccessor httpContextAccessor, ICurrentUserService currentUser, IConfiguration configuration, ILogger<EffectiveModuleResolver> logger)
    {
        _http = http;
        _httpContextAccessor = httpContextAccessor;
        _currentUser = currentUser;
        _logger = logger;
        // Padrão ATIVADO; desligável explicitamente via Access:LiveEffectiveModuleCheck=false.
        _enabled = bool.TryParse(configuration?["Access:LiveEffectiveModuleCheck"], out var f) ? f : true;
    }

    public async Task<IReadOnlySet<string>?> GetModulesAsync()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is null) return null;

        if (!_enabled) return null; // check desativado (ex.: fábrica de testes Web): degrada aos claims

        if (ctx.Items.TryGetValue(CacheKey, out var cached))
            return cached as IReadOnlySet<string>; // null => já tentou e falhou; set => ok

        if (_currentUser.IsGlobalAdmin())
        {
            var all = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "*" };
            ctx.Items[CacheKey] = all;
            return all;
        }

        try
        {
            using var client = _http.CreateClient("PlantaoProApi");
            var token = ctx.Session?.GetString("JwtToken");
            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            using var resp = await client.GetAsync("api/auth/effective-modules");
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Resolução live dos módulos efetivos recusada pela API. Status:{Status} Tenant:{Tenant}", (int)resp.StatusCode, _currentUser.TenantId);
                ctx.Items[CacheKey] = null;
                return null;
            }

            var body = await resp.Content.ReadAsStringAsync();
            var envelope = JsonSerializer.Deserialize<ApiResponse<string[]>>(body, JsonOpts);
            if (envelope?.Success != true)
            {
                _logger.LogWarning("Envelope de módulos efetivos inválido. StatusCode:{Code} Tenant:{Tenant}", envelope?.StatusCode ?? -1, _currentUser.TenantId);
                ctx.Items[CacheKey] = null;
                return null;
            }

            var set = (envelope.Data ?? Array.Empty<string>())
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            ctx.Items[CacheKey] = set;
            return set;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Falha na resolução live dos módulos efetivos (fail-closed p/ não-core). Tenant:{Tenant}", _currentUser.TenantId);
            ctx.Items[CacheKey] = null;
            return null;
        }
    }
}
