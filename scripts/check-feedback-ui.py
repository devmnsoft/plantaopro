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
    if path.suffix == ".cshtml":
        for match in re.finditer(r"<button\b([^>]*)>\s*<(?:i|app-icon)\b", source, re.I | re.S):
            attributes = match.group(1)
            if "aria-label" not in attributes and not re.search(r"</(?:i|app-icon)>\s*\S", source[match.end():match.end() + 120], re.S):
                errors.append(f"{relative}: botão apenas com ícone sem aria-label")

for css_path in (WEB / "wwwroot/css").rglob("*.css"):
    relative = str(css_path.relative_to(ROOT))
    if relative not in changed:
        continue
    for number, line in enumerate(css_path.read_text(encoding="utf-8").splitlines(), 1):
        if "!important" in line:
            errors.append(f"{relative}:{number}: novo CSS com !important")

if errors:
    raise SystemExit("Falha no feedback UI:\n- " + "\n- ".join(errors))
print("Feedback UI v1.54 validado: toast acessível e confirmação sem API nativa.")
