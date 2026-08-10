#!/usr/bin/env python3
"""Valida CTAs estruturais das jornadas v1.52 sem substituir testes de runtime."""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "backend" / "PlantaoPro.Web"
VIEWS = WEB / "Views"
CONTROLLERS = WEB / "Controllers"
targets = [
    VIEWS / "Shared/_AppSidebar.cshtml", VIEWS / "Shared/_AppTopbar.cshtml",
    VIEWS / "Shared/_MobileNavigation.cshtml", VIEWS / "ClinicaDashboard/FluxoAtendimento.cshtml",
    VIEWS / "Relatorios/Index.cshtml", VIEWS / "Configuracoes/Index.cshtml",
]
errors = []
controller_text = "\n".join(p.read_text(encoding="utf-8") for p in CONTROLLERS.glob("*.cs"))
for view in targets:
    if not view.exists():
        errors.append(f"arquivo obrigatório ausente: {view.relative_to(ROOT)}")
        continue
    text = view.read_text(encoding="utf-8")
    for match in re.finditer(r'href\s*=\s*["\']\s*#["\']', text, re.I):
        errors.append(f"href vazio em {view.relative_to(ROOT)}:{text[:match.start()].count(chr(10))+1}")
    for tag in re.findall(r"<button\b[^>]*>", text, re.I):
        if not re.search(r"\b(type|form|data-[\w-]+|disabled)\s*=", tag, re.I):
            errors.append(f"botão sem contrato de ação em {view.relative_to(ROOT)}: {tag[:100]}")
    for controller in re.findall(r'asp-controller\s*=\s*["\']([^"\']+)', text, re.I):
        if controller.startswith("@"):
            continue  # expressão Razor: o contrato é resolvido pelo view model em runtime
        if not re.search(rf"class\s+{re.escape(controller)}Controller\b", controller_text, re.I):
            errors.append(f"controller não encontrado em {view.relative_to(ROOT)}: {controller}")
if errors:
    print("\n".join(f"ERRO: {error}" for error in errors), file=sys.stderr)
    raise SystemExit(1)
print(f"Rotas v1.52: {len(targets)} superfícies e seus CTAs estruturais validados.")
