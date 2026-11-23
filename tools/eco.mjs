/* =====================================================================
   آزمونِ پذیرشِ بخشِ «اکوسیستمِ یکپارچه»
   ---------------------------------------------------------------------
   اجرا:  cd /home/user/Aria/tools && node eco.mjs
   خروجی: گذر/ناگذر به‌ازایِ هر بررسی + تصویرها در /home/user/shots/eco
   ===================================================================== */
import { chromium } from 'playwright-core';
import fs from 'fs';

const BASE = process.env.BASE || 'http://localhost:5210';
const SHOTS = '/home/user/shots/eco';
fs.mkdirSync(SHOTS, { recursive: true });

const VIEWPORTS = [
  { name: '1920x1080', width: 1920, height: 1080 },
  { name: '1440x900',  width: 1440, height: 900  },
  { name: '1280x720',  width: 1280, height: 720  },
  { name: '1024x768',  width: 1024, height: 768  },
  { name: '768x1024',  width: 768,  height: 1024 },
  { name: '390x844',   width: 390,  height: 844  },
];

let pass = 0, fail = 0;
const failures = [];
function check(name, ok, detail = '') {
  if (ok) { pass++; console.log(`   ✅ ${name}`); }
  else { fail++; failures.push(`${name} — ${detail}`); console.log(`   ❌ ${name}  ${detail}`); }
}

/* ---- اندازه‌گیریِ درون‌صفحه: هندسهٔ offset در برابرِ مسیرِ SVG ---- */
const GEOM = `(() => {
  const root = document.querySelector('#ecosystem');
  const scene = root.querySelector('[data-eco-scene]');
  const hub = root.querySelector('[data-eco-hub]');
  if (!scene || !hub) return { err: 'no scene' };
  const pos = (el, stop) => { let x=0,y=0,g=0; while(el && el!==stop && g++<32){x+=el.offsetLeft;y+=el.offsetTop;el=el.offsetParent;} return {x,y}; };
  const hubPos = pos(hub, scene);
  const hubCx = hubPos.x + hub.offsetWidth/2;
  const hubBox = { l: hubPos.x, r: hubPos.x + hub.offsetWidth, t: hubPos.y, b: hubPos.y + hub.offsetHeight };
  const sceneW = scene.offsetWidth, sceneH = scene.offsetHeight;

  const out = [];
  root.querySelectorAll('path[data-flow]').forEach(p => {
    const card = root.querySelector('[data-eco-card="' + p.getAttribute('data-flow') + '"]');
    if (!card) return;
    const active = card.classList.contains('is-active');
    const id = p.getAttribute('data-flow');
    const len = p.getTotalLength();
    // مسیرها در دستگاهِ مختصاتِ صحنه‌اند (viewBox = اندازهٔ صحنه)
    const s = p.getPointAtLength(0);
    const e = p.getPointAtLength(len);
    // کدام سمت است؟ مسیر از کارت به هاب (in) یا برعکس (out)
    const cp = pos(card, scene);
    const ccx = cp.x + card.offsetWidth/2;
    const rightSide = ccx > hubCx;
    const cardFacingX = rightSide ? cp.x : cp.x + card.offsetWidth;
    const hubFacingX  = rightSide ? hubBox.r : hubBox.l;
    // نقطهٔ کارت و نقطهٔ هاب در دو سرِ مسیر
    const dir = card.closest('.eco__col--people') ? 'in' : 'out';
    const cardPt = dir === 'in' ? s : e;
    const hubPt  = dir === 'in' ? e : s;
    out.push({
      id, active, dir,
      cardEdgeDx: Math.abs(cardPt.x - cardFacingX),
      cardEdgeDy: Math.abs(cardPt.y - (cp.y + card.offsetHeight/2)),
      hubEdgeDx:  Math.abs(hubPt.x - hubFacingX),
      insideScene: (pt) => pt.x >= -1 && pt.y >= -1 && pt.x <= sceneW+1 && pt.y <= sceneH+1,
      sIn: s.x >= -1 && s.y >= -1 && s.x <= sceneW+1 && s.y <= sceneH+1,
      eIn: e.x >= -1 && e.y >= -1 && e.x <= sceneW+1 && e.y <= sceneH+1,
    });
  });

  const grid = root.querySelector('.eco__grid');
  const cols = root.querySelectorAll('.eco__col');
  const people = root.querySelector('.eco__col--people');
  const systems = root.querySelector('.eco__col--systems');

  const stage = root.querySelector('.eco__stage');
  const cta = root.querySelector('.eco__cta .btn');
  const ctaBox = cta.getBoundingClientRect();
  const stageBox = stage.getBoundingClientRect();
  const wrapBox = root.querySelector('.wrap').getBoundingClientRect();

  return {
    sceneW, sceneH,
    hubCenterOffset: Math.abs(hubCx - sceneW/2),
    peopleW: people.offsetWidth, systemsW: systems.offsetWidth,
    flows: out,
    ctaIntersectsStage: !(ctaBox.right <= stageBox.left || ctaBox.left >= stageBox.right
                      || ctaBox.bottom <= stageBox.top || ctaBox.top >= stageBox.bottom),
    ctaCenterOffset: Math.abs((ctaBox.left + ctaBox.width/2) - (wrapBox.left + wrapBox.width/2)),
    scrollW: document.documentElement.scrollWidth,
    innerW: window.innerWidth,
  };
})()`;


