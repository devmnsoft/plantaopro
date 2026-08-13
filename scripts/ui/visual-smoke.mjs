#!/usr/bin/env node
import { mkdir } from "node:fs/promises";
import { chromium } from "playwright";

const baseURL = process.env.PLANTAOPRO_BASE_URL ?? "http://127.0.0.1:5000";
const storageState = process.env.PLANTAOPRO_STORAGE_STATE;
const output = new URL("../../artifacts/ui-audit/screenshots/v165/", import.meta.url);
const routes = [
  "/", "/Account/Login", "/cadastro/empresa", "/AdminSaas/Index", "/Home/Dashboard", "/MinhaCentral", "/MeuDia",
  "/Agenda", "/Plantoes", "/Escalas", "/Saude360", "/Pacientes", "/Agendamentos",
  "/Triagem", "/Consultas", "/Pagamentos", "/Financeiro", "/Relatorios", "/Configuracoes"
];
const defaults = ["360x800", "390x844", "430x932", "768x1024", "1024x768", "1366x768", "1920x1080"];
const viewports = (process.env.PLANTAOPRO_VIEWPORTS?.split(",") ?? defaults).map(value => {
  const match = value.trim().match(/^(\d+)x(\d+)$/i);
  if (!match) throw new Error(`Viewport inválido: ${value}. Use largura×altura, por exemplo 390x844.`);
  return { width: Number(match[1]), height: Number(match[2]) };
});

await mkdir(output, { recursive: true });
const browser = await chromium.launch({ headless: true });
const publicContext = await browser.newContext();
const authenticatedContext = storageState ? await browser.newContext({ storageState }) : null;
const failures = [];

for (const { width, height } of viewports) {
  for (const route of routes) {
    const publicRoute = route === "/" || route === "/Account/Login" || route === "/cadastro/empresa";
    const login = route === "/Account/Login";
    if (!publicRoute && !authenticatedContext) {
      failures.push(`${route} (${width}x${height}): informe PLANTAOPRO_STORAGE_STATE para a homologação autenticada`);
      continue;
    }
    const page = await (publicRoute ? publicContext : authenticatedContext).newPage();
    await page.setViewportSize({ width, height });
    try {
      const response = await page.goto(`${baseURL}${route}`, { waitUntil: "networkidle" });
      if (!response?.ok()) failures.push(`${route} (${width}x${height}): HTTP ${response?.status() ?? "sem resposta"}`);
      const currentPath = new URL(page.url()).pathname.toLowerCase();
      if (!login && currentPath.includes("/account/login")) throw new Error("sessão expirada ou storage state inválido");

      const result = await page.evaluate(({ desktop, login, admin, currentPath }) => {
        const rect = element => element?.getBoundingClientRect();
        const visible = element => {
          if (!element) return false;
          const box = rect(element); const style = getComputedStyle(element);
          return box.width > 0 && box.height > 0 && style.visibility !== "hidden" && style.display !== "none";
        };
        const body = document.body;
        const shell = document.querySelector(".pp-app-shell");
        const sidebar = document.querySelector(".pp-sidebar");
        const topbar = document.querySelector(".pp-topbar");
        const content = document.querySelector(".pp-content");
        const container = document.querySelector(".pp-content-container");
        const footer = document.querySelector(".pp-footer");
        const cards = [...document.querySelectorAll(".pp-card,.pp-action-card,.pp-kpi-card,.card")];
        const primaryButtons = [...document.querySelectorAll(".btn-primary,.button-primary,[type=submit]")];
        const openDrawers = [...document.querySelectorAll('[role="dialog"]:not([hidden]),dialog[open]')];
        const visibleToasts = [...document.querySelectorAll(".pp-toast:not([hidden]),.toast.show")];
        const mobileNavigation = document.querySelector(".pp-mobile-nav,.mobile-navigation");
        const tables = [...document.querySelectorAll("table")];
        const publicHero = document.querySelector(".pp-public-hero");
        const overlayRoot = document.querySelector("#pp-overlay-root");
        const confirmModal = document.querySelector("#pp-confirm-modal");
        const selfservice = document.querySelector(".pp-selfservice-page .pp-onboarding-form");
        const contentBox = rect(content); const sidebarBox = rect(sidebar);
        const footerBox = rect(footer); const navBox = rect(mobileNavigation);
        return {
          horizontalOverflow: Math.max(0, body.scrollWidth - innerWidth) <= 24,
          shellPresent: login || Boolean(shell),
          contentPresent: login || Boolean(content),
          containerPresent: login || Boolean(container),
          topbarVisible: login || visible(topbar),
          correctPageRoot: login ? Boolean(document.querySelector(".pp-auth-page .pp-auth-shell .pp-auth-card")) : !admin || Boolean(document.querySelector(".pp-admin-saas-page.pp-page")),
          authContentClear: !login || [...document.querySelectorAll(".pp-auth-shell *")].every(element => element.scrollWidth <= element.clientWidth + 2),
          topbarClear: login || !topbar || !contentBox || rect(topbar).bottom <= contentBox.top + 2,
          sidebarClear: login || !desktop || !visible(sidebar) || !contentBox || !sidebarBox || contentBox.left >= sidebarBox.right - 1,
          footerAfterContent: login || !footerBox || !contentBox || footerBox.top >= contentBox.top,
          cardsHaveWidth: cards.every(card => rect(card).width > 0),
          cardsHaveHeight: cards.every(card => rect(card).height > 0),
          tablesResponsive: tables.every(table => table.closest(".table-responsive") || document.querySelector(".pp-mobile-card") || table.scrollWidth <= table.clientWidth + 2),
          publicHeroProportional: !publicHero || rect(publicHero).height <= Math.max(900, innerHeight * 1.4),
          overlayOutOfFlow: Boolean(overlayRoot) && getComputedStyle(overlayRoot).position === "fixed" && (!confirmModal || confirmModal.hidden),
          selfserviceReady: currentPath !== "/cadastro/empresa" || Boolean(selfservice),
          primaryActionVisible: primaryButtons.length === 0 || primaryButtons.some(visible),
          drawersAboveSidebar: openDrawers.every(drawer => !sidebar || Number(getComputedStyle(drawer).zIndex || 0) > Number(getComputedStyle(sidebar).zIndex || 0)),
          toastsClearMobileNav: desktop || !navBox || visibleToasts.every(toast => rect(toast).bottom <= navBox.top)
        };
      }, { desktop: width >= 992, login, admin: route === "/AdminSaas/Index", currentPath });
      for (const [check, passed] of Object.entries(result)) if (!passed) failures.push(`${route} (${width}x${height}): ${check}`);
      const name = route.slice(1).replaceAll("/", "-");
      await page.screenshot({ path: new URL(`${width}x${height}-${name}.png`, output).pathname, fullPage: true });
    } catch (error) {
      failures.push(`${route} (${width}x${height}): ${error.message}`);
    } finally {
      await page.close();
    }
  }
}

await publicContext.close();
if (authenticatedContext) await authenticatedContext.close();
await browser.close();
if (failures.length) {
  console.error(`Smoke visual falhou:\n- ${failures.join("\n- ")}`);
  process.exitCode = 1;
} else {
  console.log(`Smoke visual v1.65 aprovado em ${routes.length} rotas e ${viewports.length} viewports.`);
}
