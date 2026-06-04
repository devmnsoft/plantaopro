using Microsoft.AspNetCore.Mvc;
namespace BarberSync.PublicWeb.Controllers;
[ApiController,Route("PublicApi")]
public class PublicApiController:ControllerBase{
static object Env(object d,string m="Operação realizada com sucesso.")=>new{success=true,message=m,data=d};
static readonly object[] Services={new{id="svc-corte",name="Corte Masculino",price=45,duration=35},new{id="svc-barba",name="Barba Tradicional",price=35,duration=25},new{id="svc-combo",name="Corte + Barba",price=75,duration=60},new{id="svc-sobrancelha",name="Sobrancelha",price=25,duration=15},new{id="svc-hidratacao",name="Hidratação Capilar",price=90,duration=45},new{id="svc-manicure",name="Manicure",price=55,duration=50}};
static readonly object[] Professionals={new{id="pro-rafael",name="Rafael Barber",rating=4.9},new{id="pro-lucas",name="Lucas Navalha",rating=4.8},new{id="pro-bruno",name="Bruno Estilo",rating=4.7},new{id="pro-camila",name="Camila Beauty",rating=4.9},new{id="pro-amanda",name="Amanda Nails",rating=4.8}};
[HttpGet("services")]public IActionResult ServicesGet()=>Ok(Env(Services));[HttpGet("professionals")]public IActionResult ProfessionalsGet()=>Ok(Env(Professionals));[HttpPost("appointments")]public IActionResult Appointment([FromBody]object body)=>Ok(Env(new{protocol=$"BS-PUB-{DateTime.UtcNow:HHmmss}",body},"Agendamento demo confirmado."));[HttpPost("leads")]public IActionResult Lead([FromBody]object body)=>Ok(Env(new{id=Guid.NewGuid(),body},"Lead recebido."));}
