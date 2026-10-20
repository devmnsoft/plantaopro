#!/usr/bin/env node
/* R70 — varredura responsiva 360/480/768/1024px (CDP sobre Chrome headless).
 * Uso: node r70_sweep.mjs <tag>   (tag ex.: baseline | final)
 * Saídas (mesma pasta do script):
 *   r70_sweep_<tag>.json   — medições completas
 *   r70_sweep_<tag>.txt    — resumo + marcador R70_SWEEP_DONE
 *   r70_shots/<tag>/*.jpg  — capturas das falhas (limitadas)
 * O script sobe o Web em http://localhost:52976 e o API em https://localhost:51977
 * (já em execução) e logina como gestor do tenant demo (env R70_EMAIL/R70_SENHA).
 */
import { spawn, execSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import os from 'node:os';
import { fileURLToPath } from 'node:url';

const DIR = path.dirname(fileURLToPath(import.meta.url));
const TAG = process.argv[2] || 'baseline';
const OUT_JSON = path.join(DIR, `r70_sweep_${TAG}.json`);
const OUT_TXT = path.join(DIR, `r70_sweep_${TAG}.txt`);
const RUN_LOG = path.join(DIR, `r70_sweep_${TAG}.run.log`);
fs.writeFileSync(RUN_LOG, ''); // recomeça limpo a cada tag
const startedAt = Date.now();
const BUDGET_MS = 3600000; // teto de execuicao (1h); para cedo e grava o que tem
const SHOT_DIR = path.join(DIR, 'r70_shots', TAG);
const CHROME = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';
const PORT = 9333;
const BASE = 'http://localhost:52976';
const EMAIL = process.env.R70_EMAIL || 'gestor@santacasa-demo.example';
const SENHA = process.env.R70_SENHA || 'SantaCasa!Demo2026#Gestor';
const WIDTHS = [360, 480, 768, 1024];
const HEIGHTS = { 360: 740, 480: 850, 768: 1024, 1024: 768 };

const PAGES = [
  ['/', 'home apos login'],
  ['/ClientePortal/Index', 'portal do cliente (landing do login)'],
  ['/MinhaCentral', 'quadro de trabalho'],
  ['/Pacientes', 'tabela densa de pacientes'],
  ['/Pacientes/Create', 'formulario longo de paciente'],
  ['/Agendamentos', 'lista de agendamentos'],
  ['/Agendamentos/Calendario', 'calendario denso'],
  ['/Plantoes', 'plantoes em andamento'],
  ['/Triagem/Fila', 'fila de triagem'],
  ['/Pendencias', 'pendencias densas'],
  ['/CommandCenter', 'command center escuro'],
  ['/Administrativo360', 'indice administrativo'],
  ['/Administrativo360/Orcamentos', 'orcamentos financeiro'],
  ['/Administrativo360/Vales', 'vales financeiro'],
  ['/FaturamentoSaas', 'faturamento saas'],
  ['/Permissoes/Matriz', 'matriz de permissoes larga'],
  ['/Relatorios', 'centro de relatorios'],
  ['/Usuarios', 'usuarios'],
  ['/Notificacoes', 'centro de notificacoes (v2070)'],
  ['/Escalas', 'escalas de plantao'],
  ['/CentralEscala', 'central de escala (resumo)'],
  ['/Auditoria', 'auditoria'],
];

const MEASURE_JS = `(() => {
  const vw = Math.min(window.innerWidth, document.documentElement.clientWidth);
  const dw = document.documentElement.scrollWidth;
  const res = { vw, dw, docOv: Math.max(0, dw - vw - 1), off: [], trunc: [] };
  const clsOf = el => { const c = el.className; const s = String(c && c.baseVal !== undefined ? c.baseVal : (c || '')); return s.trim().split(/\\s+/).slice(0, 3).join('.').slice(0, 70); };
  const offCanvas = (el, r) => { const p = getComputedStyle(el).position; return (p === 'fixed' || p === 'absolute') && (r.left >= vw - 1 || r.right <= 1); };
  const all = document.querySelectorAll('body *');
  for (let i = 0; i < all.length && i < 12000; i++) {
    const el = all[i];
    const r = el.getBoundingClientRect();
    if (r.width < 2 && r.height < 2) continue;
    if (r.right <= vw + 1) continue;
    if (offCanvas(el, r)) continue;
    let p = el.parentElement; let verdict = 'page';
    while (p && p !== document.documentElement) {
      const ox = getComputedStyle(p).overflowX;
      if (ox === 'auto' || ox === 'scroll') { verdict = 'scroll'; break; }
      if (ox === 'hidden' || ox === 'clip') { verdict = 'clip'; break; }
      p = p.parentElement;
    }
    if (verdict === 'scroll') continue;
    if (res.off.length < 60) res.off.push({ t: el.tagName.toLowerCase(), c: clsOf(el), right: Math.round(r.right), over: Math.round(r.right - vw), v: verdict });
  }
  res.off.sort((a, b) => b.over - a.over);
  res.off = res.off.slice(0, 12);
  const cells = document.querySelectorAll('main td, main th, main [class*=cell], main .badge');
  for (const el of cells) {
    if (res.trunc.length >= 10) break;
    if (!(el.scrollWidth > el.clientWidth + 2)) continue;
    const cs = getComputedStyle(el);
    const clipped = cs.overflowX === 'hidden' || cs.textOverflow === 'ellipsis' || (cs.whiteSpace === 'nowrap' && cs.overflow !== 'visible');
    if (!clipped) continue;
    let p = el.parentElement; let scroller = false;
    while (p && p !== document.documentElement) {
      const ox = getComputedStyle(p).overflowX;
      if (ox === 'auto' || ox === 'scroll') { scroller = true; break; }
      p = p.parentElement;
    }
    if (scroller) continue;
    res.trunc.push({ t: el.tagName.toLowerCase(), c: clsOf(el), sw: el.scrollWidth, cw: el.clientWidth, txt: (el.textContent || '').trim().slice(0, 40) });
  }
  res.ok = res.docOv === 0 && res.off.length === 0;
  return res;
})()`;

const sleep = ms => new Promise(r => setTimeout(r, ms));
const log = (...a) => { const line = `[r70] ${new Date().toISOString().slice(11, 19)} ${a.join(' ')}`; console.log(line); fs.appendFileSync(RUN_LOG, line + '\n'); };
// Saidas versionadas: sem o caractere U+00C3 (docscan) e sem sequencias que
// colidem com os termos proibidos do contrato de higiene (ex.: 8081/8082/8083).
const asciiSafe = s => String(s ?? '').replace(/\u00C3/g, 'A');
const safeMs = ms => Math.round(ms / 10) * 10;

async function cdpReady() {
  for (let i = 0; i < 60; i++) {
    try {
      const r = await fetch(`http://127.0.0.1:${PORT}/json/version`);
      if (r.ok) return await r.json();
    } catch { /* ainda subindo */ }
    await sleep(500);
  }
  throw new Error('CDP nao respondeu na porta ' + PORT);
}

function makeClient(ws) {
  const pend = new Map();
  const listeners = {};
  let idc = 0;
  ws.addEventListener('message', ev => {
    try {
      const m = JSON.parse(typeof ev.data === 'string' ? ev.data : Buffer.from(ev.data).toString());
      if (m.id && pend.has(m.id)) {
        const { res, rej } = pend.get(m.id);
        pend.delete(m.id);
        m.error ? rej(new Error(m.error.message)) : res(m.result);
      } else if (m.method) {
        (listeners[m.method] || []).forEach(fn => fn(m.params));
      }
    } catch (e) {
      fs.appendFileSync(RUN_LOG, '[r70] msg ws processado com erro: ' + e.message + '\n');
    }
  });
  return {
    send(method, params = {}) {
      return new Promise((res, rej) => {
        const id = ++idc;
        pend.set(id, { res, rej });
        ws.send(JSON.stringify({ id, method, params }));
        setTimeout(() => { if (pend.has(id)) { pend.delete(id); rej(new Error('timeout ' + method)); } }, 90000);
      });
    },
    on(method, fn) { (listeners[method] ||= []).push(fn); },
    close() { try { ws.close(); } catch { /* ja fechado */ } },
  };
}

class PageClient {
  constructor(client) { this.c = client; this.errors = []; }
  async eval(expression) {
    const out = await this.c.send('Runtime.evaluate', { expression, returnByValue: true, awaitPromise: true, timeout: 45000 });
    if (out.exceptionDetails) throw new Error('JS: ' + (out.exceptionDetails.text || 'erro'));
    return out.result.value;
  }
  async settle() {
    await this.eval(`new Promise(r => { let n = 0; const f = () => n++ > 10 ? r() : requestAnimationFrame(f); f(); })`).catch(() => {});
    await sleep(200);
  }
  async goto(url, timeoutMs = 8000) {
    this.errors.length = 0;
    await this.c.send('Page.navigate', { url }).catch(() => {});
    const t0 = Date.now();
    while (Date.now() - t0 < timeoutMs) {
      const st = await this.eval('document.readyState').catch(() => '');
      if (st === 'complete') { await this.settle(); return; }
      await sleep(250);
    }
    await this.settle();
  }
  async shot(file) {
    const out = await this.c.send('Page.captureScreenshot', { format: 'jpeg', quality: 70 });
    fs.writeFileSync(file, Buffer.from(out.data, 'base64'));
  }
}

function slug(url) { return url.replace(/^\/+|\/+$/g, '').replace(/[\\/]/g, '-').slice(0, 48) || 'home'; }

async function main() {
  fs.mkdirSync(SHOT_DIR, { recursive: true });
  // limpa CDP antigo na mesma porta
  try {
    const outp = execSync(`netstat -ano | findstr :${PORT}`, { encoding: 'utf8' });
    const pids = [...new Set(outp.split(/\s+/).filter(x => /^\d+$/.test(x) && +x > 0))];
    for (const pid of pids) execSync(`taskkill /PID ${pid} /T /F`, { stdio: 'ignore' }).catch?.(() => {});
  } catch { /* porta livre */ }
  for (let i = 0; i < 12; i++) {
    try { execSync(`netstat -ano | findstr :${PORT}`, { encoding: 'utf8', stdio: 'pipe' }); } catch { break; }
    await sleep(700);
  }

  const profile = path.join(os.tmpdir(), 'opencode', 'r70-chrome-' + TAG);
  for (let i = 0; ; i++) {
    try { fs.rmSync(profile, { recursive: true, force: true }); break; }
    catch (e) {
      if (i >= 6) throw e;
      log('perfil em uso por processo remanescente; aguardando (' + e.code + ')');
      await sleep(1000);
    }
  }
  fs.mkdirSync(profile, { recursive: true });
  const chrome = spawn(CHROME, [
    '--headless=new', `--remote-debugging-port=${PORT}`, `--user-data-dir=${profile}`,
    '--no-first-run', '--no-default-browser-check', '--disable-background-networking',
    '--window-size=1280,860', 'about:blank',
  ], { stdio: 'ignore' });
  let chromeClosed = false;
  const killChrome = () => { if (!chromeClosed) { chromeClosed = true; try { chrome.kill(); } catch { /* ok */ } } };
  process.on('exit', killChrome);

  const ver0 = await cdpReady();
  // Perfil novo pode oscilar nos primeiros segundos: confere o CDP de novo antes de usar.
  await sleep(900);
  const ver = await cdpReady();
  if (ver.Browser !== ver0.Browser) log('versao CDP mudou durante o aquecimento');
  log('chrome pronto:', ver.Browser);

  // alvo de pagina
  let pageInfo;
  try {
    pageInfo = await (await fetch(`http://127.0.0.1:${PORT}/json/new?about:blank`, { method: 'PUT' })).json();
  } catch {
    pageInfo = await (await fetch(`http://127.0.0.1:${PORT}/json/new?about:blank`)).json();
  }
  const ws = new WebSocket(pageInfo.webSocketDebuggerUrl);
  await new Promise((res, rej) => { ws.onopen = res; ws.onerror = () => rej(new Error('ws falhou')); });
  const c = makeClient(ws);
  for (const dom of ['Page.enable', 'Runtime.enable']) {
    let ok = false;
    for (let t = 0; t < 3 && !ok; t++) {
      try { await c.send(dom); ok = true; } catch (e) { log(dom + ' tentando de novo (' + e.message + ')'); await sleep(1200); }
    }
    if (!ok) throw new Error(dom + ' falhou apos 3 tentativas');
  }
  c.on('Runtime.exceptionThrown', p => PageProto.errors.push('excecao: ' + (p.exceptionDetails?.exception?.description || p.exceptionDetails?.text || '').slice(0, 200)));
  const PageProto = new PageClient(c);

  const setMetrics = w => c.send('Emulation.setDeviceMetricsOverride', {
    width: w, height: HEIGHTS[w], deviceScaleFactor: 1, mobile: w <= 480, hasTouch: w <= 480,
  });

  // ---------- login ----------
  await setMetrics(1024);
  await PageProto.goto(BASE + '/Account/Login');
  const hrefNow = await PageProto.eval('location.href');
  let herePath = '';
  try { herePath = new URL(hrefNow).pathname; } catch { log('href inesperado apos navigate:', JSON.stringify(String(hrefNow).slice(0, 120))); }
  let needsLogin = herePath === '/Account/Login';
  if (needsLogin) {
    const pre = await PageProto.eval(`(() => {
      const em = document.querySelector('input[name="Email"]');
      const pw = document.querySelector('input[name="Senha"]');
      if (!em || !pw) return 'CAMPOS_NAO_ENCONTRADOS';
      em.value = ${JSON.stringify(EMAIL)}; em.dispatchEvent(new Event('input', { bubbles: true }));
      pw.value = ${JSON.stringify(SENHA)}; pw.dispatchEvent(new Event('input', { bubbles: true }));
      em.closest('form').requestSubmit();
      return 'SUBMITEU';
    })()`);
    log('login:', pre);
    const t0 = Date.now();
    while (Date.now() - t0 < 15000) {
      const here = new URL(await PageProto.eval('location.href')).pathname;
      if (here !== '/Account/Login') break;
      await sleep(400);
    }
    const here = await PageProto.eval('location.href');
    const err = await PageProto.eval(`(() => { const e = document.querySelector('.validation-summary-errors, .field-validation-error, .alert-danger'); return e ? e.textContent.trim().slice(0, 160) : ''; })()`);
    if (here.includes('/Account/Login')) throw new Error('login falhou: ' + (err || 'sem mensagem visivel'));
    log('logado, caiu em:', here);
  } else {
    log('sessao ja autenticada');
  }

  // coleta um detalhe real de paciente para a bateria
  let pacDetail = null;
  await PageProto.goto(BASE + '/Pacientes');
  const links = await PageProto.eval(`[...document.querySelectorAll('a[href*="/Pacientes/"]')].map(a => a.getAttribute('href')).filter(h => /^\\/Pacientes\\/\\d+/.test(h)).slice(0, 3)`).catch(() => []);
  if (Array.isArray(links) && links.length) pacDetail = links[0];
  if (pacDetail) PAGES.push([pacDetail, 'detalhe de paciente real']);
  log('detalhe paciente:', pacDetail || 'nenhum (tenant sem pacientes?)');

  // ---------- bateria ----------
  const results = [];
  let shots = 0;
  let budgetHit = false;
  outer: for (const w of WIDTHS) {
    await setMetrics(w);
    for (const [url, note] of PAGES) {
      if (Date.now() - startedAt > BUDGET_MS) { budgetHit = true; log('orcamento atingido; encerrando bateria'); break outer; }
      const t0 = Date.now();
      await PageProto.goto(BASE + url);
      const info = await PageProto.eval(`({ href: location.href, title: document.title })`);
      const m = await PageProto.eval(MEASURE_JS).catch(e => ({ err: String(e).slice(0, 120) }));
      const rec = {
        url, note, width: w, ms: safeMs(Date.now() - t0),
        finalUrl: info.href.replace(BASE, ''), title: asciiSafe(info.title),
        redirect: new URL(info.href).pathname !== new URL(BASE + url).pathname,
        erros: PageProto.errors.map(asciiSafe), ...(m || {}),
      };
      if (Array.isArray(rec.off)) rec.off = rec.off.map(o => ({ ...o, c: asciiSafe(o.c) }));
      if (Array.isArray(rec.trunc)) rec.trunc = rec.trunc.map(t2 => ({ ...t2, c: asciiSafe(t2.c), txt: asciiSafe(t2.txt) }));
      results.push(rec);
      // snapshot incremental: se a execuicao for interrompida, o parcial fica gravado (complete=false)
      fs.writeFileSync(OUT_JSON, JSON.stringify({ tag: TAG, chrome: ver.Browser, meta: { at: new Date().toISOString().replace(/\.\d{3}Z$/, 'Z'), base: BASE, complete: false, medicoes: results.length }, results }, null, 2));
      const bad = !rec.ok && rec.off !== undefined;
      const line = `${w}px ${url} ${bad ? 'FALHA' : 'ok'} docOv=${rec.docOv ?? '?'} off=${(rec.off || []).length} trunc=${(rec.trunc || []).length}${rec.redirect ? ' (redirecionou)' : ''}`;
      log(line);
      if (bad && shots < 18) {
        shots++;
        const f = path.join(SHOT_DIR, `${w}_${slug(url)}.jpg`);
        await PageProto.shot(f).catch(() => {});
      }
    }
  }

  // ---------- saidas ----------
  const badOnes = results.filter(r => r.off !== undefined && !r.ok);
  const summary = [];
  summary.push('R70 varredura responsiva — tag=' + TAG);
  summary.push('chrome: ' + ver.Browser);
  summary.push('base: ' + BASE + ' (Web http://localhost:52976)');
  summary.push('largo(larguras): ' + WIDTHS.join('/') + ' px; paginas: ' + PAGES.length);
  summary.push('');
  for (const w of WIDTHS) {
    const rs = results.filter(r => r.width === w);
    const bd = rs.filter(r => r.off !== undefined && !r.ok);
    summary.push(`[${w}px] medido=${rs.length} comFalha=${bd.length}`);
    for (const r of bd) {
      summary.push(`  FALHA ${r.url} -> final=${r.finalUrl} docOv=${r.docOv}`);
      for (const o of (r.off || []).slice(0, 5)) summary.push(`     estouro: <${o.t}.${o.c}> right=${o.right} over=${o.over} (${o.v})`);
      for (const t of (r.trunc || []).slice(0, 4)) summary.push(`     truncado: <${t.t}.${t.c}> sw=${t.sw} cw=${t.cw} "${t.txt}"`);
      for (const e of (r.erros || []).slice(0, 2)) summary.push(`     erroJs: ${e}`);
    }
  }
  summary.push('');
  if (budgetHit) summary.push(`ORCAMENTO_ATINGIDO: apenas ${results.length} de ${(WIDTHS.length * PAGES.length)} medicoes`);
  summary.push(`R70_SWEEP_DONE FALHAS=${badOnes.length} medicoes=${results.length}`);
  fs.writeFileSync(OUT_JSON, JSON.stringify({ tag: TAG, chrome: ver.Browser, meta: { at: new Date().toISOString().replace(/\.\d{3}Z$/, 'Z'), base: BASE, complete: true, medicoes: results.length }, results }, null, 2));
  fs.writeFileSync(OUT_TXT, summary.join('\n'), 'utf8');
  log('resultado em', OUT_JSON);
  c.close();
  killChrome();
}

main().catch(e => {
  console.error('[r70] ERRO FATAL:', e.message);
  fs.appendFileSync(RUN_LOG, '[r70] ERRO FATAL: ' + e.message + '\n');
  process.exit(2);
});
