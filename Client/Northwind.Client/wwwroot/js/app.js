/* تعامل‌های سبکِ مرورگری — بدون هیچ وابستگی خارجی */
window.nw = {
  get: (key) => { try { return localStorage.getItem(key); } catch { return null; } },
  set: (key, value) => { try { localStorage.setItem(key, value); } catch { } },
  remove: (key) => { try { localStorage.removeItem(key); } catch { } },

  /* نشست (توکنِ ورود) در sessionStorage می‌نشیند، نه localStorage:
     با بستنِ مرورگر/تب پاک می‌شود و کاربر باید دوباره وارد شود؛
     ولی با تازه‌سازیِ همین صفحه پاک نمی‌شود. */
  sget: (key) => { try { return sessionStorage.getItem(key); } catch { return null; } },
  sset: (key, value) => { try { sessionStorage.setItem(key, value); } catch { } },
  sremove: (key) => { try { sessionStorage.removeItem(key); } catch { } },

  copy: async (text) => {
    try {
      if (navigator.clipboard && window.isSecureContext) {
        await navigator.clipboard.writeText(text);
        return true;
      }
      const ta = document.createElement('textarea');
      ta.value = text;
      ta.style.position = 'fixed';
      ta.style.opacity = '0';
      document.body.appendChild(ta);
      ta.select();
      const ok = document.execCommand('copy');
      document.body.removeChild(ta);
      return ok;
    } catch { return false; }
  },

  /* جایگزینیِ مقدارِ ورودی + بردنِ مکان‌نما به آخر (ورودیِ مبلغ) */
  setInputValue: (id, value) => {
    try {
      const el = document.getElementById(id);
      if (!el) return;
      if (el.value === value) return;
      el.value = value;
      if (document.activeElement === el) {
        const end = value.length;
        el.setSelectionRange(end, end);
      }
    } catch (e) { }
  },

  focusById: (id) => {
    try {
      const el = document.getElementById(id);
      if (el) el.focus();
    } catch (e) { }
  },
  scrollTop: () => { try { window.scrollTo({ top: 0, behavior: 'smooth' }); } catch { } },

  /* اسکرولِ نرم به یک بخش */
  scrollTo: (selector, offset) => {
    try {
      const el = document.querySelector(selector);
      if (!el) return;
      const top = el.getBoundingClientRect().top + window.scrollY - (offset || 80);
      window.scrollTo({ top: Math.max(0, top), behavior: 'smooth' });
    } catch (e) { }
  },

  /* اگر کاربر پایین‌تر از یک بخش باشد، نرم به بالایِ آن برمی‌گردد */
  scrollToIfNeeded: (selector, offset) => {
    try {
      const el = document.querySelector(selector);
      if (!el) return;
      const top = el.getBoundingClientRect().top + window.scrollY - (offset || 80);
      if (window.scrollY > top) window.scrollTo({ top: Math.max(0, top), behavior: 'smooth' });
    } catch (e) { }
  },

  /* پوستهٔ پنل خودش اسکرول دارد (.dash__content)؛ مرورگر هنگامِ تغییرِ مسیر
     فقط اسکرولِ سند را صفر می‌کند، پس این یکی را باید خودمان برگردانیم. */
  scrollPanelTop: () => {
    try {
      const el = document.querySelector('.dash__content');
      if (el) el.scrollTop = 0;
    } catch (e) { }
  },

  /* هدرِ چسبان: وقتی صفحه اسکرول می‌خورد، خطِ زیر هدر ظاهر می‌شود */
  watchScroll: (selector, className) => {
    const el = document.querySelector(selector);
    if (!el) return;
    const apply = () => el.classList.toggle(className, window.scrollY > 6);
    apply();
    window.addEventListener('scroll', apply, { passive: true });
  },
};

/* --------------------------------------------------------------------------
   حالتِ تاریک — اعمالِ زودهنگام تا هنگامِ بارگیریِ WASM چشمکِ سفید نزند
   -------------------------------------------------------------------------- */
