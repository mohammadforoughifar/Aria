/* =====================================================================
   موتورِ گرافیکِ متحرکِ سرصفحه
   «ماژول‌ها روی یک پایگاهِ دادهٔ مشترک می‌نشینند»
   ---------------------------------------------------------------------
   · یک حلقهٔ requestAnimationFrame برای کلِ صحنه
   · مختصات از clientWidth/clientHeightِ صحنه گرفته می‌شود، نه
     getBoundingClientRect — پس هیچ خواندنِ چیدمانی درونِ حلقه نیست
   · با بیرون رفتن از دید یا پنهان شدنِ برگه، حلقه کاملاً متوقف می‌شود
   · فقط transform / opacity / ویژگی‌هایِ SVG انیمیت می‌شوند
   ===================================================================== */

const NS = 'http://www.w3.org/2000/svg';

/* ---------------------------------------------------------------- ماژول‌ها */
const MODULES = [
  { t: 'حسابداری',        c: '#2563EB', i: 'calc'   },
  { t: 'حقوق و دستمزد',   c: '#4F46E5', i: 'people' },
  { t: 'انبار',           c: '#0891B2', i: 'box'    },
  { t: 'فروش و فاکتور',   c: '#D97706', i: 'receipt'},
  { t: 'سامانهٔ مودیان',   c: '#0D9488', i: 'shield' },
  { t: 'اتوماسیون اداری', c: '#1E3A8A', i: 'mail'   }
];

const ICONS = {
  calc:    '<rect x="5" y="3" width="14" height="18" rx="2"/><path d="M8 7h8M8 12h2M12 12h2M8 16h2M12 16h2"/>',
  people:  '<circle cx="9" cy="8" r="3"/><path d="M3 20c0-3.3 2.7-6 6-6s6 2.7 6 6M16 11a3 3 0 100-6M18 14c2 .6 3 2.6 3 5"/>',
  box:     '<path d="M3 8l9-5 9 5v8l-9 5-9-5z"/><path d="M3 8l9 5 9-5M12 13v8"/>',
  receipt: '<path d="M6 3h12v18l-3-2-3 2-3-2-3 2z"/><path d="M9 8h6M9 12h6"/>',
  shield:  '<path d="M12 3l8 3v6c0 4.5-3.2 8-8 9-4.8-1-8-4.5-8-9V6z"/><path d="M8.5 12l2.5 2.5L16 9.5"/>',
  mail:    '<rect x="3" y="5" width="18" height="14" rx="2"/><path d="M3 7l9 6 9-6"/>'
};

const BASE = MODULES.map((_, k) => (-30 + k * 60) * Math.PI / 180);
/* سرعتِ زاویه‌ای برای همه یکسان است.
   چرا؟ اگر سرعت‌ها فرق می‌کردند، اختلافِ زاویه دائماً بزرگ می‌شد و دیر یا زود
   دو چیپ روی هم می‌افتادند (تا ۳۰٪ هم‌پوشانی). با سرعتِ برابر، جداییِ ۶۰
   درجه همیشه سرِ جایش می‌ماند و چیپ‌ها هرگز برخورد نمی‌کنند.
   تنوعِ بصری را «نفس کشیدنِ شعاعی» به‌عهده دارد (در R، با فازِ متفاوت برای
   هر چیپ) که جداییِ زاویه‌ای را به هم نمی‌زند.                            */
const SPEED = MODULES.map(() => 1);

const CAP_DOCKED = 'نرم‌افزارهای یکپارچه روی یک پایگاه داده';
const CAP_FREE   = 'هر نرم‌افزار، جدا از دیگری';

/* ------------------------------- وضعیتِ سراسری ------------------------------- */
let stage, svg, linksG, dotsG, dbEl, capEl;
let chips = [];
let W = 0, H = 0, scaleFit = 1;
let th = BASE.slice();
let dock = 1, T = 0;
let rafId = null, last = 0;
let visible = true;
let ro = null, io = null;
let capTimer = null;
let frames = 0;                 // شمارنده برای آزمونِ خودکار
let reduce = false;
let disposed = false;

