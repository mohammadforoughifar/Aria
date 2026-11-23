// آزمونِ موج ۲: ورود با توکن و داشبوردِ مشتری روی /api/v1.
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
p.on('pageerror', e => errors.push(e.message.slice(0, 140)));

const setValue = async (input, value) => { await input.fill(value); await input.dispatchEvent('change'); };

// ─────────── ۱) ورود ───────────
console.log('\n■ ورود (POST /api/v1/auth/login)');
await p.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 120000 });
await p.waitForTimeout(2500);

// پیامِ خطایِ سرور برایِ رمزِ اشتباه
await setValue(p.locator('input[type=email], input[placeholder*="@"]').first(), 'casey.demo@example.com');
await setValue(p.locator('input[type=password]').first(), 'wrongpassword');
await p.locator('button', { hasText: /ورود/ }).first().click();
await p.waitForTimeout(2500);
const errText = await p.locator('body').innerText();
ok('رمزِ اشتباه پیامِ فارسیِ سرور را می‌دهد',
   errText.includes('ایمیل یا رمزِ عبور درست نیست'), 
   (errText.match(/ایمیل یا رمز[^\n]*/) ?? ['—'])[0].slice(0, 50));

// ورودِ درست
await setValue(p.locator('input[type=password]').first(), 'Password123!');
await p.locator('button', { hasText: /ورود/ }).first().click();
await p.waitForTimeout(4000);
ok('پس از ورود به داشبورد رفت', p.url().includes('/dashboard'), p.url().replace(BASE, ''));

const token = await p.evaluate(() => localStorage.getItem('nw.v1.token'));
ok('توکن در localStorage ذخیره شد', !!token && token.split('.').length === 3,
   token ? `${token.slice(0, 18)}… (${token.split('.').length} بخش)` : 'خالی');
ok('نشستِ قدیمی پاک شد', (await p.evaluate(() => localStorage.getItem('nw.user'))) === null);

// ─────────── ۲) داشبورد ───────────
console.log('\n■ داشبورد (GET /api/v1/account/overview)');
await p.waitForTimeout(2500);
const body = await p.innerText('body');
ok('نامِ کاربر از نسخهٔ ۱', body.includes('کیان رستگار'), (body.match(/سلام،[^\n]*/) ?? ['—'])[0]);
ok('کارت‌هایِ شاخص رندر شد', (await p.locator('.stat, .stat-card, [class*="stat"]').count()) > 0);

// خروجیِ API برایِ مقایسه
const api = await p.evaluate(async (base) => {
  const t = localStorage.getItem('nw.v1.token');
  const r = await fetch(base + '/api/v1/account/overview', { headers: { Authorization: 'Bearer ' + t } });
  const j = await r.json();
  return { orders: j.orderCount, licenses: j.activeLicenses, spent: j.totalSpentToman };
}, BASE);
console.log(`   (API: ${api.orders} سفارش، ${api.licenses} لایسنسِ فعال، ${api.spent} تومان)`);

// ─────────── ۳) سفارش‌ها ───────────
console.log('\n■ سفارش‌ها (GET /api/v1/orders)');
await p.goto(BASE + '/dashboard/orders', { waitUntil: 'networkidle' });
await p.waitForTimeout(3000);
const orderRows = await p.locator('tbody tr').count();
ok('ردیف‌هایِ سفارش آمد', orderRows >= 2, orderRows + ' ردیف');
const orderText = await p.innerText('tbody');
ok('شمارهٔ سفارشِ نسخهٔ ۱ (NW-…)', /NW-\d{4}-\d{4}/.test(orderText),
   (orderText.match(/NW-\d{4}-\d{4}/) ?? ['—'])[0]);
ok('شمارهٔ نسخهٔ ۱ (نه قدیمی)', /NW-2026-\d{4}/.test(orderText), (orderText.match(/NW-\d{4}-\d{4}/) ?? ['—'])[0]);
ok('مبلغِ سفارش نمایش داده شد', /[\d۰-۹٬,]{4,}\s*تومان/.test(orderText) && !/^۰ تومان/.test(orderText),
   (orderText.match(/[\d۰-۹٬,]{4,} تومان/) ?? ['—'])[0]);

// ─────────── ۴) لایسنس‌ها ───────────
console.log('\n■ لایسنس‌ها (GET /api/v1/licenses)');
await p.goto(BASE + '/dashboard/licenses', { waitUntil: 'networkidle' });
await p.waitForTimeout(3000);
const licText = await p.innerText('body');
ok('لایسنس‌ها آمد', /[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}/.test(licText),
   (licText.match(/[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}/) ?? ['—'])[0]);

