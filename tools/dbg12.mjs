import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
for (const w of [1440, 1024]) {
  const ctx=await b.newContext({viewport:{width:w,height:900}});
  const p=await ctx.newPage();
  await p.goto('http://localhost:5210/catalog',{waitUntil:'domcontentloaded',timeout:120000});
  await p.waitForSelector('.cat-card',{timeout:120000});
  await p.waitForTimeout(3500);
  const r=await p.evaluate(()=>{
    const rows = (sel) => {
      const els=[...document.querySelectorAll(sel)];
      if(!els.length) return null;
      const tops=els.map(e=>Math.round(e.getBoundingClientRect().top));
      const groups={}; tops.forEach(t=>{ groups[t]=(groups[t]||0)+1; });
      const perRow=Object.values(groups);
      return { n: els.length, perRow: perRow.join('+'), rows: perRow.length };
    };
    return {
      'کارت‌های دسته (cat-card)': rows('.cat-card'),
      'کارت‌های نرم‌افزار (sw-card)': rows('.sw-card'),
      'پرسش‌ها (faq)': rows('.faq__item, .faq details, .faq > *'),
      'مراحلِ راهنما': rows('.guide__step, .guide li, .guide__item, .guide > *'),
    };
  });
  console.log(`\n════ ${w}px ════`);
  for (const [k,v] of Object.entries(r)) {
    console.log(`   ${k.padEnd(32)} ${v ? `${v.n} مورد | در هر ردیف: ${v.perRow} | ${v.rows} ردیف` : '—'}`);
  }
  await ctx.close();
}
await b.close();