/* ----------------------------------- ابزار ---------------------------------- */
const lerp = (a, b, t) => a + (b - a) * t;
const angDiff = (a, b) => Math.atan2(Math.sin(a - b), Math.cos(a - b));
const clamp01 = (v) => Math.max(0, Math.min(1, v));

function svgEl(tag, attrs) {
  const e = document.createElementNS(NS, tag);
  for (const k in attrs) e.setAttribute(k, attrs[k]);
  return e;
}

/* --------------------------------- ساخت ---------------------------------- */
function build() {
  svg = svgEl('svg', { class: 'orb__fx', 'aria-hidden': 'true', focusable: 'false' });
  const o1 = svgEl('ellipse', { class: 'orb__orbit' });
  const o2 = svgEl('ellipse', { class: 'orb__orbit', opacity: '.6' });
  linksG = svgEl('g', {});
  dotsG  = svgEl('g', {});
  svg.append(o1, o2, linksG, dotsG);
  stage.append(svg);
  svg._orbits = [o1, o2];

  // پایگاهِ داده
  dbEl = document.createElement('div');
  dbEl.className = 'orb__db';
  dbEl.setAttribute('aria-hidden', 'true');
  dbEl.innerHTML = `
    <span class="orb__halo"></span>
    <svg viewBox="0 0 120 130" fill="none" aria-hidden="true">
      <defs>
        <linearGradient id="orbDbA" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stop-color="var(--orb-db-a)"/><stop offset="1" stop-color="var(--orb-db-b)"/>
        </linearGradient>
        <linearGradient id="orbDbB" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stop-color="#9DC0FF"/><stop offset="1" stop-color="var(--orb-db-a)"/>
        </linearGradient>
      </defs>
      <path d="M10 92v18c0 10 22 18 50 18s50-8 50-18V92" fill="url(#orbDbA)"/>
      <ellipse cx="60" cy="92" rx="50" ry="18" fill="url(#orbDbB)"/>
      <path d="M10 62v22c0 10 22 18 50 18s50-8 50-18V62" fill="url(#orbDbA)"/>
      <ellipse cx="60" cy="62" rx="50" ry="18" fill="url(#orbDbB)"/>
      <path d="M10 30v24c0 10 22 18 50 18s50-8 50-18V30" fill="url(#orbDbA)"/>
      <ellipse cx="60" cy="30" rx="50" ry="18" fill="url(#orbDbB)"/>
      <ellipse cx="60" cy="27" rx="34" ry="9" fill="rgba(255,255,255,.35)"/>
    </svg>
    <small>پایگاه داده مشترک</small>`;
  stage.append(dbEl);

  // چیپ‌ها
  chips = MODULES.map((m) => {
    const el = document.createElement('div');
    el.className = 'orb__chip';
    el.style.setProperty('--c', m.c);
    el.setAttribute('aria-hidden', 'true');
    const ic = document.createElement('span');
    ic.className = 'orb__ic';
    ic.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">' + ICONS[m.i] + '</svg>';
    const lb = document.createElement('span');
    lb.textContent = m.t;
    el.append(ic, lb);
    stage.append(el);

    const ln = svgEl('line', { class: 'orb__link', pathLength: '1' });
    linksG.append(ln);
    const dots = [0, 1].map(() => {
      const c = svgEl('circle', { class: 'orb__dot', r: '4.2', opacity: '0' });
      dotsG.append(c);
      return c;
    });
    return { el, ln, dots };
  });

  // زیرنویس
  capEl = document.createElement('div');
  capEl.className = 'orb__cap';
  capEl.setAttribute('aria-hidden', 'true');
  capEl.textContent = CAP_DOCKED;
  stage.append(capEl);
}

/* -------------------------------- اندازه ---------------------------------- */
function size() {
  if (!stage) return;
  W = stage.clientWidth;
  H = stage.clientHeight;
  scaleFit = Math.min(1, W / 520);
  if (svg) {
    svg.setAttribute('viewBox', `0 0 ${W} ${H}`);
    const rx = W * 0.335, ry = H * 0.335;
    svg._orbits.forEach((e, i) => {
      const k = i === 0 ? 1 : 1.17;
      e.setAttribute('cx', W / 2); e.setAttribute('cy', H / 2);
      e.setAttribute('rx', rx * k); e.setAttribute('ry', ry * k);
    });
  }
}

