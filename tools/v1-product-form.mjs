// آزمونِ فرمِ محصول: ریال، دستهٔ تازه، امکاناتِ یکی‌یکی و طرح‌هایِ قیمت.
import { chromium } from 'playwright-core';

const BASE = process.env.BASE || 'http://localhost:5210';
let pass = 0, fail = 0;
const ok = (n, c, x = '') => { console.log(`   ${c ? 'PASS' : 'FAIL'}  ${n}${x ? ' → ' + x : ''}`); c ? pass++ : fail++; };

const slug = 'rial-test-' + Math.random().toString(36).slice(2, 7);
const catName = 'دستهٔ آزمونِ ' + Math.random().toString(36).slice(2, 5);

const b = await chromium.launch({ args: ['--no-sandbox'] });
const p = await (await b.newContext({ viewport: { width: 1500, height: 1100 }, locale: 'fa-IR' })).newPage();
const errors = [];
p.on('pageerror', e => errors.push(e.message.slice(0, 140)));

const set = async (loc, v) => { await loc.fill(v); await loc.dispatchEvent('change'); };
const byLabel = (t) => p.locator(`xpath=//span[contains(@class,'field__label') and contains(., '${t}')]/following::input[1]`);

await p.goto(BASE + '/login', { waitUntil: 'networkidle', timeout: 120000 });
await p.waitForTimeout(2500);
await set(p.locator('input[type=email], input[placeholder*="@"]').first(), 'admin@northwindsoft.example');
await set(p.locator('input[type=password]').first(), 'Password123!');
await p.locator('button', { hasText: /ورود/ }).first().click();
await p.waitForTimeout(4000);

console.log('\n■ فرمِ محصولِ تازه');
await p.goto(BASE + '/dashboard/admin/products/new', { waitUntil: 'networkidle' });
await p.waitForTimeout(3000);

// ── دستهٔ تازه ──
const catAddBtn = p.locator('div.field:has(span.field__label:text("دسته‌بندی")) button').first();
await catAddBtn.click();
await p.waitForTimeout(500);
const catInput = p.locator('input[placeholder="نامِ دستهٔ تازه"]');
ok('کادرِ ساختِ دسته باز شد', await catInput.count() > 0);
await catInput.fill(catName);
await p.locator('button', { hasText: /بساز/ }).first().click();
await p.waitForTimeout(2500);
const catText = await p.innerText('body');
ok('دستهٔ تازه ساخته شد', catText.includes(catName), catName);

const selectedCat = await p.locator('div.field:has(span.field__label:text("دسته‌بندی")) select').first().inputValue();
ok('دستهٔ تازه انتخاب شد', selectedCat.length > 10, selectedCat);

// ── نام و شناسه ──
await set(byLabel('نام محصول'), 'آزمونِ فرمِ ریال');
await set(byLabel('شناسه (slug)'), slug);
await set(byLabel('شعارِ کوتاه'), 'برایِ آزمون');
await p.locator('textarea').first().fill('توضیحِ کاملِ محصول برایِ آزمونِ خودکارِ فرم.');
await p.locator('textarea').first().dispatchEvent('change');

// ── قیمت به ریال ──
const priceInput = p.locator('.field:has(span.field__label:text("قیمتِ پایه (ریال)")) input').first();
await priceInput.fill('12500000');
await priceInput.dispatchEvent('input');
await p.waitForTimeout(600);
const hint = await p.innerText('body');
ok('معادلِ تومان زیرِ قیمت نشان داده شد', /۱٬۲۵۰٬۰۰۰ تومان/.test(hint),
   (hint.match(/معادل:[^\n]*/) ?? ['—'])[0]);

// ── امکاناتِ یکی‌یکی ──
const featInput = p.locator('input[placeholder*="پشتیبانیِ کاملِ مالیات"]');
await featInput.fill('پشتیبانیِ کاملِ مالیات');
await featInput.press('Enter');
await p.waitForTimeout(300);
await featInput.fill('گزارش‌هایِ مدیریتی');
await featInput.press('Enter');
await p.waitForTimeout(300);
await featInput.fill('دسترسی از راهِ دور');
await p.locator('button', { hasText: /افزودن/ }).first().click();
await p.waitForTimeout(500);
const chipCount = await p.locator('span.chip').count();
ok('سه امکان یکی‌یکی اضافه شد', chipCount >= 3, chipCount + ' چیپ');

