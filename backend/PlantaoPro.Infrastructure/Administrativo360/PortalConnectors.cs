using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

public sealed class OpmenexoConnector : IPortalCotacaoConnector
{
    public string Provedor => "OPMENEXO";

    public Task<PortalConexaoStatusResult> TestarConexaoAsync(PortalContaDto conta, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(conta.UsuarioAcesso))
        {
            return Task.FromResult(new PortalConexaoStatusResult(
                Conectado: false,
                Status: "NAO_CONFIGURADA",
                Mensagem: "Credenciais oficiais da API OPMENEXO/BIONEXO não configuradas. É necessário cadastrar a Chave de API e Token no cofre de segredos da empresa.",
                VerificadoEm: DateTime.UtcNow
            ));
        }

        return Task.FromResult(new PortalConexaoStatusResult(
            Conectado: false,
            Status: "CONFIGURACAO_PENDENTE",
            Mensagem: "Endpoint oficial OPMENEXO requer homologação prévia de IP e certificado de cliente corporativo.",
            VerificadoEm: DateTime.UtcNow
        ));
    }

    public Task<IReadOnlyList<CapturarCotacaoCommand>> SincronizarCotacoesNovasAsync(PortalContaDto conta, CancellationToken ct = default)
    {
        var status = TestarConexaoAsync(conta, ct).Result;
        if (!status.Conectado)
        {
            throw new InvalidOperationException($"Integração OPMENEXO indisponível: {status.Mensagem} Utilize a opção de Importação Manual.");
        }

        return Task.FromResult<IReadOnlyList<CapturarCotacaoCommand>>(Array.Empty<CapturarCotacaoCommand>());
    }

    public Task<EnvioRespostaPortalResult> TransmitirPropostaAsync(PortalContaDto conta, CotacaoRespostaDto resposta, CotacaoDetalhesDto cotacao, CancellationToken ct = default)
    {
        return Task.FromResult(new EnvioRespostaPortalResult(
            Sucesso: false,
            StatusTransmissao: "CONFIGURACAO_PENDENTE",
            Protocolo: null,
            Mensagem: "Integração oficial OPMENEXO não configurada com credenciais corporativas no cofre de segredos. Transmissão automática pendente de configuração."
        ));
    }
}

public sealed class InpartConnector : IPortalCotacaoConnector
{
    public string Provedor => "INPART";

    public Task<PortalConexaoStatusResult> TestarConexaoAsync(PortalContaDto conta, CancellationToken ct = default)
    {
        if (string.Equals(conta.StatusIntegracao, "BLOQUEADA", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new PortalConexaoStatusResult(
                Conectado: false,
                Status: "BLOQUEADA",
                Mensagem: conta.MotivoBloqueio ?? "Integração INPART bloqueada.",
                VerificadoEm: DateTime.UtcNow
            ));
        }

        if (string.IsNullOrWhiteSpace(conta.UsuarioAcesso))
        {
            return Task.FromResult(new PortalConexaoStatusResult(
                Conectado: false,
                Status: "NAO_CONFIGURADA",
                Mensagem: "Credenciais oficiais da API INPART Saúde não configuradas. Cadastre a credencial de integração da distribuidora.",
                VerificadoEm: DateTime.UtcNow
            ));
        }

        return Task.FromResult(new PortalConexaoStatusResult(
            Conectado: false,
            Status: "CONFIGURACAO_PENDENTE",
            Mensagem: "Homologação de credencial INPART Saúde pendente de autorização do convênio.",
            VerificadoEm: DateTime.UtcNow
        ));
    }

    public Task<IReadOnlyList<CapturarCotacaoCommand>> SincronizarCotacoesNovasAsync(PortalContaDto conta, CancellationToken ct = default)
    {
        var status = TestarConexaoAsync(conta, ct).Result;
        if (!status.Conectado)
        {
            throw new InvalidOperationException($"Integração INPART indisponível: {status.Mensagem} Utilize a opção de Importação Manual.");
        }

        return Task.FromResult<IReadOnlyList<CapturarCotacaoCommand>>(Array.Empty<CapturarCotacaoCommand>());
    }

    public Task<EnvioRespostaPortalResult> TransmitirPropostaAsync(PortalContaDto conta, CotacaoRespostaDto resposta, CotacaoDetalhesDto cotacao, CancellationToken ct = default)
    {
        return Task.FromResult(new EnvioRespostaPortalResult(
            Sucesso: false,
            StatusTransmissao: "CONFIGURACAO_PENDENTE",
            Protocolo: null,
            Mensagem: "Integração oficial INPART Saúde não configurada com credenciais corporativas. Transmissão automática pendente de configuração."
        ));
    }
}

public sealed class ImportacaoManualConnector : IPortalCotacaoConnector
{
    public string Provedor => "IMPORTACAO_MANUAL";

    public Task<PortalConexaoStatusResult> TestarConexaoAsync(PortalContaDto conta, CancellationToken ct = default)
    {
        return Task.FromResult(new PortalConexaoStatusResult(
            Conectado: true,
            Status: "CONFIGURADA",
            Mensagem: "Canal de importação manual ativo e pronto para recepção de arquivos de cotação.",
            VerificadoEm: DateTime.UtcNow
        ));
    }

    public Task<IReadOnlyList<CapturarCotacaoCommand>> SincronizarCotacoesNovasAsync(PortalContaDto conta, CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<CapturarCotacaoCommand>>(Array.Empty<CapturarCotacaoCommand>());
    }

    public Task<EnvioRespostaPortalResult> TransmitirPropostaAsync(PortalContaDto conta, CotacaoRespostaDto resposta, CotacaoDetalhesDto cotacao, CancellationToken ct = default)
    {
        return Task.FromResult(new EnvioRespostaPortalResult(
            Sucesso: true,
            StatusTransmissao: "EXPORTADA_MANUALMENTE",
            Protocolo: null,
            Mensagem: "Proposta exportada manualmente para conferência e envio pelo operador ao portal."
        ));
    }
}
