#!/usr/bin/env python3
"""Fail fast when first-party UI asset contracts are violated."""
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "backend/PlantaoPro.Web"
errors = []


def parse_icon_contract():
    enum_path = WEB / "DesignSystem/AppIconKey.cs"
    registry_path = WEB / "DesignSystem/AppIconRegistry.cs"
    enum_text = enum_path.read_text(encoding="utf-8")
    registry_text = registry_path.read_text(encoding="utf-8")
    enum_body = re.search(r"enum\s+AppIconKey\s*\{(?P<body>.*?)\}", enum_text, re.DOTALL)
    if enum_body is None:
        errors.append("AppIconKey.cs: enum AppIconKey não encontrado")
        return set(), {}

    keys = set(re.findall(r"\b[A-Z][A-Za-z0-9_]*\b", enum_body.group("body")))
    registrations = dict(re.findall(
        r"\[AppIconKey\.([A-Za-z0-9_]+)\]\s*=\s*new\(AppIconKey\.\1,\s*\"([^\"]+)\"\)",
        registry_text,
    ))
    return keys, registrations


icon_keys, icon_registrations = parse_icon_contract()

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
    if path.suffix.lower() == ".cshtml":
        for icon_name in re.findall(r'<app-icon\b[^>]*\bname\s*=\s*"([^"]+)"', text, re.IGNORECASE):
            if icon_name not in icon_keys:
                errors.append(f"{relative}: AppIconKey inválida: {icon_name}")
            elif icon_name not in icon_registrations:
                errors.append(f"{relative}: AppIconKey sem registro: {icon_name}")

sprite = WEB / "wwwroot/assets/icons/sprite.svg"
try:
    tree = ET.parse(sprite)
    symbols = tree.findall(".//{http://www.w3.org/2000/svg}symbol")
    symbol_ids = {symbol.attrib.get("id") for symbol in symbols}
    if not symbols or any("viewBox" not in symbol.attrib for symbol in symbols):
        errors.append("sprite.svg: symbol ausente ou sem viewBox")
    for icon_name, symbol_id in icon_registrations.items():
        if symbol_id not in symbol_ids:
            errors.append(f"sprite.svg: symbol '{symbol_id}' ausente para AppIconKey.{icon_name}")
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
