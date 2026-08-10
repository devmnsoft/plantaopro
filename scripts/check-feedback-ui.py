#!/usr/bin/env python3
"""Guard feedback regressions in web files changed by the current branch."""
from __future__ import annotations
import re, subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def changed():
    commands=[['git','diff','--name-only','HEAD^','HEAD'],['git','diff','--name-only','HEAD']]
    names=set()
    for command in commands:
        names.update(subprocess.run(command,cwd=ROOT,text=True,capture_output=True,check=True).stdout.splitlines())
    return [ROOT/p for p in sorted(names) if p.endswith(('.cshtml','.js')) and (ROOT/p).exists()]
rules=[
 ('native alert',re.compile(r'(?<![\w.])alert\s*\(')),
 ('native confirm',re.compile(r'(?<![\w.])confirm\s*\(')),
 ('placeholder href',re.compile(r'href\s*=\s*["\']#["\']')),
 ('button without type',re.compile(r'<button\b(?![^>]*\btype\s*=)[^>]*>',re.I)),
]
issues=[]
for path in changed():
 text=path.read_text(encoding='utf-8')
 for label,rule in rules:
  for match in rule.finditer(text): issues.append((path.relative_to(ROOT),text.count('\n',0,match.start())+1,label))
print(f'Feedback UI: {len(changed())} arquivo(s) alterado(s) analisado(s).')
for path,line,label in issues: print(f'  {path}:{line}: {label}')
if issues: raise SystemExit(1)
print('Feedback UI: PASS — sem diálogos nativos, links vazios ou botões sem tipo.')
