import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
for (const w of [900, 768, 390]) {
  const ctx=await b.newContext({viewport:{width:w,height:1000}});
  const p=await ctx.newPage();
  await p.goto('http://localhost:5210/catalog',{waitUntil:'domcontentloaded',timeout:120000});
  await p.waitForSelector('.cat-card',{timeout:120000});
  await p.waitForTimeout(3000);
  const r=await p.evaluate(()=>{
    const vw = document.documentElement.clientWidth;
    const out=[];
    document.querySelectorAll('*').forEach(el=>{
      const b=el.getBoundingClientRect();
      if (b.width===0) return;
      if (b.right > vw+1 || b.left < -1) {
        out.push({tag:el.tagName.toLowerCase(),
          cls:(el.className||'').toString().trim().split(/\s+/).slice(0,3).join('.'),
          left:Math.round(b.left), right:Math.round(b.right), w:Math.round(b.width)});
      }
    });
    return {vw, scrollW: document.documentElement.scrollWidth, out: out.slice(0,10)};
  });
  console.log(`\n■ عرض ${w} — پهنایِ قابل‌دید ${r.vw}، پهنایِ پیمایش ${r.scrollW}`);
  if(!r.out.length) console.log('   هیچ عنصری بیرون نزده ✅');
  r.out.forEach(o=>console.log(`   ${o.tag}.${o.cls}`.padEnd(46)+` چپ=${o.left} راست=${o.right} عرض=${o.w}`));
  await ctx.close();
}
await b.close();
