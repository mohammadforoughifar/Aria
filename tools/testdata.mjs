import { chromium } from 'playwright-core';
const B = 'http://localhost:5210';
const b = await chromium.launch({ args: ['--no-sandbox'] });
const p = await b.newPage({ viewport: { width: 1440, height: 1000 } });
const errs = [];
p.on('pageerror', e => errs.push('PAGEERROR: ' + e.message.slice(0, 110)));
p.on('console', m => {
  if (m.type() === 'error' && !/dotnet\.wasm|ERR_CONNECTION|Failed to load resource/.test(m.text()))
    errs.push(m.text().slice(0, 110));
});

const j = async (u) => {
  try { const r = await p.evaluate(async (u) => {
    const res = await fetch(u); return res.status === 200 ? await res.json() : null; }, u); return r; }
  catch { return null; }
};

await p.goto(B + '/', { waitUntil: 'networkidle', timeout: 120000 });
await p.waitForTimeout(3000);

console.log('── دادهٔ تستی از نگاهِ API ──');
const prods = await j('api/products?pageSize=50');
console.log(`   نرم‌افزارها        : ${prods ? prods.items.length : '—'}  (پیش‌تر ۱۵)`);
const tst = await j('api/testimonials');
console.log(`   نظرِ مشتریان      : ${tst ? tst.length : '—'}  (پیش‌تر ۳)`);
const faq = await j('api/faqs');
console.log(`   پرسش‌های پرتکرار  : ${faq ? faq.length : '—'}  (پیش‌تر ۵)`);
const art = await j('api/blog');
console.log(`   مقالات            : ${art ? (art.items ? art.items.length : art.length) : '—'}  (پیش‌تر ۴)`);

// نرم‌افزارهایِ تازه
const names = prods ? prods.items.filter(x => /حسابدار|انبار|مؤدیان|حقوق و دستمزد|دریافت و پرداخت|فروش کالا|اداری/.test(x.Name)).map(x => x.Name) : [];
console.log(`\n   نرم‌افزارهایِ جدید (${names.length}):`);
names.forEach(n => console.log(`      • ${n}`));

// ورودِ مشتری و بررسیِ پنل
await p.goto(B + '/login', { waitUntil: 'domcontentloaded' });
await p.waitForTimeout(2500);
const ins = p.locator('input');
if (await ins.count() >= 2) {
  await ins.nth(0).fill('casey.demo@example.com');
  await ins.nth(1).fill('Password123!');
  await p.locator('button').filter({ hasText: /ورود|ادامه/ }).first().click().catch(async () => {
    await p.locator('button[type=submit]').first().click();
  });
  await p.waitForTimeout(4000);
  console.log(`\n── پس از ورودِ مشتری (${p.url().replace(B, '') || '/'}) ──`);

  const dl = await j('api/account/downloads');
  console.log(`   بارگیری‌ها        : ${dl ? (dl.length ?? '—') : '—'}  (پیش‌تر ۰ — صفحه خالی بود)`);
  const tk = await j('api/account/tickets');
  console.log(`   تیکت‌ها          : ${tk ? (tk.length ?? tk.items?.length) : '—'}  (پیش‌تر ۵)`);
  const nt = await j('api/account/notifications');
  console.log(`   اعلان‌ها         : ${nt ? (nt.length ?? nt.items?.length) : '—'}  (پیش‌تر ۳)`);

  // صفحهٔ بارگیری‌ها در پنل
  await p.goto(B + '/dashboard/downloads', { waitUntil: 'domcontentloaded' });
  await p.waitForTimeout(3000);
  const txt = (await p.innerText('body')).trim();
  console.log(`   صفحهٔ بارگیری‌ها  : ${txt.length} کاراکتر ${txt.length > 600 ? '✅ پر شده' : '⚠ بررسی شود'}`);
}

console.log(`\nخطاها: ${errs.length ? [...new Set(errs)].slice(0, 4).join(' | ') + ' ❌' : 'هیچ ✅'}`);
await b.close();