/* ---- بررسیِ بصری: آیا سرِ خط زیرِ کارت/هاب پنهان است؟ ---- */
const VISUAL = `(() => {
  const root = document.querySelector('#ecosystem');
  const out = [];
  root.querySelectorAll('path[data-flow]').forEach(path => {
    const card = root.querySelector('[data-eco-card="' + path.getAttribute('data-flow') + '"]');
    if (!card || !card.classList.contains('is-active')) return;
    const ctm = path.getScreenCTM();
    const pt = (x, y) => { const q = new DOMPoint(x, y).matrixTransform(ctm); return { x: q.x, y: q.y }; };
    const len = path.getTotalLength();
    const s = path.getPointAtLength(0), e = path.getPointAtLength(len);
    const dir = card.closest('.eco__col--people') ? 'in' : 'out';
    const cP = pt(dir === 'in' ? s.x : e.x, dir === 'in' ? s.y : e.y);
    const hP = pt(dir === 'in' ? e.x : s.x, dir === 'in' ? e.y : s.y);
    const cr = card.getBoundingClientRect();
    const hr = root.querySelector('[data-eco-hub]').getBoundingClientRect();
    const onRight = cr.left > hr.left;
    const cardEdge = onRight ? cr.left : cr.right;
    const hubEdge  = onRight ? hr.right : hr.left;
    // مثبت یعنی سرِ خط به درونِ کارت/هاب رفته و زیرِ آن پنهان است
    out.push({
      id: path.getAttribute('data-flow'),
      cardInto: (cP.x - cardEdge) * (onRight ? 1 : -1),
      hubInto:  (hP.x - hubEdge)  * (onRight ? -1 : 1),
    });
  });
  return out;
})()`;

const browser = await chromium.launch({ args: ['--no-sandbox'] });

