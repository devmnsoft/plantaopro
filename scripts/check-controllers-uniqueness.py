#!/usr/bin/env python3
"""Detecta controllers duplicados e actions Index conflitantes antes do build."""
from collections import defaultdict
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
CONTROLLERS = ROOT / "backend/PlantaoPro.Web/Controllers"
DECLARATION = re.compile(
    r"\b(?P<partial>partial\s+)?class\s+(?P<name>[A-Za-z_][A-Za-z0-9_]*Controller)\b"
    r"(?:\s*:\s*(?P<bases>[^\{\r\n]+))?\s*\{"
)
INDEX_ACTION = re.compile(
    r"\b(?:async\s+)?(?:Task\s*<\s*IActionResult\s*>|IActionResult|ActionResult)\s+Index\s*\((?P<parameters>[^)]*)\)"
)


def class_body(text: str, opening_brace: int) -> str:
    depth = 0
    for position in range(opening_brace, len(text)):
        if text[position] == "{":
            depth += 1
        elif text[position] == "}":
            depth -= 1
            if depth == 0:
                return text[opening_brace + 1:position]
    return text[opening_brace + 1:]


declarations: dict[str, list[tuple[Path, bool, str, str]]] = defaultdict(list)
errors: list[str] = []
for path in sorted(CONTROLLERS.rglob("*.cs")):
    text = path.read_text(encoding="utf-8", errors="ignore")
    for match in DECLARATION.finditer(text):
        body = class_body(text, match.end() - 1)
        bases = " ".join((match.group("bases") or "").split())
        declarations[match.group("name")].append((path, bool(match.group("partial")), bases, body))

for name, parts in sorted(declarations.items()):
    paths = sorted({part[0] for part in parts})
    if len(paths) > 1:
        locations = ", ".join(str(path.relative_to(ROOT)) for path in paths)
        if not all(part[1] for part in parts):
            errors.append(f"{name}: declarações em arquivos distintos sem partial ({locations})")
        bases = {part[2] for part in parts if part[2]}
        if len(bases) > 1:
            errors.append(f"{name}: partials usam bases diferentes: {', '.join(sorted(bases))}")
        # Mesmo um partial tecnicamente válido deve ser revisado: controllers espalhados
        # voltam a expor o projeto a constructors, actions e rotas conflitantes.
        errors.append(f"{name}: controller declarado em mais de um arquivo ({locations})")

    index_signatures = [
        " ".join(match.group("parameters").split())
        for part in parts
        for match in INDEX_ACTION.finditer(part[3])
    ]
    duplicates = sorted({signature for signature in index_signatures if index_signatures.count(signature) > 1})
    if duplicates:
        errors.append(f"{name}: action Index com assinatura duplicada: {', '.join(duplicates) or '(sem parâmetros)'}")

for critical in ("MinhaAssinaturaController", "FaturamentoClinicoController"):
    parts = declarations.get(critical, [])
    if len(parts) != 1:
        errors.append(f"{critical}: esperado exatamente uma declaração, encontrado {len(parts)}")

home = (CONTROLLERS / "HomeController.cs").read_text(encoding="utf-8")
dashboard_view = (ROOT / "backend/PlantaoPro.Web/Views/Home/Dashboard.cshtml").read_text(encoding="utf-8")
if 'ViewData["DashboardDataAvailable"]' not in home:
    errors.append("HomeController: dashboard não distingue dados reais do fallback técnico")
if 'RedirectToAction("Index", "MinhaAgenda")' in home:
    errors.append("HomeController: perfil médico não pode ser desviado do dashboard por perfil v1.77")
if "data-profile-dashboard" not in dashboard_view:
    errors.append("Dashboard: contrato visual por perfil v1.77 ausente")

if errors:
    raise SystemExit("Falha na unicidade de controllers:\n- " + "\n- ".join(dict.fromkeys(errors)))

print(f"Controllers v1.80 validados: {len(declarations)} nomes únicos, controllers críticos consolidados e dashboard por perfil honesto.")
