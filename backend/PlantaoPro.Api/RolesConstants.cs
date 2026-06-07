namespace PlantaoPro.Api;

public static class RolesConstants
{
    public const string AdministradorGlobal = "ADMINISTRADOR_GLOBAL";
    public const string Administrador = "ADMINISTRADOR";
    public const string Coordenacao = "COORDENACAO";
    public const string Operador = "OPERADOR";
    public const string Financeiro = "FINANCEIRO";
    public const string Medico = "MEDICO";
    public const string Hospital = "HOSPITAL";

    public const string Dashboard = AdministradorGlobal + "," + Administrador + "," + Coordenacao + "," + Operador + "," + Financeiro;
    public const string PlantoesGestao = AdministradorGlobal + "," + Administrador + "," + Coordenacao + "," + Operador + "," + Hospital;
    public const string EscalasGestao = AdministradorGlobal + "," + Administrador + "," + Coordenacao + "," + Operador;
    public const string FinanceiroGestao = AdministradorGlobal + "," + Administrador + "," + Financeiro;
    public const string CadastrosOperacao = AdministradorGlobal + "," + Administrador + "," + Coordenacao + "," + Operador;
    public const string CadastrosCoordenacao = AdministradorGlobal + "," + Administrador + "," + Coordenacao;
}
