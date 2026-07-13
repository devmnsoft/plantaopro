using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Services;

public interface IAssistenteContextualService
{
    AssistenteContextualViewModel Obter(string controller, string action, string perfil, int registros);
}

public sealed class AssistenteContextualService : IAssistenteContextualService
{
    public AssistenteContextualViewModel Obter(string controller, string action, string perfil, int registros)
    {
        var key = (controller ?? string.Empty).ToLowerInvariant();
        var model = CriarPadrao(controller, action, perfil);
        if (key.Contains("pacientes")) Configurar(model, "Pacientes", "Comece cadastrando ou localizando o paciente. Depois disso, você poderá criar um agendamento.", "Novo paciente", "/Pacientes/Create", "RECEPCAO");
        else if (key.Contains("agendamentos")) Configurar(model, "Agendamentos", "Escolha paciente, profissional, data e tipo de atendimento. Após o paciente chegar, realize o check-in.", "Fazer check-in", "/Agendamentos/CheckIn", "RECEPCAO");
        else if (key.Contains("painelchamada")) Configurar(model, "Painel de chamada", "Chame pacientes com chegada confirmada e mantenha a sala de espera orientada sem expor dados sensíveis.", "Abrir fila", "/PainelChamada/Index", "RECEPCAO");
        else if (key.Contains("triagem")) Configurar(model, "Triagem", "Registre sinais vitais e classificação de risco. Ao finalizar, o paciente fica disponível para consulta.", "Abrir fila de triagem", "/Triagem/Fila", "TRIAGEM");
        else if (key.Contains("consultas")) Configurar(model, "Consultas", "Registre anamnese, CID, conduta e prescrição. Ao finalizar, o atendimento pode gerar financeiro.", "Iniciar atendimento", "/Consultas/Atendimento", "MEDICO");
        else if (key.Contains("cid")) Configurar(model, "CID", "Use a busca de CID para vincular diagnósticos padronizados durante a consulta.", "Buscar CID", "/Cid/Index", "MEDICO");
        else if (key.Contains("prescricoes")) Configurar(model, "Prescrições", "Crie prescrições com modelos e finalize apenas quando o conteúdo estiver revisado.", "Nova prescrição", "/Prescricoes/Create", "MEDICO");
        else if (key.Contains("clinicafinanceiro")) Configurar(model, "Financeiro", "Acompanhe contas a receber, recebimentos e caixa. Use os filtros para localizar pendências.", "Receber pagamento", "/ClinicaFinanceiro/Receber", "FINANCEIRO");
        else if (key.Contains("convenios")) Configurar(model, "Convênios", "Monitore autorizações, glosas e faturamento para reduzir retrabalho e perda de receita.", "Ver autorizações", "/Convenios/Autorizacoes", "CONVENIOS");
        else if (key.Contains("planossaude")) Configurar(model, "Planos de saúde", "Vincule pacientes aos planos corretos antes da autorização ou faturamento.", "Vincular paciente", "/PlanosSaude/Pacientes", "CONVENIOS");
        else if (key.Contains("workflow") || key.Contains("clinicadashboard")) Configurar(model, "Fluxo de atendimento", "Acompanhe a jornada ponta a ponta e resolva o gargalo indicado como próxima ação.", "Ver próxima ação", "/WorkflowSaude360/ProximaAcao", "GESTOR");
        else if (key.Contains("relatorios")) Configurar(model, "Relatórios", "Use os relatórios executivos para demonstrar volume, eficiência, receita e riscos da operação.", "Relatório executivo", "/Relatorios/Executivo", "GESTOR");
        else if (key.Contains("manual")) Configurar(model, "Manual", "Consulte o roteiro do seu perfil quando precisar revisar o fluxo ou treinar a equipe.", "Abrir manual do perfil", "/Manual/Perfil", perfil);

        model.Alertas = registros == 0 ? new List<string> { "Nenhum registro carregado nesta tela. Use a ação principal ou aplique a massa demo em ambiente de desenvolvimento." } : Array.Empty<string>();
        return model;
    }

    private static AssistenteContextualViewModel CriarPadrao(string controller, string action, string perfil) { return new AssistenteContextualViewModel { Titulo = "Assistente PlantãoPro", Modulo = controller ?? string.Empty, Perfil = perfil ?? string.Empty, Prioridade = "NORMAL", Mensagem = "Você está em uma tela operacional do PlantãoPro Saúde 360. Revise os dados, resolva pendências e avance pelo próximo passo recomendado.", ProximaAcao = "Abrir fluxo de atendimento", LinkProximaAcao = "/ClinicaDashboard/FluxoAtendimento", Dicas = new List<string> { "Use os botões principais no topo da tela.", "Pendências críticas devem ser priorizadas no início do turno." } }; }
    private static void Configurar(AssistenteContextualViewModel model, string modulo, string mensagem, string acao, string link, string perfil) { model.Modulo = modulo; model.Mensagem = mensagem; model.ProximaAcao = acao; model.LinkProximaAcao = link; model.Perfil = perfil; }
}
