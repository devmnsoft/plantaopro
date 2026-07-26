#!/usr/bin/env python3
"""Normalize pg_dump schema output by removing non-semantic metadata."""
import re, sys
from pathlib import Path

VOLATILE = [
    re.compile(r'^\\restrict\s+\S+\s*$'),
    re.compile(r'^\\unrestrict\s+\S+\s*$'),
    re.compile(r'^-- Dumped (?:from database version|by pg_dump version).*$'),
    re.compile(r'^-- Started on .*$'),
    re.compile(r'^-- Completed on .*$'),
    re.compile(r'^-- Name: .*; Type: (?:ACL|OWNER TO);.*$'),
    re.compile(r'^ALTER .* OWNER TO .+;$'),
    re.compile(r'^GRANT .+;$'),
    re.compile(r'^REVOKE .+;$'),
]

def normalize(text: str) -> str:
    out=[]; blank=False
    for raw in text.replace('\r\n','\n').replace('\r','\n').split('\n'):
        line=raw.rstrip()
        if any(p.match(line) for p in VOLATILE):
            continue
        line=re.sub(r'--.*(?:20\d{2}-\d{2}-\d{2}|\d{2}:\d{2}:\d{2}).*$', '', line).rstrip()
        if not line:
            if not blank:
                out.append('')
            blank=True
            continue
        blank=False
        out.append(line)
    while out and out[-1]=='': out.pop()
    return '\n'.join(out)+'\n'

def main():
    if len(sys.argv) not in (2,3):
        print('uso: normalize-schema-dump.py <input> [output]', file=sys.stderr); return 2
    data=Path(sys.argv[1]).read_text(encoding='utf-8', errors='replace')
    result=normalize(data)
    if len(sys.argv)==3: Path(sys.argv[2]).write_text(result, encoding='utf-8', newline='\n')
    else: sys.stdout.write(result)
    return 0
if __name__=='__main__': raise SystemExit(main())
