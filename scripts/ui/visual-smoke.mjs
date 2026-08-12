#!/usr/bin/env node
import { mkdir } from "node:fs/promises";
import { chromium } from "playwright";

const baseURL = process.env.PLANTAOPRO_BASE_URL ?? "http://127.0.0.1:5000";
const storageState = process.env.PLANTAOPRO_STORAGE_STATE;
const output = new URL("../../artifacts/ui-audit/screenshots/v158/", import.meta.url);
const routes = [
  "/Account/Login", "/AdminSaas/Index", "/Home/Dashboard", "/MinhaCentral", "/MeuDia",
  "/Agenda", "/Plantoes", "/Escalas", "/Saude360", "/Pacientes", "/Agendamentos",
  "/Triagem", "/Consultas", "/Pagamentos", "/Configuracoes"
];
const widths = (process.env.PLANTAOPRO_VIEWPORTS ?? "360,390,430,768,1024,1366,1920")
  .split(",").map(Number).filter(Boolean);
await mkdir(output, { recursive: true });
const browser = await chromium.launch({ headless: true });
const context = await browser.newContext(storageState ? { storageState } : {});
const failures = [];
for (const width of widths) {
  const page = await context.newPage();
  await page.setViewportSize({ width, height: width < 768 ? 844 : 900 });
  for (const route of routes) {
    const response = await page.goto(`${baseURL}${route}`, { waitUntil: "networkidle" });
    if (!response?.ok()) failures.push(`${route} (${width}px): HTTP ${response?.status() ?? "sem resposta"}`);
    const redirectedToLogin = route !== "/Account/Login" && new URL(page.url()).pathname.toLowerCase().includes("/account/login");
    if (redirectedToLogin) failures.push(`${route} (${width}px): autenticação ausente; informe PLANTAOPRO_STORAGE_STATE`);
    const result = await page.evaluate(({ desktop, login }) => {
      const body = document.body;
      const sidebar = document.querySelector(".pp-sidebar");
      const content = document.querySelector(".pp-content");
      const footer = document.querySelector(".pp-footer");
      const rect = element => element?.getBoundingClientRect();
      const cards = [...document.querySelectorAll(".pp-card,.pp-action-card,.pp-kpi-card,.card")];
      const primaryButtons = [...document.querySelectorAll(".btn-primary,[type=submit]")];
      const contentRect = rect(content);
      const sidebarRect = rect(sidebar);
      return {
        overflow: Math.max(0, body.scrollWidth - innerWidth),
        hasSidebar: login || Boolean(sidebar),
        hasContent: login || Boolean(content),
        contentClear: login || !desktop || !sidebarRect || !contentRect || contentRect.left >= sidebarRect.right - 1,
        footerAfterContent: login || !footer || !content || rect(footer).top >= contentRect.top,
        cardsValid: cards.every(card => rect(card).width > 0),
        primaryVisible: primaryButtons.length === 0 || primaryButtons.some(button => {
          const box = rect(button); return box.width > 0 && box.height > 0;
        }),
        bodySane: body.scrollWidth <= Math.max(innerWidth + 24, innerWidth * 1.25)
      };
    }, { desktop: width >= 992, login: route === "/Account/Login" });
    for (const [check, passed] of Object.entries(result)) {
      if (check === "overflow" ? passed > 24 : !passed) failures.push(`${route} (${width}px): ${check}=${passed}`);
    }
    const name = route.replace(/^\//, "").replaceAll("/", "-") || "home";
    await page.screenshot({ path: new URL(`${width}-${name}.png`, output).pathname, fullPage: true });
  }
  await page.close();
}
await browser.close();
if (failures.length) {
  console.error(`Smoke visual falhou:\n- ${failures.join("\n- ")}`);
  process.exitCode = 1;
} else {
  console.log(`Smoke visual aprovado em ${routes.length} rotas e ${widths.length} larguras.`);
}