(function () {
  var KEY = "nw-theme";
  function current() {
    var saved = null;
    try { saved = localStorage.getItem(KEY); } catch (e) { }
    if (saved === "dark" || saved === "light") return saved;
    /* پیش‌فرضِ سایت روشن است (پس‌زمینهٔ سفید)؛ تا وقتی کاربر چیزی انتخاب
       نکرده، تنظیمِ سیستم‌عامل را نادیده می‌گیریم. */
    return "light";
  }
  function apply(t) {
    document.documentElement.setAttribute("data-theme", t);
  }
  apply(current());

  window.nw.isDark = function () { return current() === "dark"; };
  /* ترجیحِ «کاهشِ حرکت» — خودبسنده نوشته شده تا به حوزهٔ دیگری
     وابسته نباشد (بلِیزر خیلی زود و پیش از تعریفِ بقیه صدایش می‌زند). */
  window.nw.reduceMotion = function () {
    return !!(window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches);
  };
  window.nw.setTheme = function (t) {
    try { localStorage.setItem(KEY, t); } catch (e) { }
    apply(t);
  };
  window.nw.toggleTheme = function () {
    var next = current() === "dark" ? "light" : "dark";
    window.nw.setTheme(next);
    return next;
  };
})();

/* =====================================================================
   دیاگرامِ اکوسیستم — سه کارِ کوچک که در CSS انجام‌شدنی نیست:
     ۱) تشخیصِ دیده‌شدن (IntersectionObserver) برای آغازِ انیمیشن و توقفِ حلقه‌ها
     ۲) هم‌ترازیِ عمق با اشاره‌گر (parallax) با نرم‌سازیِ lerp
     ۳) محاسبهٔ مسیرِ اتصال‌ها بر اساسِ مختصاتِ واقعیِ کارت‌ها
   ===================================================================== */
(function () {
  const reduceMotion = () =>
    window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  /* ---------- ۱) دیده‌شدن ---------- */
  function observeVisibility(el) {
    if (!el || !('IntersectionObserver' in window)) { el && el.classList.add('is-visible'); return; }
    const io = new IntersectionObserver((entries) => {
      entries.forEach((e) => {
        el.classList.toggle('is-visible', e.isIntersecting);
        // حلقه‌هایِ بی‌پایان فقط وقتی بخش در دید است اجرا می‌شوند
        el.classList.toggle('is-paused', !e.isIntersecting);
      });
    }, { threshold: 0.15 });
    io.observe(el);
    return io;
  }

  /* ---------- ۲) هم‌ترازی با اشاره‌گر ---------- */
  function initParallax(el, max) {
    if (!el) return () => { };
    if (reduceMotion() || !window.matchMedia('(hover: hover) and (pointer: fine)').matches) {
      el.classList.add('is-autosway');
      return () => { };
    }
    let tx = 0, ty = 0, cx = 0, cy = 0, raf = 0, running = true;

    const onMove = (e) => {
      const r = el.getBoundingClientRect();
      // بازهٔ ‎[-1, 1]‎ از مرکزِ بخش
      tx = ((e.clientX - (r.left + r.width / 2)) / (r.width / 2));
      ty = ((e.clientY - (r.top + r.height / 2)) / (r.height / 2));
      tx = Math.max(-1, Math.min(1, tx));
      ty = Math.max(-1, Math.min(1, ty));
    };
    const tick = () => {
      if (!running) return;
      // نرم‌سازی: هر قدم ۸٪ به مقدارِ هدف نزدیک می‌شویم
      cx += (tx - cx) * 0.08;
      cy += (ty - cy) * 0.08;
      el.style.setProperty('--px', (cx * max).toFixed(2) + 'px');
      el.style.setProperty('--py', (cy * max).toFixed(2) + 'px');
      raf = requestAnimationFrame(tick);
    };
    const onLeave = () => { tx = 0; ty = 0; };

    window.addEventListener('pointermove', onMove, { passive: true });
    window.addEventListener('pointerleave', onLeave, { passive: true });
    raf = requestAnimationFrame(tick);

    return () => {
      running = false;
      cancelAnimationFrame(raf);
      window.removeEventListener('pointermove', onMove);
      window.removeEventListener('pointerleave', onLeave);
    };
  }

})();