// ─────────── ۵) فاکتورها ───────────
console.log('\n■ فاکتورها (GET /api/v1/invoices)');
await p.goto(BASE + '/dashboard/invoices', { waitUntil: 'networkidle' });
await p.waitForTimeout(3000);
const invText = await p.innerText('body');
ok('فاکتورها آمد', /INV-\d{4}-\d{4}/.test(invText), (invText.match(/INV-\d{4}-\d{4}/) ?? ['—'])[0]);

// ─────────── ۶) تیکت‌ها ───────────
console.log('\n■ تیکت‌ها (GET /api/v1/tickets)');
await p.goto(BASE + '/dashboard/tickets', { waitUntil: 'networkidle' });
await p.waitForTimeout(3000);
const tickRows = await p.locator('tbody tr, .ticket, [class*="ticket-row"]').count();
ok('فهرستِ تیکت آمد', tickRows >= 1, tickRows + ' ردیف');

// ─────────── ۶ب) جزئیاتِ تیکت و پاسخ ───────────
console.log('\n■ جزئیاتِ تیکت (GET/POST /api/v1/tickets/{id})');
const firstTicket = p.locator('tbody tr').first();
if (await firstTicket.count() > 0)
{
    await firstTicket.click();
    await p.waitForTimeout(3000);
    ok('صفحهٔ جزئیاتِ تیکت باز شد', /TK-۲۰۲۶|TK-2026/.test(await p.innerText('body')));
    const box = p.locator('textarea').first();
    if (await box.count() > 0)
    {
        await box.fill('پاسخِ آزمایشی از آزمونِ خودکار.');
        await p.locator('button', { hasText: /ارسال|پاسخ/ }).first().click();
        await p.waitForTimeout(3000);
        ok('پاسخ ثبت شد', (await p.innerText('body')).includes('پاسخِ آزمایشی از آزمونِ خودکار'));
    }
    else ok('کادرِ پاسخ هست', false);
}
else ok('صفحهٔ جزئیاتِ تیکت باز شد', false);
await p.goto(BASE + '/dashboard/tickets', { waitUntil: 'networkidle' });
await p.waitForTimeout(1500);

// ─────────── ۷) دانلودها ───────────
console.log('\n■ دانلودها (GET /api/v1/downloads)');
await p.goto(BASE + '/dashboard/downloads', { waitUntil: 'networkidle' });
await p.waitForTimeout(3000);
const dlText = await p.innerText('body');
ok('صفحهٔ دانلود بارگیری شد', !dlText.includes('خطا در بارگیری'));

// ─────────── ۸) پروفایل ───────────
console.log('\n■ پروفایل (PUT /api/v1/account/me)');
await p.goto(BASE + '/dashboard/profile', { waitUntil: 'networkidle' });
await p.waitForTimeout(2500);
const nameInput = p.locator('input').first();
await setValue(nameInput, 'کیان رستگار');
await p.locator('button', { hasText: /ذخیره/ }).first().click();
await p.waitForTimeout(2500);
const profileText = await p.innerText('body');
ok('ذخیرهٔ پروفایل موفق بود', /ذخیره شد|تغییرات/.test(profileText));

// رمزِ فعلیِ غلط → پیامِ سرور
const pwds = p.locator('input[type=password]');
await setValue(pwds.nth(0), 'definitely-wrong');
await setValue(pwds.nth(1), 'NewPassword123!');
await setValue(pwds.nth(2), 'NewPassword123!');
await p.locator('button', { hasText: /تغییر/ }).first().click();
await p.waitForTimeout(2500);
const afterPw = await p.innerText('body');
ok('رمزِ فعلیِ غلط پیام می‌دهد', /رمز|نادرست|تغییر نکرد/.test(afterPw));

// ─────────── ۹) پایداریِ نشست و خروج ───────────
console.log('\n■ نشست');
await p.goto(BASE + '/dashboard', { waitUntil: 'networkidle' });
await p.reload({ waitUntil: 'networkidle' });
await p.waitForTimeout(3000);
ok('نشست پس از رفرش می‌ماند', p.url().includes('/dashboard') && !p.url().includes('/login'), p.url().replace(BASE, ''));

await p.evaluate(() => localStorage.removeItem('nw.v1.token'));
await p.goto(BASE + '/dashboard/orders', { waitUntil: 'networkidle' });
await p.waitForTimeout(3000);
ok('بدونِ توکن به ورود هدایت می‌شود', p.url().includes('/login'), p.url().replace(BASE, ''));

ok('بدونِ خطایِ جاوااسکریپت', errors.length === 0, errors.slice(0, 2).join(' | '));

await p.goto(BASE + '/login', { waitUntil: 'networkidle' });
await b.close();
console.log(`\n══ گذر: ${pass}    ناگذر: ${fail} ══`);
process.exit(fail === 0 ? 0 : 1);
