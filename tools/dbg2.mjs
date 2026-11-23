import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
for (const w of [1440, 768, 390]) {
  const ctx=await b.newContext({viewport:{width:w,height:900}});
  const p=await ctx.newPage();
  await p.goto('http://localhost:5210/',{waitUntil:'domcontentloaded',timeout:120000});
  await p.waitForSelector('.eco-card',{timeout:120000}); await p.waitForTimeout(2500);
  await p.evaluate(()=>{const e=document.querySelector('#ecosystem');window.scrollTo(0,e.getBoundingClientRect().top+window.scrollY-80);});
  await p.waitForTimeout(1200);
  const r=await p.evaluate(()=>{
    const root=document.querySelector('#ecosystem');
    const wrap=root.querySelector('.wrap');
    const cta=root.querySelector('.eco__cta');
    const btn=root.querySelector('.eco__cta .btn');
    const stage=root.querySelector('.eco__stage');
    const bx=e=>{const b=e.getBoundingClientRect();return {l:+b.left.toFixed(1),r:+b.right.toFixed(1),w:+b.width.toFixed(1),t:+b.top.toFixed(1),b:+b.bottom.toFixed(1)};};
    return {
      wrap:bx(wrap), cta:bx(cta), btn:bx(btn), stage:bx(stage),
      ctaDisplay:getComputedStyle(cta).display,
      ctaJustify:getComputedStyle(cta).justifyContent,
      wrapDisplay:getComputedStyle(wrap).display,
      wrapMaxW:getComputedStyle(wrap).maxWidth,
      btnMarginInline:getComputedStyle(btn).marginInlineStart+'/'+getComputedStyle(btn).marginInlineEnd,
    };
  });
  console.log(`\n════ ${w}px ════`);
  console.log('  wrap :', JSON.stringify(r.wrap));
  console.log('  cta  :', JSON.stringify(r.cta), '| display:', r.ctaDisplay, '| justify:', r.ctaJustify);
  console.log('  btn  :', JSON.stringify(r.btn), '| margin-inline:', r.btnMarginInline);
  console.log('  stage:', JSON.stringify(r.stage));
  console.log('  مرکزِ wrap:', ((r.wrap.l+r.wrap.r)/2).toFixed(1), ' مرکزِ دکمه:', ((r.btn.l+r.btn.r)/2).toFixed(1));
  console.log('  wrap display:', r.wrapDisplay, '| max-width:', r.wrapMaxW);
  await ctx.close();
}
await b.close();