/* ============ ۱) برای هر اندازهٔ صفحه و هر سناریو ============ */
for (const vp of VIEWPORTS) {
  console.log(`\n■ ${vp.name}`);
  const ctx = await browser.newContext({ viewport: { width: vp.width, height: vp.height }, deviceScaleFactor: 1 });
  const page = await ctx.newPage();
  const errors = [];
  page.on('pageerror', e => errors.push('PAGEERROR: ' + e.message.slice(0, 120)));
  page.on('console', m => { if (m.type() === 'error' && !/dotnet\.wasm|Failed to load resource/.test(m.text())) errors.push(m.text().slice(0, 120)); });

  await page.goto(BASE + '/', { waitUntil: 'domcontentloaded', timeout: 120000 });
  await page.waitForSelector('#ecosystem', { timeout: 120000 });
  await page.waitForSelector('.eco-card', { timeout: 120000 });
  await page.waitForTimeout(2500);
  // اسکرولِ آرام تا بخش
  await page.evaluate(async () => {
    const el = document.querySelector('#ecosystem');
    const top = el.getBoundingClientRect().top + window.scrollY;
    const step = Math.max(60, Math.round(window.innerHeight / 8));
    for (let y = 0; y < top - 100; y += step) {
      window.scrollTo(0, y);
      await new Promise(r => requestAnimationFrame(r));
    }
    window.scrollTo(0, top - 100);
  });
  await page.waitForTimeout(1800);

  const tabs = await page.locator('.eco-tab').count();
  check(`${vp.name} · پنج تب وجود دارد`, tabs === 5, `تعداد: ${tabs}`);

  const desktop = vp.width >= 861;

  for (let t = 0; t < tabs; t++) {
    await page.locator('.eco-tab').nth(t).click();
    await page.waitForTimeout(1200);
    const g = await page.evaluate(GEOM);

    if (desktop) {
      const bad = g.flows.filter(f => f.active && (f.cardEdgeDx > 12 || f.hubEdgeDx > 12));
      check(`${vp.name} · سناریو ${t + 1}: سرهایِ مسیر به کارت/هاب چسبیده‌اند`,
        bad.length === 0,
        bad.length ? bad.map(b => `${b.id}: کارت ${b.cardEdgeDx.toFixed(1)}px، هاب ${b.hubEdgeDx.toFixed(1)}px`).join(' | ') : '');

      const outside = g.flows.filter(f => !f.sIn || !f.eIn);
      check(`${vp.name} · سناریو ${t + 1}: هیچ مسیری بیرونِ صحنه نیست`,
        outside.length === 0, outside.map(o => o.id).join(','));

      check(`${vp.name} · سناریو ${t + 1}: هاب در مرکزِ صحنه است (±2px)`,
        g.hubCenterOffset <= 2, `اختلاف: ${g.hubCenterOffset.toFixed(2)}px`);

      check(`${vp.name} · سناریو ${t + 1}: دو ستون هم‌پهنا هستند (±2px)`,
        Math.abs(g.peopleW - g.systemsW) <= 2, `${g.peopleW} در برابر ${g.systemsW}`);

      const vis = await page.evaluate(VISUAL);
      const detached = vis.filter(v => v.cardInto < 0 || v.hubInto < 0);
      check(`${vp.name} · سناریو ${t + 1}: سرِ خط‌ها زیرِ کارت و هاب پنهان‌اند`,
        detached.length === 0,
        detached.map(d => `${d.id}: کارت ${d.cardInto.toFixed(1)} / هاب ${d.hubInto.toFixed(1)}`).join(' | '));
      const tooDeep = vis.filter(v => v.cardInto > 46 || v.hubInto > 46);
      check(`${vp.name} · سناریو ${t + 1}: خط بیش از اندازه زیرِ کارت نرفته`,
        tooDeep.length === 0, tooDeep.map(d => `${d.id}: ${d.cardInto.toFixed(1)}`).join(','));
    }

    if (t === 0) {
      check(`${vp.name} · دکمه با صحنه برخورد ندارد`, !g.ctaIntersectsStage, '');
      check(`${vp.name} · دکمه در مرکز است (±2px)`, g.ctaCenterOffset <= 2, `اختلاف: ${g.ctaCenterOffset.toFixed(2)}px`);
    }
  }

  const noHScroll = await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth + 1);
  check(`${vp.name} · بدون پیمایشِ افقی`, noHScroll,
    noHScroll ? '' : await page.evaluate(() => `${document.documentElement.scrollWidth} > ${window.innerWidth}`));

  if (vp.name === '1440x900') {
    // پیش / در حالِ ورود / در مرکز / در حالِ خروج
    for (const [tag, frac] of [['before', 1.15], ['entering', 0.95], ['centered', 0.15], ['leaving', -0.35]]) {
      await page.evaluate((f) => {
        const el = document.querySelector('#ecosystem');
        const top = el.getBoundingClientRect().top + window.scrollY;
        window.scrollTo(0, Math.max(0, top - window.innerHeight * f));
      }, frac);
      await page.waitForTimeout(900);
      await page.screenshot({ path: `${SHOTS}/scroll-${tag}.png` });
    }
    await page.evaluate(() => { const el = document.querySelector('#ecosystem'); window.scrollTo(0, el.getBoundingClientRect().top + window.scrollY - 80); });
    await page.waitForTimeout(600);
    await page.screenshot({ path: `${SHOTS}/desktop-1440.png` });
  } else {
    await page.evaluate(() => { const el = document.querySelector('#ecosystem'); window.scrollTo(0, el.getBoundingClientRect().top + window.scrollY - 80); });
    await page.waitForTimeout(700);
    await page.screenshot({ path: `${SHOTS}/${vp.name}.png` });
  }

  check(`${vp.name} · بدون خطایِ ترمینال`, errors.length === 0, errors.slice(0, 2).join(' | '));
  await ctx.close();
}

