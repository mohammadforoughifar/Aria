// آزمونِ ویدیویِ آپارات: فرمِ محصول + نمایش در صفحهٔ محصول.
import { chromium } from 'playwright-core';

const BASE = process.env.BASE || 'http://localhost:5210';
let pass = 0, fail = 0;
const ok = (n, c, x = '') => { console.log(`   ${c ? 'PASS' : 'FAIL'}  ${n}${x ? ' → ' + x : ''}`); c ? pass++ : fail++; };

const b = await chromium.launch({ args: ['--no-sandbox'] });
const p = await (await b.newContext({ viewport: { width: 1440, height: 1000 }, locale: 'fa-IR' })).newPage();
const errors = [];
p.on('pageerror', e => errors.push(e.message.slice(0, 140)));
const set = async (l, v) => { await l.fill(v); await l.dispatchEvent('change'); };

// ── صفحهٔ محصول: نمایشِ ویدیو ──
console.log('■ صفحهٔ محصول (بی‌نیاز از ورود)');
await p.goto(BASE + '/products/sepidaar', { waitUntil: 'networkidle', timeout: 120000 });
await p.waitForTimeout(3000);
const body = await p.innerText('body');
ok('بخشِ «ویدیویِ معرفی» آمد', body.includes('ویدیویِ معرفی'));
ok('دکمهٔ پخش هست', (await p.locator('button[aria-label*="پخش"]').count()) > 0);

await p.locator('button[aria-label*="پخش"]').first().click();
await p.waitForTimeout(1200);
const iframe = await p.locator('iframe[src*="aparat.com"]').first().getAttribute('src').catch(() => null);
ok('پخش‌کنندهٔ آپارات جای‌گذاری شد', !!iframe && iframe.includes('videohash/nlk1rob'), iframe ?? '—');
ok('allowfullscreen دارد', (await p.locator('iframe[src*="aparat.com"][allowfullscreen]').count()) > 0);
ok('لینکِ «دیدن در آپارات» هست', (await p.locator('a[href*="aparat.com"]').count()) > 0);

// محصولی که ویدیو ندارد نباید بخشِ خالی بسازد
await p.goto(BASE + '/products/kanoon', { waitUntil: 'networkidle' });
await p.waitForTimeout(2500);
ok('محصولِ بدونِ ویدیو بخشِ خالی ندارد', !(await p.innerText('body')).includes('ویدیویِ معرفی'));

// ── فرمِ مدیریت ──
console.log('\n■ فرمِ مدیریت');
await p.goto(BASE + '/login', { waitUntil: 'networkidle' });
await p.waitForTimeout(2500);
await set(p.locator('input[type=email], input[placeholder*="@"]').first(), 'admin@northwindsoft.example');
await set(p.locator('input[type=password]').first(), 'Password123!');
await p.locator('button', { hasText: /ورود/ }).first().click();
await p.waitForTimeout(4000);

const id = await p.evaluate(async (base) => {
  const r = await fetch(base + '/api/v1/products/slug/mehraz');
  return (await r.json()).id;
}, BASE);
await p.goto(`${BASE}/dashboard/admin/products/${id}`, { waitUntil: 'networkidle' });
await p.waitForTimeout(3000);

const box = p.locator('input[placeholder*="aparat.com/v"]');
ok('کادرِ لینکِ آپارات در فرم هست', await box.count() > 0);
ok('لینکِ فعلی بارگذاری شد', (await box.inputValue()).includes('aparat.com'), await box.inputValue());

// لینکِ نامعتبر → پیامِ خطا
await box.fill('https://example.com/video/123');
await box.dispatchEvent('input');
await p.waitForTimeout(600);
ok('لینکِ نامعتبر پیام می‌دهد', (await p.innerText('body')).includes('فقط لینکِ آپارات'));

// لینکِ درست → تأیید + ذخیره
await box.fill('https://www.aparat.com/v/0sPat/معرفی');
await box.dispatchEvent('input');
await p.waitForTimeout(600);
ok('لینکِ درست شناسایی شد', (await p.innerText('body')).includes('لینک شناسایی شد'));

await p.locator('button.btn--primary', { hasText: /ذخیره/ }).first().click();
await p.waitForTimeout(4500);

const saved = await p.evaluate(async (base) => {
  const r = await fetch(base + '/api/v1/products/slug/mehraz');
  const j = await r.json();
  return j.demoVideoUrl;
}, BASE);
ok('لینک در سرور ذخیره شد', saved === 'https://www.aparat.com/v/0sPat/معرفی', String(saved));

// برگرداندن به لینکِ قبلی تا حالتِ نمایشی سالم بماند
await p.goto(`${BASE}/dashboard/admin/products/${id}`, { waitUntil: 'networkidle' });
await p.waitForTimeout(2500);
const box2 = p.locator('input[placeholder*="aparat.com/v"]');
await box2.fill('https://www.aparat.com/v/nlk1rob');
await box2.dispatchEvent('input');
await p.locator('button.btn--primary', { hasText: /ذخیره/ }).first().click();
await p.waitForTimeout(4000);

ok('بدونِ خطایِ جاوااسکریپت', errors.length === 0, errors.slice(0, 2).join(' | '));
await b.close();
console.log(`\n══ گذر: ${pass}    ناگذر: ${fail} ══`);
process.exit(fail === 0 ? 0 : 1);
