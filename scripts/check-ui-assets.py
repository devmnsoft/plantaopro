#!/usr/bin/env python3
"""Fail fast when first-party UI asset contracts are violated."""
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "backend/PlantaoPro.Web"
errors = []

for path in WEB.rglob("*"):
    if path.suffix.lower() not in {".cshtml", ".html", ".js", ".css"}:
        continue
    text = path.read_text(encoding="utf-8", errors="ignore")
    relative = path.relative_to(ROOT)
    checks = {
        "Bootstrap Icons remoto": r"cdn\.jsdelivr\.net/npm/bootstrap-icons",
        "Font Awesome": r"font-awesome|fontawesome",
        "confirmação nativa": r"(?<![\w$])(?:window\.)?confirm\s*\(",
        "alerta nativo": r"(?<![\w$])(?:window\.)?alert\s*\(",
    }
    for message, pattern in checks.items():
        if re.search(pattern, text, re.IGNORECASE):
            errors.append(f"{relative}: {message}")

sprite = WEB / "wwwroot/assets/icons/sprite.svg"
try:
    tree = ET.parse(sprite)
    symbols = tree.findall(".//{http://www.w3.org/2000/svg}symbol")
    if not symbols or any("viewBox" not in symbol.attrib for symbol in symbols):
        errors.append("sprite.svg: symbol ausente ou sem viewBox")
except (ET.ParseError, OSError) as exception:
    errors.append(f"sprite.svg inválido: {exception}")

for svg in (WEB / "wwwroot").rglob("*.svg"):
    text = svg.read_text(encoding="utf-8", errors="ignore")
    if re.search(r"<script|\son[a-z]+\s*=|https?://(?!www\.w3\.org/2000/svg)", text, re.IGNORECASE):
        errors.append(f"{svg.relative_to(ROOT)}: SVG inseguro")

if errors:
    print("UI asset gate reprovado:\n- " + "\n- ".join(errors))
    sys.exit(1)
print("UI asset gate aprovado: sprite e mídia crítica são locais e seguros.")