/* ============ ۲) تغییرِ اندازهٔ پنجره ============ */
console.log('\n■ تغییرِ اندازه از ۱۹۲۰ به ۱۰۲۴ و بازگشت');
{
  const ctx = await browser.newContext({ viewport: { width: 1920, height: 1080 } });
  const page = await ctx.newPage();
  await page.goto(BASE + '/', { waitUntil: 'domcontentloaded', timeout: 120000 });
  await page.waitForSelector('.eco-card', { timeout: 120000 });
  await page.waitForTimeout(2500);
  await page.evaluate(() => { const el = document.querySelector('#ecosystem'); window.scrollTo(0, el.getBoundingClientRect().top + window.scrollY - 80); });
  await page.waitForTimeout(1500);

  await page.setViewportSize({ width: 1024, height: 768 });
  await page.waitForTimeout(900);
  let g = await page.evaluate(GEOM);
  let bad = g.flows.filter(f => f.active && (f.cardEdgeDx > 12 || f.hubEdgeDx > 12));
  check('کوچک شدن: مسیرها هنوز چسبیده‌اند', bad.length === 0, bad.map(b => b.id).join(','));

  await page.setViewportSize({ width: 1920, height: 1080 });
  await page.waitForTimeout(900);
  g = await page.evaluate(GEOM);
  bad = g.flows.filter(f => f.active && (f.cardEdgeDx > 12 || f.hubEdgeDx > 12));
  check('بزرگ شدن: مسیرها هنوز چسبیده‌اند', bad.length === 0, bad.map(b => b.id).join(','));
  const outside = g.flows.filter(f => !f.sIn || !f.eIn);
  check('هیچ مسیرِ سرگردانی باقی نمانده', outside.length === 0, outside.map(o => o.id).join(','));
  await page.screenshot({ path: `${SHOTS}/resized-back.png` });
  await ctx.close();
}

/* ============ ۳) بدون فونت و با CPU یِ کُند ============ */
console.log('\n■ فونت مسدود + CPU یِ ۴ برابر کُندتر');
{
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  await ctx.route('**/*.woff2', r => r.abort());
  await ctx.route('**/*.woff', r => r.abort());
  await ctx.route('**/*.ttf', r => r.abort());
  const page = await ctx.newPage();
  const cdp = await ctx.newCDPSession(page);
  await cdp.send('Emulation.setCPUThrottlingRate', { rate: 4 });
  await page.goto(BASE + '/', { waitUntil: 'domcontentloaded', timeout: 180000 });
  await page.waitForSelector('.eco-card', { timeout: 180000 });
  await page.waitForTimeout(6000);
  await page.evaluate(() => { const el = document.querySelector('#ecosystem'); window.scrollTo(0, el.getBoundingClientRect().top + window.scrollY - 80); });
  await page.waitForTimeout(3000);
  const g = await page.evaluate(GEOM);
  const bad = g.flows.filter(f => f.active && (f.cardEdgeDx > 12 || f.hubEdgeDx > 12));
  check('فونت مسدود + CPU کُند: مسیرها چسبیده‌اند', bad.length === 0, bad.map(b => `${b.id}:${b.cardEdgeDx.toFixed(1)}/${b.hubEdgeDx.toFixed(1)}`).join(' '));
  await page.screenshot({ path: `${SHOTS}/nofont-throttled.png` });
  await ctx.close();
}