/* -------------------------------- رسم ------------------------------------ */
function render() {
  if (!stage || !W) return;
  const cx = W / 2, cy = H / 2 - H * 0.01, rx = W * 0.335, ry = H * 0.335;
  const lp = clamp01((dock - 0.55) / 0.45);

  chips.forEach((c, k) => {
    const R = lerp(1.14 + 0.055 * Math.sin(T * 0.7 + k * 1.3), 1, dock);
    const x = cx + Math.cos(th[k]) * rx * R;
    const y = cy + Math.sin(th[k]) * ry * R * lerp(0.9, 1, dock);
    const depth = (Math.sin(th[k]) + 1) / 2;
    const sc = lerp(0.8 + 0.32 * depth, 1, dock) * scaleFit;

    c.el.style.transform =
      `translate(${x.toFixed(1)}px,${y.toFixed(1)}px) translate(-50%,-50%) scale(${sc.toFixed(3)})`;
    c.el.style.opacity = lerp(0.7 + 0.3 * depth, 1, dock).toFixed(2);
    // چیپ‌هایِ جلویی بالایِ پایگاهِ داده می‌نشینند، پشتی‌ها زیرِ آن
    c.el.classList.toggle('is-front', depth > 0.55 && dock < 0.9);

    c.ln.setAttribute('x1', x.toFixed(1)); c.ln.setAttribute('y1', y.toFixed(1));
    c.ln.setAttribute('x2', cx.toFixed(1)); c.ln.setAttribute('y2', cy.toFixed(1));
    c.ln.style.strokeDashoffset = (1 - lp).toFixed(3);
    c.ln.style.opacity = lp > 0 ? '1' : '0';

    c.dots.forEach((d, j) => {
      if (lp < 0.98 || reduce) { d.setAttribute('opacity', '0'); return; }
      const t = (T * 0.42 + k * 0.13 + j * 0.5) % 1;
      d.setAttribute('cx', lerp(x, cx, t).toFixed(1));
      d.setAttribute('cy', lerp(y, cy, t).toFixed(1));
      d.setAttribute('opacity', Math.sin(Math.PI * t).toFixed(2));
    });
  });

  if (dbEl) {
    dbEl.classList.toggle('is-live', dock > 0.92 && !reduce);
    dbEl.style.scale = (1 + 0.03 * Math.sin(T * 2.2) * dock).toFixed(3);
  }

  // زیرنویس با محو شدنِ ۲۲۰ میلی‌ثانیه
  const want = dock > 0.5 ? CAP_DOCKED : CAP_FREE;
  if (capEl && capEl.textContent !== want && !capTimer) {
    capEl.classList.add('is-fade');
    capTimer = setTimeout(() => {
      capTimer = null;
      if (!capEl) return;
      capEl.textContent = dock > 0.5 ? CAP_DOCKED : CAP_FREE;
      capEl.classList.remove('is-fade');
    }, 220);
  }
}

/* ------------------------------- حلقه ------------------------------------- */
function frame(now) {
  const dt = Math.min(0.05, (now - last) / 1000);
  last = now;
  frames++;

  T += dt;
  /* زمان‌بندی: با T+5 آغاز می‌شود تا نخستین قاب در حالتِ «نشسته» باشد؛
     نخستین رهاسازی ۴/۵ ثانیه پس از بارگیری رخ می‌دهد. */
  const c = (T + 5) % 12;
  const target = (c >= 2.5 && c < 9.5) ? 1 : 0;
  dock += (target - dock) * (1 - Math.exp(-dt * 2.4));

  const w = 0.55 * (1 - dock);
  for (let k = 0; k < th.length; k++) {
    th[k] += w * SPEED[k] * dt;
    // با dock^3 دقیقاً روی زاویهٔ پایه می‌نشیند
    th[k] += angDiff(BASE[k], th[k]) * (1 - Math.exp(-dt * 6 * dock * dock * dock));
  }

  render();
  rafId = requestAnimationFrame(frame);
}

