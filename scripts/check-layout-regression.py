#!/usr/bin/env python3
"""Contrato v1.84; Gate estrutural do shell e do contrato de homologação visual v1.79.0."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "backend/PlantaoPro.Web"
errors: list[str] = []

layout = (WEB / "Views/Shared/_Layout.cshtml").read_text(encoding="utf-8")
for marker in ("pp-app-shell", "pp-main-shell", "pp-content", "pp-content-container"):
    if marker not in layout:
        errors.append(f"_Layout.cshtml não contém {marker}")
for marker in ('_DetailDrawer', 'detail-drawer.js'):
    if marker not in layout:
        errors.append(f"_Layout.cshtml não carrega {marker}")
if "pp-footer" not in (WEB / "Views/Shared/_AppFooter.cshtml").read_text(encoding="utf-8"):
    errors.append("_AppFooter.cshtml não contém pp-footer")
main_start = layout.find('class="pp-main-shell"')
footer_render = layout.find('_AppFooter', main_start)
main_end = layout.find("</main>", main_start)
if min(main_start, footer_render) < 0 or (main_end >= 0 and footer_render < main_end):
    errors.append("_Layout.cshtml: footer deve integrar pp-main-shell após o conteúdo principal")

for relative in ("Views/Shared/_AppTopbar.cshtml", "Views/Shared/_UserMenu.cshtml"):
    text = (WEB / relative).read_text(encoding="utf-8")
    if 'href="#"' in text:
        errors.append(f"{relative}: link placeholder href=#")
    if re.search(r"<button(?![^>]*\btype=)[^>]*>", text, re.I):
        errors.append(f"{relative}: button sem type")
    if relative.endswith("_UserMenu.cshtml") and re.search(r"<(?:ul|li)\b", text, re.I):
        errors.append(f"{relative}: menu não pode depender de lista crua")

css_files = list((WEB / "wwwroot/css/design-system").glob("v161-*.css"))
for css in css_files:
    for number, line in enumerate(css.read_text(encoding="utf-8").splitlines(), 1):
        if len(line) > 300:
            errors.append(f"{css.name}:{number}: linha CSS gigante ({len(line)} caracteres)")
        if "!important" in line:
            errors.append(f"{css.name}:{number}: uso novo de !important")

user_menu = (WEB / "Views/Shared/_UserMenu.cshtml").read_text(encoding="utf-8")
for marker in ("data-user-menu", 'role="menu"', "hidden", "aria-expanded"):
    if marker not in user_menu:
        errors.append(f"_UserMenu.cshtml não contém {marker}")

critical_views = (
    "Views/Home/Dashboard.cshtml", "Views/MinhaCentral/Index.cshtml",
    "Views/MeuDia/Index.cshtml", "Views/Agenda/Index.cshtml",
    "Views/Plantoes/Index.cshtml", "Views/Escalas/Index.cshtml",
    "Views/Financeiro/Index.cshtml", "Views/Saude360/Modulo.cshtml",
    "Views/Relatorios/Index.cshtml", "Views/Configuracoes/Index.cshtml",
    "Views/AdminSaas/Dashboard.cshtml", "Views/Planos/Index.cshtml",
    "Views/MinhaAssinatura/Index.cshtml",
    "Views/Onboarding/NovoCliente.cshtml",
)
for relative in critical_views:
    source = (WEB / relative).read_text(encoding="utf-8")
    if not any(marker in source for marker in ("pp-page", "_PageIntroduction", "clinical-workspace")):
        errors.append(f"{relative}: view crítica sem composição pp-page equivalente")
    if 'href="#"' in source:
        errors.append(f"{relative}: link placeholder href=#")
    if re.search(r"<button(?![^>]*\btype=)[^>]*>", source, re.I):
        errors.append(f"{relative}: button sem type")

responsive_tables = (
    "Views/Home/Dashboard.cshtml", "Views/Agenda/Index.cshtml",
    "Views/Plantoes/Index.cshtml", "Views/Escalas/Index.cshtml",
)
for relative in responsive_tables:
    source = (WEB / relative).read_text(encoding="utf-8")
    if "<table" in source and not any(marker in source for marker in ("table-responsive", "pp-mobile-card")):
        errors.append(f"{relative}: tabela crítica sem wrapper ou alternativa mobile")

medical_css = (WEB / "wwwroot/css/design-system/clinical.css").read_text(encoding="utf-8")
for pattern, message in (
    (r"\.pp-app-shell\s*\{[^}]*display:\s*grid[^}]*grid-template-columns:", "pp-app-shell deve usar grid no desktop"),
    (r"\.pp-sidebar\s*\{[^}]*position:\s*sticky", "pp-sidebar deve ser sticky no desktop"),
    (r"\.pp-content-container\s*\{", "pp-content-container deve limitar a largura interna"),
):
    if not re.search(pattern, medical_css, re.S):
        errors.append(f"clinical.css: {message}")
if not re.search(r"\.pp-content\s*\{[^}]*\bflex:\s*1", medical_css, re.S):
    errors.append("clinical.css: pp-content deve preservar flex: 1")

smoke = (ROOT / "scripts/ui/visual-smoke.mjs").read_text(encoding="utf-8")
for route in (
    "/", "/Account/Login", "/cadastro/empresa", "/Planos", "/AdminSaas/Index", "/Home/Dashboard", "/MinhaCentral", "/MeuDia",
    "/Agenda", "/Plantoes", "/Escalas", "/Saude360", "/Pacientes", "/Agendamentos",
    "/Triagem", "/Consultas", "/Pagamentos", "/Financeiro", "/Relatorios", "/Configuracoes", "/MinhaAssinatura",
):
    if route not in smoke:
        errors.append(f"visual-smoke.mjs sem rota obrigatória: {route}")
for viewport in ("360x800", "390x844", "430x932", "768x1024", "1024x768", "1366x768", "1440x900", "1920x1080"):
    if viewport not in smoke:
        errors.append(f"visual-smoke.mjs sem viewport obrigatório: {viewport}")
for contract in ("noHorizontalOverflow", "noClippedCards", "cardsInsideViewport", "responsiveTables", "accessibleDialogs",
                 "dialogsStartHidden", "overlaysOutOfFlow", "formsStructured", "fieldsHaveLabels",
                 "formsHaveLabels", "buttonsHaveType", "iconButtonsHaveAriaLabel", "drawersAccessible",
                 "commandPaletteAccessible", "notificationDrawerAccessible", "loginResponsive",
                 "selfServiceResponsive", "financialJourneyHonest", "clinicalJourneyClear", "clinicalMvpJourneyVisible", "patientContextVisible", "nextActionVisible", "triageRulesVisible", "consultationBillingActionHonest", "operationalJourneyClear", "actionsWithoutBackendDisabled", "noFakeValues", "noBrokenLinks",
                 "topbarDoesNotOverlap", "sidebarDoesNotOverlap", "pageContract", "shellClear",
                 "commandPaletteOpens", "commandPaletteCloses", "notificationDrawerOpens",
                 "notificationDrawerCloses", "notificationTriggerRegainsFocus", "subscriptionHonestState"):
    if contract not in smoke:
        errors.append(f"visual-smoke.mjs sem verificação: {contract}")
for output in ("screenshots/v185", "v185-visual-smoke-results.json", "v185-visual-smoke-summary.md"):
    if output not in smoke:
        errors.append(f"visual-smoke.mjs sem saída v1.79.0: {output}")

notification_menu = (WEB / "Views/Shared/_NotificationMenu.cshtml").read_text(encoding="utf-8")
notification_drawer = (WEB / "Views/Shared/_NotificationDrawer.cshtml").read_text(encoding="utf-8")
notification_js = (WEB / "wwwroot/js/components/notification-drawer.js").read_text(encoding="utf-8")
for marker in ('aria-label=', 'aria-controls="notificationDrawer"', 'data-notification-count-value'):
    if marker not in notification_menu:
        errors.append(f"_NotificationMenu.cshtml sem contrato acessível: {marker}")
for marker in ('role="dialog"', 'aria-modal="true"', 'aria-live="polite"', 'hidden'):
    if marker not in notification_drawer:
        errors.append(f"_NotificationDrawer.cshtml sem contrato acessível: {marker}")
for marker in ("safeDestination", "same-origin", "textContent", "createElement", "Escape"):
    if marker not in notification_js:
        errors.append(f"notification-drawer.js sem contrato seguro: {marker}")
if ".innerHTML" in notification_js:
    errors.append("notification-drawer.js usa innerHTML")

if errors:
    raise SystemExit("Falha no layout v1.79.0:\n- " + "\n- ".join(errors))
print("Layout v1.80 validado: shell, jornadas, notificações e contrato executável de smoke sem regressões críticas.")
