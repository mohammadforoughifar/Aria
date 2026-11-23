// بررسیِ چیدمانِ فهرست‌ها: چندستونگی در دسکتاپ، تک‌ستونگی در موبایل،
// نبودِ سرریزِ افقی و نبودِ هم‌پوشانی — در همهٔ صفحه‌ها و پهناها.
import { chromium } from 'playwright-core';

const BASE = 'http://localhost:5210';
const PATHS = ['/', '/catalog', '/pricing', '/blog', '/solutions', '/systems', '/support', '/about', '/contact', '/dashboard', '/compare'];
const WIDTHS = [1920, 1440, 1280, 1024, 900, 768, 600, 390, 360];

let pass = 0, fail = 0;
const failures = [];
const ok = (cond, label, detail = '') => {
  if (cond) { pass++; }
  else { fail++; failures.push(`${label}${detail ? ' — ' + detail : ''}`); }
};

const browser = await chromium.launch({ args: ['--no-sandbox'] });

// ── ۱. سرریزِ افقی و هم‌پوشانی در همهٔ صفحه‌ها × پهناها ───────────────
for (const path of PATHS) {
  for (const w of WIDTHS) {
    const ctx = await browser.newContext({ viewport: { width: w, height: 1000 } });
    const page = await ctx.newPage();
    const errs = [];
    page.on('console', m => { if (m.type() === 'error') errs.push(m.text()); });
    page.on('pageerror', e => errs.push(String(e)));
    try {
      await page.goto(BASE + path, { waitUntil: 'domcontentloaded', timeout: 120000 });
      await page.waitForTimeout(2600);
      const r = await page.evaluate(() => {
        const vw = document.documentElement.clientWidth;
        const over = [];
        document.querySelectorAll('body *').forEach(el => {
          const b = el.getBoundingClientRect();
          if (b.width === 0 || b.height === 0) return;
          const cs = getComputedStyle(el);
          if (cs.position === 'fixed') return;
          if (b.right > vw + 1 || b.left < -1) {
            over.push(el.tagName.toLowerCase() + '.' + (el.className || '').toString().trim().split(/\s+/)[0]);
          }
        });
        // هم‌پوشانی فقط میان فرزندانِ یک شبکه
        let overlap = 0;
        document.querySelectorAll('[class*=grid]').forEach(g => {
          const kids = [...g.children].map(k => k.getBoundingClientRect()).filter(x => x.width > 0 && x.height > 0);
          for (let i = 0; i < kids.length; i++) for (let j = i + 1; j < kids.length; j++) {
            const a = kids[i], c = kids[j];
            const ox = Math.max(0, Math.min(a.right, c.right) - Math.max(a.left, c.left));
            const oy = Math.max(0, Math.min(a.bottom, c.bottom) - Math.max(a.top, c.top));
            if (ox > 2 && oy > 2) overlap++;
          }
        });
        return { vw, scrollW: document.documentElement.scrollWidth, over: [...new Set(over)].slice(0, 4), overlap };
      });
      ok(r.scrollW <= r.vw + 1, `سرریزِ افقی ${path} @${w}`,
        r.scrollW > r.vw + 1 ? `پیمایش ${r.scrollW} > ${r.vw} | ${r.over.join(' , ')}` : '');
      ok(r.overlap === 0, `هم‌پوشانی کارت‌ها ${path} @${w}`, r.overlap ? `${r.overlap} مورد` : '');
      ok(errs.length === 0, `خطای کنسول ${path} @${w}`, errs.slice(0, 2).join(' | '));
    } catch (e) {
      ok(false, `بارگیری ${path} @${w}`, String(e).slice(0, 90));
    }
    await ctx.close();
  }
}

// ── ۲. چندستونگیِ فهرست‌ها در دسکتاپ و تک‌ستونگی در موبایل ─────────────
const LISTS = [
  ['/',        '.prod-card',  2],
  ['/',        '.card',       2],
  ['/catalog', '.cat-card',   3],
  ['/catalog', '.sw-card',    2],
  ['/blog',    '.card',       2],
  ['/systems', '.card',       2],
  ['/solutions', '.card',     2],
];
const perRow = async (path, sel, width) => {
  const ctx = await browser.newContext({ viewport: { width, height: 1000 } });
  const page = await ctx.newPage();
  await page.goto(BASE + path, { waitUntil: 'domcontentloaded', timeout: 120000 });
  await page.waitForSelector(sel, { timeout: 120000 }).catch(() => {});
  await page.waitForTimeout(2600);
  const v = await page.evaluate((sel) => {
    const els = [...document.querySelectorAll(sel)];
    if (els.length < 2) return 0;
    const tops = {};
    els.forEach(e => { const t = Math.round(e.getBoundingClientRect().top / 4) * 4; tops[t] = (tops[t] || 0) + 1; });
    return Math.max(...Object.values(tops));
  }, sel);
  await ctx.close();
  return v;
};

for (const [path, sel, minDesk] of LISTS) {
  const desk = await perRow(path, sel, 1440);
  ok(desk >= minDesk, `دسکتاپ ${path} ${sel} دست‌کم ${minDesk} در ردیف`, `شد ${desk}`);
  const tablet = await perRow(path, sel, 900);
  ok(tablet >= 2, `تبلت ${path} ${sel} دست‌کم ۲ در ردیف`, `شد ${tablet}`);
  const mob = await perRow(path, sel, 390);
  ok(mob === 1 || mob === 0, `موبایل ${path} ${sel} تک‌ستونه`, `شد ${mob}`);
}

// ── ۳. سربرگ در همهٔ پهناها سالم می‌ماند ────────────────────────────────
for (const w of [1440, 1024, 768, 390]) {
  const ctx = await browser.newContext({ viewport: { width: w, height: 900 } });
  const page = await ctx.newPage();
  await page.goto(BASE + '/', { waitUntil: 'domcontentloaded', timeout: 120000 });
  await page.waitForTimeout(2500);
  const r = await page.evaluate(() => {
    const h = document.querySelector('.site-header__inner');
    if (!h) return null;
    const b = h.getBoundingClientRect();
    const vw = document.documentElement.clientWidth;
    const kids = [...h.children].filter(k => k.getBoundingClientRect().width > 0);
    return { visible: b.left >= -1 && b.right <= vw + 1, n: kids.length,
             stacked: kids.length > 1 && Math.abs(kids[0].getBoundingClientRect().top - kids[1].getBoundingClientRect().top) > 8 };
  });
  ok(r && r.visible, `سربرگ در محدوده @${w}`, r ? '' : 'یافت نشد');
  ok(r && r.n >= 2, `سربرگ کامل @${w}`, r ? `${r.n} بخش` : '');
  await ctx.close();
}

await browser.close();

console.log('\n════════════════════════════════════════════════════════════');
console.log(`گذر: ${pass}    ناگذر: ${fail}`);
if (failures.length) {
  console.log('\nمواردِ ناگذر:');
  [...new Set(failures)].slice(0, 30).forEach(f => console.log('  · ' + f));
}
process.exit(fail ? 1 : 0);
