#!/usr/bin/env python3
"""Gate estrutural do shell v1.55; evita regressões de navegação e acessibilidade."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "backend/PlantaoPro.Web"
errors: list[str] = []

layout = (WEB / "Views/Shared/_Layout.cshtml").read_text(encoding="utf-8")
for marker in ("pp-app-shell", "pp-main-shell", "pp-content"):
    if marker not in layout:
        errors.append(f"_Layout.cshtml não contém {marker}")
if "pp-footer" not in (WEB / "Views/Shared/_AppFooter.cshtml").read_text(encoding="utf-8"):
    errors.append("_AppFooter.cshtml não contém pp-footer")

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

if errors:
    raise SystemExit("Falha no layout v1.55:\n- " + "\n- ".join(errors))
print("Layout v1.55 validado: shell, navegação e CSS sem regressões críticas.")
