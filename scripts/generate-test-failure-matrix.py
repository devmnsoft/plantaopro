#!/usr/bin/env python3
"""Generate an honest failure matrix from baseline and final Visual Studio TRX files."""

from __future__ import annotations

import argparse
import json
from pathlib import Path
import xml.etree.ElementTree as ET


def local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def first(element: ET.Element, name: str) -> ET.Element | None:
    return next((item for item in element.iter() if local_name(item.tag) == name), None)


def text_of(element: ET.Element, name: str) -> str:
    item = first(element, name)
    return (item.text or "").strip() if item is not None else ""


def parse_trx(path: Path) -> tuple[dict[str, int], list[dict[str, str]], dict[str, str]]:
    if not path.is_file() or path.stat().st_size == 0:
        raise ValueError(f"TRX inexistente ou vazio: {path}")

    root = ET.parse(path).getroot()
    counters = first(root, "Counters")
    if counters is None:
        raise ValueError(f"TRX sem Counters: {path}")

    execution = {
        key: int(counters.attrib.get(key, "0"))
        for key in ("total", "executed", "passed", "failed")
    }
    if execution["total"] <= 0 or execution["executed"] <= 0:
        raise ValueError(f"TRX não representa uma execução real: {path}")

    definitions: dict[str, str] = {}
    for definition in (item for item in root.iter() if local_name(item.tag) == "UnitTest"):
        method = first(definition, "TestMethod")
        definitions[definition.attrib.get("id", "")] = (
            method.attrib.get("codeBase", "desconhecido") if method is not None else "desconhecido"
        )

    failures: list[dict[str, str]] = []
    outcomes: dict[str, str] = {}
    for result in (item for item in root.iter() if local_name(item.tag) == "UnitTestResult"):
        name = result.attrib.get("testName", "teste sem nome")
        outcome = result.attrib.get("outcome", "Unknown").upper()
        outcomes[name] = outcome
        if outcome == "FAILED":
            failures.append({
                "test": name,
                "sourceFile": definitions.get(result.attrib.get("testId", ""), "desconhecido"),
                "message": text_of(result, "Message") or "Falha sem mensagem registrada no TRX.",
            })
    if len(failures) != execution["failed"]:
        raise ValueError(
            f"TRX inconsistente: Counters.failed={execution['failed']}, resultados falhos={len(failures)}"
        )
    return execution, failures, outcomes


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("baseline_trx", type=Path, nargs="?")
    parser.add_argument("--build-failed", action="store_true")
    parser.add_argument("--final-trx", type=Path)
    parser.add_argument("--decisions", type=Path, help="JSON indexado pelo nome completo do teste")
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    if args.build_failed:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(
            json.dumps({
                "status": "BUILD_FAILED",
                "execution": {"total": 0, "executed": 0, "passed": 0, "failed": 0},
                "failures": [],
                "message": "A compilação falhou; a suíte de testes não foi executada e nenhum TRX válido foi produzido.",
            }, ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8",
        )
        return 0
    if args.baseline_trx is None:
        parser.error("baseline_trx é obrigatório quando --build-failed não é informado")

    execution, failures, _ = parse_trx(args.baseline_trx)
    final_outcomes: dict[str, str] = {}
    if args.final_trx:
        _, _, final_outcomes = parse_trx(args.final_trx)
    decisions = json.loads(args.decisions.read_text(encoding="utf-8")) if args.decisions else {}

    allowed = {
        "PRODUCT_DEFECT", "MISSING_IMPLEMENTATION", "STALE_CONTRACT", "TEST_INFRASTRUCTURE",
        "FALSE_POSITIVE", "MISSING_DOCUMENTATION", "ENVIRONMENT_CONFIGURATION",
    }
    rows = []
    for failure in failures:
        decision = decisions.get(failure["test"], {})
        category = decision.get("category", "TEST_INFRASTRUCTURE")
        if category not in allowed:
            raise ValueError(f"Categoria inválida para {failure['test']}: {category}")
        rows.append({
            **failure,
            "category": category,
            "rootCause": decision.get("rootCause", "Pendente de triagem técnica."),
            "decision": decision.get("decision", "Investigar antes de alterar produto ou teste."),
            "change": decision.get("change", "Nenhuma correção registrada."),
            "evidence": args.final_trx.name if args.final_trx else args.baseline_trx.name,
            "finalResult": final_outcomes.get(failure["test"], "NOT_EXECUTED"),
        })

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps({"execution": execution, "failures": rows}, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