/* ============ ۴) ترجیحِ کاهشِ حرکت ============ */
console.log('\n■ ترجیحِ کاهشِ حرکت');
{
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, reducedMotion: 'reduce' });
  const page = await ctx.newPage();
  await page.goto(BASE + '/', { waitUntil: 'domcontentloaded', timeout: 120000 });
  await page.waitForSelector('.eco-card', { timeout: 120000 });
  await page.waitForTimeout(3000);
  await page.evaluate(() => { const el = document.querySelector('#ecosystem'); window.scrollTo(0, el.getBoundingClientRect().top + window.scrollY - 80); });
  await page.waitForTimeout(1500);
  const st = await page.evaluate(() => {
    const c = document.querySelector('.eco-card.is-active');
    const inactive = document.querySelector('.eco-card:not(.is-active)');
    return {
      activeOpacity: parseFloat(getComputedStyle(c).opacity),
      inactiveOpacity: parseFloat(getComputedStyle(inactive).opacity),
      dots: document.querySelectorAll('.eco__dot').length,
    };
  });
  check('حالتِ پایانی پیداست (فعال شفافیت ۱)', st.activeOpacity > 0.95, String(st.activeOpacity));
  check('کارتِ غیرفعال پیداست (نه صفر)', st.inactiveOpacity > 0.2, String(st.inactiveOpacity));
  check('ذرات ساخته نشده‌اند', st.dots === 0, String(st.dots));
  await page.screenshot({ path: `${SHOTS}/reduced-motion.png` });
  await ctx.close();
}

/* ============ ۵) CLS ============ */
console.log('\n■ جابه‌جاییِ چیدمان (CLS)');
{
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  await page.addInitScript(() => {
    window.__cls = 0;
    window.__cls = 0; window.__src = {};
    new PerformanceObserver((l) => {
      for (const e of l.getEntries()) {
        if (e.hadRecentInput) continue;
        window.__cls += e.value;
        for (const s of (e.sources || [])) {
          const n = s.node; if (!n || !n.tagName) continue;
          const key = n.tagName.toLowerCase() + (n.id ? '#' + n.id : '')
                    + (n.className && typeof n.className === 'string'
                        ? '.' + n.className.trim().split(/\s+/).slice(0, 2).join('.') : '');
          window.__src[key] = (window.__src[key] || 0) + e.value;
        }
      }
    }).observe({ type: 'layout-shift', buffered: true });
  });
  await page.goto(BASE + '/', { waitUntil: 'domcontentloaded', timeout: 120000 });
  await page.waitForSelector('.eco-card', { timeout: 120000 });
  // شیوهٔ لایت‌هاوس: بارگیری و نشستنِ صفحه، بدون پیمایش
  await page.waitForTimeout(7000);
  const cls = await page.evaluate(() => window.__cls);
  const src = await page.evaluate(() => window.__src);
  check('CLS زیرِ ۰/۰۵ است', cls < 0.05, cls.toFixed(4));
  const top = Object.entries(src).sort((a, b) => b[1] - a[1]).slice(0, 5);
  if (top.length) { console.log('   بزرگ‌ترین منبع‌هایِ جابه‌جایی:'); top.forEach(([k, v]) => console.log(`      ${v.toFixed(4)}  ${k}`)); }
  const ecoShift = Object.entries(src).filter(([k]) => /eco/i.test(k)).reduce((a, [, v]) => a + v, 0);
  check('بخشِ اکوسیستم جابه‌جاییِ چیدمان ندارد (<۰/۰۱)', ecoShift < 0.01, ecoShift.toFixed(4));
  await ctx.close();
}

await browser.close();

console.log(`\n${'═'.repeat(60)}`);
console.log(`گذر: ${pass}    ناگذر: ${fail}`);
if (failures.length) { console.log('\nمواردِ ناگذر:'); failures.forEach(f => console.log('  · ' + f)); }
console.log(`تصویرها: ${SHOTS}`);
process.exit(fail ? 1 : 0);
