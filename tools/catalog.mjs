import { chromium } from 'playwright-core';
const BASE = 'http://localhost:5210';
const b = await chromium.launch({ args: ['--no-sandbox'] });
const p = await b.newPage({ viewport: { width: 1440, height: 1000 } });
const errs = [];
p.on('pageerror', e => errs.push('PAGEERROR: ' + e.message.slice(0, 110)));
p.on('console', m => {
  if (m.type() === 'error' && !/dotnet\.wasm|ERR_CONNECTION|Failed to load resource/.test(m.text()))
    errs.push(m.text().slice(0, 110));
});

await p.goto(BASE + '/catalog', { waitUntil: 'networkidle', timeout: 120000 });
await p.waitForTimeout(4000);

console.log('── اجزایِ صفحهٔ نرم‌افزارها ──');
console.log(`  کارت‌هایِ گروه      : ${await p.locator('.cat-card').count()}`);
console.log(`  کارت‌هایِ نرم‌افزار : ${await p.locator('.sw-card').count()}`);
console.log(`  ویژگی‌ها (نمونه)   : ${await p.locator('.sw-card__feats li').count()} مورد`);
console.log(`  پرسش‌هایِ پرتکرار  : ${await p.locator('.faq__item').count()}`);
console.log(`  قیمت‌ها           : ${await p.locator('.sw-card__price').count()}`);
const firstPrice = await p.locator('.sw-card__price').first().innerText().catch(() => '—');
console.log(`  نمونهٔ قیمت        : ${firstPrice.replace(/\n/g, ' ')}`);
console.log(`  ستاره‌ها          : ${await p.locator('.sw-card .stars, .sw-card__meta svg').count()}`);

// فیلتر بر اساسِ دسته
const cats = await p.locator('.cat-card').count();
if (cats > 1) {
  await p.locator('.cat-card').nth(1).click();
  await p.waitForTimeout(2500);
  const active = await p.locator('.cat-card.is-active').count();
  const after = await p.locator('.sw-card').count();
  console.log(`\n── فیلترِ دسته ──`);
  console.log(`  گروهٔ فعال: ${active} ${active === 1 ? '✅' : '❌'}   نرم‌افزارها پس از فیلتر: ${after}`);
}

// جست‌وجو
await p.goto(BASE + '/catalog', { waitUntil: 'networkidle' });
await p.waitForTimeout(3000);
const input = p.locator('input.input').first();
if (await input.count()) {
  await input.fill('حساب');
  await input.press('Enter');
  await p.waitForTimeout(2500);
  console.log(`\n── جست‌وجوی «حساب» ──`);
  console.log(`  نتیجه: ${await p.locator('.sw-card').count()} کارت`);
}

// موبایل
await p.setViewportSize({ width: 390, height: 844 });
await p.goto(BASE + '/catalog', { waitUntil: 'networkidle' });
await p.waitForTimeout(3500);
const overflow = await p.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
console.log(`\n── موبایل (۳۹۰ پیکسل) ──`);
console.log(`  بیرون‌زدگیِ افقی: ${overflow} پیکسل ${overflow <= 2 ? '✅' : '❌'}`);
console.log(`  ستون‌هایِ گروه  : ${await p.evaluate(() => getComputedStyle(document.querySelector('.cat-grid')).gridTemplateColumns.split(' ').length)}`);
await p.screenshot({ path: '/home/user/shots/catalog-mobile.png' });

// حالتِ تاریک
await p.setViewportSize({ width: 1440, height: 1000 });
await p.evaluate(() => localStorage.setItem('nw-theme', 'dark'));
await p.goto(BASE + '/catalog', { waitUntil: 'networkidle' });
await p.waitForTimeout(3500);
console.log(`\n── حالتِ تاریک ──`);
console.log(`  پس‌زمینه: ${await p.evaluate(() => getComputedStyle(document.body).backgroundColor)}`);
await p.screenshot({ path: '/home/user/shots/catalog-dark.png' });

await p.evaluate(() => localStorage.setItem('nw-theme', 'light'));
await p.goto(BASE + '/catalog', { waitUntil: 'networkidle' });
await p.waitForTimeout(3500);
await p.screenshot({ path: '/home/user/shots/catalog.png' });
console.log(`\nخطاها: ${errs.length ? [...new Set(errs)].slice(0, 5).join(' | ') + ' ❌' : 'هیچ ✅'}`);
await b.close();
