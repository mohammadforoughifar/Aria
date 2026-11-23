import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
for (const w of [1440,1280,1024,390]) {
  const ctx=await b.newContext({viewport:{width:w,height:900}});
  const p=await ctx.newPage();
  await p.goto('http://localhost:5210/',{waitUntil:'domcontentloaded',timeout:120000});
  await p.waitForSelector('.bundle',{state:'attached',timeout:30000}); await p.waitForTimeout(1200);
  const r=await p.evaluate(()=>{
    const c=document.querySelector('.bundle');
    const btns=[...c.querySelectorAll('.bundle__price')];
    const h=btns.map(x=>Math.round(x.getBoundingClientRect().height));
    const a=c.querySelector('.bundle__amount');
    const lines=Math.round(a.getBoundingClientRect().height / parseFloat(getComputedStyle(a).lineHeight));
    return {name:c.querySelector('.bundle__name').textContent.trim(), h,
      equalH: h[0]===h[1], lines, txt:a.textContent.trim(),
      clipped: a.scrollWidth - Math.round(a.getBoundingClientRect().width)};
  });
  console.log(`▌ ${w}px  ${r.name} | ارتفاع دکمه‌ها ${r.h.join(' و ')} ${r.equalH?'✅ برابر':'❌ نابرابر'} | «${r.txt}» در ${r.lines} خط | بریدگی ${r.clipped>1?'❌':'✅ ندارد'}`);
  await ctx.close();
}
await b.close();
