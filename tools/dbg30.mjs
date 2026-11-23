import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
for (const w of [1536,1440,1280,1024,768,390]) {
  const ctx=await b.newContext({viewport:{width:w,height:900}});
  const p=await ctx.newPage();
  await p.goto('http://localhost:5210/',{waitUntil:'domcontentloaded',timeout:120000});
  await p.waitForSelector('.bundle',{state:'attached',timeout:30000});
  await p.waitForTimeout(1200);
  const r=await p.evaluate(()=>{
    const out=[];
    document.querySelectorAll('.bundle').forEach(card=>{
      const cr=card.getBoundingClientRect();
      const name=card.querySelector('.bundle__name')?.textContent.trim();
      card.querySelectorAll('.bundle__price').forEach(btn=>{
        const br=btn.getBoundingClientRect();
        const amt=btn.querySelector('.bundle__amount');
        const per=btn.querySelector('.bundle__period')?.textContent.trim();
        const bleedL=Math.round(cr.left-br.left), bleedR=Math.round(br.right-cr.right);
        out.push({name, per,
          bleed: Math.max(bleedL,bleedR),
          btnW: Math.round(br.width),
          amtScroll: amt?amt.scrollWidth:0, amtClient: amt?Math.round(amt.getBoundingClientRect().width):0});
      });
    });
    return out;
  });
  const bad=r.filter(x=>x.bleed>1 || x.amtScroll-x.amtClient>1);
  console.log(`\n▌ ${w}px — ${bad.length?'❌ '+bad.length+' جعبهٔ مشکل‌دار':'✅ سالم'}`);
  bad.slice(0,4).forEach(x=>console.log(`   ${x.name} / ${x.per}: بیرون‌زدگی ${x.bleed}px | پهنای دکمه ${x.btnW} | متن ${x.amtScroll} در ${x.amtClient}`));
  await ctx.close();
}
await b.close();
