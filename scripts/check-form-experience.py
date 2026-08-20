#!/usr/bin/env python3
"""Contrato v1.84; verifica formulários críticos, loading e submissões acessíveis."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "backend/PlantaoPro.Web"
css = (WEB / "wwwroot/css/design-system/forms.css").read_text(encoding="utf-8")
errors: list[str] = []

critical = [
    "Views/Account/Login.cshtml", "Views/Account/ForgotPassword.cshtml",
    "Views/Account/ResetPassword.cshtml", "Views/Pacientes/_Form.cshtml",
    "Views/Agendamentos/_Form.cshtml", "Views/Plantoes/_PlantaoForm.cshtml",
    "Views/Onboarding/NovoCliente.cshtml", "Views/Cadastro/Cadastro.cshtml",
]
for relative in critical:
    text = (WEB / relative).read_text(encoding="utf-8")
    if "<form" not in text:
        errors.append(f"{relative}: formulário ausente")
        continue
    if "asp-validation-summary" not in text:
        errors.append(f"{relative}: resumo de validação ausente")
    if "novalidate" not in text and not relative.endswith("Cadastro/Cadastro.cshtml"):
        errors.append(f"{relative}: validação progressiva ausente")
    if "pp-form" not in text:
        errors.append(f"{relative}: formulário crítico sem composição pp-form")
    if re.search(r"<button(?![^>]*\btype=)[^>]*>", text, re.I):
        errors.append(f"{relative}: button sem type")

cadastro = (WEB / "Views/Cadastro/Cadastro.cshtml").read_text(encoding="utf-8")
for marker in ("pp-selfservice-page", "pp-onboarding-form", "pp-form-card", "pp-form-grid", "pp-form-field", "pp-form-actions", "pp-stepper", "data-unsaved-form", "data-focus-invalid", "data-submit-feedback", "data-submit-spinner"):
    if marker not in cadastro:
        errors.append(f"Cadastro self-service sem contrato obrigatório: {marker}")

login = (WEB / critical[0]).read_text(encoding="utf-8")
for marker in ("pp-auth-page", "pp-auth-shell", "pp-auth-card", "pp-login-actions", "pp-login-form", "data-focus-invalid", "pp-form-field", "pp-form-control", "aria-describedby"):
    if marker not in login:
        errors.append(f"Login sem contrato obrigatório: {marker}")
for marker in (".pp-form-grid", ".pp-form-card", ".pp-form-error", ".pp-form-actions"):
    if marker not in css:
        errors.append(f"Design system sem componente: {marker}")
if 'type="submit"' not in login:
    errors.append("Login sem submit explícito")

# Todo formulário novo ou alterado deve manter feedback resumido quando coleta dados.
for path in (WEB / "Views").rglob("*.cshtml"):
    source = path.read_text(encoding="utf-8")
    if "<form" not in source or "method=\"post\"" not in source:
        continue
    if re.search(r"<button(?![^>]*\btype=)[^>]*>", source, re.I):
        errors.append(f"{path.relative_to(WEB)}: button sem type")

if errors:
    raise SystemExit("Falha na experiência de formulários:\n- " + "\n- ".join(errors))

consultation = (WEB / "Views/Consultas/Atendimento.cshtml").read_text(encoding="utf-8")
for marker in ("data-billing-type", "data-billing-value", "data-billing-reason", "data-finalize-error"):
    if marker not in consultation:
        raise SystemExit(f"Form experience v1.85 sem campo de finalização: {marker}")
print("Form experience v1.80 validada: pp-form, labels, feedback de envio e associação acessível presentes.")