// ── نیازمندی‌ها ──
const reqInput = p.locator('input[placeholder*="ویندوز ۱۰ یا بالاتر"]');
await reqInput.fill('ویندوز ۱۱');
await reqInput.press('Enter');
await p.waitForTimeout(300);
ok('نیازمندی اضافه شد', (await p.innerText('body')).includes('ویندوز ۱۱'));

// ── طرحِ قیمت ──
await p.locator('input[placeholder="نامِ طرح — مثل: تک‌کاربره"]').fill('تک‌کاربره');
const planPrice = p.locator('input[placeholder="قیمتِ طرح (ریال)"]');
await planPrice.fill('12000000');
await planPrice.dispatchEvent('input');
await p.waitForTimeout(400);
const afterPlanInput = await p.innerText('body');
ok('معادلِ تومانِ طرح نشان داده شد', /۱٬۲۰۰٬۰۰۰ تومان/.test(afterPlanInput));

await p.locator('button', { hasText: /افزودن به فهرست/ }).first().click();
await p.waitForTimeout(700);
ok('طرح به فهرست اضافه شد', /۱ طرح/.test(await p.innerText('body')));

// ── ذخیره ──
await p.locator('button.btn--primary', { hasText: /ذخیره/ }).first().click();
await p.waitForTimeout(5000);
ok('پس از ذخیره به فهرست برگشت', p.url().includes('/dashboard/admin/products') && !p.url().includes('/new'),
   p.url().replace(BASE, ''));

// ── بررسیِ سرور ──
const saved = await p.evaluate(async ([base, s]) => {
    const r = await fetch(`${base}/api/v1/products/slug/${s}`);
    if (!r.ok) return { status: r.status };
    const j = await r.json();
    return {
        status: r.status, name: j.name, price: j.basePriceToman, category: j.categoryName,
        features: j.features.length, reqs: j.systemRequirements.length,
        plans: j.plans.map(x => ({ name: x.name, price: x.priceToman, seats: x.seats }))
    };
}, [BASE, slug]);

ok('محصول رویِ سرور ساخته شد', saved.status === 200, 'HTTP ' + saved.status);
ok('قیمتِ ریالی درست ذخیره شد (۱۲٬۵۰۰٬۰۰۰ ریال = ۱٬۲۵۰٬۰۰۰ تومان)', saved.price === 1250000, String(saved.price));
ok('دستهٔ تازه رویِ محصول نشست', saved.category === catName, saved.category);
ok('امکانات ذخیره شد', saved.features === 3, String(saved.features) + ' مورد');
ok('نیازمندی ذخیره شد', saved.reqs === 1, String(saved.reqs) + ' مورد');
ok('طرحِ قیمت واقعاً ساخته شد', saved.plans?.length === 1 && saved.plans[0].price === 1200000,
   JSON.stringify(saved.plans));

// ── ویرایشِ همان محصول: قیمت باید ریالی نمایش داده شود ──
await p.goto(`${BASE}/dashboard/product/${slug}`, { waitUntil: 'networkidle' }).catch(() => {});
await p.goto(BASE + '/dashboard/admin/products', { waitUntil: 'networkidle' });
await p.waitForTimeout(2500);
const productId = await p.evaluate(async ([base, s]) => {
    const r = await fetch(`${base}/api/v1/products/slug/${s}`);
    return (await r.json()).id;
}, [BASE, slug]);
await p.goto(`${BASE}/dashboard/admin/products/${productId}`, { waitUntil: 'networkidle' });
await p.waitForTimeout(3000);
const editPrice = await p.locator('.field:has(span.field__label:text("قیمتِ پایه (ریال)")) input').first().inputValue();
ok('ویرایش: قیمت به ریال نشان داده می‌شود', editPrice.startsWith('12500000'), editPrice);

ok('بدونِ خطایِ جاوااسکریپت', errors.length === 0, errors.slice(0, 2).join(' | '));

await b.close();
console.log(`\n══ گذر: ${pass}    ناگذر: ${fail} ══`);
process.exit(fail === 0 ? 0 : 1);
