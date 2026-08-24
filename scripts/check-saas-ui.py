#!/usr/bin/env python3
"""Contrato v1.84; Regressão das superfícies SaaS, assinatura e centrais operacionais v1.77."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "backend/PlantaoPro.Web"
errors: list[str] = []

# Evita a regressão que causou CS0263/CS0101/CS0111: o controller de
# assinatura deve existir uma única vez e manter sua rota/API no arquivo dedicado.
controllers = WEB / "Controllers"
controller_declarations: list[Path] = []
for path in controllers.rglob("*.cs"):
    if re.search(r"\bclass\s+MinhaAssinaturaController\b", path.read_text(encoding="utf-8")):
        controller_declarations.append(path)
if controller_declarations != [controllers / "MinhaAssinaturaController.cs"]:
    found = ", ".join(str(path.relative_to(WEB)) for path in controller_declarations) or "nenhuma"
    errors.append(f"MinhaAssinaturaController deve ter uma definição dedicada; encontrado: {found}")
else:
    assinatura_controller = controller_declarations[0].read_text(encoding="utf-8")
    for marker in ('[Route("MinhaAssinatura")]', "BaseWebController", '"api/minha-assinatura"', "MinhaAssinaturaViewModel"):
        if marker not in assinatura_controller:
            errors.append(f"MinhaAssinaturaController sem contrato obrigatório: {marker}")

faturamento_declarations = []
for path in controllers.rglob("*.cs"):
    if re.search(r"\bclass\s+FaturamentoClinicoController\b", path.read_text(encoding="utf-8")):
        faturamento_declarations.append(path)
if faturamento_declarations != [controllers / "FaturamentoClinicoController.cs"]:
    found = ", ".join(str(path.relative_to(WEB)) for path in faturamento_declarations) or "nenhuma"
    errors.append(f"FaturamentoClinicoController deve ter uma definição dedicada; encontrado: {found}")
else:
    faturamento = faturamento_declarations[0].read_text(encoding="utf-8")
    for marker in ('[Route("FaturamentoClinico")]', "BaseWebController", '"api/v115/faturamento/contas-receber"'):
        if marker not in faturamento:
            errors.append(f"FaturamentoClinicoController sem contrato obrigatório: {marker}")
required = {
    "Views/AdminSaas/Index.cshtml": ("pp-page", "pp-admin-governance", "data-admin-governance", "data-permissions-matrix", "data-admin-next-action"),
    "Views/AdminSaas/Dashboard.cshtml": ("pp-page-hero", "pp-checklist-grid", "pp-checklist-card"),
    "Views/B2BLaunch/Index.cshtml": ("pp-page-hero", "pp-clinical-grid", "pp-action-card"),
    "Views/Planos/Index.cshtml": ("pp-plan-grid", "pp-plan-card", "pp-feature-list"),
    "Views/Onboarding/NovoCliente.cshtml": ("pp-stepper", "pp-wizard-layout", "pp-form-grid", "asp-validation-summary"),
    "Views/Onboarding/Index.cshtml": ("pp-page-hero", "pp-stepper", "pp-section"),
}

landing = (WEB / "Views/CommercialDemoWeb/Landing.cshtml").read_text(encoding="utf-8")
for marker in ("pp-public-hero", "pp-public-card-grid", "pp-public-card", "pp-action-card", "pp-data-card"):
    if marker not in landing:
        errors.append(f"Landing comercial sem componente obrigatório: {marker}")

functional_surfaces = {
    "Views/Home/Dashboard.cshtml": ("pp-page", "pp-page-hero", "pp-kpi-strip", "_EmptyState", "pp-mobile-card"),
    "Views/Configuracoes/Index.cshtml": ("pp-page", "pp-governance-grid", "pp-governance-card", "data-configuration-groups"),
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

css = (WEB / "wwwroot/css/design-system/clinical.css").read_text(encoding="utf-8")
if "!important" in css:
    errors.append("clinical.css: uso de !important")

medical_css = (WEB / "wwwroot/css/design-system/clinical.css").read_text(encoding="utf-8")

for marker in (".pp-admin-layout", ".pp-kpi-grid--admin", ".pp-public-hero", ".pp-auth-shell"):
    if marker not in medical_css:
        errors.append(f"clinical.css: contrato v1.63 ausente: {marker}")
for marker in (".pp-public-card-grid", ".pp-auth-card", ".pp-form-field"):
    if marker not in medical_css:
        errors.append(f"clinical.css: acabamento premium v1.68 ausente: {marker}")

if errors:
    raise SystemExit("Falha na UI SaaS v1.77:\n- " + "\n- ".join(errors))
print("SaaS UI v1.81 validada: controllers críticos únicos, hero, planos, checklist e onboarding estruturados.")

# Contrato administrativo v1.81
admin = (WEB / "Views/AdminSaas/Index.cshtml").read_text(encoding="utf-8")
settings = (WEB / "Views/Configuracoes/Index.cshtml").read_text(encoding="utf-8")
reports = (WEB / "Views/Relatorios/Index.cshtml").read_text(encoding="utf-8")
smoke = (ROOT / "scripts/ui/visual-smoke.mjs").read_text(encoding="utf-8")
css181 = (WEB / "wwwroot/css/design-system/clinical.css").read_text(encoding="utf-8")
for marker, source in (("data-admin-governance", admin), ("data-permissions-matrix", admin), ("data-configuration-groups", settings), ("data-admin-reports-honest", reports), ("adminGovernanceVisible", smoke), ("screenshots/v185", smoke)):
    if marker not in source: raise SystemExit(f"Contrato v1.81 ausente: {marker}")
if "!important" in css181: raise SystemExit("clinical.css contém !important")
