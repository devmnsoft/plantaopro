#!/usr/bin/env node
import { mkdir } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { chromium } from 'playwright';

const baseURL = process.env.PLANTAOPRO_BASE_URL ?? 'http://localhost:5000';
const output = resolve(process.env.PLANTAOPRO_STORAGE_STATE ?? 'artifacts/auth/storage-state.json');
const email = process.env.PLANTAOPRO_LOGIN_EMAIL;
const password = process.env.PLANTAOPRO_LOGIN_PASSWORD;
const timeout = Number(process.env.PLANTAOPRO_AUTH_TIMEOUT_MS ?? 180000);
new URL(baseURL);
if (Boolean(email) !== Boolean(password)) throw new Error('Informe PLANTAOPRO_LOGIN_EMAIL e PLANTAOPRO_LOGIN_PASSWORD juntos, ou omita ambos para login manual.');
await mkdir(dirname(output), { recursive: true });
const browser = await chromium.launch({ headless: Boolean(email) });
try {
  const context = await browser.newContext();
  const page = await context.newPage();
  await page.goto(new URL('/Account/Login', baseURL).href, { waitUntil: 'domcontentloaded' });
  if (email && password) {
    await page.locator('input[name="Email"], input[type="email"]').first().fill(email);
    await page.locator('input[name="Senha"], input[type="password"]').first().fill(password);
    await page.locator('button[type="submit"], input[type="submit"]').first().click();
  } else {
    console.log(`Faça o login real na janela aberta. Aguardando até ${Math.round(timeout / 1000)} segundos; nenhuma credencial será registrada no log.`);
  }
  await page.waitForURL(url => !url.pathname.toLowerCase().startsWith('/account/login'), { timeout });
  if (new URL(page.url()).pathname.toLowerCase().startsWith('/account/login')) throw new Error('O login não foi concluído.');
  await context.storageState({ path: output });
  console.log(`Storage state salvo em ${output}. Trate esse arquivo como segredo local.`);
  await context.close();
} catch (error) {
  throw new Error(`Não foi possível gerar o storage state: ${error.message}`);
} finally {
  await browser.close();
}
