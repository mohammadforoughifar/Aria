import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
for (const block of [false, true]) {
  const ctx=await b.newContext({viewport:{width:1440,height:900}});
  if(block){ for(const p of ['**/*.woff2','**/*.woff','**/*.ttf']) await ctx.route(p,r=>r.abort()); }
  const p=await ctx.newPage();
  await p.addInitScript(()=>{
    window.__sh=[];
    new PerformanceObserver(l=>{for(const e of l.getEntries()){
      if(e.hadRecentInput) continue;
      window.__sh.push({t:+e.startTime.toFixed(0), v:+e.value.toFixed(4),
        nodes:(e.sources||[]).slice(0,3).map(s=>s.node?(s.node.tagName||'').toLowerCase()+
          (typeof s.node.className==='string'&&s.node.className?'.'+s.node.className.trim().split(/\s+/)[0]:''):'?')});
    }}).observe({type:'layout-shift',buffered:true});
  });
  await p.goto('http://localhost:5210/catalog',{waitUntil:'domcontentloaded',timeout:120000});
  await p.waitForSelector('.orb__chip',{timeout:120000});
  await p.waitForTimeout(9000);
  const r=await p.evaluate(()=>window.__sh);
  console.log(`\n■ فونت ${block?'مسدود':'آزاد'} — مجموع: ${r.reduce((a,x)=>a+x.v,0).toFixed(4)}`);
  r.forEach(s=>console.log(`   ${String(s.t).padStart(6)}ms  ${String(s.v).padEnd(8)}  ${s.nodes.join(' , ')}`));
  await ctx.close();
}
await b.close();
