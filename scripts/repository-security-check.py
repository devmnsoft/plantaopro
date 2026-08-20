from pathlib import Path
import json
import subprocess
import re

IGNORED_PARTS = {'.git', '.vs', 'artifacts', 'bin', 'obj', 'node_modules'}
TEXT_SUFFIXES = {'.cs', '.cshtml', '.ts', '.tsx', '.js', '.jsx', '.json', '.sql', '.md', '.config', '.xml', '.yml', '.yaml'}
ROOTS = [Path('backend/PlantaoPro.Api'), Path('backend/PlantaoPro.Web'), Path('mobile/PlantaoPro.App/src'), Path('database'), Path('docs')]
INVALID_PLACEHOLDERS = ('__SET_', '<senha', '<password', 'change-me', 'example.invalid')

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

# Configuration invariants are parsed rather than inferred from a textual match.
for path in [*Path('backend').rglob('appsettings*.json')]:
    data = json.loads(path.read_text(encoding='utf-8'))
    database = data.get('Database', {})
    if database.get('AllowLegacyPostgresDatabase') is True:
        blocked.append({'file': str(path), 'rule': 'legacy database enabled'})
    if database.get('AllowDevelopmentAutoCreate') is True:
        blocked.append({'file': str(path), 'rule': 'development auto-create enabled'})
    for value_name, value in [('JWT', data.get('Jwt', {}).get('Key')), ('connection string', data.get('ConnectionStrings', {}).get('Default'))]:
        if value and not any(marker.lower() in str(value).lower() for marker in INVALID_PLACEHOLDERS):
            blocked.append({'file': str(path), 'rule': f'non-placeholder {value_name}'})

tracked = subprocess.run(['git','ls-files'],check=True,text=True,capture_output=True).stdout.splitlines()
for name in tracked:
    base=Path(name).name
    if base == '.env' or (base.startswith('.env.') and base not in {'.env.example','.env.sample'}):
        blocked.append({'file': name, 'rule': 'tracked environment file'})

if blocked:
    details = ', '.join(f"{item['file']} ({item['rule']})" for item in blocked)
    raise SystemExit('repository-security violations: ' + details)

print('repository-security ok')
