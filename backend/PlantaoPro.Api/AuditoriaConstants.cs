namespace PlantaoPro.Api;

public static class AuditoriaConstants
{
    public static class Acoes
    {
        public const string LoginSucesso = "LOGIN_SUCESSO";
        public const string LoginFalha = "LOGIN_FALHA";
        public const string AcessoNegado = "ACESSO_NEGADO";
        public const string Criar = "CRIAR";
        public const string Editar = "EDITAR";
        public const string Inativar = "INATIVAR";
        public const string Reativar = "REATIVAR";
        public const string ExcluirLogico = "EXCLUIR_LOGICO";
        public const string PublicarPlantao = "PUBLICAR_PLANTAO";
        public const string CancelarPlantao = "CANCELAR_PLANTAO";
        public const string SolicitarEscala = "SOLICITAR_ESCALA";
        public const string ConfirmarEscala = "CONFIRMAR_ESCALA";
        public const string RecusarEscala = "RECUSAR_ESCALA";
        public const string CancelarEscala = "CANCELAR_ESCALA";
        public const string SubstituirEscala = "SUBSTITUIR_ESCALA";
        public const string RealizarEscala = "REALIZAR_ESCALA";
        public const string NaoCompareceu = "NAO_COMPARECEU";
        public const string CriarConvite = "CRIAR_CONVITE";
        public const string AceitarConvite = "ACEITAR_CONVITE";
        public const string RecusarConvite = "RECUSAR_CONVITE";
        public const string CancelarConvite = "CANCELAR_CONVITE";
        public const string GerarPagamento = "GERAR_PAGAMENTO";
        public const string ConfirmarPagamento = "CONFIRMAR_PAGAMENTO";
        public const string CancelarPagamento = "CANCELAR_PAGAMENTO";
        public const string ContestarPagamento = "CONTESTAR_PAGAMENTO";
        public const string AlterarPermissao = "ALTERAR_PERMISSAO";
        public const string AlterarConfiguracao = "ALTERAR_CONFIGURACAO";
        public const string ExportarRelatorio = "EXPORTAR_RELATORIO";
        public const string DownloadDocumento = "DOWNLOAD_DOCUMENTO";
        public const string BloqueioTenant = "BLOQUEIO_TENANT";
        public const string BloqueioPermissao = "BLOQUEIO_PERMISSAO";
    }

    public static class Entidades
    {
        public const string Usuario = "USUARIO";
        public const string Cliente = "CLIENTE";
        public const string Medico = "MEDICO";
        public const string Hospital = "HOSPITAL";
        public const string Especialidade = "ESPECIALIDADE";
        public const string Plantao = "PLANTAO";
        public const string Escala = "ESCALA";
        public const string Convite = "CONVITE";
        public const string Pagamento = "PAGAMENTO";
        public const string Notificacao = "NOTIFICACAO";
        public const string Comunicacao = "COMUNICACAO";
        public const string Suporte = "SUPORTE";
        public const string Relatorio = "RELATORIO";
        public const string Tenant = "TENANT";
        public const string Permissao = "PERMISSAO";
        public const string Configuracao = "CONFIGURACAO";
        public const string ApiMobile = "API_MOBILE";
    }
}
