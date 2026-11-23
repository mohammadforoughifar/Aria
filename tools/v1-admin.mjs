// آزمونِ موج ۳: پنلِ مدیریت روی /api/v1 — همهٔ صفحه‌ها با توکنِ مدیر.
import { chromium } from 'playwright-core';

const BASE = process.env.BASE || 'http://localhost:5210';
let pass = 0, fail = 0;
const ok = (name, cond, extra = '') => {
  console.log(`   ${cond ? 'PASS' : 'FAIL'}  ${name}${extra ? ' → ' + extra : ''}`);
  cond ? pass++ : fail++;
};

const b = await chromium.launch({ args: ['--no-sandbox'] });
const ctx = await b.newContext({ viewport: { width: 1440, height: 1000 }, locale: 'fa-IR' });
const p = await ctx.newPage();
const errors = [];
p.on('pageerror', e => errors.push(e.message.slice(0, 160)));

const setValue = async (el, v) => { await el.fill(v); await el.dispatchEvent('change'); };
const body = () => p.innerText('body');

// ─────────── ورودِ مدیر ───────────
console.log('\n■ ورودِ مدیر');
await p.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 120000 });
await p.waitForTimeout(2500);
await setValue(p.locator('input[type=email], input[placeholder*="@"]').first(), 'admin@northwindsoft.example');
await setValue(p.locator('input[type=password]').first(), 'Password123!');
await p.locator('button', { hasText: /ورود/ }).first().click();
await p.waitForTimeout(4000);
const token = await p.evaluate(() => localStorage.getItem('nw.v1.token'));
ok('توکنِ مدیر گرفته شد', !!token && token.split('.').length === 3);

// ─────────── نمایِ کلی ───────────
console.log('\n■ /dashboard/admin (GET /api/v1/admin/overview)');
await p.goto(BASE + '/dashboard/admin', { waitUntil: 'networkidle' });
await p.waitForTimeout(3500);
const dash = await body();
ok('کارت‌هایِ شاخص آمد', /درآمدِ این ماه/.test(dash) && /لایسنس‌های فعال/.test(dash));
ok('اسکلتِ بی‌پایان نماند', !dash.includes('در حالِ بارگیری') && (await p.locator('.sk').count()) === 0);
ok('اعداد واقعی (نه صفرِ همه‌جا)', /[۰-۹]/.test(dash));

const api = await p.evaluate(async (base) => {
  const t = localStorage.getItem('nw.v1.token');
  const r = await fetch(base + '/api/v1/admin/overview', { headers: { Authorization: 'Bearer ' + t } });
  const j = await r.json();
  return { products: j.totalProducts, customers: j.totalCustomers, series: j.revenueSeries.length, top: j.topProducts.length };
}, BASE);
console.log(`   (API: ${api.products} محصول، ${api.customers} کاربر، ${api.series} نقطهٔ درآمد، ${api.top} پرفروش)`);
ok('نمودارِ درآمد رسم شد', (await p.locator('svg').count()) > 0);

// ─────────── صفحه‌هایِ پنل ───────────
const pages = [
  ['admin/orders',           'سفارش',      /NW-2026-\d{4}/],
  ['admin/customers',        'کاربران',     /@example\.com/],
  ['admin/products',         'محصولات',     /سپیدار|محصول/],
  ['admin/licenses',         'لایسنس‌ها',   /کپی|فعال|تمدید/],
  ['admin/discounts',        'تخفیف‌ها',    /ARIA20/],
  ['admin/reviews',          'دیدگاه‌ها',   /دیدگاه|نقد|امتیاز/],
  ['admin/tickets',          'تیکت‌ها',     /TK-2026-\d{4}/],
  ['admin/blog',             'وبلاگ',       /مقاله|وبلاگ/],
  ['admin/demo-requests',    'درخواستِ دمو', /دمو/],
  ['admin/settings',         'تنظیمات',     /فروغ آریا|نامِ سایت/],
  ['admin/categories',       'دسته‌ها',     /دسته|مالی/],
];

for (const [route, label, needle] of pages) {
  await p.goto(`${BASE}/dashboard/${route}`, { waitUntil: 'networkidle' });
  await p.waitForTimeout(3000);
  const text = await body();
  const broken = /Unhandled|An unhandled error|خطایِ پیش‌بینی‌نشده/.test(text);
  const rows = await p.locator('tbody tr').count();
  ok(`${label} (${route})`, !broken && needle.test(text), broken ? 'صفحهٔ خطا' : `${rows} ردیف`);
}

// ─────────── ویرایشِ محصول ───────────
console.log('\n■ ویرایشِ یکی از محصولات');
const firstProductLink = await p.evaluate(async (base) => {
  const t = localStorage.getItem('nw.v1.token');
  const r = await fetch(base + '/api/v1/products?pageSize=1', { headers: { Authorization: 'Bearer ' + t } });
  return (await r.json()).items[0].id;
}, BASE);
await p.goto(`${BASE}/dashboard/admin/products/${firstProductLink}`, { waitUntil: 'networkidle' });
await p.waitForTimeout(3500);
const editText = await body();
ok('صفحهٔ ویرایشِ محصول پر شد', /نام|قیمت|پایه|ذخیره/.test(editText) && !/Unhandled/.test(editText));

// ─────────── دسترسیِ غیرمدیر ───────────
console.log('\n■ کاربرِ عادی نباید پنلِ مدیریت را ببیند');
await p.evaluate(() => localStorage.removeItem('nw.v1.token'));
await p.goto(BASE + '/login', { waitUntil: 'networkidle' });
await p.waitForTimeout(2500);
await setValue(p.locator('input[type=email], input[placeholder*="@"]').first(), 'casey.demo@example.com');
await setValue(p.locator('input[type=password]').first(), 'Password123!');
await p.locator('button', { hasText: /ورود/ }).first().click();
await p.waitForTimeout(3500);
await p.goto(BASE + '/dashboard/admin/orders', { waitUntil: 'networkidle' });
await p.waitForTimeout(3500);
ok('کاربرِ عادی به داشبورد برگشت', p.url().includes('/dashboard') && !p.url().includes('/dashboard/admin'), p.url().replace(BASE, ''));

const forbidden = await p.evaluate(async (base) => {
  const t = localStorage.getItem('nw.v1.token');
  const r = await fetch(base + '/api/v1/admin/orders', { headers: { Authorization: 'Bearer ' + t } });
  return r.status;
}, BASE);
ok('سرور هم ۴۰۳ می‌دهد', forbidden === 403, String(forbidden));

ok('بدونِ خطایِ جاوااسکریپت', errors.length === 0, errors.slice(0, 2).join(' | '));

await b.close();
console.log(`\n══ گذر: ${pass}    ناگذر: ${fail} ══`);
process.exit(fail === 0 ? 0 : 1);
