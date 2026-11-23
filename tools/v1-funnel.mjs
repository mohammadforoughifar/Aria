// آزمونِ سرتاسریِ قیفِ خرید رویِ API نسخهٔ ۱.
//
// مسیر: کاتالوگ ← افزودن به سبد ← سبد ← تسویه‌حساب ← ثبتِ سفارش.
// هر مرحله هم ظاهرِ صفحه بررسی می‌شود و هم اینکه داده واقعاً از /api/v1 آمده
// باشد (نه از پشتهٔ قدیمی و نه از دادهٔ ساختگی).
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

// Blazor با رویدادِ «change» بایند می‌کند، نه «input»؛ پس صریح می‌فرستیمش.
const setValue = async (input, value) => {
  await input.fill(value);
  await input.dispatchEvent('change');
};

const pageErrors = [];
p.on('pageerror', e => pageErrors.push(e.message.slice(0, 140)));

// ─────────── ۰) سبد را خالی کن (تکرارپذیریِ آزمون) ───────────
await p.goto(BASE + '/catalog', { waitUntil: 'networkidle', timeout: 120000 });
await p.evaluate(async (base) => {
  for (const k of ['demo', 'guest'])
    try { await fetch(base + '/api/v1/cart?sessionKey=' + k, { method: 'DELETE' }); } catch {}
}, BASE);

// ─────────── ۱) کاتالوگ ───────────
console.log('\n■ کاتالوگ (GET /api/v1/products)');
await p.goto(BASE + '/catalog', { waitUntil: 'networkidle', timeout: 120000 });
await p.waitForTimeout(2000);

const v1 = await p.evaluate(async (base) => {
  const r = await fetch(base + '/api/v1/products?pageSize=12&sort=popular');
  const j = await r.json();
  return { total: j.meta.totalCount, first: j.items[0]?.name, firstSlug: j.items[0]?.slug, price: j.items[0]?.basePriceToman };
}, BASE).catch(() => null);

await p.reload({ waitUntil: 'networkidle' });
await p.waitForTimeout(2500);

const cards = await p.locator('.sw-card').count();
ok('کارتِ محصول رندر شد', cards > 0, cards + ' کارت');

const firstCardName = await p.locator('.sw-card__name').first().innerText();
ok('نامِ کارت همان نامِ نسخهٔ ۱ است',
   !!v1?.first && firstCardName.trim() === v1.first.trim(),
   `کارت: ${firstCardName.trim()} / api: ${v1?.first}`);

const countText = await p.locator('text=/نرم‌افزار یافت شد/').first().innerText().catch(() => '');
ok('شمارشِ نتیجه با متادیتای نسخهٔ ۱ یکی است',
   countText.replace(/[^۰-۹0-9]/g, '') !== '' &&
   Math.abs(parseInt(countText.replace(/[۰-۹]/g, d => '۰۱۲۳۴۵۶۷۸۹'.indexOf(d)).replace(/[^0-9]/g, ''), 10) - (v1?.total ?? -1)) === 0,
   `${countText.trim()} / api: ${v1?.total}`);

// هیچ «۰» گمراه‌کننده‌ای در کارت نباشد
const zeroMoney = await p.locator('.sw-card__price', { hasText: /^۰ تومان$/ }).count();
ok('قیمتِ صفر نمایش داده نمی‌شود', zeroMoney === 0);

// ─────────── ۲) افزودن به سبد از کارت ───────────
console.log('\n■ افزودن به سبد (POST /api/v1/cart/items)');
const addBtn = p.locator('.sw-card').first().locator('button', { hasText: 'افزودن' });
await addBtn.click();
await p.waitForTimeout(3500);

const url = p.url();
ok('به صفحهٔ سبد رفت', url.includes('/cart'), url.replace(BASE, ''));

const lines = await p.locator('.cart-line').count();
ok('سبد حداقل یک ردیف دارد', lines >= 1, lines + ' ردیف');

const total = await p.locator('.summary__row--total .num').first().innerText().catch(() => '');
ok('مبلغِ قابلِ پرداخت محاسبه شد', /تومان/.test(total) && total.trim() !== '۰ تومان', total.trim());

const sub = await p.locator('.summary__row .num').first().innerText().catch(() => '');
ok('جمعِ کل با مالیات جمع می‌خورد', sub !== total && /تومان/.test(sub), sub.trim() + ' / ' + total.trim());

// ─────────── ۳) کدِ تخفیف ───────────
console.log('\n■ کدِ تخفیف (POST /api/v1/cart/discount)');
await setValue(p.locator('input[placeholder="مثل LAUNCH25"]'), 'ARIA20');
await p.locator('button', { hasText: /اعمال/ }).click();
await p.waitForTimeout(2500);
const discountRow = await p.locator('text=/تخفیف \\(/').count();
ok('ردیفِ تخفیف آمد', discountRow > 0);
const shownTotal = await p.locator('.summary__row--total .num').first().innerText();
ok('مبلغ پس از تخفیف کمتر شد', shownTotal !== total, total.trim() + ' ← ' + shownTotal.trim());

