import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
for (const w of [1920,1440,1280,1024]) {
  const ctx=await b.newContext({viewport:{width:w,height:900}});
  const p=await ctx.newPage();
  await p.goto('http://localhost:5210/',{waitUntil:'domcontentloaded',timeout:120000});
  await p.waitForSelector('.eco-card',{timeout:120000}); await p.waitForTimeout(2500);
  await p.evaluate(()=>{const e=document.querySelector('#ecosystem');window.scrollTo(0,e.getBoundingClientRect().top+window.scrollY-80);});
  await p.waitForTimeout(1500);
  const tabs=await p.locator('.eco-tab').count();
  let worstCard=0, worstHub=0, n=0;
  for(let t=0;t<tabs;t++){
    await p.locator('.eco-tab').nth(t).click(); await p.waitForTimeout(1000);
    const r=await p.evaluate(()=>{
      const root=document.querySelector('#ecosystem');
      const out=[];
      root.querySelectorAll('path[data-flow]').forEach(path=>{
        const card=root.querySelector('[data-eco-card="'+path.getAttribute('data-flow')+'"]');
        if(!card||!card.classList.contains('is-active')) return;
        const ctm=path.getScreenCTM();
        const pt=(x,y)=>{const q=new DOMPoint(x,y).matrixTransform(ctm);return {x:q.x,y:q.y};};
        const len=path.getTotalLength();
        const s=path.getPointAtLength(0), e=path.getPointAtLength(len);
        const dir=card.closest('.eco__col--people')?'in':'out';
        const cardPtScreen=pt(dir==='in'?s.x:e.x, dir==='in'?s.y:e.y);
        const hubPtScreen =pt(dir==='in'?e.x:s.x, dir==='in'?e.y:s.y);
        const cr=card.getBoundingClientRect();
        const hr=root.querySelector('[data-eco-hub]').getBoundingClientRect();
        // لبهٔ رو‌به‌رویِ کارت و هاب روی صفحه
        const cardEdgeX = cr.left > hr.left ? cr.left : cr.right;
        const hubEdgeX  = cr.left > hr.left ? hr.right : hr.left;
        out.push({
          id:path.getAttribute('data-flow'),
          cardGap: Math.abs(cardPtScreen.x - cardEdgeX),
          hubGap:  Math.abs(hubPtScreen.x - hubEdgeX),
        });
      });
      return out;
    });
    for(const o of r){ n++; worstCard=Math.max(worstCard,o.cardGap); worstHub=Math.max(worstHub,o.hubGap); }
  }
  console.log(`■ ${w}px — بدترین خطایِ بصری در میانِ ${n} اتصالِ فعال:`);
  console.log(`   فاصلهٔ سرِ خط تا لبهٔ کارت : ${worstCard.toFixed(1)}px`);
  console.log(`   فاصلهٔ سرِ خط تا لبهٔ هاب  : ${worstHub.toFixed(1)}px`);
  await ctx.close();
}
await b.close();
