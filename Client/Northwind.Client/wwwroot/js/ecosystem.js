/* =====================================================================
   اکوسیستمِ نرم‌افزار — هندسه و چرخهٔ حیات
   ---------------------------------------------------------------------
   این ماژول مالکِ گره‌هایِ SVG است؛ بلِیزر آن‌ها را لمس نمی‌کند تا
   انیمیشن‌ها با هر بازرندر از نو شروع نشوند. بلِیزر فقط کلاسِ
   is-active را روی کارت‌ها می‌گذارد و اینجا آن را می‌خوانیم.

   چرا اندازه‌گیری با offsetLeft/offsetTop و نه getBoundingClientRect؟
     · صحنه ترَنسفورمِ سه‌بعدی دارد و کارت‌ها شناور و مقیاس‌دار می‌شوند؛
       مستطیلِ حاصل از getBoundingClientRect هر فریم عوض می‌شود.
     · offsetLeft/Top/Width/Height به چیدمانِ بدون ترَنسفورم نگاه
       می‌کنند، پس زیرِ هر ترَنسفورم یا انیمیشنی پایدار می‌مانند.
   ===================================================================== */

const NS = 'http://www.w3.org/2000/svg';
const XLINK = 'http://www.w3.org/1999/xlink';

function mk(tag, attrs) {
  const e = document.createElementNS(NS, tag);
  if (attrs) for (const k in attrs) e.setAttribute(k, attrs[k]);
  return e;
}

function reduceMotion() {
  return window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}

