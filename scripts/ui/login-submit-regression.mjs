import { chromium } from "playwright";

const baseUrl = (process.env.PLANTAOPRO_WEB_URL || "http://localhost:52976").replace(/\/$/, "");
const loginUrl = `${baseUrl}/Account/Login`;
const browser = await chromium.launch({ headless: true });

async function scenario(name, exercise, expectedPosts) {
  const page = await browser.newPage();
  const posts = [];
  let loginHtml;

  await page.route(/\/Account\/Login(?:\?.*)?$/i, async route => {
    if (route.request().method() !== "POST") return route.continue();
    posts.push(route.request().url());
    await route.fulfill({ status: 200, contentType: "text/html; charset=utf-8", body: loginHtml });
  });

  try {
    const response = await page.goto(loginUrl, { waitUntil: "networkidle" });
    if (!response?.ok()) throw new Error(`GET do login retornou ${response?.status()}`);
    loginHtml = await response.text();
    await exercise(page);
    await page.waitForTimeout(250);
    if (posts.length !== expectedPosts) {
      throw new Error(`${name}: esperado(s) ${expectedPosts} POST(s), recebido(s) ${posts.length}`);
    }
    console.log(`PASS ${name}: ${posts.length} POST(s)`);
  } finally {
    await page.close();
  }
}

const fillValid = async page => {
  await page.locator("#Email").fill("browser.regression@example.invalid");
  await page.locator("#senha").fill("not-a-real-credential");
};

try {
  await scenario("clique envia uma vez", async page => {
    await fillValid(page);
    await page.locator("#btnLogin").click();
  }, 1);

  await scenario("Enter envia uma vez", async page => {
    await fillValid(page);
    await page.locator("#senha").press("Enter");
  }, 1);

  await scenario("duplo clique não duplica", async page => {
    await fillValid(page);
    await page.locator("#btnLogin").dblclick();
  }, 1);

  await scenario("campos inválidos permanecem utilizáveis", async page => {
    await page.locator("#btnLogin").click();
    if (await page.locator("#btnLogin").isDisabled()) throw new Error("botão permaneceu desabilitado");
    if (await page.locator("#btnLogin").getAttribute("aria-busy") === "true") throw new Error("loading permaneceu ativo");
  }, 0);

  await scenario("validação que cancela não inicia loading", async page => {
    await fillValid(page);
    await page.locator("#loginForm").evaluate(form => {
      form.addEventListener("submit", event => event.preventDefault(), { capture: true, once: true });
      form.requestSubmit();
    });
    if (await page.locator("#btnLogin").isDisabled()) throw new Error("botão permaneceu desabilitado");
    if (await page.locator("#btnLogin").getAttribute("aria-busy") === "true") throw new Error("loading permaneceu ativo");
  }, 0);

  await scenario("histórico recupera estado", async page => {
    await fillValid(page);
    await page.locator("#btnLogin").click();
    await page.waitForLoadState("domcontentloaded");
    await page.goBack({ waitUntil: "domcontentloaded" });
    if (await page.locator("#btnLogin").isDisabled()) throw new Error("botão permaneceu desabilitado após pageshow");
    if (await page.locator("#btnLogin").getAttribute("aria-busy") === "true") throw new Error("loading permaneceu ativo após pageshow");
  }, 1);
} finally {
  await browser.close();
}
