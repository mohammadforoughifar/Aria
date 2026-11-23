/* =====================================================================
   آزمونِ پذیرشِ سرصفحهٔ «نرم‌افزارها» + گرافیکِ «منظومهٔ ماژول‌ها»
   ---------------------------------------------------------------------
   اجرا:  cd /home/user/Aria/tools && node orbit.mjs
   خروجی: گذر/ناگذر + تصویرها در /home/user/shots/orbit
   ===================================================================== */
import { chromium } from 'playwright-core';
import fs from 'fs';

const BASE = process.env.BASE || 'http://localhost:5210';
const SHOTS = '/home/user/shots/orbit';
fs.mkdirSync(SHOTS, { recursive: true });

const VIEWPORTS = [
  { name: '1920x1080', width: 1920, height: 1080, desktop: true },
  { name: '1440x900',  width: 1440, height: 900,  desktop: true },
  { name: '1280x720',  width: 1280, height: 720,  desktop: true },
  { name: '1024x768',  width: 1024, height: 768,  desktop: true },
  { name: '768x1024',  width: 768,  height: 1024, desktop: false },
  { name: '390x844',   width: 390,  height: 844,  desktop: false },
];

let pass = 0, fail = 0;
const failures = [];
function check(name, ok, detail = '') {
  if (ok) { pass++; console.log(`   ✅ ${name}`); }
  else { fail++; failures.push(`${name} — ${detail}`); console.log(`   ❌ ${name}  ${detail}`); }
}

/* ---------- اندازه‌گیریِ هندسهٔ چیپ‌ها ---------- */
const GEOM = `(() => {
  const stage = document.querySelector('.orb');
  if (!stage) return { err: 'no stage' };
  const dbSvg = stage.querySelector('.orb__db svg');
  const sr = stage.getBoundingClientRect();
  const dr = dbSvg ? dbSvg.getBoundingClientRect() : null;
  const chips = [...stage.querySelectorAll('.orb__chip')].map(c => {
    const r = c.getBoundingClientRect();
    return { t: c.textContent.trim(), x: r.left, y: r.top, w: r.width, h: r.height,
             right: r.right, bottom: r.bottom, op: +getComputedStyle(c).opacity };
  });
  // انتهایِ خطوط در دستگاهِ مختصاتِ صحنه
  const vb = (stage.querySelector('.orb__fx') || {}).getAttribute
           ? stage.querySelector('.orb__fx').getAttribute('viewBox') : null;
  const links = [...stage.querySelectorAll('.orb__link')].map(l => ({
    x1: +l.getAttribute('x1'), y1: +l.getAttribute('y1'),
    x2: +l.getAttribute('x2'), y2: +l.getAttribute('y2'),
    off: +(l.style.strokeDashoffset || 1)
  }));
  return {
    stage: { x: sr.left, y: sr.top, w: sr.width, h: sr.height, right: sr.right, bottom: sr.bottom },
    db: dr ? { x: dr.left, y: dr.top, w: dr.width, h: dr.height, right: dr.right, bottom: dr.bottom,
               cx: dr.left + dr.width / 2, cy: dr.top + dr.height / 2 } : null,
    chips, links, viewBox: vb,
    frames: window.__heroOrbitFrames,
    scrollW: document.documentElement.scrollWidth,
    innerW: window.innerWidth,
  };
})()`;

