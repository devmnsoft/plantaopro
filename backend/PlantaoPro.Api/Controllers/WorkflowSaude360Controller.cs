using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/workflow-saude360")]
public sealed class WorkflowSaude360Controller : ControllerBase
{
    [HttpGet("resumo")]
    public IActionResult Resumo() { return Ok(ApiResponse<WorkflowResumoDto>.Ok(new WorkflowResumoDto { TotalEtapas = 13, EtapasConcluidas = 7, Pendencias = 6, ProximaAcao = "Fazer check-in dos agendamentos confirmados" }, "Resumo do workflow carregado.")); }

    [HttpGet("proxima-acao")]
    public IActionResult ProximaAcao() { return Ok(ApiResponse<WorkflowProximaAcaoDto>.Ok(new WorkflowProximaAcaoDto { Titulo = "Fazer check-in", Descricao = "Há agendamentos confirmados aguardando chegada do paciente.", Link = "/Agendamentos/CheckIn", PerfilResponsavel = "RECEPCAO", Prioridade = "ALTA" }, "Próxima ação carregada.")); }

    [HttpGet("etapas")]
    public IActionResult Etapas() { return Ok(ApiResponse<IEnumerable<WorkflowEtapaDto>>.Ok(CriarEtapas(), "Etapas do workflow carregadas.")); }

    [HttpGet("pendencias")]
    public IActionResult Pendencias() { return Ok(ApiResponse<IEnumerable<WorkflowPendenciaDto>>.Ok(CriarEtapas().Where(e => e.Pendencias > 0).Select(e => new WorkflowPendenciaDto { Titulo = e.Nome, Descricao = e.ProximaAcao, Prioridade = e.Pendencias > 2 ? "ALTA" : "MEDIA", LinkResolucao = e.Link, PerfilResponsavel = e.PerfilResponsavel }), "Pendências do workflow carregadas.")); }

    private static IEnumerable<WorkflowEtapaDto> CriarEtapas()
    {
        var etapas = new List<WorkflowEtapaDto>
        {
            Etapa("PACIENTE", "Paciente cadastrado", "Base de identificação operacional do paciente.", "CONCLUIDA", 20, 0, "/Pacientes", "RECEPCAO", "Criar agendamento"),
            Etapa("AGENDAMENTO", "Agendamento criado", "Paciente, profissional, data e tipo de atendimento definidos.", "ATENCAO", 15, 2, "/Agendamentos", "RECEPCAO", "Confirmar agenda e check-in"),
            Etapa("CHECKIN", "Check-in realizado", "Chegada do paciente confirmada.", "ATENCAO", 10, 3, "/Agendamentos/CheckIn", "RECEPCAO", "Chamar paciente"),
            Etapa("CHAMADA", "Paciente chamado", "Paciente orientado para sala ou triagem.", "EM_ANDAMENTO", 8, 1, "/PainelChamada", "RECEPCAO", "Iniciar triagem"),
            Etapa("TRIAGEM_INICIO", "Triagem iniciada", "Registro inicial de sinais vitais.", "CONCLUIDA", 6, 0, "/Triagem/Fila", "TRIAGEM", "Finalizar triagem"),
            Etapa("TRIAGEM_FIM", "Triagem finalizada", "Classificação de risco e encaminhamento.", "CONCLUIDA", 6, 0, "/Triagem", "TRIAGEM", "Iniciar consulta"),
            Etapa("CONSULTA_INICIO", "Consulta iniciada", "Médico assumiu atendimento.", "EM_ANDAMENTO", 6, 1, "/Consultas/Atendimento", "MEDICO", "Vincular CID"),
            Etapa("CID", "CID vinculado", "Diagnóstico codificado na consulta.", "ATENCAO", 4, 2, "/Cid", "MEDICO", "Criar prescrição quando aplicável"),
            Etapa("PRESCRICAO", "Prescrição criada", "Prescrição emitida ou dispensada conforme conduta.", "EM_ANDAMENTO", 5, 1, "/Prescricoes", "MEDICO", "Finalizar consulta"),
            Etapa("CONSULTA_FIM", "Consulta finalizada", "Atendimento pronto para financeiro e relatório.", "EM_ANDAMENTO", 4, 1, "/Consultas", "MEDICO", "Gerar conta a receber"),
            Etapa("CONTA", "Conta a receber gerada", "Cobrança criada no financeiro.", "ATENCAO", 8, 3, "/ClinicaFinanceiro/ContasReceber", "FINANCEIRO", "Receber pagamento"),
            Etapa("PAGAMENTO", "Pagamento recebido", "Recebimento baixado e caixa atualizado.", "EM_ANDAMENTO", 5, 1, "/ClinicaFinanceiro/Receber", "FINANCEIRO", "Disponibilizar relatório"),
            Etapa("RELATORIO", "Relatório disponível", "Gestão acompanha valor, operação e riscos.", "CONCLUIDA", 3, 0, "/Relatorios/Executivo", "GESTOR", "Analisar indicadores")
        };
        return etapas;
    }

    private static WorkflowEtapaDto Etapa(string codigo, string nome, string descricao, string status, int quantidade, int pendencias, string link, string perfil, string proximaAcao)
    {
        return new WorkflowEtapaDto { Codigo = codigo, Nome = nome, Descricao = descricao, Status = status, Quantidade = quantidade, Pendencias = pendencias, Link = link, PerfilResponsavel = perfil, ProximaAcao = proximaAcao };
    }
}

public sealed class WorkflowResumoDto { public int TotalEtapas { get; set; } public int EtapasConcluidas { get; set; } public int Pendencias { get; set; } public string ProximaAcao { get; set; } = string.Empty; }
public sealed class WorkflowProximaAcaoDto { public string Titulo { get; set; } = string.Empty; public string Descricao { get; set; } = string.Empty; public string Link { get; set; } = string.Empty; public string PerfilResponsavel { get; set; } = string.Empty; public string Prioridade { get; set; } = string.Empty; }
public sealed class WorkflowEtapaDto { public string Codigo { get; set; } = string.Empty; public string Nome { get; set; } = string.Empty; public string Descricao { get; set; } = string.Empty; public string Status { get; set; } = string.Empty; public int Quantidade { get; set; } public int Pendencias { get; set; } public string Link { get; set; } = string.Empty; public string PerfilResponsavel { get; set; } = string.Empty; public string ProximaAcao { get; set; } = string.Empty; }
public sealed class WorkflowPendenciaDto { public string Titulo { get; set; } = string.Empty; public string Descricao { get; set; } = string.Empty; public string Prioridade { get; set; } = string.Empty; public string LinkResolucao { get; set; } = string.Empty; public string PerfilResponsavel { get; set; } = string.Empty; }
