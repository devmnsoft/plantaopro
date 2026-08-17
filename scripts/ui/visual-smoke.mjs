#!/usr/bin/env node
import { access, mkdir, writeFile } from 'node:fs/promises';
import { chromium } from 'playwright';

const baseURL = process.env.PLANTAOPRO_BASE_URL;
const storageState = process.env.PLANTAOPRO_STORAGE_STATE;
const root = new URL('../../artifacts/ui-audit/', import.meta.url);
const screenshots = new URL('screenshots/v180/', root);
const jsonOutput = new URL('v180-visual-smoke-results.json', root);
const markdownOutput = new URL('v180-visual-smoke-summary.md', root);
const publicRoutes = new Set(['/', '/Account/Login', '/cadastro/empresa', '/Planos']);
const routes = ['/', '/Account/Login', '/cadastro/empresa', '/Planos', '/AdminSaas/Index', '/Home/Dashboard', '/MinhaCentral', '/MeuDia', '/Agenda', '/Agendamentos', '/Saude360', '/Pacientes', '/Triagem', '/Consultas', '/FaturamentoClinico', '/Financeiro', '/Pagamentos', '/Plantoes', '/Escalas', '/Fechamentos', '/Relatorios', '/Configuracoes', '/MinhaAssinatura'];
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
        const buttons = [...document.querySelectorAll('button')];
        const iconButtons = buttons.filter(button => !button.textContent.trim() && visible(button));
        const links = [...document.querySelectorAll('a[href]')];
        const sidebar = document.querySelector('.pp-sidebar');
        const content = document.querySelector('.pp-content');
        const topbar = document.querySelector('.pp-topbar');
        return {
          noHorizontalOverflow: document.documentElement.scrollWidth <= innerWidth + 2,
          cardsInsideViewport: [...document.querySelectorAll('.pp-card,.pp-action-card,.pp-kpi-card')].filter(visible).every(card => card.getBoundingClientRect().right <= innerWidth + 2),
          noClippedCards: [...document.querySelectorAll('.pp-card,.pp-action-card,.pp-kpi-card')].filter(visible).every(card => card.getBoundingClientRect().left >= -2 && card.getBoundingClientRect().right <= innerWidth + 2),
          responsiveTables: tables.every(table => table.closest('.table-responsive') || document.querySelector('.pp-mobile-card') || table.scrollWidth <= table.clientWidth + 2),
          accessibleDialogs: dialogs.every(dialog => dialog.getAttribute('aria-modal') === 'true' && (dialog.getAttribute('aria-label') || dialog.getAttribute('aria-labelledby'))),
          dialogsStartHidden: dialogs.every(dialog => dialog.hidden || dialog.getAttribute('aria-hidden') === 'true' || !visible(dialog)),
          overlaysOutOfFlow: dialogs.every(dialog => dialog.hidden || ['fixed', 'absolute'].includes(getComputedStyle(dialog).position)),
          formsStructured: [...document.querySelectorAll('form')].filter(visible).every(form => form.matches('.pp-form,.pp-filter-bar,.pp-filter-form') || form.closest('.pp-topbar')),
          fieldsHaveLabels: fields.every(field => field.labels?.length || field.getAttribute('aria-label') || field.getAttribute('aria-labelledby')),
          formsHaveLabels: fields.every(field => field.labels?.length || field.getAttribute('aria-label') || field.getAttribute('aria-labelledby')),
          buttonsHaveType: buttons.every(button => Boolean(button.getAttribute('type'))),
          iconButtonsHaveAriaLabel: iconButtons.every(button => button.getAttribute('aria-label') || button.getAttribute('aria-labelledby')),
          drawersAccessible: [...document.querySelectorAll('.pp-detail-drawer,.pp-notification-drawer')].every(drawer => drawer.getAttribute('role') === 'dialog' && drawer.getAttribute('aria-modal') === 'true'),
          commandPaletteAccessible: !document.querySelector('#commandPalette') || Boolean(document.querySelector('#commandPalette[role="dialog"] [role="listbox"]')),
          notificationDrawerAccessible: !document.querySelector('#notificationDrawer') || Boolean(document.querySelector('#notificationDrawer[role="dialog"][aria-modal="true"]')),
          loginResponsive: route !== '/Account/Login' || Boolean(document.querySelector('.pp-auth-page .pp-auth-shell .pp-auth-card')),
          selfServiceResponsive: route !== '/cadastro/empresa' || Boolean(document.querySelector('.pp-selfservice-page .pp-onboarding-form')),
          financialMvpJourneyVisible: !['/FaturamentoClinico', '/Financeiro', '/Pagamentos'].includes(route) || Boolean(document.querySelector('[data-financial-mvp-journey]')),
          billingOriginVisible: route !== '/FaturamentoClinico' || Boolean(document.querySelector('[data-billing-origin],.pp-empty-state')),
          financialStatusHonest: route !== '/Financeiro' || Boolean(document.querySelector('[data-financial-status-honest],.pp-empty-state')),
          paymentValueHonest: route !== '/Pagamentos' || Boolean(document.querySelector('[data-payment-value-honest],.pp-empty-state')),
          missingValuesNotZero: !['/FaturamentoClinico','/Financeiro','/Pagamentos'].includes(route) || !/Não informado[^\n]{0,30}R\$\s*0[,\.]00/i.test(document.body.innerText),
          glosaRulesVisible: !['/Financeiro','/Pagamentos'].includes(route) || Boolean(document.querySelector('[data-glosa-rules],table')),
          repasseRulesVisible: !['/Financeiro','/Pagamentos'].includes(route) || Boolean(document.querySelector('[data-repasse-rules],table')),
          financialNextActionVisible: !['/FaturamentoClinico','/Financeiro','/Pagamentos'].includes(route) || Boolean(document.querySelector('[data-financial-next-action],.pp-empty-state')),
          reportsActionsHonest: route !== '/Relatorios' || !document.querySelector('a[href="#"]'),
          financialJourneyHonest: !['/FaturamentoClinico', '/Financeiro', '/Pagamentos'].includes(route) || !/pagamento fake|valor fake|histórico fake/i.test(document.body.innerText),
          noFakeValues: !/pagamento fake|valor fake|histórico fake|contador fake/i.test(document.body.innerText),
          noBrokenLinks: links.every(link => { const href = link.getAttribute('href'); if (!href || href === '#') return false; try { const target = new URL(href, location.href); return ['http:', 'https:', 'mailto:', 'tel:'].includes(target.protocol); } catch { return false; } }),
          topbarDoesNotOverlap: !visible(topbar) || !visible(content) || topbar.getBoundingClientRect().bottom <= content.getBoundingClientRect().top + 2 || getComputedStyle(topbar).position === 'sticky',
          sidebarDoesNotOverlap: !desktop || !visible(sidebar) || !visible(content) || content.getBoundingClientRect().left >= sidebar.getBoundingClientRect().right - 1,
          pageContract: route === '/Account/Login' ? Boolean(document.querySelector('.pp-auth-page .pp-auth-shell .pp-auth-card')) : route === '/cadastro/empresa' ? Boolean(document.querySelector('.pp-selfservice-page .pp-onboarding-form')) : route === '/AdminSaas/Index' ? Boolean(document.querySelector('.pp-admin-layout')) : true,
          clinicalJourney: !['/Saude360', '/Agendamentos', '/Triagem', '/Consultas'].includes(route) || Boolean(document.querySelector('.pp-clinical-page,.clinical-workspace,.saude360-form')),
          clinicalMvpJourneyVisible: !['/Saude360', '/Pacientes', '/Agendamentos', '/Triagem', '/Consultas'].includes(route) || Boolean(document.querySelector('[data-clinical-mvp-journey]')),
          patientContextVisible: !['/Pacientes', '/Agendamentos', '/Consultas'].includes(route) || Boolean(document.querySelector('[data-patient-context]')),
          nextActionVisible: !['/Pacientes', '/Agendamentos', '/Triagem', '/Consultas'].includes(route) || Boolean(document.querySelector('[data-next-action]')),
          triageRulesVisible: route !== '/Triagem' || Boolean(document.querySelector('[data-triage-rules], [data-business-rule]')),
          consultationBillingActionHonest: !['/Consultas', '/FaturamentoClinico'].includes(route) || Boolean(document.querySelector('[data-consultation-billing-honest], button:disabled[title]')),
          clinicalJourneyClear: !['/Saude360', '/Agendamentos', '/Triagem', '/Consultas'].includes(route) || Boolean(document.querySelector('.pp-clinical-page,.clinical-workspace,.saude360-form')),
          operationalJourneyClear: !['/Plantoes', '/Escalas', '/Financeiro', '/Pagamentos'].includes(route) || Boolean(document.querySelector('.pp-page,.pp-operational-workspace,.pp-financial-workspace')),
          operationalMvpJourneyVisible: !['/Plantoes', '/Escalas', '/Fechamentos'].includes(route) || Boolean(document.querySelector('[data-operational-mvp-journey]')),
          shiftCoverageStatusVisible: route !== '/Plantoes' || Boolean(document.querySelector('.pp-coverage-badge,[data-operational-risk],.pp-empty-state')),
          scheduleNextActionVisible: route !== '/Escalas' || Boolean(document.querySelector('[data-next-action],.pp-empty-state')),
          invitationActionsHonest: route !== '/Plantoes' || !document.querySelector('a[href="#"]'),
          substitutionRulesVisible: route !== '/Escalas' || /substitui|motivo|sem escalas/i.test(document.body.innerText),
          closingBusinessRulesVisible: route !== '/Fechamentos' || Boolean(document.querySelector('[data-closing-business-rules]')),
          closingFinanceActionHonest: route !== '/Fechamentos' || Boolean(document.querySelector('button:disabled[title], table')),
          operationalRiskVisible: route !== '/Plantoes' || Boolean(document.querySelector('[data-operational-risk],.pp-risk-badge,.pp-empty-state')),
          profileDashboardVisible: route !== '/Home/Dashboard' || Boolean(document.querySelector('[data-profile-dashboard]')),
          businessRulesVisible: !['/Agendamentos', '/Triagem', '/Consultas', '/Plantoes', '/Escalas', '/FaturamentoClinico', '/Financeiro', '/Pagamentos'].includes(route) || Boolean(document.querySelector('.pp-empty-state,.pp-status-badge,.badge,[data-business-rule],button:disabled,.pp-data-table,table')),
          actionsWithoutBackendDisabled: !['/Agendamentos', '/Plantoes', '/Escalas', '/Pagamentos', '/Financeiro'].includes(route) || [...document.querySelectorAll('button:disabled')].every(button => button.title || button.getAttribute('aria-describedby')),
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
  const payload = { version: '1.80.0', baseURL, startedAt, finishedAt: new Date().toISOString(), totals: { executions: results.length, approved: results.filter(x => x.status === 'approved').length, failed: results.filter(x => x.status === 'failed').length }, results };
  await writeFile(jsonOutput, `${JSON.stringify(payload, null, 2)}\n`);
  const rows = results.map(item => `| ${item.route} | ${item.viewport} | ${item.authenticated ? 'Autenticada' : 'Pública'} | ${item.status === 'approved' ? 'APROVADA' : 'FALHA'} | ${item.error ?? (Object.entries(item.checks).filter(([, ok]) => !ok).map(([name]) => name).join(', ') || '—')} |`).join('\n');
  await writeFile(markdownOutput, `# Smoke visual v1.80.0\n\n- URL: \`${baseURL}\`\n- Início: ${startedAt}\n- Execuções: ${results.length}\n- Aprovadas: ${results.filter(x => x.status === 'approved').length}\n- Falhas: ${results.filter(x => x.status === 'failed').length}\n\n| Rota | Viewport | Acesso | Status | Diagnóstico |\n|---|---:|---|---|---|\n${rows}\n`);
}
if (results.some(item => item.status === 'failed')) process.exitCode = 1;
else console.log(`Smoke visual v1.80.0 aprovado: ${results.length} execuções.`);