/* ---------- تضادِ رنگ ---------- */
const CONTRAST = `(() => {
  const btn = document.querySelector('.hero--split .btn--outline-light');
  if (!btn) return { err: 'no button' };
  const parse = (s) => {
    const m = s.match(/rgba?\\(([^)]+)\\)/);
    if (!m) return null;
    const p = m[1].split(',').map(v => parseFloat(v));
    return { r: p[0], g: p[1], b: p[2], a: p.length > 3 ? p[3] : 1 };
  };
  const lum = (c) => {
    const f = (v) => { v /= 255; return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4); };
    return 0.2126 * f(c.r) + 0.7152 * f(c.g) + 0.0722 * f(c.b);
  };
  const over = (fg, bg) => ({
    r: fg.r * fg.a + bg.r * (1 - fg.a),
    g: fg.g * fg.a + bg.g * (1 - fg.a),
    b: fg.b * fg.a + bg.b * (1 - fg.a), a: 1
  });
  const ratio = (a, b) => {
    const l1 = lum(a), l2 = lum(b);
    return (Math.max(l1, l2) + 0.05) / (Math.min(l1, l2) + 0.05);
  };
  // پس‌زمینهٔ مؤثر: بالا می‌رویم تا رنگِ کدر پیدا شود
  let el = btn, bg = { r: 255, g: 255, b: 255, a: 1 };
  const stack = [];
  while (el && el !== document.documentElement.parentElement) {
    const c = parse(getComputedStyle(el).backgroundColor);
    if (c && c.a > 0) { stack.push(c); if (c.a >= 0.999) break; }
    el = el.parentElement;
  }
  bg = stack.reverse().reduce((acc, c) => over(c, acc), { r: 255, g: 255, b: 255, a: 1 });

  const fg = parse(getComputedStyle(btn).color);
  const bd = parse(getComputedStyle(btn).borderTopColor);
  return {
    text: fg ? ratio(over(fg, bg), bg) : null,
    border: bd ? ratio(over(bd, bg), bg) : null,
    bg: [Math.round(bg.r), Math.round(bg.g), Math.round(bg.b)],
    color: getComputedStyle(btn).color,
    borderColor: getComputedStyle(btn).borderTopColor,
  };
})()`;

/* ---------- کمک ---------- */
const overlapFrac = (a, b) => {
  const ox = Math.max(0, Math.min(a.right, b.right) - Math.max(a.x, b.x));
  const oy = Math.max(0, Math.min(a.bottom, b.bottom) - Math.max(a.y, b.y));
  const inter = ox * oy;
  return inter / Math.max(1, Math.min(a.w * a.h, b.w * b.h));
};

async function openPage(ctx, path = '/catalog') {
  const p = await ctx.newPage();
  const errs = [];
  p.on('pageerror', e => errs.push('PAGEERROR: ' + e.message.slice(0, 140)));
  p.on('console', m => {
    if (m.type() === 'error' && !/dotnet\.wasm|Failed to load resource/.test(m.text()))
      errs.push(m.text().slice(0, 140));
  });
  await p.goto(BASE + path, { waitUntil: 'domcontentloaded', timeout: 120000 });
  await p.waitForSelector('.orb__chip', { timeout: 120000 });
  return { p, errs };
}

const browser = await chromium.launch({ args: ['--no-sandbox'] });