// کدِ نامعتبر → پیامِ خودِ سرور
await setValue(p.locator('input[placeholder="مثل LAUNCH25"]'), 'NOPE123');
await p.locator('button', { hasText: /اعمال/ }).click();
await p.waitForTimeout(2500);
const errText = await p.locator('p.tiny').filter({ hasText: /کد|منقضی|معتبر/ }).first().innerText().catch(() => '');
ok('کدِ نامعتبر پیامِ فارسیِ سرور را نشان می‌دهد', /کد/.test(errText), errText.trim().slice(0, 60));

// ─────────── ۴) تسویه‌حساب ───────────
console.log('\n■ تسویه‌حساب (POST /api/v1/checkout)');
await p.locator('a', { hasText: 'ادامه و تسویه' }).click();
await p.waitForTimeout(2500);

const fillField = async (label, value) => {
  const field = p.locator('label.field').filter({ hasText: label }).first();
  await setValue(field.locator('input'), value);
};

await fillField('نام و نام خانوادگی', 'آزمون خودکار');
await setValue(p.locator('input[placeholder="name@company.com"]'), 'funnel@example.com');
await fillField('شرکت', 'شرکت آزمون');
await fillField('کشور', 'Iran');

// فاکتورِ رسمی: خریدارِ حقوقی
await p.locator('button', { hasText: 'شخصِ حقوقی' }).click();
await p.waitForTimeout(500);
ok('فیلدهایِ فاکتورِ رسمی ظاهر شد', (await p.locator('text=کدِ اقتصادی').count()) > 0);
await fillField('نامِ حقوقی شرکت', 'شرکت آزمونِ سرتاسری');

await p.locator('.card--pad button').filter({ hasText: 'ادامه' }).first().click();
await p.waitForTimeout(1800);
ok('به گامِ پرداخت رفتیم', (await p.locator('text=روش پرداخت').count()) > 0);

// روشِ پرداخت پیش‌فرض انتخاب شده است
await p.locator('.card--pad button').filter({ hasText: 'ادامه' }).first().click();
await p.waitForTimeout(1800);

const reviewVisible = await p.locator('text=بازبینی نهایی').count();
ok('گامِ بازبینیِ نهایی', reviewVisible > 0);

const payBtn = p.locator('button', { hasText: /پرداختِ/ }).first();
const payLabel = await payBtn.innerText();
ok('مبلغ رویِ دکمهٔ پرداخت نوشته شده', /تومان/.test(payLabel), payLabel.trim());

await payBtn.click();
await p.waitForTimeout(6000);

const success = await p.locator('text=سفارش شما ثبت شد').count();
ok('سفارش ثبت شد', success > 0);

const orderNo = await p.locator('.num--lat').first().innerText().catch(() => '');
ok('شمارهٔ سفارشِ نسخهٔ ۱ (NW-…)', /^NW-/.test(orderNo.trim()), orderNo.trim());

// ─────────── ۵) سفارش واقعاً در پایگاه‌دادهٔ نسخهٔ ۱ هست ───────────
console.log('\n■ بررسیِ نهایی از سمتِ سرور');
const persisted = await p.evaluate(async (base) => {
  try {
    const r = await fetch(base + '/api/v1/auth/login', {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: 'admin@northwindsoft.example', password: 'Password123!' })
    });
    const a = await r.json();
    const r2 = await fetch(base + '/api/v1/admin/orders?pageSize=5', {
      headers: { Authorization: 'Bearer ' + a.token }
    });
    const j = await r2.json();
    return { count: j.meta?.totalCount ?? 0, last: j.items?.[0]?.orderNumber ?? '' };
  } catch (e) { return { error: e.message }; }
}, BASE);
ok('سفارش در فهرستِ مدیر دیده می‌شود', (persisted?.count ?? 0) > 0,
   `${persisted?.count} سفارش، آخرین: ${persisted?.last}`);
ok('آخرین سفارش همان سفارشِ ماست', (persisted?.last ?? '') === orderNo.trim(),
   `${persisted?.last} / ${orderNo.trim()}`);

// ─────────── ۶) خطایِ صفحه ───────────
ok('بدونِ خطایِ جاوااسکریپت', pageErrors.length === 0, pageErrors.slice(0, 2).join(' | '));

await p.screenshot({ path: '/home/user/shots/v1-checkout-success.png', type: 'jpeg', quality: 72 });
await p.goto(BASE + '/cart', { waitUntil: 'networkidle' });
await p.waitForTimeout(2000);
await p.screenshot({ path: '/home/user/shots/v1-cart-empty.png', type: 'jpeg', quality: 72 });

console.log(`\n══ گذر: ${pass}    ناگذر: ${fail} ══`);
await b.close();
process.exit(fail === 0 ? 0 : 1);
