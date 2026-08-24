#!/usr/bin/env python3
"""Fail when backend sources or project files opt into post-C# 10 syntax."""

from __future__ import annotations

import re
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
BACKEND = ROOT / "backend"
RAW_STRING = re.compile(r"(?:=|return|=>|\(|,|:)\s*\"\"\"")
PRIMARY_CONSTRUCTOR = re.compile(r"^\s*(?:public|internal)\s+(?:sealed\s+)?class\s+\w+\s*\(")
# A collection expression is distinguished from an attribute, an array type and
# an indexer by the expression token immediately before its opening bracket.
# This deliberately accepts an empty body and runs with DOTALL so nested and
# multi-line expressions cannot evade the pre-build gate.
COLLECTION_EXPRESSION = re.compile(
    r"(?:\?\?|=>|(?<![=!<>])=(?!=)|\breturn\b|[,(?:])\s*"
    r"\[(?:[^\[\]\n]|\[[^\[\]\n]*\])*\](?!\s*=)",
    re.MULTILINE,
)
RAW_RAZOR_CSS_DIRECTIVE = re.compile(
    r"<style\b[^>]*>.*?(?<!@)@(media|supports|keyframes|layer|container|page|font-face)\b",
    re.IGNORECASE | re.DOTALL,
)
INVALID_LANG_VERSION = re.compile(
    r"<LangVersion>\s*(?:latest|preview|1[1-9](?:\.\d+)?)\s*</LangVersion>",
    re.IGNORECASE,
)


def validate() -> list[str]:
    errors: list[str] = []
    for source in BACKEND.rglob("*.cs"):
        content = source.read_text(encoding="utf-8")
        # Strings and comments may legitimately contain examples such as
        # "Password:[omitted]". Mask them while preserving offsets/newlines.
        inspected = re.sub(
            r'//[^\n]*|/\*.*?\*/|(?:\$@|@\$|\$|@)?"(?:""|\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'',
            lambda match: "".join("\n" if char == "\n" else " " for char in match.group(0)),
            content,
            flags=re.DOTALL,
        )
        inspected = re.sub(
            r"\[(?:[A-Z]\w*(?:Attribute)?(?:\([^\]\n]*\))?)(?:\s*,\s*[A-Z]\w*(?:\([^\]\n]*\))?)*\]"
            r"(?=\s*(?:(?:public|private|protected|internal|static|sealed|async)\s+)*(?:[A-Za-z_]\w*\.)*[A-Za-z_]\w*(?:[<?\[]|\s))",
            lambda match: " " * len(match.group(0)),
            inspected,
        )
        for line_number, line in enumerate(inspected.splitlines(), 1):
            if RAW_STRING.search(line):
                errors.append(f"{source.relative_to(ROOT)}:{line_number}: raw string literal")
            if PRIMARY_CONSTRUCTOR.search(line):
                errors.append(f"{source.relative_to(ROOT)}:{line_number}: primary constructor")
            if COLLECTION_EXPRESSION.search(line):
                errors.append(f"{source.relative_to(ROOT)}:{line_number}: collection expression")

        # Multi-line expressions need a whole-file pass. Avoid duplicating the
        # diagnostics already emitted by the inexpensive line-oriented pass.
        for match in COLLECTION_EXPRESSION.finditer(inspected):
            if "\n" in match.group(0):
                line_number = content.count("\n", 0, match.start()) + 1
                errors.append(f"{source.relative_to(ROOT)}:{line_number}: collection expression")

    for view in BACKEND.rglob("*.cshtml"):
        content = view.read_text(encoding="utf-8")
        for match in RAW_RAZOR_CSS_DIRECTIVE.finditer(content):
            line_number = content.count("\n", 0, match.start()) + 1
            directive = match.group(1).lower()
            errors.append(
                f"{view.relative_to(ROOT)}:{line_number}: diretiva CSS @{directive} crua em <style> Razor"
            )

    for project in [*BACKEND.rglob("*.csproj"), *BACKEND.rglob("*.props")]:
        content = project.read_text(encoding="utf-8")
        if INVALID_LANG_VERSION.search(content):
            errors.append(f"{project.relative_to(ROOT)}: LangVersion incompatível com C# 10")

    directory_props = (BACKEND / "Directory.Build.props").read_text(encoding="utf-8")
    if not re.search(r"<LangVersion>\s*10(?:\.0)?\s*</LangVersion>", directory_props):
        errors.append("backend/Directory.Build.props: o gate exige <LangVersion>10.0</LangVersion>")
    return errors


if __name__ == "__main__":
    violations = validate()
    if violations:
        print("Compatibilidade C# 10 reprovada:", file=sys.stderr)
        print("\n".join(f"- {item}" for item in violations), file=sys.stderr)
        raise SystemExit(1)
    print("Compatibilidade C# 10 e CSS Razor validada.")