/* ================= ۱ تا ۳: اندازه‌ها × تم‌ها × حالت‌ها ================= */
for (const vp of VIEWPORTS) {
  for (const theme of ['light', 'dark']) {
    console.log(`\n■ ${vp.name} · ${theme === 'light' ? 'روشن' : 'تاریک'}`);
    const ctx = await browser.newContext({ viewport: { width: vp.width, height: vp.height } });
    await ctx.addInitScript((t) => {
      try { localStorage.setItem('nw-theme', t); } catch (e) { }
    }, theme);
    const { p, errs } = await openPage(ctx);

    // حالتِ نشسته (نخستین قاب)
    await p.waitForTimeout(1500);
    const docked = await p.evaluate(GEOM);
    await p.screenshot({ path: `${SHOTS}/${vp.name}-${theme}-docked.png` });

    // حالتِ آزاد: ۸ ثانیه پس از بارگیری، بر پایهٔ زمان‌بندیِ موتور
    await p.waitForTimeout(6500);
    const free = await p.evaluate(GEOM);
    await p.screenshot({ path: `${SHOTS}/${vp.name}-${theme}-free.png` });

    for (const [label, g] of [['نشسته', docked], ['آزاد', free]]) {
      // ۱) هم‌پوشانیِ چیپ‌ها
      let worst = 0, worstPair = '';
      for (let i = 0; i < g.chips.length; i++)
        for (let j = i + 1; j < g.chips.length; j++) {
          const f = overlapFrac(g.chips[i], g.chips[j]);
          if (f > worst) { worst = f; worstPair = `${g.chips[i].t}/${g.chips[j].t}`; }
        }
      check(`${vp.name}/${theme} · ${label}: هم‌پوشانیِ چیپ‌ها < ۱۰٪`,
        worst < 0.10, `${(worst * 100).toFixed(1)}٪ (${worstPair})`);

      // بریده نشدن از دید
      const clipped = g.chips.filter(c => c.x < -0.5 || c.right > vp.width + 0.5);
      check(`${vp.name}/${theme} · ${label}: چیپی از دید بیرون نیست`,
        clipped.length === 0,
        clipped.map(c => `${c.t} x=${c.x.toFixed(0)}..${c.right.toFixed(0)}`).join(' | '));

      // پایگاهِ داده پوشانده نشود
      if (g.db) {
        const covering = g.chips.filter(c => overlapFrac(c, g.db) > 0.02);
        check(`${vp.name}/${theme} · ${label}: پایگاهِ داده پوشانده نشده`,
          covering.length === 0,
          covering.map(c => `${c.t} ${(overlapFrac(c, g.db) * 100).toFixed(0)}٪`).join(','));
      }

      // بدون پیمایشِ افقی
      check(`${vp.name}/${theme} · ${label}: بدون پیمایشِ افقی`,
        g.scrollW <= g.innerW + 1, `${g.scrollW} > ${g.innerW}`);
    }

    /* ۲) در حالتِ نشسته، سرهایِ خط بر مرکزِ چیپ و پایگاه منطبق‌اند */
    const deep = await p.evaluate(`(() => {
      const stage = document.querySelector('.orb');
      const sr = stage.getBoundingClientRect();
      const dbSvg = stage.querySelector('.orb__db svg');
      const dr = dbSvg.getBoundingClientRect();
      const dbCx = dr.left + dr.width / 2 - sr.left;
      const dbCy = dr.top + dr.height / 2 - sr.top;
      const out = [];
      stage.querySelectorAll('.orb__link').forEach((l, i) => {
        const chip = stage.querySelectorAll('.orb__chip')[i];
        const cr = chip.getBoundingClientRect();
        out.push({
          i,
          d1: Math.hypot(+l.getAttribute('x1') - (cr.left + cr.width / 2 - sr.left),
                         +l.getAttribute('y1') - (cr.top + cr.height / 2 - sr.top)),
          d2: Math.hypot(+l.getAttribute('x2') - dbCx, +l.getAttribute('y2') - dbCy),
          off: +(l.style.strokeDashoffset || 1),
        });
      });
      return out;
    })()`);
    const worstEnd = Math.max(...deep.map(d => Math.max(d.d1, d.d2)));
    check(`${vp.name}/${theme} · سرهایِ خط بر مرکز منطبق‌اند (±1px)`,
      worstEnd <= 1.0, `بیشینهٔ خطا: ${worstEnd.toFixed(2)}px`);

    /* ۳) جایِ قرص، گرافیک و نبودِ دایرهٔ خالی */
    const layout = await p.evaluate(`(() => {
      const sec = document.querySelector('.hero--split');
      const pill = sec.querySelector('.hero__eyebrow');
      const h1 = sec.querySelector('.hero__title');
      const copy = sec.querySelector('.hero__copy');
      const vis = sec.querySelector('.hero__visual');
      const r = e => { const b = e.getBoundingClientRect(); return { x: +b.left.toFixed(1), y: +b.top.toFixed(1), w: +b.width.toFixed(1), r: +b.right.toFixed(1), b: +b.bottom.toFixed(1) }; };
      const cs = getComputedStyle(vis);
      return {
        pill: r(pill), h1: r(h1), copy: r(copy), vis: r(vis),
        visDisplay: cs.display, visVisible: cs.visibility !== 'hidden' && +cs.opacity > 0.01,
        chipCount: sec.querySelectorAll('.orb__chip').length,
        dbCount: sec.querySelectorAll('.orb__db').length,
        // هیچ عنصرِ گردِ تهی: عنصری با border-radius:50% که فرزندی ندارد
        emptyCircles: [...sec.querySelectorAll('*')].filter(e => {
          const s = getComputedStyle(e);
          const b = e.getBoundingClientRect();
          return s.borderRadius.includes('50%') && e.children.length === 0
              && b.width > 40 && !e.className.toString().includes('orb')
              && !e.closest('.orb');
        }).map(e => e.className.toString().slice(0, 40)),
      };
    })()`);
    check(`${vp.name}/${theme} · قرص بالایِ تیتر است`,
      layout.pill.b <= layout.h1.y + 2,
      `قرص تا ${layout.pill.b} / تیتر از ${layout.h1.y}`);
    check(`${vp.name}/${theme} · ستونِ گرافیک دیده می‌شود`,
      layout.visVisible && layout.chipCount === 6 && layout.dbCount === 1,
      `چیپ ${layout.chipCount} / پایگاه ${layout.dbCount}`);
    check(`${vp.name}/${theme} · دایرهٔ تهیِ جای‌نگهدار ندارد`,
      layout.emptyCircles.length === 0, layout.emptyCircles.join(','));
    if (vp.desktop) {
      check(`${vp.name}/${theme} · در راست‌چین گرافیک چپِ متن است`,
        layout.vis.r <= layout.copy.x + 2 || layout.vis.x >= layout.copy.r - 2,
        `متن ${layout.copy.x}..${layout.copy.r} / گرافیک ${layout.vis.x}..${layout.vis.r}`);
    }

    /* ۴) تضادِ دکمهٔ «مشاورهٔ رایگان» */
    const ct = await p.evaluate(CONTRAST);
    check(`${vp.name}/${theme} · تضادِ متنِ دکمه ≥ ۴/۵`,
      ct.text !== null && ct.text >= 4.5,
      ct.text ? `${ct.text.toFixed(2)}:1 (${ct.color} روی rgb(${ct.bg}))` : 'یافت نشد');
    check(`${vp.name}/${theme} · تضادِ کادرِ دکمه ≥ ۴/۵`,
      ct.border !== null && ct.border >= 4.5,
      ct.border ? `${ct.border.toFixed(2)}:1` : 'یافت نشد');

    check(`${vp.name}/${theme} · بدون خطایِ ترمینال`, errs.length === 0, errs.slice(0, 2).join(' | '));
    await ctx.close();
  }
}

