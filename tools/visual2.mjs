import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
const ctx=await b.newContext({viewport:{width:1440,height:900}});
const p=await ctx.newPage();
await p.goto('http://localhost:5210/',{waitUntil:'domcontentloaded',timeout:120000});
await p.waitForSelector('.eco-card',{timeout:120000}); await p.waitForTimeout(2500);
await p.evaluate(()=>{const e=document.querySelector('#ecosystem');window.scrollTo(0,e.getBoundingClientRect().top+window.scrollY-80);});
await p.waitForTimeout(1500);
for(let t=0;t<5;t++){
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
      const cP=pt(dir==='in'?s.x:e.x, dir==='in'?s.y:e.y);
      const hP=pt(dir==='in'?e.x:s.x, dir==='in'?e.y:s.y);
      const cr=card.getBoundingClientRect();
      const hr=root.querySelector('[data-eco-hub]').getBoundingClientRect();
      const onRight = cr.left > hr.left;
      const cardEdge = onRight ? cr.left : cr.right;
      const hubEdge  = onRight ? hr.right : hr.left;
      const dirIntoCard = onRight ? 1 : -1;   // به سمتِ درونِ کارت
      const dirIntoHub  = onRight ? -1 : 1;   // به سمتِ درونِ هاب
      out.push({
        id:path.getAttribute('data-flow'),
        side: onRight?'راست':'چپ',
        // مثبت = سرِ خط زیرِ کارت پنهان است (خوب)؛ منفی = بیرون مانده (بد)
        cardInto: ((cP.x - cardEdge) * dirIntoCard),
        hubInto:  ((hP.x - hubEdge)  * dirIntoHub),
      });
    });
    return out;
  });
  console.log(`\n── سناریو ${t+1}`);
  r.forEach(o=>console.log(`   ${o.id.padEnd(11)} ${o.side}  کارت: ${o.cardInto>=0?'✅ زیرِ کارت '+o.cardInto.toFixed(1):'❌ بیرون '+Math.abs(o.cardInto).toFixed(1)}px   هاب: ${o.hubInto>=0?'✅ زیرِ هاب '+o.hubInto.toFixed(1):'❌ بیرون '+Math.abs(o.hubInto).toFixed(1)}px`));
}
await b.close();
