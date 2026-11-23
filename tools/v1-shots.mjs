import { chromium } from 'playwright-core';
const BASE = 'http://localhost:5210';
const b = await chromium.launch({ args: ['--no-sandbox'] });
const p = await (await b.newContext({ viewport: { width: 1440, height: 1050 } })).newPage();

await p.goto(BASE + '/catalog', { waitUntil: 'networkidle' });
await p.waitForTimeout(3000);
await p.screenshot({ path: '/home/user/shots/v1-catalog.png', type: 'jpeg', quality: 75 });

await p.locator('.sw-card').first().locator('button', { hasText: 'افزودن' }).click();
await p.waitForTimeout(3500);
await p.screenshot({ path: '/home/user/shots/v1-cart.png', type: 'jpeg', quality: 75 });

await p.locator('a', { hasText: 'ادامه و تسویه' }).click();
await p.waitForTimeout(2500);
await p.locator('button', { hasText: 'شخصِ حقوقی' }).click();
await p.waitForTimeout(600);
await p.screenshot({ path: '/home/user/shots/v1-checkout.png', type: 'jpeg', quality: 75 });
await b.close();
