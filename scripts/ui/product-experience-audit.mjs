import { readFile, readdir } from 'node:fs/promises';
import { dirname, join, relative, resolve } from 'node:path';

const root = resolve(import.meta.dirname, '../..');
const web = join(root, 'backend/PlantaoPro.Web');
const failures = [];
async function files(dir, extension) {
  const entries = await readdir(dir, { withFileTypes: true });
  const nested = await Promise.all(entries.map(entry => entry.isDirectory()
    ? files(join(dir, entry.name), extension)
    : entry.name.endsWith(extension) ? [join(dir, entry.name)] : []));
  return nested.flat();
}
const entry = join(web, 'wwwroot/css/plantaopro.css');
const cssEntry = await readFile(entry, 'utf8');
for (const match of cssEntry.matchAll(/@import url\(['"](.+?)['"]\)/g)) {
  const target = resolve(dirname(entry), match[1]);
  try { await readFile(target); } catch { failures.push(`Import inexistente: ${relative(root, target)}`); }
  if (/\/v\d+-/i.test(target)) failures.push(`CSS versionado importado: ${relative(root, target)}`);
}
for (const file of await files(join(web, 'Views'), '.cshtml')) {
  const source = await readFile(file, 'utf8');
  if (/href=["']#["']/i.test(source)) failures.push(`Link sem destino: ${relative(root, file)}`);
  if (/PlantãoPro v1\.63/i.test(source)) failures.push(`Versão histórica no shell: ${relative(root, file)}`);
}
for (const file of await files(join(web, 'wwwroot/css'), '.css')) {
  const source = await readFile(file, 'utf8');
  if (/z-index\s*:\s*\d{5,}/i.test(source)) failures.push(`z-index fora da escala: ${relative(root, file)}`);
}
if (failures.length) {
  console.error(failures.join('\n'));
  process.exitCode = 1;
} else {
  console.log('Product Experience gate: OK');
}
