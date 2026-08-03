#!/usr/bin/env python3
"""Valida invariantes estruturais da Premium Experience sem substituir testes no navegador."""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "backend" / "PlantaoPro.Web"
TOKENS = WEB / "wwwroot/css/design-system/tokens.css"
REQUIRED = {
    "--pp-brand-primary", "--pp-brand-primary-hover", "--pp-brand-primary-active",
    "--pp-brand-primary-soft", "--pp-brand-dark", "--pp-brand-dark-hover",
    "--pp-brand-ink", "--pp-brand-muted", "--pp-brand-light", "--pp-page-background",
    "--pp-surface-default", "--pp-surface-subtle", "--pp-surface-elevated",
    "--pp-text-primary", "--pp-text-secondary", "--pp-text-muted", "--pp-text-inverse",
    "--pp-border-subtle", "--pp-border-default", "--pp-border-strong", "--pp-focus-ring",
    "--pp-success", "--pp-warning", "--pp-danger", "--pp-info",
}

def fail(message: str) -> None:
    print(f"ERRO: {message}", file=sys.stderr)
    raise SystemExit(1)

tokens = TOKENS.read_text(encoding="utf-8")
missing = sorted(token for token in REQUIRED if not re.search(rf"{re.escape(token)}\s*:", tokens))
if missing:
    fail("tokens obrigatórios ausentes: " + ", ".join(missing))

layout = (WEB / "Views/Shared/_Layout.cshtml").read_text(encoding="utf-8")
if layout.count("design-system/tokens.css"):
    fail("_Layout carrega tokens duplicados fora do bundle")

auth = (WEB / "Views/Shared/_AuthLayout.cshtml").read_text(encoding="utf-8")
if "cdn.jsdelivr.net" in auth:
    fail("_AuthLayout ainda depende de CDN")

login = (WEB / "Views/Account/Login.cshtml").read_text(encoding="utf-8")
for invariant in ("logo-horizontal-color.svg", "form-label", "aria-live", "data-password-toggle"):
    if invariant not in login:
        fail(f"login não atende ao invariante: {invariant}")

print(f"Premium UI: {len(REQUIRED)} tokens e invariantes estruturais validados.")
