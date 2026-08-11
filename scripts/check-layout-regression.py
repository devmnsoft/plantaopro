#!/usr/bin/env python3
"""Gate estrutural do shell v1.55 e overlays operacionais v1.57."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "backend/PlantaoPro.Web"
errors: list[str] = []

layout = (WEB / "Views/Shared/_Layout.cshtml").read_text(encoding="utf-8")
for marker in ("pp-app-shell", "pp-main-shell", "pp-content"):
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

css_files = list((WEB / "wwwroot/css/design-system").glob("v155-*.css"))
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

medical_css = (WEB / "wwwroot/css/design-system/v155-medical-experience.css").read_text(encoding="utf-8")
if not re.search(r"\.pp-content\s*\{[^}]*\bflex:\s*1", medical_css, re.S):
    errors.append("v155-medical-experience.css: pp-content deve preservar flex: 1")

if errors:
    raise SystemExit("Falha no layout v1.55:\n- " + "\n- ".join(errors))
print("Layout v1.55 validado: shell, navegação e CSS sem regressões críticas.")
