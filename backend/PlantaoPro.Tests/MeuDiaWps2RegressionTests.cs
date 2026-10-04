using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlantaoPro.Web.Services;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// WP-S2 (Meu Dia / Central de Ações) — regressão do comportamento HTTP do BFF Web contra a API:
/// uma falha DURANTE A LEITURA DO CORPO da resposta deve ser classificada honestamente
/// (TIMEOUT quando o prazo do cliente venceu no meio do corpo; CANCELADO quando o chamador
/// cancelou), nunca escapar como erro interno (500). Antes da correção, apenas o SendAsync
/// estava dentro do bloco classificatório e o OperationCanceledException do ReadFromJsonAsync
/// vazava para fora do GetAsync.
/// </summary>
public sealed class MeuDiaWps2RegressionTests
{
    [Fact]
    public async Task TimeoutDuranteLeituraDoCorpo_ClassificaComoTimeoutEmVezDeFalhar()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient("PlantaoProApi")
            .ConfigurePrimaryHttpMessageHandler(() => new SlowBodyHandler())
            .ConfigureHttpClient(c => { c.BaseAddress = new Uri("http://api.test.local/"); c.Timeout = TimeSpan.FromMilliseconds(400); });
        await using var provider = services.BuildServiceProvider();
        var web = new ProductivityWebService(provider.GetRequiredService<IHttpClientFactory>(),
            provider.GetRequiredService<ILogger<ProductivityWebService>>());

        var model = await web.GetMyDayAsync("token-teste", CancellationToken.None);

        Assert.Equal("TIMEOUT", model.ErrorKind);
        Assert.False(string.IsNullOrWhiteSpace(model.Error));
        Assert.Empty(model.Items);
    }

    [Fact]
    public async Task CancelamentoDoChamadorDuranteLeituraDoCorpo_ClassificaComoCancelado()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient("PlantaoProApi")
            .ConfigurePrimaryHttpMessageHandler(() => new WaitingBodyHandler())
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://api.test.local/"));
        await using var provider = services.BuildServiceProvider();
        var web = new ProductivityWebService(provider.GetRequiredService<IHttpClientFactory>(),
            provider.GetRequiredService<ILogger<ProductivityWebService>>());

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        var model = await web.GetMyDayAsync("token-teste", cts.Token);

        Assert.Equal("CANCELADO", model.ErrorKind);
        Assert.False(string.IsNullOrWhiteSpace(model.Error));
        Assert.Empty(model.Items);
    }

    /// <summary>
    /// Responde os cabeçalhos imediatamente e simula um transporte que é cancelado na primeira
    /// leitura do corpo — exatamente o que o SocketsHttpHandler faz em produção quando o
    /// RequestTimeout vence no meio da resposta (TaskCanceledException durante o ReadFromJsonAsync).
    /// </summary>
    private sealed class SlowBodyHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = new StreamContent(new BodyReadTimeoutStream());
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = content;
            return Task.FromResult(response);
        }
    }

    /// <summary>
    /// Responde os cabeçalhos imediatamente e bloqueia a leitura do corpo até o token do
    /// chamador ser cancelado (simula um servidor que parou de enviar bytes).
    /// </summary>
    private sealed class WaitingBodyHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = new StreamContent(new BodyWaitsForCallerStream());
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = content;
            return Task.FromResult(response);
        }
    }

    private sealed class BodyReadTimeoutStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => 0; set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count)
            => ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50), CancellationToken.None).ConfigureAwait(false);
            throw new TaskCanceledException("Simulando timeout do cliente durante a leitura do corpo.");
        }
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class BodyWaitsForCallerStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => 0; set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count)
            => ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // Fica esperando o corpo; o token do chamador (300 ms no teste) sempre chega antes.
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            return 0; // Inatingível: o token é cancelado primeiro.
        }
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }
}
