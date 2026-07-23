from pathlib import Path
import re

IGNORED_PARTS = {'.git', '.vs', 'artifacts', 'bin', 'obj', 'node_modules'}
TEXT_SUFFIXES = {'.cs', '.cshtml', '.ts', '.tsx', '.js', '.jsx', '.json', '.sql', '.md', '.config', '.xml', '.yml', '.yaml'}
ROOTS = [Path('backend/PlantaoPro.Api'), Path('backend/PlantaoPro.Web'), Path('mobile/PlantaoPro.App/src'), Path('database'), Path('docs')]

RULES = [
    ('token logging', re.compile(r'\bconsole\.log\s*\([^\n)]*\b(token|jwt|bearer)\b', re.IGNORECASE)),
    ('structured token logging', re.compile(r'\blogger\.log(?:information|debug|trace|warning)\s*\([^\n)]*\b(token|jwt|bearer)\b', re.IGNORECASE)),
    ('plain password assignment', re.compile(r'\b(senha|password)\b\s*[:=]\s*["\'][^"\']{8,}["\']', re.IGNORECASE)),
    ('jwt secret assignment', re.compile(r'\b(jwt(secret|key)?|secretkey)\b\s*[:=]\s*["\'][^"\']{16,}["\']', re.IGNORECASE)),
    ('cpf in log', re.compile(r'\b(log|logger|console)\w*[^\n]*(\d{3}\.\d{3}\.\d{3}-\d{2}|\d{11})', re.IGNORECASE)),
    ('unsafe connection string', re.compile(r'\b(host|server)\s*=.+\b(password|pwd)\s*=', re.IGNORECASE)),
]


def should_scan(path: Path) -> bool:
    if any(part in IGNORED_PARTS for part in path.parts):
        return False
    if path.suffix.lower() not in TEXT_SUFFIXES:
        return False
    name = path.name.lower()
    if name in {'package-lock.json', 'yarn.lock', 'pnpm-lock.yaml'}:
        return False
    return True


def scrub_hash_like_lines(text: str) -> str:
    kept = []
    for line in text.splitlines():
        low = line.lower()
        if 'integrity' in low or 'sha256' in low or 'checksum' in low:
            continue
        if '<senha' in low or '<password' in low or '<usuario' in low or 'change-me' in low:
            continue
        kept.append(line)
    return '\n'.join(kept)

blocked = []
for root in ROOTS:
    if not root.exists():
        continue
    for path in root.rglob('*'):
        if path.is_dir() or not should_scan(path):
            continue
        text = scrub_hash_like_lines(path.read_text(errors='ignore'))
        for label, pattern in RULES:
            if pattern.search(text):
                blocked.append({'file': str(path), 'rule': label})

if blocked:
    details = ', '.join(f"{item['file']} ({item['rule']})" for item in blocked)
    raise SystemExit('repository-security violations: ' + details)

print('repository-security ok')