/* ================= ۵: API مسدود ================= */
console.log('\n■ API مسدود — آمار باید مقدارِ جایگزین نشان دهد');
{
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  await ctx.route('**/api/**', r => r.abort());
  const p = await ctx.newPage();
  await p.goto(BASE + '/catalog', { waitUntil: 'domcontentloaded', timeout: 120000 });
  await p.waitForSelector('.hero__stats', { timeout: 120000 });
  await p.waitForTimeout(4000);
  const st = await p.evaluate(`(() => {
    const fa = '۰۱۲۳۴۵۶۷۸۹';
    const num = s => parseInt(String(s).replace(/[۰-۹]/g, d => fa.indexOf(d)).replace(/[^\\d]/g,''), 10);
    return {
      stats: [...document.querySelectorAll('.hero__stat')].map(s => ({
        val: num(s.querySelector('b').textContent),
        text: s.querySelector('b').textContent.trim(),
        label: s.querySelector('span').textContent.trim(),
        target: s.querySelector('b').dataset.n,
      })),
      banner: !!document.querySelector('.api-warn'),
      bannerTop: document.querySelector('.api-warn')
        ? document.querySelector('.api-warn').getBoundingClientRect().top : null,
      bannerRect: (() => { const e = document.querySelector('.api-warn'); if (!e) return null;
        const b = e.getBoundingClientRect();
        return { top: +b.top.toFixed(1), bottom: +b.bottom.toFixed(1), left: +b.left.toFixed(1), right: +b.right.toFixed(1) }; })(),
      headerRect: (() => { const e = document.querySelector('.site-header'); if (!e) return null;
        const b = e.getBoundingClientRect();
        return { top: +b.top.toFixed(1), bottom: +b.bottom.toFixed(1), left: +b.left.toFixed(1), right: +b.right.toFixed(1) }; })(),
      headerBottom: document.querySelector('.site-header')
        ? document.querySelector('.site-header').getBoundingClientRect().bottom : null,
      chips: document.querySelectorAll('.orb__chip').length,
    };
  })()`);
  console.log('   آمار: ' + st.stats.map(s => `${s.text} «${s.label}»`).join(' · '));
  check('API مسدود: هیچ آماری صفر نیست', st.stats.every(s => s.val > 0),
    st.stats.filter(s => !(s.val > 0)).map(s => s.label).join(','));
  check('API مسدود: مقدارِ جایگزینِ ۲۸ نمایش داده می‌شود',
    st.stats[0] && st.stats[0].val === 28, st.stats[0] ? String(st.stats[0].val) : '—');
  check('API مسدود: گرافیک هنوز کار می‌کند', st.chips === 6, String(st.chips));
  if (st.banner) {
    // برخوردِ واقعیِ دو مستطیل در هر دو محور
    const a = st.bannerRect, b = st.headerRect;
    const hit = !(a.right <= b.left || a.left >= b.right || a.bottom <= b.top || a.top >= b.bottom);
    check('API مسدود: هشدار با سرصفحهٔ چسبان برخورد ندارد', !hit,
      `هشدار ${a.top}..${a.bottom} / سرصفحه ${b.top}..${b.bottom}`);
  } else {
    console.log('   · هشداری در کار نیست (خوب)');
  }
  await p.screenshot({ path: `${SHOTS}/api-blocked.png`, fullPage: false });
  await ctx.close();
}

