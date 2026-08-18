#!/usr/bin/env python3
"""Contrato v1.84; Gate estático v1.79 das jornadas clínicas e operacionais."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "backend/PlantaoPro.Web"
errors: list[str] = []
layout = (WEB / "Views/Shared/_Layout.cshtml").read_text(encoding="utf-8")
if "pp-content-container" not in layout:
    errors.append("_Layout.cshtml sem container responsivo do conteúdo autenticado")


drawer = (WEB / "Views/Shared/_DetailDrawer.cshtml").read_text(encoding="utf-8")
drawer_js = (WEB / "wwwroot/js/detail-drawer.js").read_text(encoding="utf-8")
drawer_css = (WEB / "wwwroot/css/design-system/drawers.css").read_text(encoding="utf-8")
for marker in ('pp-detail-drawer', 'role="dialog"', 'aria-modal="true"', 'aria-live="polite"', 'data-detail-loading', 'data-detail-error', 'data-detail-timeline', 'data-detail-actions'):
    if marker not in drawer:
        errors.append(f"_DetailDrawer.cshtml sem contrato obrigatório: {marker}")
for marker in ("Escape", "trigger?.focus()", "aria-busy", "document.createElement", "detailPrimaryUrl"):
    if marker not in drawer_js:
        errors.append(f"detail-drawer.js sem comportamento obrigatório: {marker}")
if "@media(max-width:600px)" not in drawer_css or "height:100dvh" not in drawer_css:
    errors.append("drawers.css sem drawer full-screen no mobile")

critical = (
    "Views/Plantoes/Index.cshtml", "Views/Escalas/Index.cshtml",
    "Views/MinhaCentral/Index.cshtml", "Views/Saude360/Modulo.cshtml",
    "Views/Pacientes/Index.cshtml", "Views/Agendamentos/Index.cshtml",
    "Views/Consultas/Index.cshtml", "Views/Financeiro/Index.cshtml",
    "Views/Pagamentos/Index.cshtml", "Views/Convites/Index.cshtml",
    "Views/Relatorios/Index.cshtml", "Views/Configuracoes/Index.cshtml",
)
for relative in critical:
    source = (WEB / relative).read_text(encoding="utf-8")
    if not any(marker in source for marker in ("pp-page", "_PageIntroduction", "premium-workspace", "Saude360/Modulo", "clinical-page")):
        errors.append(f"{relative}: view crítica sem pp-page ou composição equivalente")
    if re.search(r'href\s*=\s*["\']#["\']', source, re.I):
        errors.append(f"{relative}: href placeholder")
    if re.search(r"\b(?:alert|confirm)\s*\(", source):
        errors.append(f"{relative}: API nativa alert/confirm")
    if re.search(r"<button(?![^>]*\btype=)[^>]*>", source, re.I):
        errors.append(f"{relative}: button sem type")
    if "<table" in source and not any(marker in source for marker in ("table-responsive", "pp-mobile-card", "data-label=")):
        errors.append(f"{relative}: tabela sem wrapper responsivo ou cards mobile")

for relative in ("Views/Plantoes/Index.cshtml", "Views/Escalas/Index.cshtml"):
    if "data-detail-open" not in (WEB / relative).read_text(encoding="utf-8"):
        errors.append(f"{relative}: sem abertura do drawer operacional")

work_drawer = (WEB / "Views/MinhaCentral/_WorkItemDrawer.cshtml").read_text(encoding="utf-8")
work_drawer_js = (WEB / "wwwroot/js/components/work-item-drawer.js").read_text(encoding="utf-8")
central = (WEB / "Views/MinhaCentral/Index.cshtml").read_text(encoding="utf-8")
for marker in ('role="dialog"', 'aria-describedby=', 'data-work-item-loading', 'data-work-item-error', 'data-work-item-history', 'data-work-item-actions'):
    if marker not in work_drawer:
        errors.append(f"_WorkItemDrawer.cshtml sem contrato v1.58: {marker}")
for marker in ("/assumir", "/concluir", "/adiar", "/reabrir", "/comentar", "/historico", "textContent", "response.status===409", "response.status===403"):
    if marker not in work_drawer_js:
        errors.append(f"work-item-drawer.js sem ação/tratamento real: {marker}")
if "innerHTML" in work_drawer_js:
    errors.append("work-item-drawer.js interpola HTML; use construção segura do DOM")

agenda = (WEB / "Views/Agendamentos/AgendaPremium.cshtml").read_text(encoding="utf-8")
agenda_js = (WEB / "wwwroot/js/pages/agendamentos.js").read_text(encoding="utf-8")
agenda_controller = (WEB / "Controllers/Saude360WebControllers.cs").read_text(encoding="utf-8")
for marker in ('role="dialog"', 'aria-describedby="agendaActionDescription"', 'data-agenda-confirm', 'data-agenda-reason'):
    if marker not in agenda:
        errors.append(f"Agenda clínica sem confirmação acessível: {marker}")
for marker in ("RequestVerificationToken", "aria-busy", "response.ok", "textContent"):
    if marker not in agenda_js:
        errors.append(f"agendamentos.js sem contrato de ação real: {marker}")
if "ExecutarAcao" not in agenda_controller or "ValidateAntiForgeryToken" not in agenda_controller:
    errors.append("AgendamentosController sem BFF protegido para ações operacionais")
for marker in ("data-detail-open", "TipoAtendimento", "Convenio", "Sala", "Tempo de espera", "Atraso", "Próxima ação", "Reagendar", "Abrir triagem", "Abrir consulta"):
    if marker not in agenda:
        errors.append(f"Agenda clínica sem contexto da recepção v1.60: {marker}")

saude = (WEB / "Views/Saude360/Modulo.cshtml").read_text(encoding="utf-8")
for etapa in ("Paciente", "Agendamento", "Check-in", "Chamada", "Triagem", "Consulta", "Prescrição", "Financeiro"):
    if f"<span>{etapa}</span>" not in saude:
        errors.append(f"Saúde 360 sem etapa real da jornada: {etapa}")

fechamentos = (WEB / "Views/OperacaoPremium/Fechamentos.cshtml").read_text(encoding="utf-8")
for etapa in ("Plantão realizado", "Divergências", "Conferência", "Aprovação", "Financeiro", "Pagamento"):
    if etapa not in fechamentos:
        errors.append(f"Fechamentos sem etapa operacional: {etapa}")
for marker in ("Model.Pendentes.Any()", "table-responsive", "data-label=", "Model.Timeline.Any()"):
    if marker not in fechamentos:
        errors.append(f"Fechamentos sem contrato de dados reais/responsivo: {marker}")

forms = (WEB / "Views/Saude360/Formulario.cshtml").read_text(encoding="utf-8")
models = (WEB / "Models/Saude360WebViewModels.cs").read_text(encoding="utf-8")
for marker in ('min="50" max="260"', 'min="30" max="45"', 'min="50" max="100"'):
    if marker not in forms:
        errors.append(f"Triagem sem limite clínico no formulário: {marker}")
for marker in ("ValidarTriagem", "classificação de risco", "alto risco"):
    if marker.lower() not in models.lower():
        errors.append(f"Triagem sem validação server-side: {marker}")

palette = (WEB / "wwwroot/js/command-palette.js").read_text(encoding="utf-8")
for marker in ("ctrlKey", "metaKey", "Escape", "activeTrigger.focus()", "/GlobalSearch", "querySelectorAll('[data-command-open]')", "aria-activedescendant", "ArrowDown", "aria-selected", "url.origin === window.location.origin"):
    if marker not in palette:
        errors.append(f"Command Palette sem contrato acessível/real: {marker}")
for marker in ("data-filter-priority", "data-filter-type", "data-filter-due", "data-filter-owner"):
    if marker not in central:
        errors.append(f"Minha Central sem filtro operacional: {marker}")

# O fechamento v1.78 exige destinos reais e ausência financeira tratada honestamente.
faturamento_controller = (WEB / "Controllers/FaturamentoClinicoController.cs").read_text(encoding="utf-8")
saude_controllers = (WEB / "Controllers/Saude360WebControllers.cs").read_text(encoding="utf-8")
faturamento_view = (WEB / "Views/FaturamentoClinico/Index.cshtml").read_text(encoding="utf-8")
pagamentos_view = (WEB / "Views/Pagamentos/Index.cshtml").read_text(encoding="utf-8")
for marker in ('[Route("FaturamentoClinico")]', '[HttpGet("")]', '[HttpGet("Index")]'):
    if marker not in faturamento_controller:
        errors.append(f"Faturamento clínico sem rota real: {marker}")
for marker in ("class ConsultasController", "class ClinicaFinanceiroController", "Task<IActionResult> ContasReceber()"):
    if marker not in saude_controllers:
        errors.append(f"Jornada consulta/financeiro sem destino real: {marker}")
for marker in ('asp-controller="Consultas"', 'asp-controller="Financeiro"', 'asp-controller="Pagamentos"', 'asp-controller="ClinicaFinanceiro"'):
    if marker not in faturamento_view:
        errors.append(f"Faturamento sem navegação operacional: {marker}")
for marker in ("ValorPago.HasValue", "Não informado"):
    if marker not in pagamentos_view:
        errors.append(f"Pagamentos não preserva ausência de valor: {marker}")

# Contratos visuais críticos v1.65 também protegem a navegação operacional.
layout = (WEB / "Views/Shared/_Layout.cshtml").read_text(encoding="utf-8")
portal = (WEB / "Views/Shared/_OverlayPortal.cshtml").read_text(encoding="utf-8")
for required_marker in ("pp-app-shell", "pp-main-shell", "pp-content"):
    if required_marker not in layout:
        errors.append(f"Shell operacional sem {required_marker}")
if "pp-overlay-root" not in portal:
    errors.append("Shell operacional sem portal global de overlays")
for drawer_view in ("Views/Shared/_DetailDrawer.cshtml", "Views/MinhaCentral/_WorkItemDrawer.cshtml"):
    source = (WEB / drawer_view).read_text(encoding="utf-8")
    if 'role="dialog"' not in source or 'aria-modal="true"' not in source:
        errors.append(f"{drawer_view}: drawer sem semântica modal acessível")

if errors:
    raise SystemExit("Falha na UX operacional v1.78:\n- " + "\n- ".join(errors))
for marker in ("clinicalJourneyClear", "clinicalMvpJourneyVisible", "nextActionVisible", "triageRulesVisible", "consultationBillingActionHonest", "operationalJourneyClear", "operationalMvpJourneyVisible", "shiftCoverageStatusVisible", "scheduleNextActionVisible", "invitationActionsHonest", "substitutionRulesVisible", "closingBusinessRulesVisible", "closingFinanceActionHonest", "operationalRiskVisible", "actionsWithoutBackendDisabled", "noFakeValues", "noBrokenLinks", "screenshots/v184", "version: '1.84.0'"):
    smoke = (ROOT / "scripts/ui/visual-smoke.mjs").read_text(encoding="utf-8")
    if marker not in smoke:
        errors.append(f"Smoke v1.78 sem contrato operacional: {marker}")

if errors:
    raise SystemExit("Falha na UX operacional v1.78:\n- " + "\n- ".join(errors))
operational_files = {
    "Views/Plantoes/Index.cshtml": ("Cobertura", "Risco", "Próxima ação"),
    "Views/Escalas/Index.cshtml": ("Conflito", "Próxima ação", "data-operational-mvp-journey"),
    "Views/OperacaoPremium/Fechamentos.cshtml": ("data-closing-business-rules", "Gerar financeiro", "disabled"),
    "Views/Convites/Index.cshtml": ("DataEnvio", "data-next-action", "Reenviar convite"),
}
for relative, markers in operational_files.items():
    source = (WEB / relative).read_text(encoding="utf-8")
    for marker in markers:
        if marker not in source:
            errors.append(f"{relative}: contrato operacional v1.79 ausente: {marker}")
if errors:
    raise SystemExit("Falha na UX operacional v1.80:\n- " + "\n- ".join(errors))
print("UX operacional v1.80 validada: cobertura, convites, escalas, substituições e fechamento honesto.")
