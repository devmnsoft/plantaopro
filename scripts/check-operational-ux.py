#!/usr/bin/env python3
"""Gate estático dos contratos operacionais e drawers da v1.58."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "backend/PlantaoPro.Web"
errors: list[str] = []

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
for marker in ("data-filter-priority", "data-filter-type", "data-filter-due", "data-filter-owner"):
    if marker not in central:
        errors.append(f"Minha Central sem filtro operacional: {marker}")

if errors:
    raise SystemExit("Falha na UX operacional v1.58:\n- " + "\n- ".join(errors))
print("UX operacional v1.58 validada: drawers acessíveis, ações reais e superfícies responsivas.")
