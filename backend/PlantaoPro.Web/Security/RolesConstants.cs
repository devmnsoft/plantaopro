using PlantaoPro.CrossCutting.Security;

namespace PlantaoPro.Web.Security;

/// <summary>
/// Papéis do app Web. Os 23 códigos BASE vêm de <see cref="AppRoles"/> (fonte única R6);
/// as composições abaixo codificam a política de exibição/navegação desta ponta.
/// </summary>
public static class RolesConstants
{
    public const string AdministradorGlobal = AppRoles.AdministradorGlobal;
    public const string Administrador = AppRoles.Administrador;
    public const string AdministradorCliente = AppRoles.AdministradorCliente;
    public const string Diretor = AppRoles.Diretor;
    public const string Coordenacao = AppRoles.Coordenacao;
    public const string Coordenador = AppRoles.Coordenador;
    public const string Operador = AppRoles.Operador;
    public const string Financeiro = AppRoles.Financeiro;
    public const string Medico = AppRoles.Medico;
    public const string Hospital = AppRoles.Hospital;
    public const string Parceiro = AppRoles.Parceiro;
    public const string Suporte = AppRoles.Suporte;
    public const string Auditor = AppRoles.Auditor;
    public const string Comercial = AppRoles.Comercial;
    public const string CustomerSuccess = AppRoles.CustomerSuccess;
    public const string Recepcao = AppRoles.Recepcao;
    public const string Triagem = AppRoles.Triagem;
    public const string Enfermagem = AppRoles.Enfermagem;
    public const string CoordenadorClinico = AppRoles.CoordenadorClinico;
    public const string AuditorClinico = AppRoles.AuditorClinico;
    public const string FinanceiroClinica = AppRoles.FinanceiroClinica;
    public const string FaturamentoConvenio = AppRoles.FaturamentoConvenio;
    public const string AdministradorClinica = AppRoles.AdministradorClinica;

    public const string AdminSaas = AdministradorGlobal + "," + Suporte + "," + Auditor;
    public const string AuditoriaAcesso = AdministradorGlobal + "," + Administrador + "," + Auditor;
    public const string TenantAdmin = Administrador + "," + AdministradorCliente + "," + Diretor;
    public const string Operacao = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + Coordenacao + "," + Coordenador + "," + Operador + "," + Hospital;
    public const string EscalasGestao = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + Coordenacao + "," + Coordenador + "," + Operador;
    public const string FinanceiroArea = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + Financeiro;
    public const string ComercialArea = AdministradorGlobal + "," + Comercial;
    public const string Saude360Recepcao = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + AdministradorClinica + "," + Recepcao + "," + Coordenacao + "," + Coordenador + "," + Operador;
    public const string Saude360Assistencial = Saude360Recepcao + "," + Triagem + "," + Enfermagem + "," + CoordenadorClinico + "," + AuditorClinico + "," + Medico;
    public const string Saude360Financeiro = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + AdministradorClinica + "," + Financeiro + "," + FinanceiroClinica;
    public const string Saude360Convenios = Saude360Financeiro + "," + FaturamentoConvenio;
    public const string CadastrosCoordenacao = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + Coordenacao + "," + Coordenador + "," + Operador;
}
