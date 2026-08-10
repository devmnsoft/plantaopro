#!/usr/bin/env python3
"""Validate the v1.53 critical forms contract without failing untouched legacy views."""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
critical = [
    "backend/PlantaoPro.Web/Views/Account/Login.cshtml",
    "backend/PlantaoPro.Web/Views/Account/ForgotPassword.cshtml",
    "backend/PlantaoPro.Web/Views/Account/ResetPassword.cshtml",
    "backend/PlantaoPro.Web/Views/Pacientes/_Form.cshtml",
    "backend/PlantaoPro.Web/Views/Agendamentos/_Form.cshtml",
    "backend/PlantaoPro.Web/Views/Plantoes/_PlantaoForm.cshtml",
]
issues = []
for relative in critical:
    path = ROOT / relative
    text = path.read_text(encoding="utf-8")
    if "<form" not in text: issues.append(f"{relative}: formulário ausente"); continue
    if "asp-validation-summary" not in text: issues.append(f"{relative}: resumo de validação ausente")
    if "novalidate" not in text: issues.append(f"{relative}: contrato de validação progressiva ausente")
    for match in re.finditer(r"<button(?![^>]*\btype=)[^>]*>", text, re.I):
        issues.append(f"{relative}:{text.count(chr(10), 0, match.start()) + 1}: botão sem type")
    if re.search(r"class=[\"'][^\"']*alert alert-", text): issues.append(f"{relative}: alerta Bootstrap cru")
print(f"Form experience: {len(issues)} ocorrência(s) bloqueadora(s) em {len(critical)} formulários críticos.")
for issue in issues: print(f"- {issue}")
sys.exit(1 if issues else 0)
