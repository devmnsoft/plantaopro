#!/usr/bin/env python3
"""Valida região viva e contrato do diálogo de confirmação sem APIs nativas."""
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "backend/PlantaoPro.Web"
files = {
    "modal": WEB / "Views/Shared/_ConfirmModal.cshtml",
    "toast": WEB / "Views/Shared/_ToastRegion.cshtml",
    "ui": WEB / "wwwroot/js/plantaopro-ui.js",
}
texts = {name: path.read_text(encoding="utf-8") for name, path in files.items()}
errors: list[str] = []

for marker in ('role="dialog"', 'aria-modal="true"', "data-pp-confirm-action", "data-pp-confirm-loading"):
    if marker not in texts["modal"]:
        errors.append(f"Modal sem {marker}")
if 'aria-live="polite"' not in texts["toast"]:
    errors.append("Toast region sem aria-live")
for name, text in texts.items():
    if re.search(r"\b(?:alert|confirm)\s*\(", text):
        errors.append(f"{name}: API nativa alert/confirm detectada")

changed = subprocess.run(
    ["git", "diff", "--name-only", "HEAD"], cwd=ROOT, text=True,
    capture_output=True, check=True,
).stdout.splitlines()
rules = {
    "alert/confirm nativo": re.compile(r"(?<![\w.])(?:alert|confirm)\s*\("),
    "href placeholder": re.compile(r'href\s*=\s*["\']#["\']', re.I),
    "button sem type": re.compile(r"<button(?![^>]*\btype=)[^>]*>", re.I),
}
for relative in changed:
    path = ROOT / relative
    if path.suffix not in (".js", ".cshtml") or not path.exists() or WEB not in path.parents:
        continue
    source = path.read_text(encoding="utf-8")
    for label, pattern in rules.items():
        if pattern.search(source):
            errors.append(f"{relative}: {label}")

if errors:
    raise SystemExit("Falha no feedback UI:\n- " + "\n- ".join(errors))
print("Feedback UI v1.54 validado: toast acessível e confirmação sem API nativa.")
