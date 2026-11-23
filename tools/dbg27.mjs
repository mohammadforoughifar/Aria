import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
for(let i=1;i<=14;i++){
  const ctx=await b.newContext({viewport:{width:1440,height:900}});
  const p=await ctx.newPage();
  await p.addInitScript(()=>{
    window.__cls=0; window.__big=[];
    new PerformanceObserver(l=>{for(const e of l.getEntries()){ if(e.hadRecentInput) continue;
      window.__cls+=e.value;
      if(e.value>0.2){
        window.__big.push({t:Math.round(e.startTime), v:+e.value.toFixed(4),
          srcs:(e.sources||[]).map(s=>{
            const n=s.node; const cn=(n&&typeof n.className==='string'&&n.className.trim())?'.'+n.className.trim().split(/\s+/).slice(0,2).join('.'):'';
            return {el:(n&&n.tagName?n.tagName.toLowerCase():'?')+cn,
              prev:s.previousRect?[Math.round(s.previousRect.y),Math.round(s.previousRect.height)]:null,
              cur:s.currentRect?[Math.round(s.currentRect.y),Math.round(s.currentRect.height)]:null};
          })});
      }
    }}).observe({type:'layout-shift',buffered:true});
  });
  await p.goto('http://localhost:5210/',{waitUntil:'domcontentloaded',timeout:120000});
  await p.waitForTimeout(8000);
  const {cls,big}=await p.evaluate(()=>({cls:+window.__cls.toFixed(4), big:window.__big}));
  if(big.length){
    console.log(`\n★ اجرای ${i} — CLS ${cls}`);
    big.forEach(s=>{
      console.log(`   ${s.v} در ${s.t}ms`);
      s.srcs.forEach(x=>console.log(`      ${x.el.padEnd(28)} y/ارتفاع: ${JSON.stringify(x.prev)} → ${JSON.stringify(x.cur)}`));
    });
    await ctx.close(); break;
  } else { console.log(`   اجرای ${i}: ${cls} — بدون لغزشِ بزرگ`); await ctx.close(); }
}
await b.close();
