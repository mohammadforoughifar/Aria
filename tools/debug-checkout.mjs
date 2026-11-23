import { chromium } from 'playwright-core';
const BASE = 'http://localhost:5210';
const b = await chromium.launch({ args: ['--no-sandbox'] });
const p = await (await b.newContext({ viewport: { width: 1440, height: 1000 } })).newPage();
p.on('console', m => console.log('[console]', m.type(), m.text().slice(0, 160)));
p.on('pageerror', e => console.log('[pageerror]', e.message.slice(0, 160)));

await p.goto(BASE + '/checkout', { waitUntil: 'networkidle' });
await p.waitForTimeout(3000);

const setValue = async (input, value) => { await input.fill(value); await input.dispatchEvent('change'); };
const fillField = async (label, value) => {
  const f = p.locator('label.field').filter({ hasText: label }).first();
  await setValue(f.locator('input'), value);
};

console.log('fields on page:', await p.locator('label.field').count());
await fillField('نام و نام خانوادگی', 'آزمون خودکار');
await setValue(p.locator('input[placeholder="name@company.com"]'), 'funnel@example.com');
await fillField('شرکت', 'شرکت آزمون');
await fillField('کشور', 'Iran');
await p.waitForTimeout(500);

// مقادیرِ واقعیِ input ها
const vals = await p.locator('label.field input').evaluateAll(els => els.map(e => e.value));
console.log('input values:', JSON.stringify(vals));

const btns = await p.locator('.card--pad button').evaluateAll(els => els.map(e => e.innerText.trim()));
console.log('buttons:', JSON.stringify(btns));

await p.locator('.card--pad button.btn--primary').first().click();
await p.waitForTimeout(2500);

const toasts = await p.locator('.toast, [class*=toast]').evaluateAll(els => els.map(e => e.innerText.replace(/\n/g, ' | ')));
console.log('toasts:', JSON.stringify(toasts));
console.log('has payment step:', await p.locator('text=روش پرداخت').count());
console.log('still step1:', await p.locator('text=اطلاعات صورتحساب').count());
await p.screenshot({ path: '/home/user/shots/debug-checkout.png', type: 'jpeg', quality: 70 });
await b.close();
