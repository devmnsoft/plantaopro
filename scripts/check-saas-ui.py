#!/usr/bin/env python3
"""Regressão das superfícies SaaS e centrais operacionais até a v1.57."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "backend/PlantaoPro.Web"
errors: list[str] = []
required = {
    "Views/AdminSaas/Index.cshtml": ("pp-page", "pp-admin-saas-page", "pp-page-hero", "pp-kpi-grid", "pp-section-grid"),
    "Views/AdminSaas/Dashboard.cshtml": ("pp-page-hero", "pp-checklist-grid", "pp-checklist-card"),
    "Views/B2BLaunch/Index.cshtml": ("pp-page-hero", "pp-clinical-grid", "pp-action-card"),
    "Views/Planos/Index.cshtml": ("pp-plan-grid", "pp-plan-card", "pp-feature-list"),
    "Views/Onboarding/NovoCliente.cshtml": ("pp-stepper", "pp-wizard-layout", "pp-form-grid", "asp-validation-summary"),
    "Views/Onboarding/Index.cshtml": ("pp-page-hero", "pp-stepper", "pp-section"),
}

functional_surfaces = {
    "Views/Home/Dashboard.cshtml": ("pp-page", "pp-page-hero", "pp-kpi-strip", "_EmptyState", "pp-mobile-card"),
    "Views/Configuracoes/Index.cshtml": ("pp-page", "pp-action-grid", "pp-action-card"),
    "Views/Planos/Index.cshtml": ("pp-page", "pp-plan-grid", "pp-plan-card"),
    "Views/Plantoes/Index.cshtml": ("_PageIntroduction", "data-detail-open", "pp-mobile-card"),
    "Views/Escalas/Index.cshtml": ("_PageIntroduction", "data-detail-open", "table-responsive"),
}

for relative, markers in required.items():
    text = (WEB / relative).read_text(encoding="utf-8")
    for marker in markers:
        if marker not in text:
            errors.append(f"{relative}: componente obrigatório ausente: {marker}")
    if 'href="#"' in text:
        errors.append(f"{relative}: link placeholder href=#")
    if re.search(r"\b(?:alert|confirm)\s*\(", text):
        errors.append(f"{relative}: API nativa de feedback")
    for match in re.finditer(r"<button\b([^>]*)>", text, re.I | re.S):
        if not re.search(r"\btype\s*=", match.group(1), re.I):
            errors.append(f"{relative}: button sem type")

for relative, markers in functional_surfaces.items():
    text = (WEB / relative).read_text(encoding="utf-8")
    for marker in markers:
        if marker not in text:
            errors.append(f"{relative}: superfície funcional ausente: {marker}")

plans = (WEB / "Views/Planos/Index.cshtml").read_text(encoding="utf-8")
if "Model.Planos.Items" not in plans:
    errors.append("Planos/Index: cards não usam o catálogo real")

onboarding = (WEB / "Views/Onboarding/NovoCliente.cshtml").read_text(encoding="utf-8")
if onboarding.count("data-step=") != 5:
    errors.append("Onboarding: stepper deve declarar cinco etapas")
for field in ("Cnpj", "PlanoId", "UnidadeNome", "UsuarioEmail"):
    if f'asp-for="{field}"' not in onboarding or f'{field}-error' not in onboarding:
        errors.append(f"Onboarding: campo {field} sem erro associado")

css = (WEB / "wwwroot/css/design-system/v154-clinical-pages.css").read_text(encoding="utf-8")
if "!important" in css:
    errors.append("v154-clinical-pages.css: uso de !important")
for line_number, line in enumerate(css.splitlines(), 1):
    if len(line) > 300:
        errors.append(f"v154-clinical-pages.css:{line_number}: CSS em linha gigante")

if errors:
    raise SystemExit("Falha na UI SaaS v1.54:\n- " + "\n- ".join(errors))
print("SaaS UI v1.61 validada: hero, planos, checklist e onboarding estruturados.")
