// بررسیِ سلامتِ سایت: پاسخِ API، همهٔ صفحه‌ها، ورود، بخشِ اکوسیستم
import { chromium } from 'playwright-core';

const BASE = process.env.BASE || 'http://localhost:5210';
const b = await chromium.launch({ args: ['--no-sandbox'] });
const p = await b.newPage({ viewport: { width: 1440, height: 1000 } });
const errs = [];
p.on('pageerror', e => errs.push('PAGEERROR: ' + e.message.slice(0, 120)));
p.on('console', m => {
  if (m.type() === 'error' && !/dotnet\.wasm|ERR_CONNECTION|Failed to load resource/.test(m.text()))
    errs.push(m.text().slice(0, 120));
});

// ۱) آیا داده از پایگاه‌داده می‌رسد؟
await p.goto(BASE + '/catalog', { waitUntil: 'networkidle', timeout: 120000 });
await p.waitForTimeout(3000);
const api = await p.evaluate(async () => {
  const out = {};
  const get = async (u) => { try { const r = await fetch(u); return await r.json(); } catch (e) { return null; } };
  const s = await get('api/settings'); out.settings = s ? s.siteName : 'خطا';
  const pr = await get('api/products?pageSize=3'); out.products = pr ? pr.items.length + ' مورد' : 'خطا';
  const c = await get('api/categories'); out.categories = c ? c.length + ' مورد' : 'خطا';
  const bk = await get('api/products/bulk?ids=b-edari,b-tolidi');
  out.bulk = bk ? bk.length + ' بسته / ' + (bk[0]?.plans?.[0]?.priceToman ?? '—') : 'خطا';
  return out;
});
console.log('── پاسخِ API ──');
Object.entries(api).forEach(([k, v]) => console.log(`   ${k.padEnd(12)} ${v}`));

// ۲) همهٔ صفحه‌ها
const routes = ['/', '/catalog', '/pricing', '/solutions', '/systems', '/cart', '/compare',
  '/about', '/contact', '/support', '/blog', '/login', '/register', '/dashboard'];
console.log('\n── صفحه‌ها ──');
const bad = [];
for (const r of routes) {
  await p.goto(BASE + r, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await p.waitForTimeout(1600);
  const t = (await p.innerText('body')).trim();
  const ok = t.length > 400 && !t.includes('این صفحه وجود ندارد') && !await p.locator('.api-warn').count();
  if (!ok) bad.push(`${r}(${t.length})`);
  console.log(`   ${r.padEnd(13)} ${String(t.length).padStart(5)} ${ok ? '✅' : '❌'}`);
}

// ۳) اکوسیستم
await p.goto(BASE + '/', { waitUntil: 'networkidle' });
await p.waitForTimeout(4000);
console.log('\n── اکوسیستم ──');
console.log(`   تب‌ها: ${await p.locator('.eco-tab').count()}   کارت‌ها: ${await p.locator('.eco-card').count()}   رابط‌ها: ${await p.locator('.eco__wire').count()}`);
// صفحهٔ نخست حالا محصولاتِ ویژه را از API نسخهٔ ۱ می‌گیرد؛ پس وجودِ نامِ محصول
// دیگر نشانهٔ باقی‌ماندنِ دادهٔ قدیمی نیست. جایِ آن، «کارتِ محصول دارد» و
// «قیمتِ صفر نشان نمی‌دهد» بررسی می‌شود.
const homeCards = await p.locator('.sw-card, .prod-card').count();
const zeroPrices = await p.locator('.sw-card__price, .prod-card__price', { hasText: /^۰ تومان$/ }).count();
console.log(`   محصول در صفحهٔ نخست: ${homeCards} کارت   قیمتِ صفر: ${zeroPrices === 0 ? 'هیچ ✅' : zeroPrices + ' ❌'}`);

console.log(`\nخطاها: ${errs.length ? [...new Set(errs)].slice(0, 5).join(' | ') + ' ❌' : 'هیچ ✅'}`);
console.log(`صفحه‌های مشکل‌دار: ${bad.length ? bad.join(' , ') + ' ❌' : 'هیچ ✅'}`);
await b.close();
