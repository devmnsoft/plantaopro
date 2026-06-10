namespace PlantaoPro.Web.Security;

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
    public const string Recepcao = "RECEPCAO";
    public const string Triagem = "TRIAGEM";
    public const string FinanceiroClinica = "FINANCEIRO_CLINICA";
    public const string FaturamentoConvenio = "FATURAMENTO_CONVENIO";
    public const string AdministradorClinica = "ADMINISTRADOR_CLINICA";

    public const string AdminSaas = AdministradorGlobal + "," + Suporte + "," + Auditor;
    public const string TenantAdmin = Administrador + "," + AdministradorCliente + "," + Diretor;
    public const string Operacao = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + Coordenacao + "," + Coordenador + "," + Operador + "," + Hospital;
    public const string FinanceiroArea = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + Financeiro;
    public const string ComercialArea = AdministradorGlobal + "," + Comercial;
    public const string Saude360Recepcao = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + AdministradorClinica + "," + Recepcao + "," + Coordenacao + "," + Coordenador + "," + Operador;
    public const string Saude360Assistencial = Saude360Recepcao + "," + Triagem + "," + Medico;
    public const string Saude360Financeiro = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + AdministradorClinica + "," + Financeiro + "," + FinanceiroClinica;
    public const string Saude360Convenios = Saude360Financeiro + "," + FaturamentoConvenio;
    public const string CadastrosCoordenacao = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + Coordenacao + "," + Coordenador + "," + Operador;
}
