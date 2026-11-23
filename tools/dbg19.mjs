import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
for (let run=1; run<=4; run++) {
  const ctx=await b.newContext({viewport:{width:1440,height:900}});
  const p=await ctx.newPage();
  await p.addInitScript(()=>{
    window.__shifts=[];
    new PerformanceObserver(l=>{ for(const e of l.getEntries()){
      if(e.hadRecentInput) continue;
      window.__shifts.push({t:Math.round(e.startTime), v:+e.value.toFixed(4),
        src:(e.sources||[]).map(s=>s.node?(s.node.tagName||'').toLowerCase()+'.'+((s.node.className||'').toString().trim().split(/\s+/)[0]||'?'):'?').slice(0,3)});
    }}).observe({type:'layout-shift',buffered:true});
  });
  await p.goto('http://localhost:5210/',{waitUntil:'domcontentloaded',timeout:120000});
  await p.waitForTimeout(7000);
  const r=await p.evaluate(()=>{
    const s=window.__shifts||[];
    const total=s.reduce((a,x)=>a+x.v,0);
    return {total:+total.toFixed(4), n:s.length, top:s.sort((a,x)=>x.v-a.v).slice(0,4)};
  });
  console.log(`\nاجرای ${run}: کل=${r.total} (${r.n} لغزش)`);
  r.top.forEach(x=>console.log(`    ${String(x.v).padStart(7)} در ${String(x.t).padStart(5)}ms | ${x.src.join(' , ')}`));
  await ctx.close();
}
await b.close();
