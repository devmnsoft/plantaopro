#!/usr/bin/env python3
"""Catalog normalized PostgreSQL schema dumps for semantic CI comparison."""
from __future__ import annotations
import json, re, sys
from pathlib import Path

def catalog(text: str) -> dict:
    items = {"tables": {}, "indexes": [], "constraints": [], "functions": [], "triggers": [], "extensions": []}
    current = None
    for raw in text.splitlines():
        line = raw.strip()
        m = re.match(r"CREATE TABLE(?: IF NOT EXISTS)? ([^ ]+) \(", line)
        if m:
            current = m.group(1); items["tables"][current] = []; continue
        if current:
            if line == ");": current = None; continue
            if line and not line.startswith("--"):
                items["tables"][current].append(re.sub(r",$", "", line))
        for key, pat in [("indexes", r"CREATE (?:UNIQUE )?INDEX .+"), ("constraints", r"ALTER TABLE .+ (?:CONSTRAINT|FOREIGN KEY).+"), ("functions", r"CREATE FUNCTION .+"), ("triggers", r"CREATE TRIGGER .+"), ("extensions", r"CREATE EXTENSION .+")]:
            if re.match(pat, line): items[key].append(line)
    for key in items:
        if isinstance(items[key], list): items[key] = sorted(items[key])
    return items

def main() -> int:
    if len(sys.argv) not in (3,4):
        print("usage: catalog-schema.py <input.sql> <output.json> [compare-with.json]", file=sys.stderr); return 2
    out = catalog(Path(sys.argv[1]).read_text())
    if len(sys.argv) == 4:
        other = json.loads(Path(sys.argv[3]).read_text())
        report = {"equivalent": out == other, "leftOnly": {}, "rightOnly": {}}
        for key in out:
            if isinstance(out[key], dict):
                report["leftOnly"][key] = sorted(set(out[key]) - set(other.get(key, {})))
                report["rightOnly"][key] = sorted(set(other.get(key, {})) - set(out[key]))
            else:
                report["leftOnly"][key] = sorted(set(out[key]) - set(other.get(key, [])))
                report["rightOnly"][key] = sorted(set(other.get(key, [])) - set(out[key]))
        Path(sys.argv[2]).write_text(json.dumps(report, indent=2, ensure_ascii=False)+"\n")
    else:
        Path(sys.argv[2]).write_text(json.dumps(out, indent=2, ensure_ascii=False)+"\n")
    return 0
if __name__ == "__main__": raise SystemExit(main())
