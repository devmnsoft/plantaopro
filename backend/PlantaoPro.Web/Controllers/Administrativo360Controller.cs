using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles="ADMINISTRADOR,ADMINISTRADOR_CLIENTE,DIRETOR,COORDENACAO,COORDENADOR")]
public sealed class Administrativo360Controller : BaseWebController
{
 public Administrativo360Controller(IHttpClientFactory factory,ILogger<Administrativo360Controller> logger):base(factory,logger){}
 public async Task<IActionResult> Index()
 {
  using var client=CreateApiClient(); if(!AddBearerToken(client)) return HandleUnauthorized();
  var resumo=await ReadApiResponse<Administrativo360ResumoViewModel>(client,"api/administrativo360/resumo");
  var departamentos=await ReadApiResponse<IReadOnlyList<Departamento360ViewModel>>(client,"api/administrativo360/departamentos");
  var cargos=await ReadApiResponse<IReadOnlyList<Cargo360ViewModel>>(client,"api/administrativo360/cargos");
  var colaboradores=await ReadApiResponse<IReadOnlyList<Colaborador360ViewModel>>(client,"api/administrativo360/colaboradores");
  var contratos=await ReadApiResponse<IReadOnlyList<Contrato360ViewModel>>(client,"api/administrativo360/contratos");
  return View(new Administrativo360PageViewModel { Resumo=resumo.Data??new(0,0,0,0),Departamentos=departamentos.Data??Array.Empty<Departamento360ViewModel>(),Cargos=cargos.Data??Array.Empty<Cargo360ViewModel>(),Colaboradores=colaboradores.Data??Array.Empty<Colaborador360ViewModel>(),Contratos=contratos.Data??Array.Empty<Contrato360ViewModel>(),Erro=resumo.Error??departamentos.Error??cargos.Error??colaboradores.Error??contratos.Error });
 }
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Departamento(string codigo,string nome)=>await Send("api/administrativo360/departamentos",new{codigo,nome},"Departamento cadastrado.");
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Cargo(string codigo,string nome,Guid? departamentoId)=>await Send("api/administrativo360/cargos",new{codigo,nome,departamentoId},"Cargo cadastrado.");
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Colaborador(string matricula,string nome,string cpf,string email,Guid cargoId)=>await Send("api/administrativo360/colaboradores",new{matricula,nome,cpf,email,cargoId},"Colaborador cadastrado.");
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Contrato(Guid colaboradorId,string tipo,DateOnly inicio,DateOnly? fim,decimal salario,int cargaHorariaSemanal)=>await Send("api/administrativo360/contratos",new{colaboradorId,tipo,inicio,fim,salario,cargaHorariaSemanal},"Contratação registrada.");
 private async Task<IActionResult> Send<T>(string endpoint,T payload,string success)
 {
  using var client=CreateApiClient(); if(!AddBearerToken(client)) return HandleUnauthorized();
  var response=await SendApiAsync<T,System.Text.Json.JsonElement>(client,HttpMethod.Post,endpoint,payload);
  var ok=response.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
  TempData[ok?"SuccessMessage":"ErrorMessage"]=ok?success:response.Error;
  return RedirectToAction(nameof(Index));
 }
}
