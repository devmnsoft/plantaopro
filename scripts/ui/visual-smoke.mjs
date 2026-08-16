#!/usr/bin/env node
import { access, mkdir, writeFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const baseURL = process.env.PLANTAOPRO_BASE_URL;
const storageState = process.env.PLANTAOPRO_STORAGE_STATE;
const root = new URL('../../artifacts/ui-audit/', import.meta.url);
const screenshots = new URL('screenshots/v175/', root);
const jsonOutput = new URL('v175-visual-smoke-results.json', root);
const markdownOutput = new URL('v175-visual-smoke-summary.md', root);
const publicRoutes = new Set(['/', '/Account/Login', '/cadastro/empresa', '/Planos']);
const routes = ['/', '/Account/Login', '/cadastro/empresa', '/Planos', '/AdminSaas/Index', '/Home/Dashboard', '/MinhaCentral', '/MeuDia', '/Agenda', '/Plantoes', '/Escalas', '/Saude360', '/Pacientes', '/Agendamentos', '/Triagem', '/Consultas', '/Pagamentos', '/Financeiro', '/Relatorios', '/Configuracoes', '/MinhaAssinatura', '/FaturamentoClinico'];
const defaults = ['360x800', '390x844', '430x932', '768x1024', '1024x768', '1366x768', '1440x900', '1920x1080'];
const selectedRoutes = process.env.PLANTAOPRO_PUBLIC_ONLY === '1' ? routes.filter(route => publicRoutes.has(route)) : routes;
const viewports = (process.env.PLANTAOPRO_VIEWPORTS?.split(',') ?? defaults).map(value => { const match = value.trim().match(/^(\d+)x(\d+)$/i); if (!match) throw new Error(`Viewport inválido: ${value}`); return { width: +match[1], height: +match[2] }; });
if (!baseURL) throw new Error('Defina PLANTAOPRO_BASE_URL (ex.: http://127.0.0.1:5000).');
new URL(baseURL);
if (selectedRoutes.some(route => !publicRoutes.has(route)) && !storageState) throw new Error('Rotas autenticadas exigem PLANTAOPRO_STORAGE_STATE. Autentique no Playwright e salve com: await page.context().storageState({ path: "playwright/.auth/user.json" }). Use PLANTAOPRO_PUBLIC_ONLY=1 para auditar apenas rotas públicas.');
if (storageState) await access(storageState);
await mkdir(screenshots, { recursive: true });
const startedAt = new Date().toISOString();
const results = [];
let browser;
try {
  browser = await chromium.launch({ headless: true });
  const publicContext = await browser.newContext();
  const authenticatedContext = storageState ? await browser.newContext({ storageState }) : null;
  for (const viewport of viewports) for (const route of selectedRoutes) {
    const context = publicRoutes.has(route) ? publicContext : authenticatedContext;
    const page = await context.newPage(); const checks = {}; let status = 'approved'; let error = null;
    try {
      await page.setViewportSize(viewport);
      const response = await page.goto(new URL(route, baseURL).href, { waitUntil: 'networkidle' });
      checks.http = Boolean(response?.ok());
      checks.notRedirectedToLogin = route === '/Account/Login' || !new URL(page.url()).pathname.toLowerCase().includes('/account/login');
      Object.assign(checks, await page.evaluate(({ route, desktop }) => {
        const visible = element => element && element.getBoundingClientRect().width > 0 && element.getBoundingClientRect().height > 0 && getComputedStyle(element).visibility !== 'hidden';
        const dialogs = [...document.querySelectorAll('[role="dialog"]')]; const tables = [...document.querySelectorAll('table')];
        const fields = [...document.querySelectorAll('form input:not([type="hidden"]):not([type="submit"]):not([type="button"]), form select, form textarea')].filter(visible);
        return {
          noHorizontalOverflow: document.documentElement.scrollWidth <= innerWidth + 2,
          cardsInsideViewport: [...document.querySelectorAll('.pp-card,.pp-action-card,.pp-kpi-card')].filter(visible).every(card => card.getBoundingClientRect().right <= innerWidth + 2),
          responsiveTables: tables.every(table => table.closest('.table-responsive') || document.querySelector('.pp-mobile-card') || table.scrollWidth <= table.clientWidth + 2),
          accessibleDialogs: dialogs.every(dialog => dialog.getAttribute('aria-modal') === 'true' && (dialog.getAttribute('aria-label') || dialog.getAttribute('aria-labelledby'))),
          dialogsStartHidden: dialogs.every(dialog => dialog.hidden || dialog.getAttribute('aria-hidden') === 'true' || !visible(dialog)),
          overlaysOutOfFlow: dialogs.every(dialog => dialog.hidden || ['fixed', 'absolute'].includes(getComputedStyle(dialog).position)),
          formsStructured: [...document.querySelectorAll('form')].filter(visible).every(form => form.matches('.pp-form,.pp-filter-bar,.pp-filter-form') || form.closest('.pp-topbar')),
          fieldsHaveLabels: fields.every(field => field.labels?.length || field.getAttribute('aria-label') || field.getAttribute('aria-labelledby')),
          pageContract: route === '/Account/Login' ? Boolean(document.querySelector('.pp-auth-page .pp-auth-shell .pp-auth-card')) : route === '/cadastro/empresa' ? Boolean(document.querySelector('.pp-selfservice-page .pp-onboarding-form')) : route === '/AdminSaas/Index' ? Boolean(document.querySelector('.pp-admin-layout')) : true,
          clinicalJourney: !['/Saude360', '/Agendamentos', '/Triagem', '/Consultas'].includes(route) || Boolean(document.querySelector('.pp-clinical-page,.clinical-workspace,.saude360-form')),
          operationalActionsAreExplicit: !['/Agendamentos', '/Plantoes', '/Escalas', '/Pagamentos', '/Financeiro'].includes(route) || [...document.querySelectorAll('button:disabled')].every(button => button.title || button.getAttribute('aria-describedby')),
          shellClear: !desktop || !visible(document.querySelector('.pp-sidebar')) || !document.querySelector('.pp-content') || document.querySelector('.pp-content').getBoundingClientRect().left >= document.querySelector('.pp-sidebar').getBoundingClientRect().right - 1
        };
      }, { route, desktop: viewport.width >= 992 }));
      if (Object.values(checks).some(value => !value)) status = 'failed';
      const name = route === '/' ? 'home' : route.slice(1).replaceAll('/', '-').toLowerCase();
      await page.screenshot({ path: new URL(`${viewport.width}x${viewport.height}-${name}.png`, screenshots).pathname, fullPage: true });
      if (route === '/Home/Dashboard') { await page.keyboard.press('Control+K'); checks.commandPaletteOpens = await page.locator('#commandPalette:not([hidden])').isVisible(); await page.keyboard.press('Escape'); checks.commandPaletteCloses = await page.locator('#commandPalette').isHidden(); if (!checks.commandPaletteOpens || !checks.commandPaletteCloses) status = 'failed'; }
      if (route === '/Home/Dashboard' && await page.locator('[data-notification-open]').count()) {
        await page.locator('[data-notification-open]').first().click();
        checks.notificationDrawerOpens = await page.locator('#notificationDrawer:not([hidden])').isVisible();
        await page.keyboard.press('Escape');
        checks.notificationDrawerCloses = await page.locator('#notificationDrawer').isHidden();
        checks.notificationTriggerRegainsFocus = await page.locator('[data-notification-open]').first().evaluate(element => element === document.activeElement);
        if (!checks.notificationDrawerOpens || !checks.notificationDrawerCloses || !checks.notificationTriggerRegainsFocus) status = 'failed';
      }
      if (route === '/FaturamentoClinico') {
        checks.clinicalBillingHonestState = await page.locator('#billing-empty-title, #billing-error-title, #billing-list-title').count() === 1;
        if (!checks.clinicalBillingHonestState) status = 'failed';
      }
      if (route === '/Financeiro' || route === '/Pagamentos') {
        checks.noFabricatedFinancialPlaceholders = await page.locator('text=/pagamento fake|valor fake|histórico fake/i').count() === 0;
        checks.financialStateRendered = await page.locator('.pp-empty-state, .pp-data-table, table').count() >= 1;
        if (!checks.noFabricatedFinancialPlaceholders || !checks.financialStateRendered) status = 'failed';
      }
      if (route === '/MinhaAssinatura') {
        checks.subscriptionHonestState = await page.locator('#subscription-title').count() === 1
          && await page.locator('#subscription-empty-title, [aria-label="Resumo da assinatura"], [role="alert"]').count() >= 1;
        if (!checks.subscriptionHonestState) status = 'failed';
      }
    } catch (caught) { status = 'failed'; error = caught.message; }
    finally { results.push({ route, authenticated: !publicRoutes.has(route), viewport: `${viewport.width}x${viewport.height}`, status, checks, error }); await page.close(); }
  }
  await publicContext.close(); if (authenticatedContext) await authenticatedContext.close();
} finally {
  if (browser) await browser.close();
  const payload = { version: '1.75.0', baseURL, startedAt, finishedAt: new Date().toISOString(), totals: { executions: results.length, approved: results.filter(x => x.status === 'approved').length, failed: results.filter(x => x.status === 'failed').length }, results };
  await writeFile(jsonOutput, `${JSON.stringify(payload, null, 2)}\n`);
  const rows = results.map(item => `| ${item.route} | ${item.viewport} | ${item.authenticated ? 'Autenticada' : 'Pública'} | ${item.status === 'approved' ? 'APROVADA' : 'FALHA'} | ${item.error ?? (Object.entries(item.checks).filter(([, ok]) => !ok).map(([name]) => name).join(', ') || '—')} |`).join('\n');
  await writeFile(markdownOutput, `# Smoke visual v1.75.0\n\n- URL: \`${baseURL}\`\n- Início: ${startedAt}\n- Execuções: ${results.length}\n- Aprovadas: ${results.filter(x => x.status === 'approved').length}\n- Falhas: ${results.filter(x => x.status === 'failed').length}\n\n| Rota | Viewport | Acesso | Status | Diagnóstico |\n|---|---:|---|---|---|\n${rows}\n`);
}
if (results.some(item => item.status === 'failed')) process.exitCode = 1;
else console.log(`Smoke visual v1.75.0 aprovado: ${results.length} execuções.`);
