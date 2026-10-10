namespace PlantaoPro.CrossCutting.Security;

/// <summary>
/// R6-BlocoA item 1 (complemento): códigos de papel BASE canônicos (valores persistidos),
/// compartilhados por API e Web. O adendo D.3 (#8) do inventário registrou os dois
/// RolesConstants como quase-duplicados divergentes; a parte efetivamente duplicada é esta
/// lista base. As COMPOSIÇÕES de acesso (ex.: Saude360Assistencial, Operacao) permanecem
/// locais a cada aplicação porque codificam políticas distintas de cada ponta (a API tem
/// conjuntos clínicos de leitura/escrita que o Web não usa, e vice-versa).
/// </summary>
public static class AppRoles
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
    public const string Enfermagem = "ENFERMAGEM";
    public const string CoordenadorClinico = "COORDENADOR_CLINICO";
    public const string AuditorClinico = "AUDITOR_CLINICO";
    public const string FinanceiroClinica = "FINANCEIRO_CLINICA";
    public const string FaturamentoConvenio = "FATURAMENTO_CONVENIO";
    public const string AdministradorClinica = "ADMINISTRADOR_CLINICA";
}