/* ================= ۶: پیمایش و تغییرِ اندازه ================= */
console.log('\n■ پیمایش و تغییرِ اندازهٔ مکرر');
{
  const ctx = await browser.newContext({ viewport: { width: 1920, height: 1080 } });
  const { p, errs } = await openPage(ctx);
  await p.waitForTimeout(1500);

  const framesAt = async () => p.evaluate(() => window.__heroOrbitFrames);

  // حلقه در حالِ اجراست؟
  const f1 = await framesAt();
  await p.waitForTimeout(1000);
  const f2 = await framesAt();
  check('حلقه در حالِ اجراست', f2 > f1 + 10, `${f1} → ${f2}`);

  // پنج بار بالا و پایین
  for (let i = 0; i < 5; i++) {
    await p.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
    await p.waitForTimeout(320);
    await p.evaluate(() => window.scrollTo(0, 0));
    await p.waitForTimeout(320);
  }
  const f3 = await framesAt();
  await p.waitForTimeout(900);
  const f4 = await framesAt();
  check('پس از پیمایشِ مکرر حلقه هنوز می‌چرخد', f4 > f3 + 10, `${f3} → ${f4}`);

  // خارج از دید: حلقه باید کاملاً بایستد
  await p.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
  await p.waitForTimeout(1200);
  const g1 = await framesAt();
  await p.waitForTimeout(1200);
  const g2 = await framesAt();
  check('با بیرون رفتن از دید، rAF می‌ایستد', g2 === g1, `${g1} → ${g2}`);

  // بازگشت: دوباره می‌چرخد
  await p.evaluate(() => window.scrollTo(0, 0));
  await p.waitForTimeout(1200);
  const g3 = await framesAt();
  check('با بازگشت به دید، دوباره می‌چرخد', g3 > g2 + 10, `${g2} → ${g3}`);

  // تغییرِ اندازه
  for (const w of [1024, 390, 1920]) {
    await p.setViewportSize({ width: w, height: 900 });
    await p.waitForTimeout(900);
    const g = await p.evaluate(GEOM);
    const bad = g.chips.filter(c => c.x < -0.5 || c.right > w + 0.5);
    check(`پس از تغییر اندازه به ${w}: چیپی بیرون نیست`, bad.length === 0,
      bad.map(c => `${c.t} ${c.x.toFixed(0)}..${c.right.toFixed(0)}`).join(','));
    const stray = g.links.filter(l => !isFinite(l.x1) || !isFinite(l.y1) || l.x1 < -50 || l.x1 > g.stage.w + 50);
    check(`پس از تغییر اندازه به ${w}: خطِ سرگردان نیست`, stray.length === 0, String(stray.length));
  }
  check('بدون خطایِ ترمینال پس از پیمایش/تغییرِ اندازه', errs.length === 0, errs.slice(0, 2).join(' | '));
  await p.screenshot({ path: `${SHOTS}/after-resize.png` });
  await ctx.close();
}

