#!/usr/bin/env python3
"""Check that forms changed in this release expose labels, summaries and accessible errors."""
from __future__ import annotations
import re, subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
commands=[['git','diff','--name-only','HEAD^','HEAD'],['git','diff','--name-only','HEAD']]
names=set()
for command in commands:
 names.update(subprocess.run(command,cwd=ROOT,text=True,capture_output=True,check=True).stdout.splitlines())
files=[ROOT/p for p in sorted(names) if p.endswith('.cshtml') and (ROOT/p).exists()]
issues=[]
for path in files:
 text=path.read_text(encoding='utf-8')
 if '<form' not in text: continue
 if not re.search(r'asp-validation-summary|data-validation-summary|pp-validation-summary',text): issues.append((path,'formulário sem resumo de validação'))
 # Tag Helpers bind asp-for labels to inputs; require a label for every visible asp-for input.
 labels=set(re.findall(r'<label[^>]+asp-for=["\']([^"\']+)',text,re.I))
 fields=set(re.findall(r'<(?:input|select|textarea)[^>]+asp-for=["\']([^"\']+)',text,re.I))
 hidden=set(re.findall(r'<input[^>]+asp-for=["\']([^"\']+)[^>]+type=["\']hidden',text,re.I))
 for field in sorted(fields-labels-hidden): issues.append((path,f'campo {field} sem label visível'))
print(f'Form experience: {len(files)} view(s) alterada(s) analisada(s).')
for path,label in issues: print(f'  {path.relative_to(ROOT)}: {label}')
if issues: raise SystemExit(1)
print('Form experience: PASS — labels e resumos presentes nos formulários alterados.')
