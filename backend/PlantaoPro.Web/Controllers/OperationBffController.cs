using System.Net.Http.Headers; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc;
namespace PlantaoPro.Web.Controllers;
[Authorize,ApiController,Route("bff/operacao")]
public sealed class OperationBffController:ControllerBase
{
 private readonly IHttpClientFactory factory; public OperationBffController(IHttpClientFactory factory)=>this.factory=factory;
 [AcceptVerbs("GET","POST","PUT","DELETE"),Route("{**path}")] public async Task<IActionResult> Proxy(string path,CancellationToken ct){var token=HttpContext.Session.GetString("jwt")??HttpContext.Session.GetString("JwtToken")??User.FindFirst("jwt")?.Value??User.FindFirst("Token")?.Value;if(string.IsNullOrWhiteSpace(token))return Unauthorized(new{message="Sessão expirada. Entre novamente para consultar a operação."});var client=factory.CreateClient("PlantaoProApi");client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",token);using var request=new HttpRequestMessage(new HttpMethod(Request.Method),"api/"+path);if(Request.ContentLength>0)request.Content=new StreamContent(Request.Body);if(!string.IsNullOrWhiteSpace(Request.ContentType))request.Content!.Headers.ContentType=MediaTypeHeaderValue.Parse(Request.ContentType);using var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,ct);var bytes=await response.Content.ReadAsByteArrayAsync(ct);Response.StatusCode=(int)response.StatusCode;return File(bytes,response.Content.Headers.ContentType?.ToString()??"application/json");}
}
