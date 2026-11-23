import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
const ctx=await b.newContext({viewport:{width:1440,height:900}});
const p=await ctx.newPage();
await p.addInitScript(()=>{
  window.__s=[];
  new PerformanceObserver(l=>{for(const e of l.getEntries()){ if(e.hadRecentInput) continue;
    window.__s.push({t:Math.round(e.startTime), v:+e.value.toFixed(4),
      src:(e.sources||[]).map(s=>{const n=s.node; if(!n||!n.tagName) return '?';
        return n.tagName.toLowerCase()+'.'+((typeof n.className==='string'&&n.className.trim())?n.className.trim().split(/\s+/).slice(0,2).join('.'):'?');}).slice(0,3)});
  }}).observe({type:'layout-shift',buffered:true});
});
await p.goto('http://localhost:5210/',{waitUntil:'domcontentloaded',timeout:120000});
await p.waitForTimeout(13000);
const r=await p.evaluate(()=>window.__s.sort((a,x)=>x.v-a.v));
console.log('همهٔ لغزش‌ها (بزرگ‌ترین‌ها نخست):');
r.slice(0,10).forEach(x=>console.log(`   ${String(x.v).padStart(8)} در ${String(x.t).padStart(6)}ms | ${x.src.join(' , ')}`));
console.log('\nلغزش‌هایِ پس از ۵ ثانیه:');
r.filter(x=>x.t>5000).slice(0,6).forEach(x=>console.log(`   ${String(x.v).padStart(8)} در ${String(x.t).padStart(6)}ms | ${x.src.join(' , ')}`));
await b.close();
