import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
const ctx=await b.newContext({viewport:{width:1440,height:900}});
const p=await ctx.newPage();
await p.addInitScript(()=>{
  window.__cls=0; window.__src={};
  new PerformanceObserver(l=>{for(const e of l.getEntries()){
    if(e.hadRecentInput) continue; window.__cls+=e.value;
    for(const s of (e.sources||[])){ const n=s.node; if(!n||!n.tagName) continue;
      const k=n.tagName.toLowerCase()+(n.id?'#'+n.id:'')+(typeof n.className==='string'&&n.className?'.'+n.className.trim().split(/\s+/).slice(0,2).join('.'):'');
      window.__src[k]=(window.__src[k]||0)+e.value; }
  }}).observe({type:'layout-shift',buffered:true});
  window.__long=[];
  new PerformanceObserver(l=>{for(const e of l.getEntries())
    window.__long.push({t:+e.startTime.toFixed(0), d:+e.duration.toFixed(0), n:e.name});
  }).observe({type:'longtask',buffered:true});
  window.__t0=performance.now();
});
await p.goto('http://localhost:5210/catalog',{waitUntil:'domcontentloaded',timeout:120000});
await p.waitForSelector('.orb__chip',{timeout:120000});
const tChips = await p.evaluate(()=>performance.now());
console.log('چیپ‌ها در', tChips.toFixed(0), 'میلی‌ثانیه ظاهر شدند');
await p.waitForTimeout(9000);
const r=await p.evaluate(()=>({cls:window.__cls, src:window.__src, long:window.__long}));
console.log('\nCLS:', r.cls.toFixed(4));
console.log('منبع‌ها:'); Object.entries(r.src).sort((a,b)=>b[1]-a[1]).slice(0,6).forEach(([k,v])=>console.log(`   ${v.toFixed(4)}  ${k}`));
console.log('\nکارهایِ سنگین (' + r.long.length + '):');
r.long.forEach(l=>console.log(`   شروع ${String(l.t).padStart(6)}ms  مدت ${String(l.d).padStart(4)}ms  ${l.n}`));
const after = r.long.filter(l=>l.t > tChips+1500);
console.log(`\nپس از نشستنِ برنامه (بعد از ${(tChips+1500).toFixed(0)}ms): ${after.length} مورد`);
after.forEach(l=>console.log(`   شروع ${l.t}ms مدت ${l.d}ms`));
await b.close();