function start() {
  if (disposed || reduce || rafId !== null) return;
  last = performance.now();
  rafId = requestAnimationFrame(frame);
}
function stop() {
  if (rafId !== null) { cancelAnimationFrame(rafId); rafId = null; }
}

/* ------------------------------ شمارنده‌ها -------------------------------- */
const FA_DIGITS = '۰۱۲۳۴۵۶۷۸۹';
const toFa = (n) => Math.round(n).toString().replace(/\d/g, (d) => FA_DIGITS[+d]);
const fromFa = (s) => parseInt(String(s).replace(/[۰-۹]/g, (d) => FA_DIGITS.indexOf(d)).replace(/[^\d-]/g, ''), 10) || 0;

function countTo(el, to, from, dur) {
  if (reduce) { el.textContent = toFa(to); return; }
  const t0 = performance.now();
  const step = (now) => {
    const p = Math.min(1, (now - t0) / dur);
    const e = 1 - Math.pow(1 - p, 3);
    el.textContent = toFa(from + (to - from) * e);
    if (p < 1) requestAnimationFrame(step);
  };
  requestAnimationFrame(step);
}

let statIo = null;
export function initStats(root) {
  if (!root || reduce) return;
  const els = Array.from(root.querySelectorAll('[data-stat]'));
  if (!els.length) return;
  statIo = new IntersectionObserver((entries) => {
    for (const e of entries) {
      if (!e.isIntersecting) continue;
      statIo.unobserve(e.target);
      countTo(e.target, +e.target.dataset.n || 0, 0, 1200);
    }
  }, { threshold: 0.6 });
  els.forEach((el) => statIo.observe(el));
}

/* اگر بعداً مقدارِ واقعی از API رسید، از مقدارِ کنونی به آن می‌رویم — نه از صفر */
export function updateStats(root) {
  if (!root) return;
  root.querySelectorAll('[data-stat]').forEach((el) => {
    const to = +el.dataset.n || 0;
    const cur = fromFa(el.textContent);
    if (cur === to) return;
    countTo(el, to, cur, 900);
  });
}

/* ------------------------------ راه‌اندازی -------------------------------- */
export function init(el) {
  if (!el) return false;
  stage = el;
  reduce = !!(window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches);

  build();
  size();

  if (reduce) {
    // حالتِ نشستهٔ ایستا، بدون هیچ حلقه‌ای
    dock = 1;
    chips.forEach((_, k) => { th[k] = BASE[k]; });
    render();
    return true;
  }

  if ('ResizeObserver' in window) {
    ro = new ResizeObserver(() => { size(); render(); });
    ro.observe(stage);
  }
  if ('IntersectionObserver' in window) {
    io = new IntersectionObserver((entries) => {
      visible = entries[0].isIntersecting;
      if (visible && !document.hidden) start(); else stop();
    }, { threshold: 0 });
    io.observe(stage);
  }

  document.addEventListener('visibilitychange', onVis);
  window.addEventListener('orientationchange', onSize, { passive: true });
  if (document.fonts && document.fonts.ready) {
    document.fonts.ready.then(() => { size(); render(); }).catch(() => { });
  }

  frames = 0;
  start();
  return true;
}

function onVis() { if (document.hidden) stop(); else if (visible) start(); }
function onSize() { size(); render(); }

/* -------------------------------- پایان ---------------------------------- */
export function dispose() {
  disposed = true;
  stop();
  if (ro) { ro.disconnect(); ro = null; }
  if (io) { io.disconnect(); io = null; }
  if (statIo) { statIo.disconnect(); statIo = null; }
  if (capTimer) { clearTimeout(capTimer); capTimer = null; }
  document.removeEventListener('visibilitychange', onVis);
  window.removeEventListener('orientationchange', onSize);
  if (svg) svg.remove();
  if (dbEl) dbEl.remove();
  if (capEl) capEl.remove();
  chips.forEach((c) => c.el.remove());
  chips = []; svg = null; dbEl = null; capEl = null; stage = null;
}

/* شمارنده برای آزمونِ خودکار: فقط وقتی حلقه واقعاً می‌چرخد بالا می‌رود */
Object.defineProperty(window, '__heroOrbitFrames', {
  get: () => frames,
  configurable: true
});
