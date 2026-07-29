#!/usr/bin/env python3
"""Extract compiler errors from a real dotnet build log."""

from __future__ import annotations

import argparse
import json
from pathlib import Path
import re


ERROR = re.compile(
    r"^(?P<file>.+?)\((?P<line>\d+)(?:,\d+)?\): error (?P<code>[A-Z]+\d+): (?P<message>.*?)(?: \[.*\])?$"
)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("log", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    errors = []
    for line in args.log.read_text(encoding="utf-8", errors="replace").splitlines():
        match = ERROR.match(line.strip())
        if match:
            item = match.groupdict()
            item["line"] = int(item["line"])
            if item not in errors:
                errors.append(item)

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps({"stage": "build", "succeeded": False, "errors": errors}, indent=2) + "\n",
        encoding="utf-8",
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