export function create(root) {
  const scene = root && root.querySelector('[data-eco-scene]');
  const svg   = root && root.querySelector('[data-eco-wires]');
  const hub   = root && root.querySelector('[data-eco-hub]');
  if (!scene || !svg || !hub) return { update() { }, destroy() { } };

  const INSET = 12;          // خط ۱۲ پیکسل داخلِ لبه می‌نشیند تا خطایِ پارالاکس زیرِ کارت پنهان بماند
  const TILT  = 0.16;        // شدتِ شیبِ صحنه با اشاره‌گر

  let links = [];            // هر اتصال: کارت، مسیرِ زمینه، مسیرِ جریان، ذرات
  let raf = 0;
  let ro = null;             // دیده‌بانِ اندازه
  let io = null;             // دیده‌بانِ دیده‌شدن
  let mo = null;             // دیده‌بانِ تغییرِ کلاسِ کارت‌ها
  let vis = false;
  let armed = false;

  /* ------------------------------------------------------------------
     ساختِ گره‌ها — فقط یک‌بار به‌ازایِ هر کارت
     ------------------------------------------------------------------ */
  function cards() { return Array.from(scene.querySelectorAll('[data-eco-card]')); }

  function build() {
    const list = cards();
    if (links.length === list.length && links.every((l, i) => l.card === list[i])) return false;

    svg.textContent = '';
    links = [];

    list.forEach((card) => {
      const id = card.getAttribute('data-eco-card');
      if (!id) return;
      /* جهتِ جریان از ستون فهمیده می‌شود:
         کاربران داده را «می‌خورند» (in)، سیستم‌ها داده «می‌گیرند» (out) */
      const dir = card.closest('.eco__col--people') ? 'in' : 'out';
      const pid = 'eco-flow-' + id;

      const base = mk('path', { class: 'eco__wire', 'data-conn': id });
      const flow = mk('path', { class: 'eco__flow', id: pid, pathLength: '1', 'data-flow': id });
      svg.append(base, flow);

      const dots = [];
      if (!reduceMotion()) {
        for (let k = 0; k < 2; k++) {
          const dot = mk('circle', { class: 'eco__dot', r: '3.2', cx: '0', cy: '0' });
          const mo2 = mk('animateMotion', {
            dur: '2.8s',
            begin: (k * 1.4) + 's',
            repeatCount: 'indefinite',
            calcMode: 'linear'
          });
          const mp = mk('mpath');
          mp.setAttribute('href', '#' + pid);
          mp.setAttributeNS(XLINK, 'xlink:href', '#' + pid);
          mo2.append(mp);
          dot.append(mo2);
          svg.append(dot);
          dots.push(dot);
        }
      }

      links.push({ id, card, dir, base, flow, dots, on: false });
    });

    observeCards();
    return true;
  }

  /* ------------------------------------------------------------------
     هندسه — همه‌چیز نسبت به صحنه، بدون ترَنسفورم
     ------------------------------------------------------------------ */
  function pos(el, stop) {
    let x = 0, y = 0, guard = 0;
    while (el && el !== stop && guard++ < 32) {
      x += el.offsetLeft;
      y += el.offsetTop;
      el = el.offsetParent;
    }
    return { x, y };
  }

  function anchors(card) {
    const c = pos(card, scene);
    const h = pos(hub, scene);

    const hcx = h.x + hub.offsetWidth / 2;
    const hcy = h.y + hub.offsetHeight / 2;
    const ccx = c.x + card.offsetWidth / 2;
    const ccy = c.y + card.offsetHeight / 2;

    /* مقایسهٔ واقعیِ جایگاه — در راست‌چین و چپ‌چین هر دو درست است */
    const rightSide = ccx > hcx;

    const a = {
      x: rightSide ? c.x + INSET : c.x + card.offsetWidth - INSET,
      y: ccy
    };
    const b = {
      x: rightSide ? hcx + hub.offsetWidth / 2 - INSET : hcx - hub.offsetWidth / 2 + INSET,
      y: hcy + (ccy - hcy) * TILT          // روی لبهٔ هاب کمی بادبزن می‌شوند
    };
    return { a, b };
  }

  function bezier(s, e) {
    const k = 0.55;
    const x1 = s.x + (e.x - s.x) * k;
    const x2 = e.x - (e.x - s.x) * k;
    return `M ${s.x.toFixed(1)} ${s.y.toFixed(1)} C ${x1.toFixed(1)} ${s.y.toFixed(1)}, ${x2.toFixed(1)} ${e.y.toFixed(1)}, ${e.x.toFixed(1)} ${e.y.toFixed(1)}`;
  }

  /* ------------------------------------------------------------------
     به‌روزرسانی — حداکثر یک‌بار در هر فریم
     ------------------------------------------------------------------ */
  function schedule() {
    if (raf) return;
    raf = requestAnimationFrame(() => { raf = 0; update(); });
  }

  function update() {
    build();
    if (!links.length) return;

    const W = scene.offsetWidth;
    const H = scene.offsetHeight;
    if (!W || !H) return;

    /* viewBox باید دقیقاً برابرِ اندازهٔ چیدمانیِ صحنه باشد تا
       یک واحدِ SVG برابرِ یک پیکسل شود */
    svg.setAttribute('viewBox', `0 0 ${W} ${H}`);

    for (const l of links) {
      const { a, b } = anchors(l.card);
      // in: از کارت به هاب — out: از هاب به کارت
      const d = l.dir === 'in' ? bezier(a, b) : bezier(b, a);
      l.base.setAttribute('d', d);
      l.flow.setAttribute('d', d);
    }
    svg.setAttribute('data-ready', '1');
  }

  /* ------------------------------------------------------------------
     وضعیتِ فعال/غیرفعال — با حذف و بازگردانیِ کلاس تا ترسیم دوباره شود
     ------------------------------------------------------------------ */
  function applyActive() {
    let changed = false;
    for (const l of links) {
      const on = l.card.classList.contains('is-active');
      if (on !== l.on) { l.on = on; changed = true; }
    }
    if (!changed) return;

    for (const l of links) {
      l.flow.classList.remove('is-on');
      l.dots.forEach((d) => d.classList.remove('is-on'));
    }
    void scene.offsetWidth;                 // بازنشانیِ اجباری تا ترسیم از نو آغاز شود
    requestAnimationFrame(() => {
      for (const l of links) {
        if (!l.on) continue;
        l.flow.classList.add('is-on');
        l.dots.forEach((d) => d.classList.add('is-on'));
      }
    });
  }

  /* ------------------------------------------------------------------
     دیده‌بان‌ها
     ------------------------------------------------------------------ */
  function observeCards() {
    if (!('ResizeObserver' in window)) return;
    if (ro) ro.disconnect();
    ro = new ResizeObserver(schedule);
    ro.observe(scene);
    ro.observe(hub);
    links.forEach((l) => ro.observe(l.card));
  }

  function onResize() { schedule(); }
  function onOrient() { schedule(); }

  function mount() {
    build();

    /* ۱) نخستین دیده‌شدن — ورود یک‌بار اجرا می‌شود و دیگر تکرار نمی‌شود */
    if ('IntersectionObserver' in window) {
      io = new IntersectionObserver((entries) => {
        for (const en of entries) {
          vis = en.isIntersecting;
          if (vis) {
            if (!armed) { armed = true; root.classList.add('is-visible'); }
            root.classList.remove('is-offscreen');
            if (svg.unpauseAnimations) svg.unpauseAnimations();
            update();
          } else {
            root.classList.add('is-offscreen');
            if (svg.pauseAnimations) svg.pauseAnimations();
          }
        }
      }, { rootMargin: '0px 0px -12% 0px', threshold: 0.15 });
      io.observe(root);
    } else {
      armed = true;
      root.classList.add('is-visible');
    }

    /* ۲) تغییرِ کلاسِ کارت‌ها (تعویضِ سناریو) */
    if ('MutationObserver' in window) {
      mo = new MutationObserver(() => { applyActive(); schedule(); });
      mo.observe(scene, { subtree: true, attributes: true, attributeFilter: ['class'] });
    }

    /* ۳) فونت‌ها — جابه‌جاییِ ناگهانیِ متن ارتفاعِ کارت را عوض می‌کند */
    if (document.fonts && document.fonts.ready) {
      document.fonts.ready.then(() => { update(); schedule(); }).catch(() => { });
    }

    window.addEventListener('resize', onResize, { passive: true });
    window.addEventListener('orientationchange', onOrient, { passive: true });

    /* ۴) شیبِ ملایم با اشاره‌گر — فقط روی صحنه */
    let stopTilt = () => { };
    if (!reduceMotion() && window.matchMedia('(hover: hover) and (pointer: fine)').matches) {
      let tx = 0, ty = 0, cx = 0, cy = 0, r = 0, alive = true;
      const onMove = (e) => {
        if (!vis) return;
        const b = root.getBoundingClientRect();
        tx = Math.max(-1, Math.min(1, (e.clientX - (b.left + b.width / 2)) / (b.width / 2)));
        ty = Math.max(-1, Math.min(1, (e.clientY - (b.top + b.height / 2)) / (b.height / 2)));
      };
      const tick = () => {
        if (!alive) return;
        cx += (tx - cx) * 0.08;
        cy += (ty - cy) * 0.08;
        root.style.setProperty('--px', (cx * 10).toFixed(2));
        root.style.setProperty('--py', (cy * 10).toFixed(2));
        r = requestAnimationFrame(tick);
      };
      const onLeave = () => { tx = 0; ty = 0; };
      window.addEventListener('pointermove', onMove, { passive: true });
      window.addEventListener('pointerleave', onLeave, { passive: true });
      r = requestAnimationFrame(tick);
      stopTilt = () => {
        alive = false;
        cancelAnimationFrame(r);
        window.removeEventListener('pointermove', onMove);
        window.removeEventListener('pointerleave', onLeave);
      };
    }

    /* ۵) توقفِ حلقه‌ها هنگامِ اشاره‌گر روی بخش یا خروج از دید */
    const onEnter = () => { root.classList.add('is-paused'); if (svg.pauseAnimations) svg.pauseAnimations(); };
    const onOut   = () => {
      root.classList.remove('is-paused');
      if (vis && svg.unpauseAnimations) svg.unpauseAnimations();
    };
    root.addEventListener('pointerenter', onEnter);
    root.addEventListener('pointerleave', onOut);

    update();
    applyActive();
    // اندازه‌گیریِ دوباره پس از نشستنِ چیدمان
    requestAnimationFrame(update);
    setTimeout(update, 350);

    return () => {
      stopTilt();
      root.removeEventListener('pointerenter', onEnter);
      root.removeEventListener('pointerleave', onOut);
    };
  }

  const stopTiltAndHover = mount();

  /* ------------------------------------------------------------------
     پایان — همهٔ دیده‌بان‌ها آزاد می‌شوند
     ------------------------------------------------------------------ */
  function destroy() {
    if (raf) cancelAnimationFrame(raf);
    if (ro) ro.disconnect();
    if (io) io.disconnect();
    if (mo) mo.disconnect();
    if (stopTiltAndHover) stopTiltAndHover();
    window.removeEventListener('resize', onResize);
    window.removeEventListener('orientationchange', onOrient);
    links = [];
  }

  return { update, destroy };
}
