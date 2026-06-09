namespace PlantaoPro.Api;

public static class RolesConstants
{
    public const string AdministradorGlobal = "ADMINISTRADOR_GLOBAL";
    public const string Administrador = "ADMINISTRADOR";
    public const string AdministradorCliente = "ADMINISTRADOR_CLIENTE";
    public const string Diretor = "DIRETOR";
    public const string Coordenacao = "COORDENACAO";
    public const string Coordenador = "COORDENADOR";
    public const string Operador = "OPERADOR";
    public const string Financeiro = "FINANCEIRO";
    public const string Medico = "MEDICO";
    public const string Hospital = "HOSPITAL";
    public const string Parceiro = "PARCEIRO";
    public const string Suporte = "SUPORTE";
    public const string Auditor = "AUDITOR";
    public const string Comercial = "COMERCIAL";
    public const string CustomerSuccess = "CUSTOMER_SUCCESS";

    public const string Dashboard = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + Coordenacao + "," + Coordenador + "," + Operador + "," + Financeiro;
    public const string PlantoesGestao = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + Coordenacao + "," + Coordenador + "," + Operador + "," + Hospital;
    public const string EscalasGestao = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + Coordenacao + "," + Coordenador + "," + Operador;
    public const string FinanceiroGestao = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Financeiro;
    public const string CadastrosOperacao = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + Coordenacao + "," + Coordenador + "," + Operador;
    public const string CadastrosCoordenacao = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + Coordenacao + "," + Coordenador;
}