/* ================= ۷: کاهشِ حرکت ================= */
console.log('\n■ ترجیحِ کاهشِ حرکت');
{
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, reducedMotion: 'reduce' });
  const p = await ctx.newPage();
  await p.goto(BASE + '/catalog', { waitUntil: 'domcontentloaded', timeout: 120000 });
  await p.waitForSelector('.orb__chip', { timeout: 120000 });
  await p.waitForTimeout(4000);
  const r = await p.evaluate(`(() => {
    const stage = document.querySelector('.orb');
    return {
      frames: window.__heroOrbitFrames,
      chips: stage.querySelectorAll('.orb__chip').length,
      dots: [...stage.querySelectorAll('.orb__dot')].filter(d => +d.getAttribute('opacity') > 0.01).length,
      live: stage.querySelector('.orb__db').classList.contains('is-live'),
      stats: [...document.querySelectorAll('.hero__stat b')].map(b => b.textContent.trim()),
      offsets: [...stage.querySelectorAll('.orb__link')].map(l => +(l.style.strokeDashoffset || 1)),
    };
  })()`);
  check('کاهشِ حرکت: هیچ فریمی اجرا نشده', r.frames === 0, String(r.frames));
  check('کاهشِ حرکت: شش چیپ حاضر است', r.chips === 6, String(r.chips));
  check('کاهشِ حرکت: ذره‌ای نیست', r.dots === 0, String(r.dots));
  check('کاهشِ حرکت: خطوط کامل کشیده شده‌اند', r.offsets.every(o => o <= 0.001),
    r.offsets.map(o => o.toFixed(2)).join(','));
  check('کاهشِ حرکت: آمار مقدارِ نهایی را نشان می‌دهد',
    r.stats.every(s => s && s !== '۰' && s !== '0'), r.stats.join(' · '));
  await p.screenshot({ path: `${SHOTS}/reduced-motion.png` });
  await ctx.close();
}

/* ================= ۸: CLS و کارهایِ سنگین ================= */
console.log('\n■ جابه‌جاییِ چیدمان و کارهایِ سنگین');
{
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const p = await ctx.newPage();
  await p.addInitScript(() => {
    window.__cls = 0;
    new PerformanceObserver((l) => {
      for (const e of l.getEntries()) if (!e.hadRecentInput) window.__cls += e.value;
    }).observe({ type: 'layout-shift', buffered: true });
    window.__long = [];
    new PerformanceObserver((l) => {
      for (const e of l.getEntries())
        if (e.duration > 50) window.__long.push({ t: e.startTime, d: e.duration });
    }).observe({ type: 'longtask', buffered: true });
  });
  await p.goto(BASE + '/catalog', { waitUntil: 'domcontentloaded', timeout: 120000 });
  await p.waitForSelector('.orb__chip', { timeout: 120000 });
  const settled = await p.evaluate(() => performance.now());
  await p.waitForTimeout(8000);
  const r = await p.evaluate(() => ({ cls: window.__cls, long: window.__long }));
  check('CLS زیرِ ۰/۰۵ است', r.cls < 0.05, r.cls.toFixed(4));
  /* کارهایِ سنگینِ هنگامِ بالا آمدنِ برنامه (کامپایل و راه‌اندازیِ دات‌نت در
     مرورگر) طبیعی‌اند؛ مهم این است که پس از نشستنِ برنامه، انیمیشن هیچ کارِ
     سنگینی نسازد. */
  const all = r.long;
  const after = all.filter((l) => l.t > settled + 1500);
  console.log(`   · کارهایِ سنگین: ${all.length} در هنگامِ بالا آمدن، ${after.length} پس از نشستن`);
  check('انیمیشن هیچ کارِ سنگینِ بیش از ۵۰ میلی‌ثانیه نمی‌سازد', after.length === 0,
    after.map((l) => `${Math.round(l.t)}ms/${Math.round(l.d)}ms`).join(', '));
  await ctx.close();
}

await browser.close();
console.log(`\n${'═'.repeat(60)}`);
console.log(`گذر: ${pass}    ناگذر: ${fail}`);
if (failures.length) { console.log('\nمواردِ ناگذر:'); failures.forEach(f => console.log('  · ' + f)); }
console.log(`تصویرها: ${SHOTS}`);
process.exit(fail ? 1 : 0);
