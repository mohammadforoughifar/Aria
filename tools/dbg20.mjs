import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
for (const delay of [0, 1500, 3000]) {
  const ctx=await b.newContext({viewport:{width:1440,height:900}});
  const p=await ctx.newPage();
  await p.addInitScript(()=>{
    window.__cls=0;
    new PerformanceObserver(l=>{for(const e of l.getEntries()){ if(e.hadRecentInput) continue; window.__cls+=e.value;}})
      .observe({type:'layout-shift',buffered:true});
  });
  if (delay) {
    await p.route('**/api/**', async r => { await new Promise(s=>setTimeout(s,delay)); await r.continue(); });
  }
  await p.goto('http://localhost:5210/',{waitUntil:'domcontentloaded',timeout:120000});
  await p.waitForTimeout(9000);
  const cls=await p.evaluate(()=>+window.__cls.toFixed(4));
  console.log(`تأخیرِ API ${String(delay).padStart(4)}ms  →  CLS ${cls}`);
  await ctx.close();
}
await b.close();
