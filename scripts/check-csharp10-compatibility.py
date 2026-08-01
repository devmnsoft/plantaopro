#!/usr/bin/env python3
"""Fail when backend sources or project files opt into post-C# 10 syntax."""

from __future__ import annotations

import re
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
BACKEND = ROOT / "backend"
RAW_STRING = re.compile(r"(?:=|return|=>|\(|,|:)\s*\"\"\"")
INVALID_LANG_VERSION = re.compile(
    r"<LangVersion>\s*(?:latest|preview|1[1-9](?:\.\d+)?)\s*</LangVersion>",
    re.IGNORECASE,
)


def validate() -> list[str]:
    errors: list[str] = []
    for source in BACKEND.rglob("*.cs"):
        for line_number, line in enumerate(source.read_text(encoding="utf-8").splitlines(), 1):
            if RAW_STRING.search(line):
                errors.append(f"{source.relative_to(ROOT)}:{line_number}: raw string literal")

    for project in [*BACKEND.rglob("*.csproj"), *BACKEND.rglob("*.props")]:
        content = project.read_text(encoding="utf-8")
        if INVALID_LANG_VERSION.search(content):
            errors.append(f"{project.relative_to(ROOT)}: LangVersion incompatível com C# 10")

    directory_props = (BACKEND / "Directory.Build.props").read_text(encoding="utf-8")
    if "<LangVersion>10</LangVersion>" not in directory_props:
        errors.append("backend/Directory.Build.props: o gate exige <LangVersion>10</LangVersion>")
    return errors


if __name__ == "__main__":
    violations = validate()
    if violations:
        print("Compatibilidade C# 10 reprovada:", file=sys.stderr)
        print("\n".join(f"- {item}" for item in violations), file=sys.stderr)
        raise SystemExit(1)
    print("Compatibilidade C# 10 validada: nenhum raw string literal ou LangVersion posterior encontrado.")
