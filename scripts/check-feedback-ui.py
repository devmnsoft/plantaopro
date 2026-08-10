#!/usr/bin/env python3
"""Guard v1.53 feedback surfaces against inaccessible/native interaction patterns."""
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "backend/PlantaoPro.Web"

def changed_files():
    result = subprocess.run(["git", "diff", "--name-only", "HEAD"], cwd=ROOT, text=True, capture_output=True, check=True)
    return [ROOT / line for line in result.stdout.splitlines() if line.endswith((".js", ".cshtml"))]

rules = {
    "alert() nativo": re.compile(r"(?<![\w.])alert\s*\("),
    "confirm() nativo": re.compile(r"(?<![\w.])confirm\s*\("),
    'href="#"': re.compile(r'href\s*=\s*["\']#["\']', re.I),
    "button sem type": re.compile(r"<button(?![^>]*\btype\s*=)[^>]*>", re.I),
}
issues = []
for path in changed_files():
    if not path.exists() or WEB not in path.parents:
        continue
    text = path.read_text(encoding="utf-8")
    for label, pattern in rules.items():
        for match in pattern.finditer(text):
            line = text.count("\n", 0, match.start()) + 1
            issues.append(f"{path.relative_to(ROOT)}:{line}: {label}")

print(f"Feedback UI: {len(issues)} ocorrência(s) bloqueadora(s) em arquivos alterados.")
for issue in issues: print(f"- {issue}")
sys.exit(1 if issues else 0)
