#!/usr/bin/env python3
"""Gate estrutural do shell v1.54; evita regressões que voltam a expor listas cruas."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "backend/PlantaoPro.Web"
errors: list[str] = []

layout = (WEB / "Views/Shared/_Layout.cshtml").read_text(encoding="utf-8")
for marker in ("pp-app", "pp-main", "pp-content"):
    if marker not in layout:
        errors.append(f"_Layout.cshtml não contém {marker}")

for relative in ("Views/Shared/_AppTopbar.cshtml", "Views/Shared/_UserMenu.cshtml"):
    text = (WEB / relative).read_text(encoding="utf-8")
    if 'href="#"' in text:
        errors.append(f"{relative}: link placeholder href=#")
    if re.search(r"<button(?![^>]*\btype=)[^>]*>", text, re.I):
        errors.append(f"{relative}: button sem type")

css_files = list((WEB / "wwwroot/css/design-system").glob("v154-*.css"))
for css in css_files:
    for number, line in enumerate(css.read_text(encoding="utf-8").splitlines(), 1):
        if len(line) > 300:
            errors.append(f"{css.name}:{number}: linha CSS gigante ({len(line)} caracteres)")

if errors:
    raise SystemExit("Falha no layout v1.54:\n- " + "\n- ".join(errors))
print("Layout v1.54 validado: shell, navegação e CSS sem regressões críticas.")
