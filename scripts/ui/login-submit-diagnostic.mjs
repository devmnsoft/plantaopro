import { chromium } from "playwright";

const baseUrl = (process.env.PLANTAOPRO_WEB_URL || "http://localhost:5000").replace(/\/$/, "");
const identifier = process.env.PLANTAOPRO_LOGIN_IDENTIFIER;
const password = process.env.PLANTAOPRO_LOGIN_PASSWORD;

if (!identifier || !password) {
  console.error("Defina PLANTAOPRO_LOGIN_IDENTIFIER e PLANTAOPRO_LOGIN_PASSWORD com credenciais de homologação.");
  process.exit(2);
}

const browser = await chromium.launch({ headless: true });
const page = await browser.newPage();
const consoleErrors = [];
const loginResponses = [];

page.on("console", message => {
  if (message.type() === "error") consoleErrors.push(message.text());
});
page.on("response", response => {
  const request = response.request();
  if (request.method() === "POST") {
    loginResponses.push({ url: response.url(), status: response.status() });
  }
});

try {
  await page.goto(`${baseUrl}/Account/Login`, { waitUntil: "domcontentloaded" });
  await page.locator("#Email").fill(identifier);
  await page.locator("#senha").fill(password);
  await Promise.all([
    page.waitForLoadState("domcontentloaded").catch(() => undefined),
    page.locator("#btnLogin").click()
  ]);

  const loginPost = loginResponses.find(item => /\/Account\/Login(?:\?|$)/i.test(item.url));
  const buttonLocked = await page.locator("#btnLogin").isDisabled().catch(() => false);
  console.log(JSON.stringify({ page: page.url(), loginPost, buttonLocked, consoleErrors }, null, 2));

  if (!loginPost || buttonLocked || consoleErrors.length) process.exitCode = 1;
} finally {
  await browser.close();
}
