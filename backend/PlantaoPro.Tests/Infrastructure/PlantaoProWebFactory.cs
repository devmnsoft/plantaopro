using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace PlantaoPro.Tests.Infrastructure;

/// <summary>
/// Fábrica de integração para a aplicação Web (Razor/BFF): ambiente Testing, API operacional
/// substituída por <see cref="StubApiHandler"/> (determinística) e endpoint /__test/signin
/// disponível para emitir sessões com claims controlados (R4-A5: com a flag TestAuth:Enabled
/// fixada em "true" — a ponta fica desabilitada por padrão fora da fábrica de testes).
/// </summary>
public sealed class PlantaoProWebFactory : WebApplicationFactory<PlantaoPro.Web.WebAssemblyMarker>
{
    private readonly StubApiHandler _apiStub;

    public StubApiHandler ApiStub => _apiStub;

    public PlantaoProWebFactory() => _apiStub = new StubApiHandler();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ApiSettings:BaseUrl", "http://api.test.local");
        // R4-A5: a suíte depende do /__test/signin; fora desta fábrica a ponta exige a
        // flag TestAuth:Enabled explicitamente (e só vale em Testing).
        builder.UseSetting("TestAuth:Enabled", "true");
        builder.ConfigureTestServices(services => services.AddHttpClient("PlantaoProApi")
            .ConfigurePrimaryHttpMessageHandler(() => _apiStub));
    }
}

/// <summary>
/// Handler HTTP determinístico que simula a API operacional nos testes Web. Um único
/// responder configurável + registro de todas as requisições recebidas (para afirmar
/// chamadas esperadas/ausentes).
/// </summary>
public sealed class StubApiHandler : HttpMessageHandler
{
    private readonly object _sync = new();
    private Func<HttpRequestMessage, HttpResponseMessage> _responder = _ => Json(HttpStatusCode.OK, "{\"ok\":true}");

    public List<string> Requests { get; } = new();

    public StubApiHandler() => Reset();

    public int RequestCount { get { lock (_sync) return Requests.Count; } }

    public void Reset()
    {
        lock (_sync)
        {
            _responder = _ => Json(HttpStatusCode.OK, "{\"ok\":true}");
            Requests.Clear();
        }
    }

    public void Respond(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        lock (_sync) _responder = responder;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage resposta;
        lock (_sync)
        {
            Requests.Add($"{request.Method} {request.RequestUri}");
            resposta = _responder(request);
        }
        await Task.Yield();
        return resposta;
    }

    public static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    public static HttpResponseMessage Html(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "text/html")
    };

    public static HttpResponseMessage Redirect(Uri location) => new((HttpStatusCode)302) { Headers = { Location = location } };
}
