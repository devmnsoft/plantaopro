from pathlib import Path

blocked = []
roots = [Path('backend/PlantaoPro.Api'), Path('backend/PlantaoPro.Web'), Path('mobile/PlantaoPro.App/src')]
for root in roots:
    if not root.exists():
        continue
    for path in root.rglob('*'):
        if path.is_dir() or 'node_modules' in path.parts or path.suffix.lower() not in {'.cs', '.cshtml', '.ts', '.tsx', '.js', '.jsx'}:
            continue
        text = path.read_text(errors='ignore')
        low = text.lower()
        if 'console.log(token' in low or 'logger.loginformation(token' in low:
            blocked.append(str(path))

if blocked:
    raise SystemExit('unsafe sensitive logging: ' + ', '.join(blocked))

print('repository-security ok')
