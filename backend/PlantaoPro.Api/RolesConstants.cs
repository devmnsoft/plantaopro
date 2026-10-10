using PlantaoPro.CrossCutting.Security;

namespace PlantaoPro.Api;

public static class RolesConstants
{
    // Canonical B2B names. Legacy values remain the persisted role codes to keep upgrades compatible.
    public const string PlatformAdmin = AdministradorGlobal;
    public const string TenantAdmin = AdministradorCliente;
    public const string UnitManager = Coordenador;
    public const string ScheduleManager = Coordenacao;
    public const string Professional = Medico;
    public const string FinanceManager = Financeiro;
    public const string AuditorRole = Auditor;
    public const string Support = Suporte;

    // R6-BlocoA item 1 (complemento): códigos BASE delegados a AppRoles (fonte única);
    // as composições de acesso abaixo permanecem locais (política desta ponta).
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

    public const string Dashboard = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + Coordenacao + "," + Coordenador + "," + Operador + "," + Financeiro;
    public const string Operacao = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + Coordenacao + "," + Coordenador + "," + Operador + "," + Hospital;
    public const string PlantoesGestao = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + Coordenacao + "," + Coordenador + "," + Operador + "," + Hospital;
    public const string EscalasGestao = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + Coordenacao + "," + Coordenador + "," + Operador;
    public const string FinanceiroGestao = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Financeiro;
    public const string CadastrosOperacao = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + Coordenacao + "," + Coordenador + "," + Operador;
    public const string Saude360Recepcao = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + AdministradorClinica + "," + Recepcao + "," + Coordenacao + "," + Coordenador + "," + Operador;
    public const string Saude360Assistencial = Saude360Recepcao + "," + Triagem + "," + Enfermagem + "," + CoordenadorClinico + "," + AuditorClinico + "," + Medico;
    // Clinical data must not inherit reception access. Reception can move the
    // operational queue, but only care roles may read or change triage data.
    public const string Saude360Triagem = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + AdministradorClinica + "," + Triagem + "," + Enfermagem + "," + CoordenadorClinico + "," + AuditorClinico + "," + Medico;
    // Clinical records are deliberately narrower than the operational care
    // journey. Reception, triage and finance can advance their own queues,
    // but they must not inherit access to diagnoses or prescriptions.
    public const string Saude360ClinicoLeitura = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + AdministradorClinica + "," + CoordenadorClinico + "," + AuditorClinico + "," + Medico;
    public const string Saude360ClinicoEscrita = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + AdministradorClinica + "," + CoordenadorClinico + "," + Medico;
    public const string Saude360CidLeitura = Saude360ClinicoLeitura + "," + Triagem + "," + Enfermagem;
    public const string Saude360CidGestao = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + AdministradorClinica + "," + CoordenadorClinico;
    public const string Saude360Financeiro = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + AdministradorClinica + "," + Financeiro + "," + FinanceiroClinica;
    public const string Saude360Convenios = Saude360Financeiro + "," + FaturamentoConvenio;
    public const string Saude360Repasses = Saude360Financeiro + "," + Medico;
    public const string CadastrosCoordenacao = AdministradorGlobal + "," + Administrador + "," + AdministradorCliente + "," + Diretor + "," + Coordenacao + "," + Coordenador;
}
